"""
Quality & Compliance Validation Agent for Component C (SRS FR5, FR12, FR14, FR19, FR20)
Uses Google Gemini API (targeting gemini-3.8-flash) for LLM reasoning and multimodal analysis,
with robust deterministic guardrails and offline fallback.
"""
import os
import json
import logging
from typing import Dict, Any, List, Optional
from pydantic import BaseModel, Field

from langchain_core.messages import SystemMessage, HumanMessage
from src.app.config import settings
from src.app.tools.quality_tools import (
    validate_grade_standard,
    check_grade_discrepancy,
    check_price_and_volume_feasibility
)

logger = logging.getLogger("agriconnect.quality_agent")


class ToolLogEntry(BaseModel):
    tool: str
    input: Dict[str, Any]
    output: Dict[str, Any]


class ValidationResultOutput(BaseModel):
    passed: bool = Field(description="Whether the listing passed all quality & compliance checks")
    failed_checks: List[str] = Field(default_factory=list, description="List of failed check descriptions")
    flags: List[str] = Field(default_factory=list, description="List of warnings or advisory tags")
    grade_confidence: float = Field(default=0.9, description="Confidence in assigned quality grade [0.0 - 1.0]")
    assessed_grade: str = Field(description="Recommended or confirmed grade")
    reasoning_summary: str = Field(description="Detailed explanation of reasoning by the AI agent")
    recommended_action: str = Field(description="Approve, HoldForInspection, ReconcileDiscrepancy, or Reject")
    tool_call_log: List[ToolLogEntry] = Field(default_factory=list)


SYSTEM_PROMPT = """You are the Senior Agricultural Quality & Compliance Verification AI Agent for the AgriConnect Sri Lankan Agriculture Marketplace.
Your responsibility is to serve as the critical validation gate (SRS FR5, FR14, FR19, FR20) ensuring that produce listings meet stringent national quality and safety standards before becoming buyer-visible.

Sri Lankan Quality Grading Standards:
1. Grade A: Premium export / Tier-1 supermarket quality. Uniform shape/size, zero pest blemishes, optimal firmness/ripeness.
2. Grade B: Standard retail/wet market grade. Minor non-penetrating cosmetic spots (<10%), good structural integrity.
3. Grade C: Culinary / secondary processing grade. Irregular size or moderate surface defects, completely free of mold/rot.
4. Rejected: Unfit for trade. Active decay, fungal infections, excessive moisture damage, pest contamination.

You will receive:
- Crop name and category
- Quantity and measurement unit
- Farmer's claimed grade
- Inspector-confirmed grade (if physical inspection completed)
- Inspector notes & visual photo evidence URLs
- Price details

Execute the deterministic rule checks, analyze the contextual and visual information, and output a valid JSON object matching the exact schema:
{
  "passed": boolean,
  "failed_checks": [string],
  "flags": [string],
  "grade_confidence": float (between 0.0 and 1.0),
  "assessed_grade": "Grade A" | "Grade B" | "Grade C" | "Rejected",
  "reasoning_summary": string,
  "recommended_action": "Approve" | "HoldForInspection" | "ReconcileDiscrepancy" | "Reject"
}

Ensure strictly valid JSON in output.
"""


