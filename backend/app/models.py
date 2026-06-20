from datetime import datetime
from sqlalchemy import Column, Integer, String, DateTime, BigInteger, ForeignKey
from sqlalchemy.orm import relationship

from app.db import Base


class User(Base):
    __tablename__ = "users"

    id = Column(Integer, primary_key=True, index=True)
    username = Column(String(64), unique=True, nullable=False, index=True)
    password_hash = Column(String(255), nullable=False)
    api_token = Column(String(255), unique=True, nullable=False, index=True)
    
    # Profile
    display_name = Column(String(64), nullable=False)
    avatar_url = Column(String(512), nullable=True)
    
    # Game data
    balance = Column(Integer, default=0, nullable=False)
    equipped_weapon = Column(String(64), nullable=True)
    equipped_slots = Column(String(512), nullable=True)
    
    created_at = Column(DateTime, default=datetime.utcnow)
    last_login = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    
    # Relationships
    weapons = relationship("UserWeapon", back_populates="user", cascade="all, delete-orphan")
    stats = relationship("UserStats", back_populates="user", uselist=False, cascade="all, delete-orphan")


class UserWeapon(Base):
    __tablename__ = "user_weapons"

    id = Column(Integer, primary_key=True, index=True)
    user_id = Column(Integer, ForeignKey("users.id", ondelete="CASCADE"), nullable=False, index=True)
    weapon_name = Column(String(64), nullable=False)
    reserve_ammo = Column(Integer, default=0)
    magazine_ammo = Column(Integer, default=0)
    
    user = relationship("User", back_populates="weapons")


class UserStats(Base):
    __tablename__ = "user_stats"

    id = Column(Integer, primary_key=True, index=True)
    user_id = Column(Integer, ForeignKey("users.id", ondelete="CASCADE"), unique=True, nullable=False, index=True)
    
    # Leaderboard stats
    total_kills = Column(BigInteger, default=0)
    total_money_earned = Column(BigInteger, default=0)
    max_wave = Column(Integer, default=0)
    max_survival_time = Column(Integer, default=0)  # seconds
    total_playtime = Column(BigInteger, default=0)  # seconds
    
    user = relationship("User", back_populates="stats")
