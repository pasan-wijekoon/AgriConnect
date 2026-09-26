import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

// ─────────────────────────────────────────────────────────────────────────────
// AgriConnect Design System — Strictly implementing Design.md
//
// Colors: Primary Green (#2E7D32), Secondary Green (#66BB6A), Background (#F7F9F7), Cards (#FFFFFF)
// Typography: Inter hierarchy (32/28/24/20/16/14/12px)
// Border Radius: Controls (8px), Cards (12px), Containers (16px), Pills (999px)
// Spacing: 4px base scale (4, 8, 12, 16, 20, 24, 32, 40, 48px)
// Shadows: Subtle 0 1px 3px rgba(0,0,0,0.05) with 1px border
// ─────────────────────────────────────────────────────────────────────────────

class AppColors {
  AppColors._();

  // Primary brand colors (Design.md §5)
  static const Color primaryGreen = Color(0xFF2E7D32);
  static const Color primaryGreenHover = Color(0xFF1B5E20);
  static const Color primaryGreenLight = Color(0xFFE8F5E9);

  // Secondary highlights (Design.md §5)
  static const Color secondaryGreen = Color(0xFF66BB6A);

  // Backgrounds (Design.md §5)
  static const Color background = Color(0xFFF7F9F7);
  static const Color cardBackground = Color(0xFFFFFFFF);
  static const Color surfaceVariant = Color(0xFFF1F8F1);

  // Text hierarchy (Design.md §5)
  static const Color textPrimary = Color(0xFF1F2937);
  static const Color textSecondary = Color(0xFF6B7280);
  static const Color textMuted = Color(0xFF9CA3AF);
  static const Color textInverse = Color(0xFFFFFFFF);

  // Borders & Dividers (Design.md §10)
  static const Color borderLight = Color(0xFFE5E7EB);
  static const Color borderSubtle = Color(0xFFF3F4F6);
  static const Color borderMedium = Color(0xFFD1D5DB);

  // Semantic Status Colors (Design.md §6 & §20)
  // Success (Approved, Completed, Published, Grade A)
  static const Color statusSuccess = Color(0xFF15803D);
  static const Color statusSuccessBg = Color(0xFFDCFCE7);
  static const Color statusSuccessBorder = Color(0xFF86EFAC);

  // Pending (PendingApproval, Waiting)
  static const Color statusPending = Color(0xFFB45309);
  static const Color statusPendingBg = Color(0xFFFEF3C7);
  static const Color statusPendingBorder = Color(0xFFFDE68A);

  // Warning (Grade B, Discrepancies)
  static const Color statusWarning = Color(0xFFC2410C);
  static const Color statusWarningBg = Color(0xFFFFEDD5);
  static const Color statusWarningBorder = Color(0xFFFED7AA);

  // Error (Rejected, Cancelled, Grade C)
  static const Color statusError = Color(0xFFB91C1C);
  static const Color statusErrorBg = Color(0xFFFEE2E2);
  static const Color statusErrorBorder = Color(0xFFFCA5A5);

  // Info (Scheduled, Proposed)
  static const Color statusInfo = Color(0xFF0369A1);
  static const Color statusInfoBg = Color(0xFFE0F2FE);
  static const Color statusInfoBorder = Color(0xFFBAE6FD);

  // Neutral (Draft, Withdrawn, Not Inspected)
  static const Color statusNeutral = Color(0xFF4B5563);
  static const Color statusNeutralBg = Color(0xFFF3F4F6);
  static const Color statusNeutralBorder = Color(0xFFE5E7EB);
}

class AppSpacing {
  AppSpacing._();

  // Design.md §8 — Spacing System (4px base scale)
  static const double xs = 4.0;
  static const double sm = 8.0;
  static const double md = 12.0;
  static const double base = 16.0;
  static const double lg = 20.0;
  static const double xl = 24.0;
  static const double xxl = 32.0;
  static const double xxxl = 40.0;
  static const double huge = 48.0;
}

class AppRadius {
  AppRadius._();

  // Design.md §9 — Border Radius
  static const double control = 8.0;    // Small controls, buttons, inputs
  static const double card = 12.0;      // Cards
  static const double container = 16.0; // Large containers, bottom sheets
  static const double pill = 999.0;     // Pills / status badges
}

class AppShadows {
  AppShadows._();

  // Design.md §10 — Subtle light shadows paired with subtle borders
  static const List<BoxShadow> subtle = [
    BoxShadow(
      color: Color.fromRGBO(0, 0, 0, 0.04),
      blurRadius: 3,
      offset: Offset(0, 1),
    ),
    BoxShadow(
      color: Color.fromRGBO(0, 0, 0, 0.02),
      blurRadius: 2,
      offset: Offset(0, 1),
    ),
  ];

