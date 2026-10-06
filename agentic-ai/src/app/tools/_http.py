from typing import Any

import httpx

from src.app.config import Settings
from src.app.tools.errors import ToolError


def get_json(settings: Settings, path: str, params: dict[str, str] | None = None) -> Any:
    """GET a JSON resource from the ASP.NET Core API, converting failures into ToolError."""
    headers = {"X-Internal-Secret": settings.internal_api_secret} if settings.internal_api_secret else {}
    url = f"{settings.backend_api_base_url.rstrip('/')}{path}"
    try:
        response = httpx.get(url, params=params, headers=headers, timeout=settings.tool_timeout_seconds)
        response.raise_for_status()
        return response.json()
    except (httpx.HTTPError, ValueError) as exc:
        raise ToolError(f"GET {path} failed: {exc}") from exc
