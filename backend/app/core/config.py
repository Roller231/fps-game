from pydantic_settings import BaseSettings
from functools import lru_cache


class Settings(BaseSettings):
    app_name: str = "FPS Backend"

    mysql_host: str = "mysql"
    mysql_port: int = 3306
    mysql_user: str = "game_user"
    mysql_password: str = "game_pass"
    mysql_db: str = "game_db"

    secret_key: str = "change_me"

    class Config:
        env_file = ".env"
        env_prefix = "BACKEND_"

    @property
    def database_url(self) -> str:
        return (
            f"mysql+pymysql://{self.mysql_user}:{self.mysql_password}"
            f"@{self.mysql_host}:{self.mysql_port}/{self.mysql_db}"
        )


@lru_cache
def get_settings() -> Settings:
    return Settings()
