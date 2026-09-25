import 'package:flutter/foundation.dart';

/// Base URL of the ASP.NET Core backend API — the Flutter counterpart to
/// `web/.env.example`'s `VITE_API_BASE_URL`. Flutter has no built-in .env
/// loading, so this is a compile-time default overridable via
/// `--dart-define=API_BASE_URL=...` (documented in Decisions, PROGRESS.md).
///
/// The default differs by platform because "localhost" means different
/// things to an Android emulator (which needs the host-loopback alias
/// 10.0.2.2) versus iOS simulator/desktop/web (where localhost is correct).
/// Uses `defaultTargetPlatform` (not `dart:io`'s `Platform`) so this compiles
/// on Flutter web too.
String get apiBaseUrl {
  const override = String.fromEnvironment('API_BASE_URL');
  if (override.isNotEmpty) return override;

  if (!kIsWeb && defaultTargetPlatform == TargetPlatform.android) {
    return 'http://10.0.2.2:5000';
  }
  return 'http://localhost:5000';
}
