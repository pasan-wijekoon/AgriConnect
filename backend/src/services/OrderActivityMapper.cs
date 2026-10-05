using System.Globalization;
using System.Text.Json;
using AgriConnect.Api.Dtos;
using AgriConnect.Api.Models;

namespace AgriConnect.Api.Services;

/// <summary>
/// Turns raw audit-log rows (FR20) into the order activity timeline clients show (FR11).
/// Only a fixed set of known fields is read out of each row's JSON details, and the raw
/// details are never passed on, so internal ids and other context cannot leak to a Buyer
/// or Farmer through this feed.
/// </summary>
public static class OrderActivityMapper
{
    public const string SystemActorName = "System";

    public static OrderActivityItem ToItem(AuditLog entry, string actorName)
    {
        var details = ParseDetails(entry.Details);

        string? explanation = null;
        string summary;
        switch (entry.Action)
        {
            case "OrderCreated":
                summary = "Order placed.";
                break;

            case "OrderStatusChanged":
                var to = Text(details, "To");
                summary = to is null
                    ? "Order status changed."
                    : to == nameof(OrderStatus.Approved)
                        ? "Order approved."
                        : $"Order status changed from {Text(details, "From") ?? "?"} to {to}.";
                break;

            case "OrderCancelled":
                var reason = Text(details, "Reason");
                summary = Text(details, "Role") == "System"
                    ? "Order cancelled automatically: the stock reservation expired."
                    : string.IsNullOrWhiteSpace(reason) ? "Order cancelled." : $"Order cancelled. Reason: {reason}";
                break;

            case "BuyerFarmerMatch":
                summary = "Nearest collection centre suggested.";
                explanation = Text(details, "Explanation") ?? Text(details, "Notes");
                break;

            case "SchedulePropose":
                summary = Time(details, "SlotStart") is { } slot
                    ? $"Pickup slot proposed for {slot} UTC."
                    : "Pickup slot proposed.";
                break;

            case "ScheduleRevisionRequested":
                var revisionReason = Text(details, "Reason");
                summary = string.IsNullOrWhiteSpace(revisionReason)
                    ? "A different pickup window was requested."
                    : $"A different pickup window was requested. Reason: {revisionReason}";
                break;

            case "ScheduleDecision":
                summary = Text(details, "Decision") switch
                {
                    "Approve" => "Pickup slot confirmed.",
                    "Reject" => "Pickup slot rejected.",
                    _ => "Pickup slot reviewed."
                };
                break;

            default:
                summary = Humanize(entry.Action) + ".";
                break;
        }

        return new OrderActivityItem(entry.Timestamp, entry.Action, summary, actorName, explanation);
    }

    private static JsonElement? ParseDetails(string? details)
    {
        if (string.IsNullOrWhiteSpace(details))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(details);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null; // a malformed row just yields the generic summary
        }
    }

    private static string? Text(JsonElement? details, string property)
    {
        if (details is { ValueKind: JsonValueKind.Object } d
            && d.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        return null;
    }

    private static string? Time(JsonElement? details, string property) =>
        Text(details, property) is { } raw
        && DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed.UtcDateTime.ToString("d MMM, HH:mm", CultureInfo.InvariantCulture)
            : null;

    /// <summary>"SomeNewAction" -> "Some new action" for audit actions this mapper doesn't know yet.</summary>
    private static string Humanize(string action)
    {
        var chars = new List<char>(action.Length + 4);
        for (var i = 0; i < action.Length; i++)
        {
            if (i > 0 && char.IsUpper(action[i]))
            {
                chars.Add(' ');
            }

            chars.Add(i == 0 ? action[i] : char.ToLowerInvariant(action[i]));
        }

        return new string(chars.ToArray());
    }
}
