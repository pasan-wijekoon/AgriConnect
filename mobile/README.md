# AgriConnect — Mobile App

Flutter app for Farmers and Buyers. It talks only to the ASP.NET Core API.

## Setup

```bash
flutter pub get
```

## Run

Start the API first (`backend/`, `dotnet run`), then:

```bash
flutter run
```

The app calls `http://localhost:5000`, or `http://10.0.2.2:5000` on the Android emulator (the
emulator's address for the host machine). Point it elsewhere with:

```bash
flutter run --dart-define=API_BASE_URL=https://api.example.com
```

In development there is no login yet: requests send `X-Dev-Role: Farmer`, which the API's
development-only fake sign-in accepts.

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
