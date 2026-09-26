from langchain_core.language_models import BaseChatModel

from src.app.config import Settings


class LlmConfigurationError(RuntimeError):
    """The configured LLM provider cannot be used, e.g. its API key is missing."""


def get_chat_model(settings: Settings) -> BaseChatModel | None:
    """The chat model for the configured provider, or None in mock mode (no LLM calls)."""
    if settings.llm_provider == "mock":
        return None

    if settings.llm_provider == "gemini":
        if not settings.gemini_api_key:
            raise LlmConfigurationError("LLM_PROVIDER=gemini but GEMINI_API_KEY is not set.")
        from langchain_google_genai import ChatGoogleGenerativeAI

        return ChatGoogleGenerativeAI(
            model=settings.default_model, google_api_key=settings.gemini_api_key, temperature=0
        )

    if not settings.openai_api_key:
        raise LlmConfigurationError("LLM_PROVIDER=openai but OPENAI_API_KEY is not set.")
    from langchain_openai import ChatOpenAI

    return ChatOpenAI(model=settings.default_model, api_key=settings.openai_api_key, temperature=0)
