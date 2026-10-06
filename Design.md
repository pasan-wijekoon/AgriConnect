# AgriConnect — UI/UX Design System

## 1. Purpose

This document defines the common UI/UX design system for the AgriConnect project.

All frontend interfaces developed by the team must follow this design system so that the entire application looks like **one professional system**, rather than separate features developed by different students.

This document defines:

* Visual style
* Colors
* Typography
* Spacing
* Buttons
* Forms
* Cards
* Tables
* Navigation
* Status indicators
* Alerts
* Loading states
* Empty states
* Error states
* Responsive behavior
* Accessibility
* Component consistency

Component-specific screens may add necessary elements, but they must not introduce a completely different visual style.

---

# 2. Overall Design Direction

AgriConnect should have a:

**Modern + Clean + Professional + Agriculture-focused + Simple**

visual identity.

The interface should feel:

* Trustworthy
* Easy to understand
* Modern
* Lightweight
* Professional
* Suitable for farmers, buyers, officers and administrators

Avoid overly decorative interfaces.

The UI should prioritize:

```text
Clarity
   ↓
Usability
   ↓
Consistency
   ↓
Visual quality
```

The application should not look like a generic admin dashboard or a template copied from another project.

---

# 3. Design Philosophy

The UI should follow these principles:

### 3.1 Simple

Users should understand what to do without reading long instructions.

### 3.2 Consistent

The same component should always look and behave the same way.

For example:

```text
Primary Button
      ↓
same style everywhere
```

### 3.3 Information First

Important information such as:

* Order status
* Quantity
* Schedule
* Centre
* Price
* Availability

should be visually easy to find.

### 3.4 Minimal

Do not add UI elements simply to make the screen look full.

Every element should have a purpose.

### 3.5 Responsive

The same design language must work across:

* Desktop
* Tablet
* Mobile

---

# 4. Brand Style

AgriConnect is an agricultural platform.

The visual identity should therefore use a **natural green-based primary theme**, supported by neutral backgrounds and status colors.

The design should feel similar to a modern agricultural technology platform rather than a traditional government website.

---

# 5. Color System

Use a consistent semantic color system.

## Primary

Use a deep/agriculture green as the primary brand color.

Purpose:

* Main buttons
* Active navigation
* Links
* Important actions
* Selected states

Example:

```text
Primary Green
#2E7D32
```

Use the exact existing project color if another team member has already established a brand color.

**Important:** Do not independently introduce another primary green.

---

## Secondary

Use a lighter green for supporting elements.

Example:

```text
Secondary Green
#66BB6A
```

Use for:

* Secondary highlights
* Supporting icons
* Positive visual elements
* Charts where appropriate

---

## Background

Main application background:

```text
#F7F9F7
```

Cards:

```text
#FFFFFF
```

The interface should primarily use white cards over a very light neutral/green-tinted background.

---

## Text

Primary text:

```text
#1F2937
```

Secondary text:

```text
#6B7280
```

Muted text:

```text
#9CA3AF
```

Avoid pure black for most UI text.

---

# 6. Semantic Status Colors

Status colors must have consistent meanings throughout the entire system.

| Meaning           | Color family |
| ----------------- | ------------ |
| Success           | Green        |
| Pending / Waiting | Amber        |
| Warning           | Orange       |
| Error / Cancelled | Red          |
| Information       | Blue         |
| Neutral           | Gray         |

For example:

```text
Pending       → Amber
Approved      → Green
Scheduled     → Blue
Completed     → Green
Cancelled     → Red
Proposed      → Purple/Blue
```

The exact colors should remain consistent across:

* Web
* Mobile
* Cards
* Tables
* Badges
* Notifications

---

# 7. Typography

Use one primary font family across the application.

Preferred:

```text
Inter
```

If the existing project already uses another consistent font, use that instead of introducing a second font.

Typography hierarchy:

```text
Page Title
    ↓
Section Heading
    ↓
Card Heading
    ↓
Body Text
    ↓
Secondary Text
    ↓
Caption
```

Recommended:

```text
Page title:       28–32px
Section heading:  20–24px
Card heading:     16–18px
Body:             14–16px
Secondary:        13–14px
Caption:          12px
```

