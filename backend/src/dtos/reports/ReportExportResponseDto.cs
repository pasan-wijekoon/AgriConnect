using AgriConnect.Api.Models;

namespace AgriConnect.Api.Dtos.Reports;

public class ReportExportResponseDto
{
    public Guid Id { get; set; }

    public string Type { get; set; } = string.Empty;

    public DateTimeOffset GeneratedAt { get; set; }

    public string FileUrl { get; set; } = string.Empty;

    public static ReportExportResponseDto From(ReportExport report) => new()
    {
        Id = report.Id,
        Type = report.Type,
        GeneratedAt = report.GeneratedAt,
        FileUrl = report.FileUrl,
    };
}
