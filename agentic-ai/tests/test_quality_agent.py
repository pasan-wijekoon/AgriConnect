import pytest
from app.tools.quality_tools import validate_grade_standard, check_grade_discrepancy, check_price_and_volume_feasibility
from app.agents.quality_compliance_agent import QualityComplianceAgent


def test_grade_rules_tool():
    res_valid = validate_grade_standard.invoke({"grade": "Grade A"})
    assert res_valid["valid"] is True
    assert res_valid["is_publishable"] is True

    res_rejected = validate_grade_standard.invoke({"grade": "Rejected"})
    assert res_rejected["valid"] is True
    assert res_rejected["is_publishable"] is False

    res_invalid = validate_grade_standard.invoke({"grade": "Grade Z"})
    assert res_invalid["valid"] is False


def test_discrepancy_check_tool():
    # No mismatch
    res_match = check_grade_discrepancy.invoke({"claimed_grade": "Grade A", "confirmed_grade": "Grade A"})
    assert res_match["has_discrepancy"] is False

    # Mismatch flagged (FR14)
    res_mismatch = check_grade_discrepancy.invoke({"claimed_grade": "Grade A", "confirmed_grade": "Grade B"})
    assert res_mismatch["has_discrepancy"] is True
    assert res_mismatch["fr14_flag"] is True
    assert res_mismatch["severity"] in ["Medium", "High"]


def test_price_volume_tool():
    res_pass = check_price_and_volume_feasibility.invoke({"quantity": 100, "min_price": 200, "proposed_price": 220})
    assert res_pass["passed"] is True

    res_zero_qty = check_price_and_volume_feasibility.invoke({"quantity": 0})
    assert res_pass["passed"] is True
    assert res_zero_qty["passed"] is False


@pytest.mark.asyncio
async def test_quality_compliance_agent_evaluation():
    agent = QualityComplianceAgent()
    # Test valid Grade A produce
    result = await agent.evaluate_listing(
        listing_id="e1111111-1111-1111-1111-111111111111",
        crop_name="Tomatoes",
        quantity=250.0,
        unit="kg",
        claimed_grade="Grade A",
        confirmed_grade="Grade A",
        inspector_notes="Uniform size, zero blemishes.",
        min_price=280.0
    )
    assert result.passed is True
    assert result.assessed_grade == "Grade A"
    assert result.recommended_action == "Approve"
    assert len(result.tool_call_log) >= 3

    # Test discrepancy scenario (FR14)
    disc_result = await agent.evaluate_listing(
        listing_id="e2222222-2222-2222-2222-222222222222",
        crop_name="Bell Peppers",
        quantity=120.0,
        unit="kg",
        claimed_grade="Grade A",
        confirmed_grade="Grade B",
        inspector_notes="Minor surface spots, downgraded to Grade B."
    )
    assert disc_result.recommended_action == "ReconcileDiscrepancy"
    assert any("FR14" in flag or "discrepancy" in flag.lower() for flag in disc_result.flags)
