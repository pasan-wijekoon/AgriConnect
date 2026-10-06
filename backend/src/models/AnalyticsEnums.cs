namespace AgriConnect.Api.Models;

/// <summary>
/// Whether a detected market event represents too little or too much supply.
/// Persisted as a string to match the DB CHECK constraint on
/// <c>ShortageOversupplyEvent.Type</c> (DFD 6.3).
/// </summary>
public enum SupplyEventType
{
    Shortage,
    Oversupply
}

/// <summary>
/// How far a shortage/oversupply event sits from its rolling baseline.
/// Persisted as a string to match the DB CHECK constraint on
/// <c>ShortageOversupplyEvent.Severity</c> (DFD 6.3).
/// </summary>
public enum SupplyEventSeverity
{
    Low,
    Medium,
    High
}

/// <summary>
/// Review state of a price anomaly raised against a listing.
/// Persisted as a string to match the DB CHECK constraint on
/// <c>PriceAnomalyFlag.Status</c> (DFD 6.3).
/// </summary>
public enum AnomalyStatus
{
    Open,
    Reviewed,
    Dismissed
}
