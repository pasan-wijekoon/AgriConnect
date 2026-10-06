"""
Optional LLM-backed reasoning writer for the Fair-Price Estimation Agent.

Design constraint: the LLM (when configured) only phrases the human-readable
explanation of numbers that were already computed deterministically by
fair_price_agent.py. It never sets or adjusts the price itself — that keeps
the price-bounds validator and human-approval checkpoint meaningful regardless
of whether an LLM is wired in. Controlled by the LLM_PROVIDER env var:

  LLM_PROVIDER=mock (default, or unset) -> deterministic template, no LLM call
  LLM_PROVIDER=gemini                    -> ChatGoogleGenerativeAI (needs GEMINI_API_KEY)
  LLM_PROVIDER=openai                    -> ChatOpenAI (needs OPENAI_API_KEY)

Any LLM failure (missing key, network, quota) falls back to the deterministic
template so a price estimate is never blocked by an LLM outage.
"""
import os
from typing import Any, Dict, Optional

_PROMPT_TEMPLATE = """You are writing a short, factual explanation for a Sri Lankan farmer and a \
collection-centre officer, explaining a fair produce price range that has ALREADY been computed \
by deterministic rules below. Do not invent numbers, do not change the range, and do not add new \
claims about the market. Just explain, in 2-4 plain sentences, why this range makes sense given the \
inputs. Mention the wholesale benchmark, the grade adjustment, and the market momentum. Write in \
clear English suitable for translation into Sinhala and Tamil.

Crop: {crop}
Region: {region}
Wholesale market: {market}
Wholesale benchmark: LKR {wholesale:.2f}/kg
Farmgate baseline: LKR {farmgate:.2f}/kg
Claimed grade: {grade} ({grade_desc})
Volume note: {vol_desc}
7-day momentum: {momentum_pct:+.1f}% ({trend_direction})
Data source: {data_source}
Suggested range: LKR {suggested_min:.2f} - LKR {suggested_max:.2f}
Confidence: {confidence_pct}%
"""


def _deterministic_template(ctx: Dict[str, Any]) -> str:
    return (
        f"Fair price anchored on {ctx['market']} wholesale benchmark "
        f"(LKR {ctx['wholesale']:.2f}/kg) with farmgate baseline LKR {ctx['farmgate']:.2f}/kg. "
        f"Adjustments: {ctx['grade_desc']}, {ctx['vol_desc']}, and {ctx['trend_direction'].lower()} 7d momentum "
        f"({ctx['momentum_pct']:+.1f}%). Recommended fair trading range: LKR {ctx['suggested_min']:.2f} - "
        f"LKR {ctx['suggested_max']:.2f}/kg with {ctx['confidence_pct']}% market confidence "
        f"[deterministic estimate, {ctx['data_source']} data]."
    )


def _try_llm(ctx: Dict[str, Any], provider: str) -> Optional[str]:
    prompt = _PROMPT_TEMPLATE.format(**ctx)
    try:
        if provider == "gemini":
            from langchain_google_genai import ChatGoogleGenerativeAI
            if not os.getenv("GEMINI_API_KEY"):
                return None
            model = ChatGoogleGenerativeAI(
                model=os.getenv("DEFAULT_MODEL", "gemini-1.5-flash"),
                google_api_key=os.getenv("GEMINI_API_KEY"),
                temperature=0.3,
            )
        elif provider == "openai":
            from langchain_openai import ChatOpenAI
            if not os.getenv("OPENAI_API_KEY"):
                return None
            model = ChatOpenAI(
                model=os.getenv("DEFAULT_MODEL", "gpt-4o-mini"),
                api_key=os.getenv("OPENAI_API_KEY"),
                temperature=0.3,
            )
        else:
            return None

        response = model.invoke(prompt)
        return _extract_text(getattr(response, "content", None))
    except Exception:
        # Any LLM/provider failure (bad key, network, quota, import) — never block pricing.
        return None


def _extract_text(content: Any) -> Optional[str]:
    """
    Newer Gemini models return `.content` as a list of structured blocks
    (e.g. [{"type": "text", "text": "...", "extras": {...}}]) instead of a
    plain string. Handle both shapes.
    """
    if isinstance(content, str):
        return content.strip() or None

    if isinstance(content, list):
        parts = []
        for block in content:
            if isinstance(block, str):
                parts.append(block)
            elif isinstance(block, dict) and isinstance(block.get("text"), str):
                parts.append(block["text"])
        joined = "".join(parts).strip()
        return joined or None

    return None


def generate_reasoning(ctx: Dict[str, Any]) -> str:
    """
    Returns the reasoning summary for a price estimate. Tries the configured
    LLM provider first (if any); always falls back to a deterministic,
    honestly-labeled template so this never fails or lies about its source.
    """
    provider = os.getenv("LLM_PROVIDER", "mock").strip().lower()
    if provider in ("gemini", "openai"):
        llm_text = _try_llm(ctx, provider)
        if llm_text:
            return llm_text
    return _deterministic_template(ctx)
