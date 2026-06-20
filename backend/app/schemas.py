from typing import List, Optional
from pydantic import BaseModel, Field


class RegisterRequest(BaseModel):
    username: str = Field(..., min_length=3, max_length=64)
    password: str = Field(..., min_length=6, max_length=128)
    confirm_password: str = Field(..., min_length=6, max_length=128)
    display_name: Optional[str] = None


class LoginRequest(BaseModel):
    username: str
    password: str


class TokenResponse(BaseModel):
    username: str
    token: str


class ValidateResponse(BaseModel):
    username: str
    token: str
    created_at: str


class WeaponData(BaseModel):
    weapon_name: str
    reserve_ammo: int
    magazine_ammo: int


class StatsData(BaseModel):
    total_kills: int = 0
    total_money_earned: int = 0
    max_wave: int = 0
    max_survival_time: int = 0
    total_playtime: int = 0


class ProfileResponse(BaseModel):
    username: str
    display_name: str
    avatar_url: Optional[str]
    balance: int
    equipped_weapon: Optional[str]
    weapons: List[WeaponData]
    stats: StatsData


class UpdateProfileRequest(BaseModel):
    display_name: Optional[str] = None
    avatar_url: Optional[str] = None
    balance: Optional[int] = None
    equipped_weapon: Optional[str] = None
    weapons: Optional[List[WeaponData]] = None
    stats: Optional[StatsData] = None


class ChangePasswordRequest(BaseModel):
    current_password: str
    new_password: str = Field(..., min_length=6, max_length=128)
