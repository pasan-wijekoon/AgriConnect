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
    # Any OpenAI-compatible gateway, e.g. https://opencode.ai/zen/v1. Empty = api.openai.com.
    openai_base_url: str = ""

    # Where tools get data. Unset: mock when LLM_PROVIDER=mock, otherwise the API.
    # Set TOOLS_MODE=mock to use a real LLM before Component B's endpoints exist.
    tools_mode: Literal["mock", "api"] | None = None

    # The ASP.NET Core API that owns orders and collection centres (Component B).
    backend_api_base_url: str = "http://localhost:5000"
    internal_api_secret: str = ""
    tool_timeout_seconds: float = 10.0

    @property
    def use_mock_tools(self) -> bool:
        return (self.tools_mode or ("mock" if self.llm_provider == "mock" else "api")) == "mock"


@lru_cache
def get_settings() -> Settings:
    return Settings()
