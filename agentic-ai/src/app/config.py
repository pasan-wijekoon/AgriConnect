from functools import lru_cache
from typing import Literal

from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Service configuration, read from environment variables (and .env if present)."""

    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    # "mock" skips every LLM call and uses the mock tools, so demos never need an API key.
    llm_provider: Literal["mock", "gemini", "openai"] = "mock"
    default_model: str = "gemini-1.5-flash"
    gemini_api_key: str = ""
    openai_api_key: str = ""

    # The ASP.NET Core API that owns orders and collection centres (Component B).
    backend_api_base_url: str = "http://localhost:5000"
    internal_api_secret: str = ""
    tool_timeout_seconds: float = 10.0


@lru_cache
def get_settings() -> Settings:
    return Settings()
