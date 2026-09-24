from src.app.tools.centre_capacity_tool import CentreCapacityTool


def test_check_with_room_reports_has_capacity_true():
    result = CentreCapacityTool.check(capacity=5, current_confirmed_bookings=2)

    assert result.has_capacity is True
    assert result.remaining_slots == 3


def test_check_at_full_capacity_reports_has_capacity_false():
    result = CentreCapacityTool.check(capacity=3, current_confirmed_bookings=3)

    assert result.has_capacity is False
    assert result.remaining_slots == 0


def test_check_over_capacity_does_not_go_negative():
    result = CentreCapacityTool.check(capacity=2, current_confirmed_bookings=5)

    assert result.has_capacity is False
    assert result.remaining_slots == 0
