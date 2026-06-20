import json
import logging
import os
import uuid
from pathlib import Path

from fastapi import FastAPI, Depends, HTTPException, status, Request, File, UploadFile
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import HTMLResponse
from fastapi.staticfiles import StaticFiles
from fastapi.templating import Jinja2Templates
from sqlalchemy import inspect, text
from sqlalchemy.orm import Session
from sqlalchemy.exc import IntegrityError

from app import models
from app.db import engine, get_db
from app.schemas import (
    RegisterRequest, LoginRequest, TokenResponse, ValidateResponse,
    ProfileResponse, UpdateProfileRequest, ChangePasswordRequest,
    WeaponData, StatsData, DonateGrantRequest
)
from app.utils import hash_password, verify_password, generate_token

logger = logging.getLogger(__name__)

models.Base.metadata.create_all(bind=engine)


def ensure_equipped_slots_column():
    inspector = inspect(engine)
    column_names = {col["name"] for col in inspector.get_columns("users")}
    if "equipped_slots" in column_names:
        return

    logger.info("Adding equipped_slots column to users table")
    try:
        with engine.begin() as conn:
            conn.execute(text("ALTER TABLE users ADD COLUMN equipped_slots VARCHAR(512)"))
    except Exception as exc:
        logger.warning("Failed to add equipped_slots column (maybe exists already): %s", exc)


ensure_equipped_slots_column()


def _deserialize_slots(raw_value: str | None) -> list[str | None]:
    if not raw_value:
        return []
    try:
        data = json.loads(raw_value)
        if isinstance(data, list):
            return data
    except json.JSONDecodeError:
        logger.warning("Failed to decode equipped_slots JSON: %s", raw_value)
    return []


def _serialize_slots(slots: list[str | None] | None) -> str | None:
    if slots is None:
        return None
    return json.dumps(slots)

app = FastAPI(title="FPS Backend")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

UPLOAD_DIR = Path("app/uploads")
UPLOAD_DIR.mkdir(parents=True, exist_ok=True)

app.mount("/static", StaticFiles(directory="app/static"), name="static")
app.mount("/uploads", StaticFiles(directory="app/uploads"), name="uploads")
templates = Jinja2Templates(directory="app/templates")


@app.get("/", response_class=HTMLResponse)
def landing(request: Request):
    server_host = os.getenv("SERVER_HOST", "localhost")
    backend_port = os.getenv("BACKEND_PORT", "8000")
    webgl_port = os.getenv("WEBGL_PORT", "8080")
    
    return templates.TemplateResponse("index.html", {
        "request": request,
        "server_host": server_host,
        "backend_port": backend_port,
        "webgl_port": webgl_port,
    })


@app.post("/api/register", response_model=TokenResponse)
def register_user(payload: RegisterRequest, db: Session = Depends(get_db)):
    if payload.password != payload.confirm_password:
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Passwords do not match")

    existing = db.query(models.User).filter(models.User.username == payload.username).first()
    if existing:
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Username already exists")

    token = generate_token()
    display_name = payload.display_name if payload.display_name else payload.username
    
    user = models.User(
        username=payload.username,
        password_hash=hash_password(payload.password),
        api_token=token,
        display_name=display_name,
        balance=0,
    )
    db.add(user)
    try:
        db.flush()
    except IntegrityError:
        db.rollback()
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Username already exists")
    
    # Create stats
    stats = models.UserStats(user_id=user.id)
    db.add(stats)

    db.commit()

    db.refresh(user)

    return TokenResponse(username=user.username, token=user.api_token)


@app.post("/api/login", response_model=TokenResponse)
def login_user(payload: LoginRequest, db: Session = Depends(get_db)):
    user = db.query(models.User).filter(models.User.username == payload.username).first()
    if not user or not verify_password(payload.password, user.password_hash):
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Invalid credentials")

    return TokenResponse(username=user.username, token=user.api_token)


@app.get("/api/token/validate", response_model=ValidateResponse)
def validate_token(token: str, db: Session = Depends(get_db)):
    user = db.query(models.User).filter(models.User.api_token == token).first()
    if not user:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Token not found")

    return ValidateResponse(username=user.username, token=user.api_token, created_at=user.created_at.isoformat())


@app.get("/api/profile", response_model=ProfileResponse)
def get_profile(token: str, db: Session = Depends(get_db)):
    user = db.query(models.User).filter(models.User.api_token == token).first()
    if not user:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Token not found")

    weapons = [WeaponData(weapon_name=w.weapon_name, reserve_ammo=w.reserve_ammo, magazine_ammo=w.magazine_ammo) 
               for w in user.weapons]
    
    stats_data = StatsData()
    if user.stats:
        stats_data = StatsData(
            total_kills=user.stats.total_kills,
            total_money_earned=user.stats.total_money_earned,
            max_wave=user.stats.max_wave,
            max_survival_time=user.stats.max_survival_time,
            total_playtime=user.stats.total_playtime
        )

    equipped_slots = _deserialize_slots(user.equipped_slots)

    return ProfileResponse(
        username=user.username,
        display_name=user.display_name,
        avatar_url=user.avatar_url,
        balance=user.balance,
        equipped_weapon=user.equipped_weapon,
        equipped_slots=equipped_slots,
        weapons=weapons,
        stats=stats_data
    )


