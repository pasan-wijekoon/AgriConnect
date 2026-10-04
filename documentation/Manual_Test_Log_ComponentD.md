# Manual Test Log — Component D: Market Price Analytics & Reporting (FR15–FR18)

Fill the **Actual result**, **Status** and **Evidence** columns while you test.
Status must be exactly **Passed** or **Failed**. Every **Failed** row needs a Defect ID in section 7.

| | |
|---|---|
| Tester | |
| Date | |
| Branch / commit | `yasDev2` @ |
| Browser + version | |
| Test environment | Web http://localhost:3000 · API http://localhost:5000 · DB `agriconnect_dev` |

## 1. Before you start

- [ ] Docker Desktop is running, and the web container is up (http://localhost:3000 opens).
- [ ] The API is running (Terminal tab "AgriConnect API"; http://localhost:5000/swagger opens).
- [ ] The AI agent is running (http://localhost:8000/docs opens). Only needed for the AI Scheduling and listing tests.
- [ ] Sign-in details: the seeded demo accounts and passwords are in the comment on line 648 of `backend/src/config/AgriConnectDbContext.cs`. Emails are `farmer@`, `buyer@`, `officer@` and `admin@agriconnect.lk`. Do not write passwords in this log.
- [ ] Take screenshots into `testing-evidence/manual/` and name them with the test ID, e.g. `D-M-07.png`.

**Known test data** (seeded): 10 crops, of which only **Carrots, Onions and Tomatoes** have price history. The other 7 show "(no data yet)". There are about 10 anomaly flags and 4 shortage/oversupply events.

**Back-office role picker:** the Back office (Orders, Analytics, Quality) uses the role selector at the top right of the page, not the marketplace login. Set it to the role named in each test.

## 2. Access and navigation (Back office)

| ID | Role | Steps | Expected result | Actual result | Status | Evidence |
|---|---|---|---|---|---|---|
| D-M-01 | Officer | Log in on http://localhost:3000 as the officer. | The "Officer tools" page opens, with an "Open the back office" button. | | | |
| D-M-02 | Officer | Click "Open the back office". | The back office opens at `/orders`. The Analytics menu lists Price Trends, Shortages, Anomaly Queue, Reports and AI Scheduling. | | | |
| D-M-03 | Administrator | Log in as admin. Open the back office. Set the role picker to Administrator. | All Analytics items are shown, including Reports. | | | |
| D-M-04 | Buyer | Log in as the buyer. | No "Back office" link and no "Officer tools" page. | | | |
| D-M-05 | Any | In the back office, click "← Marketplace" at the top left. | You return to the marketplace home page. | | | |

## 3. Price Trends (FR15) — web

Open Back office → Analytics → **Price Trends** (`/analytics/price-trends`). Role picker: Officer.

| ID | Role | Steps | Expected result | Actual result | Status | Evidence |
|---|---|---|---|---|---|---|
| D-M-10 | Officer | Open Price Trends. | It opens on **Carrots** (a crop with data), not an empty page. The stat tiles (Current average, Change vs 4 weeks ago, Latest price range, Listings) and a chart are shown. | | | |
| D-M-11 | Officer | Open the Crop list. | Crops without data show "(no data yet)". Carrots, Onions and Tomatoes do not. | | | |
| D-M-12 | Officer | Pick **Onions**, then **Tomatoes**. | The tiles and chart change each time. The title matches the crop. | | | |
| D-M-13 | Officer | Pick a crop marked "(no data yet)", e.g. Banana. | A friendly "no data" message appears. There is no broken chart and no error. | | | |
| D-M-14 | Officer | On Carrots, change Region to a single region, then back to "All regions combined". | The values change per region and return to the combined values. | | | |
| D-M-15 | Officer | Change Period (e.g. Last 16 weeks to a shorter period). | The chart range changes. The latest week stays the same. | | | |
| D-M-16 | Officer | Hover over the chart line. | A tooltip shows the week, the average, and the lowest to highest price. | | | |
| D-M-17 | Officer | Check the numbers: the average is shown in LKR per kg, and "Change vs 4 weeks ago" has an arrow with a percentage. | The values are plausible and have no `NaN`, `undefined` or `0` for a missing week. | | | |
| D-M-18 | Farmer | Set the role picker to Farmer and open Price Trends. | The page works (a farmer may read trends). | | | |
| D-M-19 | Buyer | Set the role picker to Buyer and open Price Trends. | The page works (buyers may read trends on this branch). | | | |
| D-M-20 | Officer | Stop the API (Ctrl-C in its Terminal tab), then reload Price Trends. Start the API again afterwards. | A clear error message is shown, with no blank page and no crash. | | | |

## 4. Shortages and oversupply (FR17) — web

Open **Shortages** (`/analytics/shortages`). Role picker: Officer.

| ID | Role | Steps | Expected result | Actual result | Status | Evidence |
|---|---|---|---|---|---|---|
| D-M-30 | Officer | Open Shortages. | The "Shortages & oversupply" page shows the "Supply heatmap" and the event list. | | | |
| D-M-31 | Officer | Look at the heatmap colour scale legend. | The colours match the cells. Shortage and oversupply are distinguishable. | | | |
| D-M-32 | Officer | Use the type and severity filters (Shortage / Oversupply, Low / Medium / High). | Only matching events remain. Clearing the filter shows all events again. | | | |
| D-M-33 | Officer | Pick a filter combination with no events. | A "No events" empty state is shown. | | | |
| D-M-34 | Farmer | Set the role picker to Farmer and open Shortages. | Access is refused with a clear message. The shortage data is not shown. | | | |

## 5. Prices to check — the Anomaly Queue (FR16) — web

Open **Anomaly Queue** in the menu (`/analytics/anomalies`). The page title is **Prices to check**. Role: Officer.
Status words: **To check** (Open), **Checked** (Reviewed), **Ignored** (Dismissed).

| ID | Role | Steps | Expected result | Actual result | Status | Evidence |
|---|---|---|---|---|---|---|
| D-M-40 | Officer | Open the Anomaly Queue. | "Prices to check" lists prices, newest first, with Date, Produce, Asking price, "Compared with fair price" (e.g. "52.7% too high") and Status. All text is easy to read. | | | |
| D-M-41 | Officer | Use the tabs (To check / Checked / Ignored / All) and the Crop filter. | The list filters correctly and the tab counts update. | | | |
| D-M-42 | Officer | Click **See details** on one row. | A "Price check" panel opens with: a plain sentence ("The farmer is asking LKR … The AI fair price is about LKR … That is …% too high"), a green **What to do** box, **Compare the prices** bars, **Why this might have happened**, and **Other checks**. | | | |
| D-M-43 | Officer | In the panel, check the numbers: asking price, AI fair price, usual weekly price. | The AI fair price is the asking price divided by (1 + deviation). For LKR 168 and 52.7%, it is about LKR 110.02. The longest bar is the highest price. | | | |
| D-M-44 | Officer | Open a price that is too **high** (e.g. Tomatoes · Matale), then one that is too **low** (Onions · Nuwara Eliya). | High: "Typing mistake" is shown first, with advice to confirm the price with the farmer. Low (30%): "Needs to sell fast". The wording says "too high" or "too low" correctly. | | | |
| D-M-45 | Officer | Look at **Other checks** for a listing that has no inspection and no orders. | "Not inspected yet." and "No orders yet." No developer wording such as "Component". | | | |
| D-M-46 | Officer | Click **Mark as checked** on a "To check" price. | A message "…: marked as checked." The price leaves To check and appears under Checked, and a reload keeps it. | | | |
| D-M-47 | Officer | Click **Ignore** on another price. | "…: marked as ignored." It appears under Ignored, and a reload keeps it. | | | |
| D-M-48 | Officer | Open a Checked or Ignored price. | The panel says "This price was already checked/ignored." and has no action buttons. It cannot go back to To check. | | | |
| D-M-49 | Officer | Use the pager if there is more than one page. | Next and previous pages work. No price is repeated or lost between pages. | | | |
| D-M-4A | Farmer | Set the role to Farmer and open the Anomaly Queue. | Access is refused with a clear message. | | | |
| D-M-4B | Officer | Create an inspection and an order for a flagged listing, then open its details. | "Other checks" shows the grade ("Inspected on …: Grade A confirmed.") and the orders ("2 orders, 60 kg in total. Latest order: …"). | | | |

## 6. Reports (FR18) and AI Scheduling

| ID | Role | Steps | Expected result | Actual result | Status | Evidence |
|---|---|---|---|---|---|---|
| D-M-50 | Officer | Set the role picker to Officer. Open Reports. | A "Reports are for administrators" message. The reports feature is not available. | | | |
| D-M-51 | Administrator | Set the role picker to Administrator. Open Reports. | "Generate a report", "Recent reports" and "Find a report by ID" are shown. | | | |
| D-M-52 | Administrator | Generate a report (choose a type and a date range). | A "Report ready" message appears, and the report appears in Recent reports. | | | |
| D-M-53 | Administrator | Open or download the generated report. | The file opens and its contents match the selected range. | | | |
| D-M-54 | Administrator | Generate a report with an invalid range (end before start). | A clear validation message. No report is created. | | | |
| D-M-55 | Administrator | Paste a random ID into "Find a report by ID". | A friendly "not found" message, with no crash. | | | |
| D-M-56 | Officer | Open **AI Scheduling**. Choose an example scenario and run it. | An "Agent's proposal" with a proposed slot and the agent's reasoning is shown. | | | |
| D-M-57 | Officer | Run a scenario where the centre is fully booked. | A "No free slot" message that explains why. | | | |
| D-M-58 | Officer | Stop the AI agent container (`docker stop agriconnect-agent`), then run a scenario. Start it again afterwards (`docker start agriconnect-agent`). | A clear "agent unavailable" style message, with no crash. | | | |

## 7. Cross-component checks (Component A → D)

| ID | Role | Steps | Expected result | Actual result | Status | Evidence |
|---|---|---|---|---|---|---|
| D-M-60 | Farmer | Log in as the farmer. Create a listing for Carrots, 100 kg, with a very high floor price (e.g. 50000). Submit. | The listing is created and shows "Pending approval". | | | |
| D-M-61 | Officer | Open the Anomaly Queue and filter by Carrots on the To check tab. | A **new** "To check" price for that listing is at the top, with a large positive deviation (DEF-D-01 fix). | | | |
| D-M-62 | Farmer | Create another listing with **no** floor price. | The listing is created. No anomaly flag is created for it. | | | |
| D-M-63 | Farmer | Create a listing at a price close to the AI fair range shown in the form. | The listing is created. No anomaly flag is created. | | | |

## 8. Mobile app (Flutter) — Price Trends

Run `flutter run -d chrome --web-port=5173` in `mobile/`. Log in with the Farmer demo button.

| ID | Role | Steps | Expected result | Actual result | Status | Evidence |
|---|---|---|---|---|---|---|
| D-M-70 | Farmer | Open the Price Trends screen. | It opens on a crop with data (Carrots). The current average (LKR), the change vs 4 weeks ago, and a chart are shown. | | | |
| D-M-71 | Farmer | Pick a crop with no data. | A "No prices recorded for … in the last 16 weeks" message. | | | |
| D-M-72 | Farmer | Open "Weekly prices". | The weeks are listed newest first. A week with no data is missing, not shown as 0. | | | |
| D-M-73 | Farmer | Stop the API, then pull to refresh or reopen the screen. | A clear message and a "Try again" button. Start the API again and tap "Try again". The data returns. | | | |

## 9. Optional: API checks in Swagger (http://localhost:5000/swagger)

Click Authorize and paste a login token for the role, or use the role picker's header where the endpoint allows it.

| ID | Request | Expected result | Actual result | Status |
|---|---|---|---|---|
| D-M-80 | `GET /api/analytics/price-trends` without `cropId` | 400 with a validation message | | |
| D-M-81 | `GET /api/analytics/price-trends` with `from` after `to` | 400 | | |
| D-M-82 | `GET /api/analytics/price-trends` with `bucket=year` | 400, naming `week` and `month` | | |
| D-M-83 | `GET /api/analytics/anomalies?size=101` as officer | 400 | | |
| D-M-84 | `PATCH /api/analytics/anomalies/{id}` with status `Open` | 400 | | |
| D-M-85 | `POST /api/analytics/snapshots/refresh` as officer | 403 | | |
| D-M-86 | Any analytics endpoint with no token | 401 | | |

## 10. Defect log

| Defect ID | Test ID | Description | Severity (Critical / High / Medium / Low) | Steps to reproduce | Evidence | Status | Retest result |
|---|---|---|---|---|---|---|---|
| DEF-D-01 | D-M-61 | Overpriced listing never reached the Anomaly Queue | High | Create a listing with a very high price, then check the Prices to check list | `testing-evidence/backend/defect-D-01-*.trx` | Fixed (commit `079d5f2`) | Passed |
| DEF-D-02 | D-M-30 | Shortages heatmap: district names and "No event" text overlap the crop labels | Medium | Open Shortages as an officer in the dark app shell | `testing-evidence/manual/D-M-30.png` | Fixed (analytics.css) | Retest: pending |
| DEF-D-03 | D-M-30, D-M-40 | Analytics tables, tile numbers and card titles are near-invisible (light text on white cards) | Medium | Open Shortages and the Anomaly Queue | `testing-evidence/manual/D-M-40.png` | Fixed (analytics.css) | Retest: pending |
| | | | | | | | |

## 11. Summary (fill in at the end)

| Section | Planned | Passed | Failed | Not run |
|---|---|---|---|---|
| 2 Access and navigation | 5 | | | |
| 3 Price Trends | 11 | | | |
| 4 Shortages | 5 | | | |
| 5 Prices to check (Anomaly Queue) | 12 | | | |
| 6 Reports and AI Scheduling | 9 | | | |
| 7 Cross-component | 4 | | | |
| 8 Mobile | 4 | | | |
| 9 API (Swagger) | 7 | | | |
| **Total** | **57** | | | |

Conclusion: ______________________________________________

Tester signature / date: ____________________
