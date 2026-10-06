# Manual Test Flow — Orders and Pickup Schedule (Buyer → Officer → Buyer)

Fill **Actual result**, **Status** (exactly **Passed** or **Failed**) and **Evidence** (a screenshot name, saved in `testing-evidence/manual/`).
A Failed row needs a defect ID.

| | |
|---|---|
| Tester | |
| Date | |
| Branch / commit | `yasDev2` @ |
| Environment | Web http://localhost:3000 · API http://localhost:5000 · AI agent http://localhost:8000 · DB `agriconnect_dev` |

## 1. Before you start

- [ ] Web, API and AI agent are running (the three links above open).
- [ ] Accounts: `buyer@agriconnect.lk`, `officer@agriconnect.lk`, `farmer@agriconnect.lk`, `admin@agriconnect.lk`. Passwords: see the comment on line 648 of `backend/src/config/AgriConnectDbContext.cs`, or `web/.env.test-account` for the test accounts. Do not write passwords in this file.
- [ ] Data: there are **2 published listings**: Carrots (Kandy, 500 kg) and Tomatoes (Galle, 300 kg). Some of the Carrots stock is already reserved by demo orders, so the free stock is less than 500 kg.
- [ ] Use two browser windows: a normal one for the **Buyer** and a private/incognito one for the **Officer**, so you can switch without logging out.

## 2. The working flow

```
Buyer places order  ->  Officer approves order  ->  AI proposes a pickup slot
        ->  Officer approves the slot  ->  Order is Scheduled  ->  Officer marks Completed
```

Order statuses: **Pending → Approved → Scheduled → Completed** (or **Cancelled**).

## 3. Buyer: place and track an order

