namespace AgriConnect.Api.Config;

/// <summary>
/// Role names used in [Authorize(Roles = ...)] attributes across the API, matching
/// the four SRS roles (DFD §1: Farmer, Buyer, Collection-Centre Officer, Administrator).
/// Kept as constants so a typo in a role string is a compile error, not a silent
/// always-403.
/// </summary>
public static class Roles
{
    public const string Buyer = "Buyer";
    public const string Farmer = "Farmer";
    public const string Officer = "Officer";
    public const string Admin = "Admin";

    // Compile-time-constant combinations for [Authorize(Roles = ...)] — string
    // concatenation of const operands is itself a constant expression, so these
    // are usable directly in attributes (string interpolation is not).
    public const string BuyerFarmerOfficer = Buyer + "," + Farmer + "," + Officer;
    public const string BuyerOfficer = Buyer + "," + Officer;
    public const string BuyerFarmer = Buyer + "," + Farmer;
}
