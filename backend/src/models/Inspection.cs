using System;
using System.Collections.Generic;

namespace backend.src.models;

public static class QualityGrade
{
    public const string GradeA = "Grade A";
    public const string GradeB = "Grade B";
    public const string GradeC = "Grade C";
    public const string Rejected = "Rejected";

    public static readonly HashSet<string> ValidGrades = new(StringComparer.OrdinalIgnoreCase)
    {
        GradeA, GradeB, GradeC, Rejected
    };

    public static bool IsValid(string grade) => ValidGrades.Contains(grade);
}

public class Inspection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ListingId { get; set; }
    public Guid OfficerId { get; set; }
    public string ConfirmedGrade { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime InspectedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Listing? Listing { get; set; }
    public User? Officer { get; set; }
    public ICollection<InspectionPhoto> Photos { get; set; } = new List<InspectionPhoto>();
}

public class InspectionPhoto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InspectionId { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Inspection? Inspection { get; set; }
}

public class GradeDiscrepancyFlag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ListingId { get; set; }
    public Guid? InspectionId { get; set; }
    public string ClaimedGrade { get; set; } = string.Empty;
    public string ConfirmedGrade { get; set; } = string.Empty;
    public DateTime FlaggedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public Guid? ResolvedByOfficerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Listing? Listing { get; set; }
    public Inspection? Inspection { get; set; }
    public User? ResolvedByOfficer { get; set; }
}
