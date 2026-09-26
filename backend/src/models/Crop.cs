using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class Crop
{
    public Guid Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Category { get; set; } = string.Empty;
}
