import pytest

from src.app.validation.matching_validation import MatchValidationError, validate_match_response


def test_valid_response_passes():
    validate_match_response("c1", 0.8, "Matched to Centre A.", {"c1", "c2"})


def test_confidence_out_of_range_raises():
    with pytest.raises(MatchValidationError):
        validate_match_response("c1", 1.5, "notes", {"c1"})


def test_confidence_negative_raises():
    with pytest.raises(MatchValidationError):
        validate_match_response("c1", -0.1, "notes", {"c1"})


def test_matched_centre_not_in_candidates_raises():
    with pytest.raises(MatchValidationError):
        validate_match_response("hallucinated-id", 0.8, "notes", {"c1", "c2"})


def test_no_match_with_nonzero_confidence_raises():
    with pytest.raises(MatchValidationError):
        validate_match_response(None, 0.5, "no match", set())


def test_no_match_with_zero_confidence_passes():
    validate_match_response(None, 0.0, "No suitable centre found.", set())


def test_empty_notes_raises():
    with pytest.raises(MatchValidationError):
        validate_match_response("c1", 0.5, "   ", {"c1"})


def test_notes_too_long_raises():
    with pytest.raises(MatchValidationError):
        validate_match_response("c1", 0.5, "x" * 2001, {"c1"})
