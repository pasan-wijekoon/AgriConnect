"""
Deterministic Price Bounds Validation.
Ensures AI-suggested fair price ranges fall within sane bounds of historical data
and economic consistency before any proposal is handed off to an officer.
"""
from typing import Dict, Any, List


class PriceBoundsValidator:
    """Validates suggested price range against historical data and business rules."""

    @staticmethod
    def validate(
        suggested_min: float,
        suggested_max: float,
        historical_ma_7d: float,
        claimed_grade: str,
        quantity: float
    ) -> Dict[str, Any]:
        passed = True
        failed_checks: List[str] = []
        flags: List[str] = []

        # Check 1: Non-negative and positive price values
        if suggested_min <= 0 or suggested_max <= 0:
            passed = False
            failed_checks.append("Suggested price values must be strictly greater than 0.")

        # Check 2: Max >= Min
        if suggested_max < suggested_min:
            passed = False
            failed_checks.append("Suggested maximum price cannot be lower than minimum price.")

        # Check 3: Price spread rationality (max spread should not be broader than 80%)
        if suggested_min > 0:
            spread_ratio = suggested_max / suggested_min
            if spread_ratio > 1.80:
                flags.append(f"Price spread is wide ({spread_ratio:.2f}x). Officer review advised.")

        # Check 4: Historical sanity bounds (reject severe outliers: < 40% or > 220% of 7d MA)
        if historical_ma_7d > 0:
            lower_sane_bound = historical_ma_7d * 0.40
            upper_sane_bound = historical_ma_7d * 2.20

            if suggested_min < lower_sane_bound:
                passed = False
                failed_checks.append(
                    f"Outlier rejected: Minimum price LKR {suggested_min:.2f} is below sane historical floor (LKR {lower_sane_bound:.2f})."
                )

            if suggested_max > upper_sane_bound:
                passed = False
                failed_checks.append(
                    f"Outlier rejected: Maximum price LKR {suggested_max:.2f} exceeds sane historical ceiling (LKR {upper_sane_bound:.2f})."
                )

        # Check 5: Quantity positive check
        if quantity <= 0:
            passed = False
            failed_checks.append("Quantity must be greater than zero.")

        # Check 6: Grade premium check
        normalized_grade = claimed_grade.upper().strip()
        if normalized_grade not in ["A", "B", "C"]:
            flags.append(f"Non-standard grade '{claimed_grade}' provided. Baseline B assumed.")

        checks_performed = [
            "Positive price bounds check",
            "Max >= Min range consistency",
            "Rational spread ratio check (<1.8x)",
            "Historical 7-day MA sanity bound (0.4x - 2.2x)",
            "Quantity validation",
            "Grade classification check"
        ]

        return {
            "passed": passed,
            "failed_checks": failed_checks,
            "flags": flags,
            "checks_performed": checks_performed,
            "summary": "Validation PASSED: Price range conforms to historical market bounds." if passed else f"Validation FAILED: {'; '.join(failed_checks)}"
        }