class QualityComplianceAgent:
    def __init__(self):
        self.provider = settings.llm_provider
        self.model_name = settings.default_model or "gemini-3.8-flash"
        self.api_key = settings.gemini_api_key or os.getenv("GEMINI_API_KEY", "")
        self._llm = None
        self._init_llm()

    def _init_llm(self):
        if not self.api_key or self.provider == "mock":
            logger.info("Using heuristic/mock LLM fallback for Quality Agent (no GEMINI_API_KEY or provider=mock).")
            self._llm = None
            return

        try:
            from langchain_google_genai import ChatGoogleGenerativeAI
            # Initialize Gemini model (gemini-3.8-flash or fallback to available gemini flash)
            self._llm = ChatGoogleGenerativeAI(
                model=self.model_name,
                google_api_key=self.api_key,
                temperature=0.2,
                max_output_tokens=1024
            )
            logger.info(f"Initialized Google Gemini LLM with model: {self.model_name}")
        except Exception as e:
            logger.warning(f"Could not initialize ChatGoogleGenerativeAI ({e}). Falling back to heuristic engine.")
            self._llm = None

    async def evaluate_listing(
        self,
        listing_id: str,
        crop_name: str,
        quantity: float,
        unit: str,
        claimed_grade: str,
        confirmed_grade: Optional[str] = None,
        inspector_notes: Optional[str] = None,
        photo_urls: Optional[List[str]] = None,
        min_price: Optional[float] = None,
        proposed_price: Optional[float] = None
    ) -> ValidationResultOutput:
        """
        Executes the agentic evaluation workflow:
        1. Tool Execution: Deterministic Grade Rule validation
        2. Tool Execution: Discrepancy Check (FR14)
        3. Tool Execution: Price & Volume sanity checks
        4. Gemini LLM Reasoning (or heuristic fallback)
        5. Consolidation of findings and final gate recommendation (FR5)
        """
        tool_logs: List[ToolLogEntry] = []
        failed_checks: List[str] = []
        flags: List[str] = []

        # Step 1: Grade Rule Check Tool
        grade_rule_res = validate_grade_standard.invoke({"grade": claimed_grade})
        tool_logs.append(ToolLogEntry(
            tool="validate_grade_standard",
            input={"grade": claimed_grade},
            output=grade_rule_res
        ))
        if not grade_rule_res.get("valid"):
            failed_checks.append(grade_rule_res.get("error", "Invalid claimed grade"))

        # If confirmed grade exists, validate it too
        if confirmed_grade:
            conf_rule_res = validate_grade_standard.invoke({"grade": confirmed_grade})
            tool_logs.append(ToolLogEntry(
                tool="validate_grade_standard",
                input={"grade": confirmed_grade},
                output=conf_rule_res
            ))
            if not conf_rule_res.get("valid"):
                failed_checks.append(conf_rule_res.get("error", "Invalid confirmed grade"))
            elif not conf_rule_res.get("is_publishable"):
                failed_checks.append(f"Produce confirmed as '{confirmed_grade}' and is not publishable.")

        # Step 2: Discrepancy Check Tool (FR14)
        disc_res = check_grade_discrepancy.invoke({
            "claimed_grade": claimed_grade,
            "confirmed_grade": confirmed_grade
        })
        tool_logs.append(ToolLogEntry(
            tool="check_grade_discrepancy",
            input={"claimed_grade": claimed_grade, "confirmed_grade": confirmed_grade},
            output=disc_res
        ))
        if disc_res.get("has_discrepancy"):
            flags.append(f"FR14 Discrepancy: Claimed {claimed_grade} vs Confirmed {confirmed_grade} (Severity: {disc_res.get('severity')})")

        # Step 3: Quantity & Price Tool
        feasibility_res = check_price_and_volume_feasibility.invoke({
            "quantity": quantity,
            "min_price": min_price,
            "proposed_price": proposed_price,
            "unit": unit
        })
        tool_logs.append(ToolLogEntry(
            tool="check_price_and_volume_feasibility",
            input={"quantity": quantity, "min_price": min_price, "proposed_price": proposed_price, "unit": unit},
            output=feasibility_res
        ))
        if not feasibility_res.get("passed"):
            failed_checks.extend(feasibility_res.get("errors", []))
        flags.extend(feasibility_res.get("warnings", []))

        # Step 4: LLM Reasoning with Gemini 3.8 Flash (or Heuristic Fallback)
        llm_assessment = await self._run_llm_reasoning(
            crop_name=crop_name,
            quantity=quantity,
            unit=unit,
            claimed_grade=claimed_grade,
            confirmed_grade=confirmed_grade,
            inspector_notes=inspector_notes,
            photo_urls=photo_urls or [],
            min_price=min_price,
            proposed_price=proposed_price,
            tool_findings={"failed_checks": failed_checks, "flags": flags, "discrepancy": disc_res}
        )

        # Merge results
        all_failed = list(dict.fromkeys(failed_checks + llm_assessment.get("failed_checks", [])))
        all_flags = list(dict.fromkeys(flags + llm_assessment.get("flags", [])))
        
        # Hard deterministic gate: If produce is Rejected or grade is invalid, gate MUST NOT pass
        has_critical_failure = any("rejected" in f.lower() or "invalid" in f.lower() for f in all_failed)
        passed = (len(all_failed) == 0 and not has_critical_failure)

        assessed_grade = confirmed_grade or llm_assessment.get("assessed_grade", claimed_grade)
        
        # Determine recommended action
        if not confirmed_grade:
            recommended_action = "HoldForInspection"
        elif disc_res.get("has_discrepancy"):
            recommended_action = "ReconcileDiscrepancy"
        elif not passed:
            recommended_action = "Reject"
        else:
            recommended_action = "Approve"

        return ValidationResultOutput(
            passed=passed,
            failed_checks=all_failed,
            flags=all_flags,
            grade_confidence=llm_assessment.get("grade_confidence", 0.92),
            assessed_grade=assessed_grade,
            reasoning_summary=llm_assessment.get("reasoning_summary", "Deterministic rules evaluated successfully."),
            recommended_action=recommended_action,
            tool_call_log=tool_logs
        )

    async def _run_llm_reasoning(
        self,
        crop_name: str,
        quantity: float,
        unit: str,
        claimed_grade: str,
        confirmed_grade: Optional[str],
        inspector_notes: Optional[str],
        photo_urls: List[str],
        min_price: Optional[float],
        proposed_price: Optional[float],
        tool_findings: Dict[str, Any]
    ) -> Dict[str, Any]:
        """
        Executes Gemini 3.8 Flash model invocation or intelligent fallback heuristic.
        """
        user_prompt = f"""Evaluate this produce listing for marketplace gate publication:
Produce: {crop_name}
Quantity: {quantity} {unit}
Claimed Grade by Farmer: {claimed_grade}
Physical Confirmed Grade: {confirmed_grade or 'Pending physical inspection'}
Inspector Notes: {inspector_notes or 'None provided'}
Photo Evidence URLs: {json.dumps(photo_urls)}
Stated Floor Price: LKR {min_price if min_price is not None else 'N/A'}
AI Benchmark Price: LKR {proposed_price if proposed_price is not None else 'N/A'}
Deterministic Tool Output: {json.dumps(tool_findings)}

Analyze these parameters. Verify if the grade matches standard tolerances and determine if any defect/pest flags exist."""

        if self._llm:
            try:
                messages = [
                    SystemMessage(content=SYSTEM_PROMPT),
                    HumanMessage(content=user_prompt)
                ]
                response = await self._llm.ainvoke(messages)
                raw_text = response.content.strip()
                # Clean possible markdown fence
                if raw_text.startswith("```json"):
                    raw_text = raw_text[7:]
                elif raw_text.startswith("```"):
                    raw_text = raw_text[3:]
                if raw_text.endswith("```"):
                    raw_text = raw_text[:-3]
                
                parsed = json.loads(raw_text.strip())
                return parsed
            except Exception as e:
                logger.error(f"Gemini API call failed: {e}. Utilizing heuristic analysis.")

        # Heuristic fallback if Gemini API is offline or not configured
        is_disc = tool_findings.get("discrepancy", {}).get("has_discrepancy", False)
        confirmed = confirmed_grade or claimed_grade
        
        reasoning = (
            f"Heuristic AI analysis for {crop_name}: "
            f"Evaluated claimed grade '{claimed_grade}' with {quantity} {unit} volume. "
        )
        if is_disc:
            reasoning += f"Detected grade discrepancy between farmer-claimed grade ({claimed_grade}) and confirmed inspection ({confirmed_grade}). Requires officer reconciliation."
        elif confirmed_grade == "Rejected":
            reasoning += "Physical inspection classified produce as Rejected. Gate publication blocked."
        else:
            reasoning += "Quality parameters meet Sri Lankan standards for marketplace distribution."

        return {
            "passed": confirmed_grade != "Rejected",
            "failed_checks": ["Produce rejected by inspector"] if confirmed_grade == "Rejected" else [],
            "flags": ["Claimed vs confirmed grade mismatch"] if is_disc else [],
            "grade_confidence": 0.88 if is_disc else 0.95,
            "assessed_grade": confirmed,
            "reasoning_summary": reasoning,
            "recommended_action": "ReconcileDiscrepancy" if is_disc else ("Reject" if confirmed == "Rejected" else "Approve")
        }
