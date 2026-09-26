import 'package:flutter/material.dart';
import '../theme/app_theme.dart';

// ─────────────────────────────────────────────────────────────────────────────
// AppCard — Bespoke Card Container implementing Design.md §14
//
// White background (#FFFFFF), 12px radius, subtle 1px border (#E5E7EB),
// light shadow (0 1px 3px rgba(0,0,0,0.04)), consistent 16–24px padding.
// ─────────────────────────────────────────────────────────────────────────────

class AppCard extends StatelessWidget {
  final Widget child;
  final EdgeInsetsGeometry padding;
  final EdgeInsetsGeometry? margin;
  final VoidCallback? onTap;
  final Color? backgroundColor;
  final Color? borderColor;
  final double? borderRadius;
  final List<BoxShadow>? boxShadow;

  const AppCard({
    super.key,
    required this.child,
    this.padding = const EdgeInsets.all(AppSpacing.base),
    this.margin,
    this.onTap,
    this.backgroundColor,
    this.borderColor,
    this.borderRadius,
    this.boxShadow,
  });

  @override
  Widget build(BuildContext context) {
    final radius = BorderRadius.circular(borderRadius ?? AppRadius.card);
    final cardDecoration = BoxDecoration(
      color: backgroundColor ?? AppColors.cardBackground,
      borderRadius: radius,
      border: Border.all(
        color: borderColor ?? AppColors.borderLight,
        width: 1,
      ),
      boxShadow: boxShadow ?? AppShadows.subtle,
    );

    Widget content = Padding(
      padding: padding,
      child: child,
    );

    if (onTap != null) {
      return Container(
        margin: margin,
        decoration: cardDecoration,
        child: Material(
          color: Colors.transparent,
          borderRadius: radius,
          child: InkWell(
            borderRadius: radius,
            onTap: onTap,
            splashColor: AppColors.primaryGreenLight.withValues(alpha: 0.3),
            highlightColor: AppColors.primaryGreenLight.withValues(alpha: 0.15),
            child: content,
          ),
        ),
      );
    }

    return Container(
      margin: margin,
      decoration: cardDecoration,
      child: content,
    );
  }
}
