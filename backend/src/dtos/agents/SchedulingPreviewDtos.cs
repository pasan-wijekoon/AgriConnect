using System.ComponentModel.DataAnnotations;

namespace AgriConnect.Api.Dtos.Agents;

public class SchedulingPreviewRequestDto
{
    /// <summary>The collection centre to schedule at.</summary>
    [Required, MaxLength(50)]
    public string CentreId { get; set; } = string.Empty;

    [Required]
    public TimeWindowDto? PreferredWindow { get; set; }

    [MaxLength(50)]
    public List<BookingDto> ExistingBookings { get; set; } = [];
}

public class TimeWindowDto
{
    [Required]
    public DateTimeOffset? Start { get; set; }

    [Required]
    public DateTimeOffset? End { get; set; }
}

public class BookingDto
{
    [Required]
    public DateTimeOffset? SlotStart { get; set; }

    [Required]
    public DateTimeOffset? SlotEnd { get; set; }
}

public class SchedulingPreviewResponseDto
{
    public DateTimeOffset ProposedSlotStart { get; set; }

    public DateTimeOffset ProposedSlotEnd { get; set; }

    public bool ConflictChecked { get; set; }

    /// <summary>The agent's explanation for the officer.</summary>
    public string Reasoning { get; set; } = string.Empty;
}
