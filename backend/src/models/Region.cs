using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class Region
{
    public Guid Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    // FK to CollectionCentre — will be wired when Component B adds that entity
    public Guid? CollectionCentreId { get; set; }
}