Avoid excessive font-size variation.

---

# 8. Spacing System

Use a consistent spacing scale.

Base unit:

```text
4px
```

Recommended spacing:

```text
4px
8px
12px
16px
20px
24px
32px
40px
48px
```

Do not randomly use values such as:

```text
13px
17px
23px
27px
```

unless there is a specific design reason.

---

# 9. Border Radius

Use moderate rounded corners.

Recommended:

```text
Small controls: 8px
Cards:           12px
Large containers:16px
Pills/badges:    999px
```

Avoid extremely rounded interfaces.

The application should remain professional.

---

# 10. Shadows

Use subtle shadows.

Cards should not have heavy shadows.

Preferred:

```text
very light shadow
+
subtle border
```

The UI should feel clean rather than floating.

---

# 11. Layout

The desktop application should use a consistent application shell.

```text
┌──────────────────────────────────────────────────────┐
│                    Top Header                        │
├───────────────┬──────────────────────────────────────┤
│               │                                      │
│   Sidebar     │             Main Content             │
│               │                                      │
│   Dashboard   │                                      │
│   Orders      │                                      │
│   Listings    │                                      │
│   Centres     │                                      │
│   Reports     │                                      │
│   Settings    │                                      │
│               │                                      │
└───────────────┴──────────────────────────────────────┘
```

The sidebar and header should remain visually consistent across all team-developed pages.

---

# 12. Navigation

Navigation must clearly indicate the current page.

Example:

```text
Dashboard
Orders
Listings
Collection Centres
Analytics
Notifications
Settings
```

Active navigation:

```text
Primary green background or green accent
+
clear text/icon contrast
```

Do not create a different sidebar design for individual components.

---

# 13. Page Structure

Every major page should generally follow:

```text
Page
│
├── Page Header
│     ├── Title
│     ├── Description
│     └── Primary Action
│
├── Filters / Search
│
├── Main Content
│
└── Supporting Content
```

Example:

```text
Orders
Manage and track agricultural orders

[ Search orders... ] [Status ▼] [+ Create Order]

┌──────────────────────────────────────────────┐
│ Order #1024                                  │
│ Tomato • 50 kg                               │
│                                              │
│ Buyer: John                                  │
│ Centre: Colombo Collection Centre            │
│                                              │
│ Status: Scheduled                            │
└──────────────────────────────────────────────┘
```

---

# 14. Cards

Cards are the primary content container.

A card should contain:

```text
Header
──────
Title
Supporting information

Main content

Optional footer/action area
```

Cards should have:

* White background
* Subtle border
* 12px radius
* Light shadow
* Consistent padding

Recommended padding:

```text
16px – 24px
```

---

# 15. Buttons

Use three primary button levels.

## Primary

For the main action.

Examples:

```text
Place Order
Approve
Confirm Schedule
Save
```

Use the primary brand color.

---

## Secondary

For supporting actions.

Examples:

```text
Cancel
Back
View Details
```

Use an outlined or neutral style.

---

## Destructive

For actions such as:

```text
Cancel Order
Delete
Reject
```

Use the semantic error color.

Do not use red for normal actions.

---

# 16. Button Rules

Buttons should:

* Have clear labels
* Use action-oriented text
* Show hover state on web
* Show pressed/loading state
* Be disabled when action is unavailable
* Never silently perform destructive actions

For important destructive actions:

```text
Button
  ↓
Confirmation dialog
  ↓
Confirm / Cancel
```

---

# 17. Forms

Forms must be simple and clearly structured.

Example:

```text
Place Order

Listing
[ Tomato — Grade A                  ▼ ]

Quantity
[ 50.00 kg                         ]

Delivery Preference

(●) Pickup
( ) Delivery

Collection Centre
[ Select centre                    ▼ ]

             [ Cancel ] [ Place Order ]
```

Each field should have:

* Label
* Input
* Optional helper text
* Validation message

Do not rely only on placeholder text as the field label.

---

# 18. Form Validation

Validation messages should appear close to the relevant field.

Bad:

```text
Something went wrong.
```

