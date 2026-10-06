"""
Coordinator / Planner Agent Orchestration using LangGraph.
A single LangGraph StateGraph acts as the planner/coordinator:
it receives the triggering domain objective (e.g. 'new listing submitted'),
builds a structured multi-step plan, routes execution to specialised agents,
validates price bounds deterministically, and pauses at the human-approval checkpoint.
"""
from typing import TypedDict, List, Dict, Any, Optional
from datetime import datetime, timezone
from langgraph.graph import StateGraph, END

from ..agents.fair_price_agent import FairPriceEstimationAgent
from ..validation.price_bounds_validator import PriceBoundsValidator


# Define State Schema for Coordinator StateGraph
class CoordinatorState(TypedDict, total=False):
    # Trigger & Context
    trigger_type: str
    trigger_entity_id: Optional[str]
    objective_text: str
    listing_context: Dict[str, Any]

    # Structured Multi-Step Plan
    plan_steps: List[Dict[str, Any]]
    current_step: int

    # Execution & Audit Logs
    tool_call_log: List[Dict[str, Any]]
    fair_price_result: Optional[Dict[str, Any]]
    validation_result: Optional[Dict[str, Any]]
    anomaly_investigation: Optional[Dict[str, Any]]

    # Human-in-the-Loop Checkpoint State
    approval_status: str  # "PendingOfficerApproval", "Approved", "Rejected"
    approved_by: Optional[str]
    final_outcome: Optional[Dict[str, Any]]
    execution_timestamp: str