@app.post("/api/profile", response_model=ProfileResponse)
def update_profile(token: str, payload: UpdateProfileRequest, db: Session = Depends(get_db)):
    user = db.query(models.User).filter(models.User.api_token == token).first()
    if not user:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Token not found")

    if payload.display_name is not None:
        user.display_name = payload.display_name
    if payload.avatar_url is not None:
        user.avatar_url = payload.avatar_url
    if payload.balance is not None:
        user.balance = payload.balance
    if payload.equipped_weapon is not None:
        user.equipped_weapon = payload.equipped_weapon
    if payload.equipped_slots is not None:
        user.equipped_slots = _serialize_slots(payload.equipped_slots)

    if payload.weapons is not None:
        # Clear existing weapons
        db.query(models.UserWeapon).filter(models.UserWeapon.user_id == user.id).delete()
        # Add new weapons
        for w in payload.weapons:
            weapon = models.UserWeapon(
                user_id=user.id,
                weapon_name=w.weapon_name,
                reserve_ammo=w.reserve_ammo,
                magazine_ammo=w.magazine_ammo
            )
            db.add(weapon)

    if payload.stats is not None:
        if not user.stats:
            user.stats = models.UserStats(user_id=user.id)
        user.stats.total_kills = payload.stats.total_kills
        user.stats.total_money_earned = payload.stats.total_money_earned
        user.stats.max_wave = payload.stats.max_wave
        user.stats.max_survival_time = payload.stats.max_survival_time
        user.stats.total_playtime = payload.stats.total_playtime

    db.commit()
    db.refresh(user)

    weapons = [WeaponData(weapon_name=w.weapon_name, reserve_ammo=w.reserve_ammo, magazine_ammo=w.magazine_ammo) 
               for w in user.weapons]
    
    stats_data = StatsData()
    if user.stats:
        stats_data = StatsData(
            total_kills=user.stats.total_kills,
            total_money_earned=user.stats.total_money_earned,
            max_wave=user.stats.max_wave,
            max_survival_time=user.stats.max_survival_time,
            total_playtime=user.stats.total_playtime
        )

    equipped_slots = _deserialize_slots(user.equipped_slots)

    return ProfileResponse(
        username=user.username,
        display_name=user.display_name,
        avatar_url=user.avatar_url,
        balance=user.balance,
        equipped_weapon=user.equipped_weapon,
        equipped_slots=equipped_slots,
        weapons=weapons,
        stats=stats_data
    )


@app.post("/api/profile/password")
def change_password(token: str, payload: ChangePasswordRequest, db: Session = Depends(get_db)):
    user = db.query(models.User).filter(models.User.api_token == token).first()
    if not user:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Token not found")

    if not verify_password(payload.current_password, user.password_hash):
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Current password is incorrect")

    user.password_hash = hash_password(payload.new_password)
    db.commit()

    return {"message": "Password changed successfully"}


@app.post("/api/profile/avatar")
def upload_avatar(token: str, file: UploadFile = File(...), db: Session = Depends(get_db)):
    user = db.query(models.User).filter(models.User.api_token == token).first()
    if not user:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Token not found")

    # Validate file type
    allowed_types = {"image/jpeg", "image/png", "image/gif", "image/webp"}
    if file.content_type not in allowed_types:
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Invalid file type. Only images allowed.")

    # Generate unique filename
    ext = file.filename.split(".")[-1] if "." in file.filename else "jpg"
    filename = f"{uuid.uuid4()}.{ext}"
    filepath = UPLOAD_DIR / filename

    # Save file
    with open(filepath, "wb") as f:
        f.write(file.file.read())

    # Update user avatar URL
    avatar_url = f"/uploads/{filename}"
    user.avatar_url = avatar_url
    db.commit()

    return {"avatar_url": avatar_url}


# ── Donate / Payment ────────────────────────────────────────────────────────
# This endpoint is called after a successful payment is confirmed.
# INTEGRATION POINT: Before crediting currency, verify the payment on your
# backend by checking the payment provider's webhook or server-side API.
# Example flow:
#   1. Frontend initiates payment via SDK (Stripe, PayPal, YooKassa, etc.)
#   2. Payment provider sends a webhook to /api/donate/webhook (implement separately)
#   3. Your webhook handler verifies the payment and calls grant_currency() logic
#   4. Alternatively, frontend calls this endpoint with a verified payment_id
#
# WARNING: In production, NEVER trust client-side payment confirmation alone.
#          Always verify payment server-side before crediting currency.

@app.post("/api/donate/grant")
def grant_currency(payload: DonateGrantRequest, db: Session = Depends(get_db)):
    user = db.query(models.User).filter(models.User.api_token == payload.token).first()
    if not user:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Token not found")

    # TODO: Verify payment with provider before crediting
    # verify_payment(payload.package_id, payment_confirmation_id)

    if payload.amount <= 0:
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Invalid amount")

    user.balance = (user.balance or 0) + payload.amount
    db.commit()

    logger.info(f"[Donate] +{payload.amount} credited to {user.username} (pkg={payload.package_id})")

    return {"success": True, "new_balance": user.balance, "credited": payload.amount}