Good:

```text
Quantity must be greater than 0.
```

Validation should be:

* Clear
* Short
* Human-readable
* Actionable

---

# 19. Tables

Tables should be used for information-heavy desktop views.

Example:

```text
Orders

┌────────┬──────────┬──────────┬──────────┬──────────┐
│ Order  │ Buyer    │ Quantity │ Centre   │ Status   │
├────────┼──────────┼──────────┼──────────┼──────────┤
│ #1024  │ Kamal    │ 50 kg    │ Colombo  │ Approved │
│ #1025  │ Nimal    │ 20 kg    │ Gampaha  │ Pending  │
└────────┴──────────┴──────────┴──────────┴──────────┘
```

Rules:

* Clear column headings
* Consistent alignment
* Status badge instead of raw status text
* Row hover state
* Pagination for large datasets
* Responsive alternative on small screens

---

# 20. Status Badges

Statuses must use a common badge component.

Example:

```text
[ Pending ]
[ Approved ]
[ Scheduled ]
[ Completed ]
[ Cancelled ]
```

Do not create different badge designs on different pages.

Example:

```text
Pending     → amber badge
Approved    → green badge
Scheduled   → blue badge
Completed   → green badge
Cancelled   → red badge
```

---

# 21. Dashboard Cards

Dashboard statistics should use consistent cards.

Example:

```text
┌────────────────┐
│ Pending Orders │
│                │
│      24        │
│                │
│ +5 this week   │
└────────────────┘
```

Use icons sparingly.

The number should be visually dominant.

---

# 22. Search and Filters

Search should be positioned near the page header.

Example:

```text
[ 🔍 Search orders... ]

[ Status ▼ ] [ Centre ▼ ] [ Date ▼ ]
```

Filters should be:

* Easy to remove
* Clearly labelled
* Consistent across pages

---

# 23. Modal / Dialog Design

Dialogs should be used for:

* Confirmation
* Important decisions
* Short forms
* Destructive actions

Example:

```text
┌────────────────────────────────────┐
│ Cancel Order?                      │
│                                    │
│ Are you sure you want to cancel    │
│ order #1024?                       │
│                                    │
│ Reason                             │
│ [...............................]  │
│                                    │
│ [ Keep Order ]   [ Cancel Order ]  │
└────────────────────────────────────┘
```

Dialogs must not contain unnecessary information.

---

# 24. Notifications

Use a consistent notification/toast system.

Success:

```text
✓ Order created successfully.
```

Error:

```text
⚠ Unable to place order. Please try again.
```

Warning:

```text
⚠ This order has an expired reservation.
```

Information:

```text
ℹ A schedule proposal is waiting for your review.
```

Notifications should disappear automatically where appropriate, but important errors should remain visible.

---

# 25. Loading States

Never leave a blank screen while data is loading.

Use:

* Skeleton loaders
* Spinners for small actions
* Loading indicators inside buttons

Example:

```text
[ ⟳ Placing Order... ]
```

instead of:

```text
[ Place Order ]
```

while the request is processing.

---

# 26. Empty States

Every list page must have an intentional empty state.

Bad:

```text
Nothing
```

Good:

```text
No Orders Found

There are no orders matching your current filters.

[ Clear Filters ]
```

For a first-time user:

```text
No Orders Yet

Orders placed by buyers will appear here.
```

---

# 27. Error States

Errors should be understandable.

Example:

```text
Unable to load orders

We couldn't retrieve the order information.

[ Try Again ]
```

Avoid exposing:

* Stack traces
* Database errors
* Internal exception messages
* API keys
* Technical implementation details

---

# 28. Confirmation States

After important successful actions, show a clear result.

Example:

```text
✓ Order Placed Successfully

Order #1024 has been created.

Quantity: 50 kg
Status: Pending
Reservation: Active

[ View Order ]
```

---

# 29. Component B — Order UI

Component B should use the global design system.

Main screens:

```text
Orders
│
├── Order List
├── Order Details
├── Place Order
├── Order Tracking
├── Schedule
├── Schedule Proposal Review
└── Nearest Collection Centre
```

---

# 30. Order List Design

