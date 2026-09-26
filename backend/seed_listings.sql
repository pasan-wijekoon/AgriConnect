-- Clean any old seed if wanted, or just insert new ones with fixed IDs
INSERT INTO "Listings" ("Id", "FarmerId", "CropId", "RegionId", "Quantity", "Unit", "ClaimedGrade", "PickupWindowStart", "PickupWindowEnd", "Status", "MinPrice", "Description", "CreatedAt", "UpdatedAt")
VALUES 
(
  'c0000000-0000-0000-0000-000000000001',
  'f0000000-0000-0000-0000-000000000001', -- Kamal
  'a1000000-0000-0000-0000-000000000004', -- Tomatoes
  'b1000000-0000-0000-0000-000000000008', -- Nuwara Eliya
  500.00,
  'kg',
  'A',
  NOW() + interval '2 days',
  NOW() + interval '5 days',
  'Published',
  280.00,
  'Highland fresh greenhouse cherry & beef tomatoes, organically cultivated in Nuwara Eliya cool climate. Firm texture, vibrant crimson color, ideal for premium retail supermarkets and hotel chains.',
  NOW() - interval '2 days',
  NOW()
),
(
  'c0000000-0000-0000-0000-000000000002',
  'f0000000-0000-0000-0000-000000000001', -- Kamal
  'a1000000-0000-0000-0000-000000000005', -- Carrots
  'b1000000-0000-0000-0000-000000000008', -- Nuwara Eliya
  1200.00,
  'kg',
  'A',
  NOW() + interval '1 day',
  NOW() + interval '4 days',
  'Published',
  190.00,
  'Crisp, sweet highland carrots freshly harvested from hillside terraces. Washed, graded A uniform 15-20cm roots with zero chemical post-harvest treatment. Packed in 25kg aerated crates.',
  NOW() - interval '4 hours',
  NOW()
),
(
  'c0000000-0000-0000-0000-000000000003',
  'f0000000-0000-0000-0000-000000000002', -- Saman Silva
  'a1000000-0000-0000-0000-000000000002', -- Tea
  'b1000000-0000-0000-0000-000000000002', -- Kandy
  350.00,
  'kg',
  'A',
  NOW() + interval '3 days',
  NOW() + interval '7 days',
  'Published',
  1450.00,
  'Single-estate mid-grown Ceylon BOPF tea leaves from Kandy hills. Hand-plucked two leaves and a bud, artisanal CTC processing with floral aroma and rich copper liquor.',
  NOW() - interval '3 days',
  NOW()
),
(
  'c0000000-0000-0000-0000-000000000004',
  'f0000000-0000-0000-0000-000000000002', -- Saman Silva
  'a1000000-0000-0000-0000-000000000003', -- Coconut
  'b1000000-0000-0000-0000-000000000007', -- Kurunegala
  2500.00,
  'pieces',
  'A',
  NOW() + interval '2 days',
  NOW() + interval '6 days',
  'Published',
  110.00,
  'Prime mature export-quality Kurunegala coconuts. Average circumference 13-14 inches, thick kernel meat, rich water content. Perfect for bulk oil pressing or wholesale distribution.',
  NOW() - interval '1 day',
  NOW()
),
(
  'c0000000-0000-0000-0000-000000000005',
  'f0000000-0000-0000-0000-000000000002', -- Saman Silva
  'a1000000-0000-0000-0000-000000000006', -- Chili
  'b1000000-0000-0000-0000-000000000004', -- Jaffna
  400.00,
  'kg',
  'B',
  NOW() + interval '1 day',
  NOW() + interval '3 days',
  'Published',
  620.00,
  'Spicy Jaffna green and red bird-eye chilies. Sun-drenched high heat level (Scoville 50,000+), clean sorted, packed in moisture-resistant gunny bags for regional wholesale dispatch.',
  NOW() - interval '6 hours',
  NOW()
),
(
  'c0000000-0000-0000-0000-000000000006',
  'f0000000-0000-0000-0000-000000000001', -- Kamal
  'a1000000-0000-0000-0000-000000000008', -- Potatoes
  'b1000000-0000-0000-0000-000000000008', -- Nuwara Eliya
  800.00,
  'kg',
  'B',
  NOW() + interval '5 days',
  NOW() + interval '10 days',
  'Published',
  240.00,
  'Granola variety highland red potatoes. Solid starch density, dry skin, suitable for chips and commercial wholesale supply.',
  NOW() - interval '1 day',
  NOW()
)
ON CONFLICT ("Id") DO NOTHING;

