/// There is no `Listing`/Component A API anywhere in this repo yet (plan §3
/// — "Not implemented on any branch"), and Component B does not own a
/// listings endpoint. The place-order screen needs *something* selectable,
/// so this mirrors `backend/src/config/OrderLogisticsFixtures.cs`'s own
/// demo listings exactly (same GUIDs, same names) — the same stand-in
/// strategy the backend itself already uses, not a second invented one.
/// Swap for a real `GET /api/listings` call once Component A lands.
library;

class ListingFixture {
  final String id;
  final String cropName;
  final double availableQuantity;

  const ListingFixture({
    required this.id,
    required this.cropName,
    required this.availableQuantity,
  });
}

const listingFixtures = [
  ListingFixture(
    id: '4f2b1001-0000-0000-0000-000000000001',
    cropName: 'Carrots',
    availableQuantity: 500,
  ),
  ListingFixture(
    id: '4f2b1002-0000-0000-0000-000000000002',
    cropName: 'Tomatoes',
    availableQuantity: 300,
  ),
];
