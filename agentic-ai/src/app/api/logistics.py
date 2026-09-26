import logging
from functools import lru_cache
from typing import Annotated

from fastapi import APIRouter, Depends, HTTPException, status
from pydantic import ValidationError

from src.app.agents.logistics_scheduling_agent import (
    LogisticsSchedulingAgent,
    NoSlotAvailableError,
    build_logistics_agent,
)
from src.app.tools.errors import ToolError
from src.app.validation.logistics_schemas import ScheduleProposal, ScheduleRequest

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/agents/logistics", tags=["Logistics Scheduling Agent"])


@lru_cache
def get_logistics_agent() -> LogisticsSchedulingAgent:
    return build_logistics_agent()


@router.post(
    "/schedule",
    response_model=ScheduleProposal,
    responses={
        409: {"description": "No free slot in the preferred window or the next 3 days"},
        422: {"description": "Invalid request"},
        502: {"description": "A tool's data source failed, or the LLM returned an invalid proposal"},
    },
)
def schedule(
    request: ScheduleRequest,
    agent: Annotated[LogisticsSchedulingAgent, Depends(get_logistics_agent)],
) -> ScheduleProposal:
    """Propose a conflict-free pickup/delivery slot. The proposal still needs officer approval."""
    try:
        return ScheduleProposal.model_validate(agent.schedule(request))
    except NoSlotAvailableError as exc:
        raise HTTPException(status.HTTP_409_CONFLICT, detail=str(exc)) from exc
    except ToolError as exc:
        logger.warning("Tool failure while scheduling order %s: %s", request.orderId, exc)
        raise HTTPException(status.HTTP_502_BAD_GATEWAY, detail=f"Scheduling data unavailable: {exc}") from exc
    except ValidationError as exc:
        # The request was already valid, so this is the agent's own output failing its checks.
        logger.error("Rejected invalid proposal for order %s: %s", request.orderId, exc)
        raise HTTPException(
            status.HTTP_502_BAD_GATEWAY, detail="The agent produced an invalid proposal; nothing was returned."
        ) from exc
