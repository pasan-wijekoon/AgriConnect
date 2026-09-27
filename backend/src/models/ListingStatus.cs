namespace AgriConnect.Api.Models;

/// <summary>
/// Named constants for Listing.Status (Component A's field; Component C's
/// InspectionService is the first consumer to need named constants rather than
/// inline string literals).
/// </summary>
public static class ListingStatus
{
    public const string Draft = "Draft";
    public const string PendingApproval = "PendingApproval";
    public const string Published = "Published";
    public const string Withdrawn = "Withdrawn";
    public const string SoldOut = "SoldOut";
}