Desktop:

```text
Orders
Manage and track orders

[ Search... ] [ Status ▼ ] [ Centre ▼ ]

┌─────────────────────────────────────────────────────────┐
│ Order     Listing       Buyer       Qty     Status       │
├─────────────────────────────────────────────────────────┤
│ #1024     Tomatoes      Kamal       50kg    Pending      │
│ #1025     Carrots       Nimal       20kg    Approved     │
│ #1026     Beans         Amal        30kg    Scheduled    │
└─────────────────────────────────────────────────────────┘
```

Mobile:

Use order cards instead of forcing a wide table.

```text
┌────────────────────────────┐
│ Order #1024                │
│ Tomatoes                   │
│                            │
│ Quantity      50 kg        │
│ Centre        Colombo      │
│                            │
│ Status        [Pending]    │
│                            │
│ [ View Details ]           │
└────────────────────────────┘
```

---

# 31. Order Details

The order detail screen should clearly separate information.

```text
Order #1024
[ Scheduled ]

────────────────────────────

Order Information

Listing
Tomatoes — Grade A

Quantity
50 kg

Delivery Preference
Pickup

────────────────────────────

Buyer

Name
Contact information

────────────────────────────

Collection

Collection Centre
Colombo Collection Centre

Schedule
25 Sep 2026
10:00 AM – 11:00 AM

────────────────────────────

Order Timeline

● Order placed
│
● Approved
│
● Schedule confirmed
│
○ Completed
```

---

# 32. Order Tracking

The tracking UI should use a timeline.

```text
Order Tracking

● Pending
│
● Approved
│
● Scheduled
│
○ Completed
```

Completed stages should be visually distinguishable from upcoming stages.

The timeline should work consistently on mobile and web.

---

# 33. Schedule Calendar

The Officer scheduling interface should use a calendar-oriented layout.

```text
Schedule

< September 2026 >

Mon   Tue   Wed   Thu   Fri
─────────────────────────────
21    22    23    24    25
                 ┌─────────┐
                 │10:00 AM │
                 │Order1024│
                 └─────────┘
```

Bookings should show:

* Order
* Time
* Status
* Collection centre
* Capacity information

Conflicts should be visually obvious.

---

# 34. Schedule Proposal Review

Officer review screen:

```text
Schedule Proposal

Order #1024

Recommended Centre
Colombo Collection Centre

Proposed Time
25 Sep 2026
10:00 AM – 11:00 AM

AI Match Confidence
87%

Reason
Suitable centre based on location and availability.

────────────────────────────

[ Reject ] [ Request Revision ] [ Approve ]
```

The UI must make it clear that this is an **AI-generated recommendation awaiting human approval**.

---

# 35. Nearest Collection Centre

Mobile design:

```text
Find Collection Centre

Use your current location

        [ Use My Location ]

────────────────────────

Nearby Centres

┌──────────────────────────┐
│ Colombo Collection      │
│ Centre                   │
│                          │
│ 2.4 km                   │
│ ~8 min                   │
│                          │
│ [ View Details ]         │
└──────────────────────────┘

┌──────────────────────────┐
│ Centre B                 │
│                          │
│ 4.1 km                   │
│ ~14 min                  │
└──────────────────────────┘
```

If the Maps API is unavailable:

```text
Approximate distance

Distance is estimated using the available location data.
```

The degraded state should be visible but should not make the interface look broken.

---

# 36. Mobile Design

The Flutter application should prioritize:

* Large touch targets
* Simple navigation
* Short forms
* Clear status information
* Bottom navigation where appropriate
* Cards instead of complex tables
* Easy-to-read typography

Recommended structure:

```text
┌────────────────────────────┐
│ AgriConnect                │
│                            │
│ Page Content               │
│                            │
│                            │
│                            │
├────────────────────────────┤
│ Home │ Orders │ Centres │ Me │
└────────────────────────────┘
```

---

# 37. Responsive Design

Desktop:

```text
Sidebar + Main Content
```

Tablet:

```text
Collapsed Sidebar + Main Content
```

Mobile:

```text
Top Bar
+
Main Content
+
Bottom Navigation
```

