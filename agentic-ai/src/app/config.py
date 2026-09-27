import os
from pydantic_settings import BaseSettings, SettingsConfigDict
from pydantic import Field


class Settings(BaseSettings):
    # Server
    host: str = Field(default="0.0.0.0", alias="HOST")
    port: int = Field(default=8000, alias="PORT")
    environment: str = Field(default="development", alias="ENVIRONMENT")
    log_level: str = Field(default="INFO", alias="LOG_LEVEL")

    # LLM Settings
    llm_provider: str = Field(default="gemini", alias="LLM_PROVIDER")
    default_model: str = Field(default="gemini-3.8-flash", alias="DEFAULT_MODEL")
    gemini_api_key: str = Field(default="", alias="GEMINI_API_KEY")
    openai_api_key: str = Field(default="", alias="OPENAI_API_KEY")

    # Security
    allowed_origins: str = Field(default="*", alias="ALLOWED_ORIGINS")
    internal_api_secret: str = Field(default="dev-secret-key-12345", alias="INTERNAL_API_SECRET")

    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        extra="ignore"
    )


settings = Settings()
