from fastapi import FastAPI, Depends, HTTPException, status, Request
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import HTMLResponse
from fastapi.staticfiles import StaticFiles
from fastapi.templating import Jinja2Templates
from sqlalchemy.orm import Session
from sqlalchemy.exc import IntegrityError

from app import models
from app.db import engine, get_db
from app.schemas import (
    RegisterRequest, LoginRequest, TokenResponse, ValidateResponse,
    ProfileResponse, UpdateProfileRequest, ChangePasswordRequest,
    WeaponData, StatsData
)
from app.utils import hash_password, verify_password, generate_token

models.Base.metadata.create_all(bind=engine)

app = FastAPI(title="FPS Backend")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.mount("/static", StaticFiles(directory="app/static"), name="static")
templates = Jinja2Templates(directory="app/templates")


@app.get("/", response_class=HTMLResponse)
def landing(request: Request):
    return templates.TemplateResponse("index.html", {"request": request})


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

    return ProfileResponse(
        username=user.username,
        display_name=user.display_name,
        avatar_url=user.avatar_url,
        balance=user.balance,
        equipped_weapon=user.equipped_weapon,
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

    return ProfileResponse(
        username=user.username,
        display_name=user.display_name,
        avatar_url=user.avatar_url,
        balance=user.balance,
        equipped_weapon=user.equipped_weapon,
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