class LangGraphCoordinator:
    """
    LangGraph-based Coordinator & Planner.
    Satisfies 'planning and delegation' requirements via a compiled StateGraph.
    """

    def __init__(self):
        self.fair_price_agent = FairPriceEstimationAgent()
        self.validator = PriceBoundsValidator()
        self.graph = self._build_graph()

    def _build_graph(self):
        workflow = StateGraph(CoordinatorState)

        # 1. Add nodes
        workflow.add_node("planner", self._planner_node)
        workflow.add_node("fair_price_agent", self._fair_price_node)
        workflow.add_node("deterministic_validator", self._validation_node)
        workflow.add_node("anomaly_investigation", self._anomaly_investigation_node)
        workflow.add_node("human_approval_checkpoint", self._checkpoint_node)

        # 2. Add edges
        workflow.set_entry_point("planner")
        workflow.add_edge("planner", "fair_price_agent")
        workflow.add_edge("fair_price_agent", "deterministic_validator")

        # Conditional routing: a wide price spread, low confidence, or a failed
        # validation gets extra scrutiny before the human checkpoint, rather than
        # every proposal following one fixed path regardless of its risk profile.
        # Both branches still terminate at the mandatory human-approval checkpoint —
        # this never auto-approves a price without officer sign-off.
        workflow.add_conditional_edges(
            "deterministic_validator",
            self._route_after_validation,
            {
                "escalate": "anomaly_investigation",
                "standard": "human_approval_checkpoint",
            },
        )
        workflow.add_edge("anomaly_investigation", "human_approval_checkpoint")
        workflow.add_edge("human_approval_checkpoint", END)

        return workflow.compile()

    @staticmethod
    def _route_after_validation(state: CoordinatorState) -> str:
        """Conditional edge: decide whether this proposal needs escalated review."""
        val_result = state.get("validation_result") or {}
        price_res = state.get("fair_price_result") or {}

        if not val_result.get("passed", False):
            return "escalate"
        if val_result.get("flags"):
            return "escalate"
        if (price_res.get("confidence") or 1.0) < 0.65:
            return "escalate"
        return "standard"

    def _planner_node(self, state: CoordinatorState) -> Dict[str, Any]:
        """
        Planner Node: Analyzes the domain objective, evaluates available context,
        and constructs a structured multi-step execution plan.
        """
        objective = state.get("objective_text", "Determine fair price for produce listing")
        trigger = state.get("trigger_type", "NewListingSubmitted")

        plan = [
            {
                "step": 1,
                "name": "Market Intelligence & Historical Lookup",
                "agent": "FairPriceEstimationAgent",
                "tools": ["MarketPriceLookupTool", "HistoricalTrendTool"],
                "status": "planned"
            },
            {
                "step": 2,
                "name": "Fair Price Range Estimation",
                "agent": "FairPriceEstimationAgent",
                "details": "Compute grounded price range with grade, volume, and momentum weighting",
                "status": "planned"
            },
            {
                "step": 3,
                "name": "Deterministic Bounds & Outlier Validation",
                "agent": "DeterministicValidator",
                "details": "Verify price range sanity against 7-day historical bounds and business rules",
                "status": "planned"
            },
            {
                "step": 4,
                "name": "Human-in-the-Loop Officer Review Routing",
                "checkpoint": "Pause for Agricultural Officer approval",
                "status": "planned"
            }
        ]

        return {
            "plan_steps": plan,
            "current_step": 1,
            "tool_call_log": state.get("tool_call_log", [])
        }

    def _fair_price_node(self, state: CoordinatorState) -> Dict[str, Any]:
        """
        Fair-Price Agent Node: Invokes the FairPriceEstimationAgent with
        crop, region, quantity, claimedGrade, and recent sales data.
        """
        ctx = state.get("listing_context", {})
        crop_id = str(ctx.get("cropId", "default-crop"))
        crop_name = ctx.get("cropName")
        region_id = str(ctx.get("regionId", "default-region"))
        region_name = ctx.get("regionName")
        quantity = float(ctx.get("quantity", 100.0))
        grade = str(ctx.get("claimedGrade", "A"))
        recent_sales = ctx.get("recentSaleData", [])

        # Execute agent
        result = self.fair_price_agent.estimate_price(
            crop_id=crop_id,
            region_id=region_id,
            quantity=quantity,
            claimed_grade=grade,
            recent_sale_data=recent_sales,
            crop_name=crop_name,
            region_name=region_name
        )

        # Update plan status
        updated_plan = list(state.get("plan_steps", []))
        if len(updated_plan) >= 2:
            updated_plan[0]["status"] = "completed"
            updated_plan[1]["status"] = "completed"

        existing_tools = list(state.get("tool_call_log", []))
        existing_tools.extend(result.get("toolCallLog", []))

        return {
            "fair_price_result": result,
            "tool_call_log": existing_tools,
            "plan_steps": updated_plan,
            "current_step": 3
        }

    def _validation_node(self, state: CoordinatorState) -> Dict[str, Any]:
        """
        Validation Node: Runs deterministic sanity and outlier tests on the suggested price range.
        """
        price_res = state.get("fair_price_result") or {}
        ctx = state.get("listing_context", {})
        min_p = price_res.get("suggestedPriceMin", 0.0)
        max_p = price_res.get("suggestedPriceMax", 0.0)
        ma_7d = price_res.get("historicalMa7d", min_p)
        grade = str(ctx.get("claimedGrade", "A"))
        quantity = float(ctx.get("quantity", 100.0))

        val_result = self.validator.validate(
            suggested_min=min_p,
            suggested_max=max_p,
            historical_ma_7d=ma_7d,
            claimed_grade=grade,
            quantity=quantity
        )

        updated_plan = list(state.get("plan_steps", []))
        if len(updated_plan) >= 3:
            updated_plan[2]["status"] = "completed" if val_result["passed"] else "failed"

        return {
            "validation_result": val_result,
            "plan_steps": updated_plan,
            "current_step": 4
        }

    def _anomaly_investigation_node(self, state: CoordinatorState) -> Dict[str, Any]:
        """
        Anomaly Investigation Node: reached only via the conditional edge out of
        validation, for proposals that failed validation, carry flags, or have
        low confidence. Adds a senior-review recommendation and surfaces the
        specific reasons — it does not alter the computed price, and it still
        always routes onward to the mandatory human-approval checkpoint.
        """
        val_result = state.get("validation_result") or {}
        price_res = state.get("fair_price_result") or {}

        reasons: List[str] = []
        if not val_result.get("passed", False):
            reasons.extend(val_result.get("failed_checks", []))
        reasons.extend(val_result.get("flags", []))
        if (price_res.get("confidence") or 1.0) < 0.65:
            reasons.append(f"Low estimator confidence ({price_res.get('confidence')}).")

        updated_plan = list(state.get("plan_steps", []))
        if len(updated_plan) >= 4:
            updated_plan[3]["status"] = "escalated_for_senior_review"

        return {
            "anomaly_investigation": {
                "escalated": True,
                "reasons": reasons,
                "recommendation": (
                    "Reject or request revision"
                    if not val_result.get("passed", False)
                    else "Approve only after manual cross-check of the flagged reason(s) below"
                ),
            },
            "plan_steps": updated_plan,
        }

    def _checkpoint_node(self, state: CoordinatorState) -> Dict[str, Any]:
        """
        Checkpoint Node: Pauses execution at the required human-approval gate.
        Prepares structured proposal for Officer approval on the React dashboard.
        """
        val_result = state.get("validation_result", {})
        price_res = state.get("fair_price_result", {})
        anomaly = state.get("anomaly_investigation")

        # If validation passed, pause with PendingOfficerApproval; else flag rejected
        if val_result.get("passed", False):
            approval_status = "PendingOfficerApproval"
        else:
            approval_status = "RejectedByValidation"

        updated_plan = list(state.get("plan_steps", []))
        if len(updated_plan) >= 4 and updated_plan[3]["status"] != "escalated_for_senior_review":
            updated_plan[3]["status"] = "awaiting_human_approval" if val_result.get("passed") else "terminated"

        final_outcome = {
            "suggestedPriceMin": price_res.get("suggestedPriceMin"),
            "suggestedPriceMax": price_res.get("suggestedPriceMax"),
            "confidence": price_res.get("confidence"),
            "reasoningSummary": price_res.get("reasoningSummary"),
            "validationPassed": val_result.get("passed"),
            "validationSummary": val_result.get("summary"),
            "flags": val_result.get("flags", []),
            "approvalRequired": True,
            "checkpointName": "SeniorOfficerReview" if anomaly else "AgriculturalOfficerReview",
            "anomalyInvestigation": anomaly,
        }

        return {
            "approval_status": approval_status,
            "final_outcome": final_outcome,
            "plan_steps": updated_plan,
            "execution_timestamp": datetime.now(timezone.utc).isoformat()
        }

    def run_workflow(
        self,
        trigger_type: str,
        objective_text: str,
        listing_context: Dict[str, Any],
        trigger_entity_id: Optional[str] = None
    ) -> Dict[str, Any]:
        """Runs the LangGraph StateGraph end-to-end and returns the checkpoint state."""
        initial_state: CoordinatorState = {
            "trigger_type": trigger_type,
            "trigger_entity_id": trigger_entity_id,
            "objective_text": objective_text,
            "listing_context": listing_context,
            "plan_steps": [],
            "current_step": 0,
            "tool_call_log": [],
            "approval_status": "Initiated",
            "execution_timestamp": datetime.now(timezone.utc).isoformat()
        }

        final_state = self.graph.invoke(initial_state)
        return dict(final_state)
