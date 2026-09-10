# Software Requirements Specification
## AgriConnect — Smart Agriculture Marketplace & Advisory Platform

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Problem Statement](#2-problem-statement)
3. [Solution](#3-solution)
4. [Stakeholders](#4-stakeholders)
5. [Scope](#5-scope)
6. [Out of Scope](#6-out-of-scope)
7. [Functional Requirements](#7-functional-requirements)
8. [Nonfunctional Requirements](#9-nonfunctional-requirements)
9. [Assumptions and Limitations](#10-assumptions-and-limitations)

---

## 1. Introduction

AgriConnect is a marketplace and advisory platform designed to connect Sri Lankan smallholder farmers directly with buyers of agricultural produce. The platform brings together farmers, buyers, collection-centre officers, and administrators into a single system that manages produce listings, orders, quality verification, logistics, and market analytics. An AI-assisted advisory layer supports fair pricing, buyer-farmer matching, and delivery scheduling, with every AI-generated decision reviewed and approved by a human officer before it takes effect.

This document describes the problem the platform addresses, the solution it offers, the people who will use or be affected by it, what is and isn't covered by the system, and the requirements the system must satisfy.

---

## 2. Problem Statement

Smallholder farmers in Sri Lanka typically sell their produce through multiple layers of middlemen before it reaches buyers or consumers. This creates several persistent problems:

- **Low farm-gate prices** — farmers receive a small share of the final sale price because intermediaries take a cut at each stage.
- **Inflated retail prices** — buyers and consumers end up paying more than necessary due to the added markup at each layer.
- **Lack of price transparency** — farmers often have no reliable way to know what a fair market price for their produce should be.
- **Inconsistent quality standards** — there is no standardised, trusted way to verify the quality grade of produce before it is sold.
- **No auditable transaction record** — collection centres and cooperatives lack a digital, traceable record of listings, inspections, and orders.

These issues collectively reduce farmer income, distort market fairness, and make it difficult for collection centres to manage trust and accountability in the produce trade.

---

## 3. Solution

AgriConnect addresses these problems by providing a direct, digitally-mediated marketplace that removes unnecessary middlemen while preserving the essential role of collection centres in quality control and logistics coordination.

Key elements of the solution:

- **Direct listing and ordering** — farmers list their produce directly, and buyers order directly from those listings, shortening the supply chain.
- **AI-assisted fair pricing** — an advisory system analyses recent market data to suggest a fair price for each listing, helping farmers avoid being underpaid.
- **Human-approved decision-making** — every AI-suggested price, buyer-farmer match, or delivery schedule must be reviewed and approved by a collection-centre officer before it becomes final, ensuring accountability and trust.
- **Standardised quality verification** — collection-centre officers inspect and confirm the quality grade of listed produce before it becomes available for purchase.
- **Transparent market data** — price trends, shortages, and oversupply patterns are made visible to officers and administrators to support better decision-making.
- **Digital audit trail** — every listing, inspection, order, and approval decision is recorded, giving collection centres a reliable, traceable history of transactions.

The overall goals are to increase farmer income, improve price transparency, standardise produce quality grading, and provide collection centres with a dependable digital record of platform activity.

---

## 4. Stakeholders

| # | Stakeholder | Role / Interest |
|---|---|---|
| 1 | **Farmer** | Registers produce listings, uploads photos, and tracks orders. Wants fair pricing, a simple listing process, and visibility into buyer demand. |
| 2 | **Buyer** | Browses, searches, and orders produce, and tracks delivery/pickup status. Wants transparent pricing, reliable quality information, and predictable logistics. |
| 3 | **Collection-Centre Officer** | Verifies produce quality, reviews and approves AI-suggested prices and logistics, and manages orders and schedules for their centre. |
| 4 | **Administrator** | Manages user accounts, roles, and platform-wide settings, and oversees reporting and analytics across all collection centres. |

---

## 5. Scope

The AgriConnect platform covers:

- Registration and role-based access for Farmers, Buyers, Officers, and Administrators.
- Creation, browsing, searching, filtering, and sorting of produce listings.
- AI-assisted fair-price suggestions for new listings, subject to officer approval.
- Order placement, stock reservation, and pickup/delivery scheduling.
- Quality inspection recording and grade verification prior to a listing being published.
- Buyer-farmer matching and logistics scheduling assistance, with human approval before execution.
- Market price trend analytics, shortage/oversupply detection, and reporting dashboards.
- An auditable record of all listings, inspections, orders, AI proposals, and approval decisions.
- Nearest-collection-centre recommendation and distance estimation to support logistics planning.
- Notifications/status updates for farmers and buyers as their listings and orders progress.

---

## 6. Out of Scope

The following are explicitly not covered by the current version of AgriConnect:

- Real, production-scale payment processing or money transfer between parties (payment/settlement steps may be recorded for tracking only).
- Fully autonomous AI decision-making — the AI subsystem may only *propose* pricing, matching, or scheduling decisions; it cannot commit any transaction without human officer approval.
- Support for produce categories or transaction types outside the standard crop-listing and ordering workflow (e.g. auctions, contract farming, futures/forward pricing).
- Multi-language interface at launch (localisation into Sinhala and Tamil is planned for the future, but only English is required initially).
- International/cross-border trade — the platform is scoped to a single country's collection-centre network.
- Direct farmer-to-buyer communication outside the platform's structured listing/order workflow.
- Insurance, credit, or financing services for farmers or buyers.

---

## 7. Functional Requirements

1. The system shall allow Farmers and Buyers to self-register; Officer and Administrator accounts shall be created only by an Administrator.
2. The system shall authenticate users and restrict access to features based on their assigned role.
3. The system shall allow Farmers to create a produce listing with crop type, quantity, claimed quality grade, at least one photo, and a preferred pickup window.
4. The system shall generate an AI-suggested fair price for every new listing based on recent market data.
5. The system shall prevent a listing from becoming visible to buyers until it has passed quality verification and received officer approval.
6. The system shall allow Buyers to search, filter, sort, and paginate through published listings by crop, region, price range, and quality grade.
7. The system shall allow Farmers to edit or withdraw a listing that has not yet been ordered against.
8. The system shall allow Buyers to place orders against published listings, specifying quantity and delivery/pickup preferences.
9. The system shall reserve stock for an order in a way that prevents overselling when multiple buyers order concurrently.
10. The system shall propose a conflict-free pickup/delivery schedule for each order, based on collection-centre capacity and existing bookings.
11. The system shall allow Farmers and Buyers to track the status of their orders (e.g. Pending, Approved, Scheduled, Completed, Cancelled).
12. The system shall allow Officers to record inspection outcomes (confirmed grade, notes, optional photos) for a listing.
13. The system shall maintain a complete inspection history for each listing, including who performed the inspection and when.
14. The system shall flag any listing where the claimed quality grade differs from the inspected grade, for officer review.
15. The system shall display historical price trends per crop and region over a selectable time period.
16. The system shall detect and flag listings whose price deviates significantly from the AI-suggested fair-price range.
17. The system shall detect and surface recurring shortage or oversupply patterns per crop and region.
18. The system shall allow Administrators to export summary reports of listings, orders, and price trends.
19. The system shall present every AI-generated proposal (price, match, or schedule) to an Officer for Approve / Reject / Request Revision before it takes effect.
20. The system shall maintain a complete, auditable log of every AI proposal, its validation outcome, and the final human decision.
21. The system shall recommend the nearest suitable collection centre and estimate delivery distance for a buyer or farmer.
22. The system shall notify Farmers and Buyers of status changes relevant to their listings and orders.

---

## 8. Nonfunctional Requirements

- **Performance:** The system shall respond to standard search, filter, and listing operations quickly enough to avoid noticeable delay for users under normal load.
- **Reliability:** The platform shall maintain high availability, particularly during peak listing and ordering periods.
- **Safety:** No AI-generated price, stock reservation, or schedule change shall ever be committed without passing validation and receiving explicit human approval.
- **Data Protection:** Financial and price-related data shall be validated to prevent erroneous or fraudulent values from entering the system.
- **Security:** User accounts and role-based permissions shall be protected against unauthorised access; sensitive credentials (e.g. third-party service keys) shall never be exposed to end users.
- **Auditability:** All approval decisions, inspections, and AI proposals shall be logged and retrievable for at least one year to support accountability.
- **Usability:** A first-time farmer should be able to create a produce listing quickly and with minimal steps, without requiring external assistance.
- **Scalability:** The system shall be able to support a growing volume of listings, orders, and users without a significant drop in responsiveness.
- **Data Backup:** The system shall be backed up regularly to prevent irrecoverable loss of listing, order, and inspection data.

---

## 9. Assumptions and Limitations

**Assumptions:**
- Farmers and buyers have access to a smartphone with a camera and internet connectivity.
- A maps/distance service remains available and usable for calculating distances between users and collection centres.
- Recent market price reference data for common crops can be obtained to ground fair-price suggestions.
- Each collection centre has at least one staff member available to review listings, inspections, and AI proposals.
- Users generally trust and will engage with the human-approval step rather than bypassing or ignoring it.

**Limitations:**
- The platform is currently scoped for a single-country context and does not support cross-border transactions.
- The AI advisory layer is only as accurate as the market data it is given; it may not fully account for sudden, unpredictable market shifts.
- The system depends on officers being available and responsive; delays in officer approval will delay listing publication and order fulfilment.
- Real monetary settlement is out of scope, so the platform cannot fully replace all functions of a traditional trade transaction (e.g. payment guarantees).
- Initial language support is limited to English, which may affect accessibility for some farmers and buyers.