  static const List<BoxShadow> dropdown = [
    BoxShadow(
      color: Color.fromRGBO(0, 0, 0, 0.08),
      blurRadius: 15,
      offset: Offset(0, 10),
    ),
    BoxShadow(
      color: Color.fromRGBO(0, 0, 0, 0.04),
      blurRadius: 6,
      offset: Offset(0, 4),
    ),
  ];
}

class AppTypography {
  AppTypography._();

  // Design.md §7 — Typography hierarchy with Inter font
  static TextStyle pageTitle = GoogleFonts.inter(
    fontSize: 28,
    fontWeight: FontWeight.w700,
    color: AppColors.textPrimary,
    letterSpacing: -0.5,
  );

  static TextStyle sectionHeading = GoogleFonts.inter(
    fontSize: 20,
    fontWeight: FontWeight.w600,
    color: AppColors.textPrimary,
    letterSpacing: -0.3,
  );

  static TextStyle cardHeading = GoogleFonts.inter(
    fontSize: 16,
    fontWeight: FontWeight.w600,
    color: AppColors.textPrimary,
  );

  static TextStyle body = GoogleFonts.inter(
    fontSize: 14,
    fontWeight: FontWeight.w400,
    color: AppColors.textPrimary,
    height: 1.5,
  );

  static TextStyle bodyMedium = GoogleFonts.inter(
    fontSize: 14,
    fontWeight: FontWeight.w500,
    color: AppColors.textPrimary,
  );

  static TextStyle secondary = GoogleFonts.inter(
    fontSize: 13,
    fontWeight: FontWeight.w400,
    color: AppColors.textSecondary,
    height: 1.4,
  );

  static TextStyle caption = GoogleFonts.inter(
    fontSize: 12,
    fontWeight: FontWeight.w500,
    color: AppColors.textSecondary,
  );

  static TextStyle statNumber = GoogleFonts.inter(
    fontSize: 26,
    fontWeight: FontWeight.w700,
    color: AppColors.textPrimary,
    letterSpacing: -0.5,
  );

  static TextStyle button = GoogleFonts.inter(
    fontSize: 14,
    fontWeight: FontWeight.w600,
    color: Colors.white,
    letterSpacing: 0.1,
  );
}

ThemeData buildAppTheme() {
  final textTheme = GoogleFonts.interTextTheme(
    TextTheme(
      displayLarge: AppTypography.pageTitle,
      headlineMedium: AppTypography.sectionHeading,
      titleLarge: AppTypography.cardHeading,
      bodyLarge: AppTypography.body,
      bodyMedium: AppTypography.bodyMedium,
      bodySmall: AppTypography.secondary,
      labelSmall: AppTypography.caption,
    ),
  );

  return ThemeData(
    useMaterial3: false, // Bypass Material 3 default elevation and tints
    primaryColor: AppColors.primaryGreen,
    scaffoldBackgroundColor: AppColors.background,
    textTheme: textTheme,
    fontFamily: GoogleFonts.inter().fontFamily,

    // Clean top bar (Design.md §36 & §11)
    appBarTheme: AppBarTheme(
      backgroundColor: AppColors.primaryGreen,
      foregroundColor: Colors.white,
      elevation: 0,
      centerTitle: false,
      titleTextStyle: GoogleFonts.inter(
        fontSize: 18,
        fontWeight: FontWeight.w600,
        color: Colors.white,
      ),
    ),

    // Cards (Design.md §14)
    cardTheme: CardThemeData(
      color: AppColors.cardBackground,
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(AppRadius.card),
        side: const BorderSide(color: AppColors.borderLight, width: 1),
      ),
      margin: EdgeInsets.zero,
    ),

    // Input fields (Design.md §17)
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: Colors.white,
      contentPadding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.base,
        vertical: AppSpacing.md,
      ),
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(AppRadius.control),
        borderSide: const BorderSide(color: AppColors.borderLight, width: 1),
      ),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(AppRadius.control),
        borderSide: const BorderSide(color: AppColors.borderLight, width: 1),
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(AppRadius.control),
        borderSide: const BorderSide(color: AppColors.primaryGreen, width: 1.5),
      ),
      errorBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(AppRadius.control),
        borderSide: const BorderSide(color: AppColors.statusError, width: 1),
      ),
      hintStyle: GoogleFonts.inter(color: AppColors.textMuted, fontSize: 14),
      labelStyle: GoogleFonts.inter(color: AppColors.textSecondary, fontSize: 14),
    ),

    // Dividers
    dividerTheme: const DividerThemeData(
      color: AppColors.borderLight,
      thickness: 1,
      space: 1,
    ),
  );
}
