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
    // "Administrator" (not "Admin"): matches the DFD/User model's role string and
    // Component D's controllers ([Authorize(Roles = "Administrator")]) exactly, since
    // ASP.NET Core role checks are plain string equality.
    public const string Admin = "Administrator";

    // Compile-time-constant combinations for [Authorize(Roles = ...)] — string
    // concatenation of const operands is itself a constant expression, so these
    // are usable directly in attributes (string interpolation is not).
    public const string BuyerFarmerOfficer = Buyer + "," + Farmer + "," + Officer;
    public const string BuyerOfficer = Buyer + "," + Officer;
    public const string BuyerFarmer = Buyer + "," + Farmer;

    // Component C's FR5 publish gate (POST /api/listings/{id}/publish) is
    // Officer-driven, but Component A's existing Admin Dashboard also needs to
    // trigger it (it used to call the now-removed, ungated PATCH .../approve).
    public const string OfficerAdmin = Officer + "," + Admin;
    public const string BuyerFarmerOfficerAdmin = Buyer + "," + Farmer + "," + Officer + "," + Admin;
    public const string BuyerFarmerAdmin = Buyer + "," + Farmer + "," + Admin;
}
