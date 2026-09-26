import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../theme/app_theme.dart';

// ─────────────────────────────────────────────────────────────────────────────
// AppButton — Bespoke Button System implementing Design.md §15 & §16
//
// Primary: Deep Agriculture Green (#2E7D32), white text, 8px radius
// Secondary: White bg, subtle border (#E5E7EB), text #1F2937 or green
// Destructive: Semantic error red (#DC2626 / #B91C1C)
// Loading: Displays embedded spinner with action text per Design.md §25
// ─────────────────────────────────────────────────────────────────────────────

enum AppButtonVariant {
  primary,
  secondary,
  destructive,
  ghost,
}

enum AppButtonSize {
  small,
  medium,
  large,
}

class AppButton extends StatelessWidget {
  final String text;
  final VoidCallback? onPressed;
  final AppButtonVariant variant;
  final AppButtonSize size;
  final IconData? icon;
  final bool isLoading;
  final String? loadingText;
  final bool isFullWidth;

  const AppButton({
    super.key,
    required this.text,
    required this.onPressed,
    this.variant = AppButtonVariant.primary,
    this.size = AppButtonSize.medium,
    this.icon,
    this.isLoading = false,
    this.loadingText,
    this.isFullWidth = true,
  });

  @override
  Widget build(BuildContext context) {
    final bool isInteractive = onPressed != null && !isLoading;
    final double height = switch (size) {
      AppButtonSize.small => 36.0,
      AppButtonSize.medium => 44.0,
      AppButtonSize.large => 50.0,
    };

    final double fontSize = switch (size) {
      AppButtonSize.small => 13.0,
      AppButtonSize.medium => 14.0,
      AppButtonSize.large => 15.0,
    };

    final (Color bg, Color fg, Color border) = _resolveColors(isInteractive);

    Widget content;
    if (isLoading) {
      content = Row(
        mainAxisSize: MainAxisSize.min,
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          SizedBox(
            width: 16,
            height: 16,
            child: CircularProgressIndicator(
              strokeWidth: 2,
              valueColor: AlwaysStoppedAnimation<Color>(fg),
            ),
          ),
          const SizedBox(width: AppSpacing.sm),
          Flexible(
            child: Text(
              loadingText ?? 'Processing...',
              overflow: TextOverflow.ellipsis,
              maxLines: 1,
              style: GoogleFonts.inter(
                fontSize: fontSize,
                fontWeight: FontWeight.w600,
                color: fg,
              ),
            ),
          ),
        ],
      );
    } else {
      content = Row(
        mainAxisSize: MainAxisSize.min,
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          if (icon != null) ...[
            Icon(icon, size: fontSize + 4, color: fg),
            const SizedBox(width: AppSpacing.sm),
          ],
          Flexible(
            child: Text(
              text,
              overflow: TextOverflow.ellipsis,
              maxLines: 1,
              style: GoogleFonts.inter(
                fontSize: fontSize,
                fontWeight: FontWeight.w600,
                color: fg,
                letterSpacing: 0.1,
              ),
            ),
          ),
        ],
      );
    }

    final buttonDecoration = BoxDecoration(
      color: bg,
      borderRadius: BorderRadius.circular(AppRadius.control),
      border: Border.all(color: border, width: 1),
    );

    Widget result = AnimatedContainer(
      duration: const Duration(milliseconds: 150),
      height: height,
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.base),
      decoration: buttonDecoration,
      alignment: Alignment.center,
      child: content,
    );

    if (isInteractive) {
      result = Material(
        color: Colors.transparent,
        borderRadius: BorderRadius.circular(AppRadius.control),
        child: InkWell(
          borderRadius: BorderRadius.circular(AppRadius.control),
          onTap: onPressed,
          splashColor: fg.withValues(alpha: 0.1),
          highlightColor: fg.withValues(alpha: 0.05),
          child: result,
        ),
      );
    }

    if (isFullWidth) {
      return SizedBox(
        width: double.infinity,
        child: result,
      );
    }

    return result;
  }

  (Color, Color, Color) _resolveColors(bool isInteractive) {
    if (!isInteractive) {
      return (
        AppColors.statusNeutralBg,
        AppColors.textMuted,
        AppColors.borderLight,
      );
    }

    return switch (variant) {
      AppButtonVariant.primary => (
        AppColors.primaryGreen,
        Colors.white,
        AppColors.primaryGreen,
      ),
      AppButtonVariant.secondary => (
        Colors.white,
        AppColors.textPrimary,
        AppColors.borderLight,
      ),
      AppButtonVariant.destructive => (
        AppColors.statusErrorBg,
        AppColors.statusError,
        AppColors.statusErrorBorder,
      ),
      AppButtonVariant.ghost => (
        Colors.transparent,
        AppColors.primaryGreen,
        Colors.transparent,
      ),
    };
  }
}