Do not simply shrink desktop UI to fit mobile.

Mobile layouts should be intentionally designed.

---

# 38. Icons

Use one consistent icon library throughout the application.

Do not mix multiple unrelated icon styles.

Icons should support text rather than replace important labels.

Example:

```text
✓ Approved
⚠ Pending
✕ Cancelled
```

Do not rely on color alone to communicate meaning.

---

# 39. Accessibility

The UI should support:

* Sufficient color contrast
* Keyboard navigation on web
* Visible focus states
* Descriptive labels
* Accessible buttons
* Error messages associated with fields
* Icons accompanied by text when necessary

Color should never be the only indication of status.

For example:

```text
[✓ Completed]
```

is better than using only green.

---

# 40. Responsive Breakpoints

Use the project's existing responsive framework if one exists.

Otherwise use a simple structure:

```text
Mobile:  < 768px
Tablet:  768px – 1023px
Desktop: ≥ 1024px
```

Avoid creating many unnecessary breakpoints.

---

# 41. Design Consistency Rules for the Team

Every developer must reuse existing UI components before creating new ones.

Before creating a new component, check:

```text
Does an existing component already do this?
        │
       YES
        │
        ▼
Reuse it
```

Only create a new component when the existing component cannot reasonably support the requirement.

---

# 42. Component Naming

Use descriptive names.

Examples:

```text
OrderCard
OrderStatusBadge
OrderTable
ScheduleCalendar
ScheduleProposalReview
CollectionCentreCard
EmptyState
LoadingState
ErrorState
ConfirmDialog
```

Avoid:

```text
Box1
Card2
NewThing
TestComponent
```

---

# 43. Shared Design Components

The team should maintain reusable components for:

```text
Button
Input
Select
Modal
Card
Badge
Table
Pagination
Toast
Loading
EmptyState
ErrorState
PageHeader
SearchBar
FilterBar
```

These should be reused across all project components.

---

# 44. Do Not Create Component-Specific Visual Languages

Component B must not create:

```text
different green
different button style
different font
different card radius
different sidebar
different status badges
```

from the rest of AgriConnect.

All components should visually belong to the same product.

---

# 45. UI Implementation Rule for Claude Code

Before implementing any UI, Claude Code must:

1. Inspect the existing frontend.
2. Inspect UI code created by other team members.
3. Identify existing shared components.
4. Reuse existing components where possible.
5. Follow the established colors, typography, spacing and layout.
6. Avoid replacing the existing design system.
7. Only create new shared components when necessary.
8. Keep Component B-specific UI isolated where appropriate.
9. Test desktop and mobile layouts.
10. Update `PROGRESS.md` after completing each UI task.

If another team member has already established a visual design that differs from this document, **the actual existing project design should be treated as the current implementation reference**, and the difference should be recorded in `PROGRESS.md` rather than silently replacing existing UI.

---

# 46. Final UI Quality Checklist

Before considering a UI task complete:

* [ ] Follows AgriConnect visual style
* [ ] Uses existing shared components
* [ ] Uses consistent typography
* [ ] Uses consistent spacing
* [ ] Uses consistent colors
* [ ] Uses consistent status badges
* [ ] Has loading state
* [ ] Has empty state
* [ ] Has error state
* [ ] Has success feedback where necessary
* [ ] Has responsive layout
* [ ] Has accessible labels
* [ ] Does not expose technical errors
* [ ] Does not introduce a separate visual language
* [ ] Works with real API response structures
* [ ] No unnecessary UI elements
* [ ] Tested at mobile and desktop widths

---

# 47. Design Goal

The final AgriConnect system should feel like one product:

```text
                 AGRICONNECT
                      │
        ┌─────────────┼─────────────┐
        │             │             │
     Listings      Orders       Analytics
        │             │             │
        └─────────────┼─────────────┘
                      │
              Same Design System
                      │
        ┌─────────────┼─────────────┐
        │             │             │
      React        Flutter       Shared UI
```

A user should not be able to tell which student developed a particular screen simply by looking at its UI.

**Consistency across the complete AgriConnect application is more important than individual component styling.**
