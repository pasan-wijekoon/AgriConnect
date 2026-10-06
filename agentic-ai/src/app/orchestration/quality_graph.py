"""
LangGraph orchestration graph for Component C Quality & Compliance Validation Agent.
Coordinates deterministic tool execution, Gemini LLM evaluation, and gate determination.
"""
from typing import Dict, Any, List, TypedDict, Optional
from langgraph.graph import StateGraph, END
from src.app.agents.quality_compliance_agent import QualityComplianceAgent, ValidationResultOutput


class QualityWorkflowState(TypedDict):
    listing_id: str
    crop_name: str
    quantity: float
    unit: str
    claimed_grade: str
    confirmed_grade: Optional[str]
    inspector_notes: Optional[str]
    photo_urls: List[str]
    min_price: Optional[float]
    proposed_price: Optional[float]
    result: Optional[Dict[str, Any]]


_agent_singleton = QualityComplianceAgent()


async def execute_quality_validation(state: QualityWorkflowState) -> QualityWorkflowState:
    agent_output: ValidationResultOutput = await _agent_singleton.evaluate_listing(
        listing_id=state["listing_id"],
        crop_name=state["crop_name"],
        quantity=state["quantity"],
        unit=state["unit"],
        claimed_grade=state["claimed_grade"],
        confirmed_grade=state.get("confirmed_grade"),
        inspector_notes=state.get("inspector_notes"),
        photo_urls=state.get("photo_urls", []),
        min_price=state.get("min_price"),
        proposed_price=state.get("proposed_price")
    )
    state["result"] = agent_output.model_dump()
    return state


def build_quality_compliance_graph():
    builder = StateGraph(QualityWorkflowState)
    builder.add_node("evaluate_quality", execute_quality_validation)
    builder.set_entry_point("evaluate_quality")
    builder.add_edge("evaluate_quality", END)
    return builder.compile()


quality_graph = build_quality_compliance_graph()
