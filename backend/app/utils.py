import secrets
from passlib.context import CryptContext

pwd_context = CryptContext(schemes=["bcrypt"], deprecated="auto")


def _truncate_password(password: str) -> str:
    # bcrypt принимает максимум 72 байта, поэтому режем строку в UTF-8
    encoded = password.encode("utf-8")
    if len(encoded) <= 72:
        return password
    truncated = encoded[:72]
    return truncated.decode("utf-8", errors="ignore")


def hash_password(password: str) -> str:
    return pwd_context.hash(_truncate_password(password))


def verify_password(plain_password: str, hashed_password: str) -> bool:
    return pwd_context.verify(_truncate_password(plain_password), hashed_password)


def generate_token() -> str:
    return secrets.token_urlsafe(48)
