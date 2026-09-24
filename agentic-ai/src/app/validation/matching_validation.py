"""Rejects malformed Buyer-Farmer Matching Agent output before it's returned
(plan §8.5). The agent's own matching logic is deterministic, not an LLM call
(see buyer_farmer_matching_agent.py's module docstring for why), but this
validation gate stays in place regardless — if a future revision adds an LLM
step, malformed or hallucinated output (e.g. a centre id that was never a
candidate) must never reach the caller as if it were trustworthy structured
data. Per CLAUDE.md's AI rule, this agent only ever *proposes* a match; nothing
here commits anything.
"""

from __future__ import annotations

MAX_NOTES_LENGTH = 2000


class MatchValidationError(ValueError):
    """Raised when an agent's proposed match fails validation."""


def validate_match_response(
    matched_centre_id: str | None,
    match_confidence: float,
    notes: str,
    candidate_centre_ids: set[str],
) -> None:
    if not (0.0 <= match_confidence <= 1.0):
        raise MatchValidationError(f"match_confidence {match_confidence!r} is out of range [0.0, 1.0].")

    if matched_centre_id is not None and matched_centre_id not in candidate_centre_ids:
        raise MatchValidationError(
            f"matched_centre_id {matched_centre_id!r} was not among the candidates considered."
        )

    if matched_centre_id is None and match_confidence != 0.0:
        raise MatchValidationError("match_confidence must be 0.0 when no centre was matched.")

    if not notes or not notes.strip():
        raise MatchValidationError("notes must not be empty.")

    if len(notes) > MAX_NOTES_LENGTH:
        raise MatchValidationError(f"notes exceeds the maximum length of {MAX_NOTES_LENGTH} characters.")
