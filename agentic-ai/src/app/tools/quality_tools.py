"""
Deterministic Quality and Compliance validation tools for Component C per AgriConnect DFD Section 4.3.
- GradeRulesTool: Validates that produce grade adheres to accepted standards (Grade A, Grade B, Grade C, Rejected)
- DiscrepancyCheckTool: Compares farmer-claimed grade vs inspector-confirmed grade (FR14)
- PriceRangeCheckTool: Deterministic price feasibility check
"""
from typing import Dict, Any, List, Optional
from langchain_core.tools import tool


VALID_GRADES = {"Grade A", "Grade B", "Grade C", "Rejected"}

GRADE_STANDARDS = {
    "Grade A": {
        "description": "Premium export/supermarket grade. Uniform shape/size, zero insect damage, optimal ripeness.",
        "max_blemish_pct": 2.0,
        "is_publishable": True
    },
    "Grade B": {
        "description": "Standard market grade. Minor cosmetic blemishes permitted (<10%), good firmness.",
        "max_blemish_pct": 10.0,
        "is_publishable": True
    },
    "Grade C": {
        "description": "Processing/cooking grade. Moderate cosmetic defects, irregular sizing, strictly no rot.",
        "max_blemish_pct": 25.0,
        "is_publishable": True
    },
    "Rejected": {
        "description": "Unfit for consumer distribution due to rot, severe pest damage, or high moisture decay.",
        "max_blemish_pct": 100.0,
        "is_publishable": False
    }
}


@tool
def validate_grade_standard(grade: str) -> Dict[str, Any]:
    """
    Validates if a specified grade is recognized within standard Sri Lankan agricultural specifications
    and determines whether it is publishable.
    """
    normalized = grade.strip() if grade else ""
    # Case-insensitive match
    matched_key = next((k for k in VALID_GRADES if k.lower() == normalized.lower()), None)
    
    if not matched_key:
        return {
            "valid": False,
            "grade": grade,
            "error": f"Invalid quality grade '{grade}'. Allowed grades: {', '.join(VALID_GRADES)}.",
            "is_publishable": False
        }
        
    specs = GRADE_STANDARDS[matched_key]
    return {
        "valid": True,
        "grade": matched_key,
        "description": specs["description"],
        "max_blemish_pct": specs["max_blemish_pct"],
        "is_publishable": specs["is_publishable"]
    }


@tool
def check_grade_discrepancy(claimed_grade: str, confirmed_grade: Optional[str]) -> Dict[str, Any]:
    """
    Deterministic verification checking whether the farmer-claimed grade matches the
    officer/inspected grade per SRS FR14.
    """
    if not confirmed_grade:
        return {
            "has_discrepancy": False,
            "status": "AwaitingInspection",
            "message": "Produce has not undergone physical officer inspection yet."
        }

    claimed_clean = claimed_grade.strip()
    confirmed_clean = confirmed_grade.strip()
    
    is_mismatch = (claimed_clean.lower() != confirmed_clean.lower())
    
    # Assess severity
    grade_order = {"grade a": 3, "grade b": 2, "grade c": 1, "rejected": 0}
    claimed_score = grade_order.get(claimed_clean.lower(), -1)
    confirmed_score = grade_order.get(confirmed_clean.lower(), -1)
    
    severity = "None"
    if is_mismatch:
        diff = claimed_score - confirmed_score
        if diff > 1 or confirmed_clean.lower() == "rejected":
            severity = "High"
        elif diff == 1:
            severity = "Medium"
        else:
            severity = "Low"  # upgraded grade

    return {
        "has_discrepancy": is_mismatch,
        "claimed_grade": claimed_clean,
        "confirmed_grade": confirmed_clean,
        "severity": severity,
        "requires_officer_reconciliation": is_mismatch,
        "fr14_flag": is_mismatch,
        "note": f"Grade mismatch flagged: Farmer claimed '{claimed_clean}' but verified outcome is '{confirmed_clean}'." if is_mismatch else "Claimed and confirmed grades match."
    }


@tool
def check_price_and_volume_feasibility(
    quantity: float,
    min_price: Optional[float] = None,
    proposed_price: Optional[float] = None,
    unit: str = "kg"
) -> Dict[str, Any]:
    """
    Validates physical listing quantity and price consistency before publication.
    """
    warnings: List[str] = []
    errors: List[str] = []

    if quantity <= 0:
        errors.append(f"Quantity must be greater than zero. Received: {quantity} {unit}.")
    elif quantity > 50000:
        warnings.append(f"Unusually large single listing volume ({quantity} {unit}). Ensure batch warehousing capacity.")

    if min_price is not None and min_price <= 0:
        errors.append("Minimum price must be greater than 0.")

    if proposed_price is not None and min_price is not None:
        if proposed_price < min_price:
            warnings.append(f"AI proposed price ({proposed_price}) is below the farmer's stated floor price ({min_price}).")

    return {
        "passed": len(errors) == 0,
        "errors": errors,
        "warnings": warnings,
        "quantity": quantity,
        "unit": unit
    }
