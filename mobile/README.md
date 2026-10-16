# AgriConnect — Mobile App

Flutter app for Farmers and Buyers. It talks only to the ASP.NET Core API.

## Setup

```bash
flutter pub get
```

## Run

Run the app; it uses the deployed backend at `https://16-178-77-2.sslip.io` by default.
To use a local API during development, override the base URL for the current run:

```bash
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5000
```

For an Android emulator, `10.0.2.2` reaches the development computer. On other platforms,
use the appropriate local host address. You can point the app at any backend origin with:

```bash
flutter run --dart-define=API_BASE_URL=https://your-api-domain.example
```

The app uses the real login (JWT). Sign in with a demo account (password `password`) —
`buyer@agriconnect.lk` or `farmer@agriconnect.lk` — or create an account from the landing screen.
Officers and Administrators use the web console. Navigation is role-based: Buyers get
Home / Market / Orders / Centres / Account, Farmers get Home / Listings / Orders / Prices / Account.

## Check

```bash
flutter analyze
```

```bash
flutter test
```

## Structure

```
lib/
├── main.dart            # App shell and routes
├── models/              # JSON models
├── providers/           # Screen state (ChangeNotifier)
├── screens/             # One folder per screen; home/ lists the entry points
└── services/            # API clients
```

Each component adds its screen under `screens/` and a tile on `screens/home/home_screen.dart`.

## Component D — Market prices (Student 4)

`screens/price_trends/` — a read-only weekly price trend so farmers can judge whether to list
now: current average, change against 4 weeks ago, a chart with the lowest–highest range, and a
weekly table. Data: `GET /api/analytics/price-trends` and `GET /api/analytics/filters`.
