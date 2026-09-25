import 'package:flutter/material.dart';

/// Design.md §5/§6's exact color tokens — kept in lockstep with
/// `web/src/index.css`'s CSS custom properties so mobile and web render the
/// same semantic colors (Design.md §1: "look like one professional system").
class AppColors {
  AppColors._();

  static const primary = Color(0xFF2E7D32);
  static const primaryHover = Color(0xFF256628);
  static const secondary = Color(0xFF66BB6A);
  static const background = Color(0xFFF7F9F7);
  static const card = Color(0xFFFFFFFF);
  static const text = Color(0xFF1F2937);
  static const textSecondary = Color(0xFF6B7280);
  static const textMuted = Color(0xFF9CA3AF);
  static const border = Color(0xFFE5E7EB);

  static const success = Color(0xFF2E7D32);
  static const successBg = Color(0xFFE8F5E9);
  static const pending = Color(0xFFB45309);
  static const pendingBg = Color(0xFFFEF3C7);
  static const warning = Color(0xFFC2410C);
  static const warningBg = Color(0xFFFFEDD5);
  static const error = Color(0xFFB91C1C);
  static const errorBg = Color(0xFFFEE2E2);
  static const info = Color(0xFF1D4ED8);
  static const infoBg = Color(0xFFDBEAFE);
  static const proposed = Color(0xFF6D28D9);
  static const proposedBg = Color(0xFFEDE9FE);
  static const neutral = Color(0xFF4B5563);
  static const neutralBg = Color(0xFFF3F4F6);
}

ThemeData buildAppTheme() {
  final colorScheme = ColorScheme.fromSeed(
    seedColor: AppColors.primary,
    primary: AppColors.primary,
    secondary: AppColors.secondary,
    surface: AppColors.card,
    error: AppColors.error,
  );

  return ThemeData(
    useMaterial3: true,
    colorScheme: colorScheme,
    scaffoldBackgroundColor: AppColors.background,
    cardTheme: CardThemeData(
      color: AppColors.card,
      elevation: 1,
      margin: EdgeInsets.zero,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: const BorderSide(color: AppColors.border),
      ),
    ),
    appBarTheme: const AppBarTheme(
      backgroundColor: AppColors.card,
      foregroundColor: AppColors.text,
      elevation: 0,
      centerTitle: false,
    ),
    elevatedButtonTheme: ElevatedButtonThemeData(
      style: ElevatedButton.styleFrom(
        backgroundColor: AppColors.primary,
        foregroundColor: Colors.white,
        minimumSize: const Size.fromHeight(48),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
      ),
    ),
    textTheme: const TextTheme(
      bodyMedium: TextStyle(color: AppColors.text),
      bodySmall: TextStyle(color: AppColors.textSecondary),
    ),
  );
}