| ID | Role | Steps | Expected result | Actual result | Status | Evidence |
|---|---|---|---|---|---|---|
| O-M-01 | Buyer | Log in as the buyer. | The marketplace opens and lists the 2 published batches (Carrots · Kandy, Tomatoes · Galle). | | | |
| O-M-02 | Buyer | Click the **Carrots** card. | The detail window opens: quantity, grade, pickup window, farmer, price. | | | |
| O-M-03 | Buyer | Click **Request Wholesale Order**. | The "Place Wholesale Purchase Order" form opens, with **Order Quantity (kg)** and **Estimated Wholesale Total**. | | | |
| O-M-04 | Buyer | Enter quantity **50** and click **Confirm & Submit Order**. | A success message. The order is **really created** (check in O-M-06), not just a banner. | | | |
| O-M-05 | Buyer | Try quantity **0**, then a quantity above the listing quantity (e.g. 501). | The form refuses both (minimum 1, maximum the listing quantity). No order is created. | | | |
| O-M-06 | Buyer | Open **My Orders** (menu, or http://localhost:3000/my-orders). | The new order is listed: Carrots · Kandy · 50 kg, status shows it is **waiting for an officer to review**. | | | |
| O-M-07 | Buyer | Click the order. | Details show: Centre, Farmer, Handling (Pickup), Placed (date), Quantity, Region, Status, and the pickup schedule section. | | | |
| O-M-08 | Buyer | Use the order filter at the top of My Orders. | The list filters by status. A filter with no match shows "Nothing in this view…". | | | |

## 4. Officer: review the order and approve the pickup slot

Use the private window. Log in as the officer, then open the Back office.

| ID | Role | Steps | Expected result | Actual result | Status | Evidence |
|---|---|---|---|---|---|---|
| O-M-10 | Officer | Open **Orders** (left menu, ORDERS & LOGISTICS). | The Orders list shows the buyer's 50 kg order as **Pending**. The search box ("Crop, buyer, farmer, centre or order id") finds it. | | | |
| O-M-11 | Officer | Click the order. | The order detail page shows the produce, buyer, farmer, collection centre, **Reservation Expires**, and the **Order Timeline** (Pending → Approved → Scheduled → Completed). | | | |
| O-M-12 | Officer | Click **Approve Order**. | A message: "Order approved — a pickup slot was proposed automatically. Review it below." Status is **Approved**. | | | |
| O-M-13 | Officer | Look at the **Schedule Proposal** card. | It shows a **Proposed Time** (one hour), **Conflict Check** passed, and the buttons Approve / Reject / Request Revision. The slot is inside the pickup window. | | | |
| O-M-14 | Officer | Click **Request Revision**, enter a reason, send. | The proposal is replaced by a new one (a different window at the same centre). | | | |
| O-M-15 | Officer | Click **Approve** on the proposal. | "Schedule confirmed." The slot becomes **Confirmed** and the order status becomes **Scheduled**. | | | |
| O-M-16 | Officer | Open **Schedule** (left menu). | The booking appears on the calendar for that centre and day. | | | |
| O-M-17 | Officer | On the order, click **Mark Completed**. | The order status is **Completed** and the timeline is complete. | | | |

## 5. Buyer: see the result

| ID | Role | Steps | Expected result | Actual result | Status | Evidence |
|---|---|---|---|---|---|---|
| O-M-20 | Buyer | Go back to My Orders (reload). | The order shows **Scheduled** with the confirmed pickup date and time (and **Completed** after O-M-17). | | | |
| O-M-21 | Buyer | Check the notifications bell. | There are notifications for the order being approved and the pickup slot. | | | |

## 6. Cancel and stock checks

| ID | Role | Steps | Expected result | Actual result | Status | Evidence |
|---|---|---|---|---|---|---|
| O-M-30 | Buyer | Place a new order (e.g. 20 kg). Open it in My Orders and cancel it. | The status becomes **Cancelled**: "This order was cancelled and its reserved stock was released." | | | |
| O-M-31 | Buyer | Place an order for **all** the free stock (e.g. the highest quantity the form accepts), then try to order 1 kg more. | The second order is **refused** (not enough stock). | | | |
| O-M-32 | Buyer | Cancel the big order, then order 1 kg again. | The 1 kg order is **accepted**: the stock was released. | | | |
| O-M-33 | Officer | Open a **Completed** order and try to cancel it. | Cancelling is not possible (no cancel button, or a clear refusal). | | | |
| O-M-34 | Officer | Cancel a **Pending** order from the officer view. | The order is **Cancelled** and its stock is released. | | | |

## 7. Access and safety

| ID | Role | Steps | Expected result | Actual result | Status | Evidence |
|---|---|---|---|---|---|---|
| O-M-40 | Buyer | As the buyer, open http://localhost:3000/orders (the officer queue). | Access is refused or you are redirected. No officer data is shown. | | | |
| O-M-41 | Farmer | As the farmer, open My Orders. | Only orders for the farmer's own produce are shown. | | | |
| O-M-42 | Buyer | Open an order that belongs to another buyer (use another order's link). | "Not found" or refused. No details are shown. | | | |
| O-M-43 | Any | Stop the API (Ctrl-C in its Terminal tab), then reload My Orders. Start it again after. | A clear error message with "Try again". No blank page. | | | |

## 8. Summary

| Section | Planned | Passed | Failed | Not run |
|---|---|---|---|---|
| 3 Buyer places and tracks | 8 | | | |
| 4 Officer reviews and schedules | 8 | | | |
| 5 Buyer sees the result | 2 | | | |
| 6 Cancel and stock | 5 | | | |
| 7 Access and safety | 4 | | | |
| **Total** | **27** | | | |

**Already checked through the API (18 of 18 passed, 2026-10-04):** place order, over-stock refused, zero quantity refused, only the officer can approve, auto-proposed slot with conflict check, approve slot, Scheduled, Completed, cancel, and stock reserved then released. The evidence is in `testing-evidence/flow/order-flow-output.txt`.

Observation: the first auto-proposed slot started at an odd time (07:48:57 UTC, not on the hour). Check in O-M-13 whether the time shown looks sensible to an officer.