-- Photos for each listing
INSERT INTO "ListingPhotos" ("Id", "ListingId", "Url", "UploadedAt")
VALUES
-- Tomatoes
('d0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000001', 'https://images.unsplash.com/photo-1592924357228-91a4daadcfea?w=800&auto=format&fit=crop', NOW()),
('d0000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000001', 'https://images.unsplash.com/photo-1546470427-0d4db154ceb7?w=800&auto=format&fit=crop', NOW()),
('d0000000-0000-0000-0000-000000000003', 'c0000000-0000-0000-0000-000000000001', 'https://images.unsplash.com/photo-1582284540020-8acbe03f4924?w=800&auto=format&fit=crop', NOW()),

-- Carrots
('d0000000-0000-0000-0000-000000000004', 'c0000000-0000-0000-0000-000000000002', 'https://images.unsplash.com/photo-1598170845058-32b9d6a5da37?w=800&auto=format&fit=crop', NOW()),
('d0000000-0000-0000-0000-000000000005', 'c0000000-0000-0000-0000-000000000002', 'https://images.unsplash.com/photo-1447175008436-054170c2e979?w=800&auto=format&fit=crop', NOW()),

-- Tea
('d0000000-0000-0000-0000-000000000006', 'c0000000-0000-0000-0000-000000000003', 'https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800&auto=format&fit=crop', NOW()),
('d0000000-0000-0000-0000-000000000007', 'c0000000-0000-0000-0000-000000000003', 'https://images.unsplash.com/photo-1544787219-7f47ccb76574?w=800&auto=format&fit=crop', NOW()),

-- Coconuts
('d0000000-0000-0000-0000-000000000008', 'c0000000-0000-0000-0000-000000000004', 'https://images.unsplash.com/photo-1550258987-190a2d41a8ba?w=800&auto=format&fit=crop', NOW()),
('d0000000-0000-0000-0000-000000000009', 'c0000000-0000-0000-0000-000000000004', 'https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=800&auto=format&fit=crop', NOW()),

-- Chili
('d0000000-0000-0000-0000-000000000010', 'c0000000-0000-0000-0000-000000000005', 'https://images.unsplash.com/photo-1588252303782-cb80119abd6d?w=800&auto=format&fit=crop', NOW()),

-- Potatoes
('d0000000-0000-0000-0000-000000000011', 'c0000000-0000-0000-0000-000000000006', 'https://images.unsplash.com/photo-1518977676601-b53f82aba655?w=800&auto=format&fit=crop', NOW())
ON CONFLICT ("Id") DO NOTHING;

-- Price Suggestions
INSERT INTO "PriceSuggestions" ("Id", "ListingId", "SuggestedPriceMin", "SuggestedPriceMax", "Confidence", "ReasoningSummary", "Status", "CreatedAt", "UpdatedAt")
VALUES
(
  'e0000000-0000-0000-0000-000000000001',
  'c0000000-0000-0000-0000-000000000001',
  275.00,
  310.00,
  0.92,
  'Agentic AI: Based on Manning Market wholesale indices and central province greenhouse yield trends. Grade A premium pricing justified.',
  'Approved',
  NOW() - interval '2 days',
  NOW()
),
(
  'e0000000-0000-0000-0000-000000000002',
  'c0000000-0000-0000-0000-000000000002',
  180.00,
  215.00,
  0.88,
  'Agentic AI: Nuwara Eliya harvest peak has elevated supply. Farmer min price Rs 190 sits comfortably in recommended corridor Rs 180-215/kg.',
  'Proposed',
  NOW() - interval '4 hours',
  NOW()
),
(
  'e0000000-0000-0000-0000-000000000003',
  'c0000000-0000-0000-0000-000000000003',
  1400.00,
  1520.00,
  0.95,
  'Agentic AI: Colombo Tea Auction mid-grown BOPF averages steady at Rs 1460/kg. High export demand.',
  'Approved',
  NOW() - interval '3 days',
  NOW()
),
(
  'e0000000-0000-0000-0000-000000000005',
  'c0000000-0000-0000-0000-000000000005',
  580.00,
  640.00,
  0.79,
  'Agentic AI: High volatility in Dambulla green chili wholesale arrivals. Suggesting Rs 580 - 640 corridor.',
  'Proposed',
  NOW() - interval '6 hours',
  NOW()
)
ON CONFLICT ("Id") DO NOTHING;
