import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../theme/app_theme.dart';
import '../widgets/app_card.dart';
import '../widgets/info_row.dart';
import '../widgets/status_badge.dart';

// ─────────────────────────────────────────────────────────────────────────────
// ProfileScreen — Farmer profile and settings tab ("Me" bottom nav tab)
// Design.md §14 & §36
// ─────────────────────────────────────────────────────────────────────────────

class ProfileScreen extends StatelessWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: Text(
          'My Profile',
          style: GoogleFonts.inter(
            fontSize: 18,
            fontWeight: FontWeight.w600,
            color: Colors.white,
          ),
        ),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(AppSpacing.base),
        child: Column(
          children: [
            // ── Farmer Profile Header ──────────────────────────────────
            AppCard(
              padding: const EdgeInsets.all(AppSpacing.xl),
              child: Column(
                children: [
                  CircleAvatar(
                    radius: 36,
                    backgroundColor: AppColors.primaryGreenLight,
                    child: Text(
                      'SP',
                      style: GoogleFonts.inter(
                        fontSize: 24,
                        fontWeight: FontWeight.w700,
                        color: AppColors.primaryGreen,
                      ),
                    ),
                  ),
                  const SizedBox(height: AppSpacing.md),
                  Text(
                    'Sunil Perera',
                    style: GoogleFonts.inter(
                      fontSize: 18,
                      fontWeight: FontWeight.w700,
                      color: AppColors.textPrimary,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    'Registered Farmer · ID: FRM-2026-004',
                    style: GoogleFonts.inter(
                      fontSize: 13,
                      color: AppColors.textSecondary,
                    ),
                  ),
                  const SizedBox(height: AppSpacing.md),
                  const StatusBadge(
                    label: 'Verified Producer',
                    variant: BadgeVariant.success,
                    icon: Icons.verified_rounded,
                  ),
                ],
              ),
            ),
            const SizedBox(height: AppSpacing.base),

            // ── Farmer Contact & Farm Details ──────────────────────────
            AppCard(
              padding: const EdgeInsets.all(AppSpacing.base),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Farm & Contact Information',
                    style: GoogleFonts.inter(
                      fontSize: 15,
                      fontWeight: FontWeight.w700,
                      color: AppColors.textPrimary,
                    ),
                  ),
                  const SizedBox(height: AppSpacing.md),
                  const InfoRow(
                    label: 'Contact Phone',
                    value: '+94 77 123 4567',
                    icon: Icons.phone_outlined,
                  ),
                  const InfoRow(
                    label: 'Primary Region',
                    value: 'Central Region (Kandy)',
                    icon: Icons.location_on_outlined,
                  ),
                  const InfoRow(
                    label: 'Assigned Collection Hub',
                    value: 'Kandy Central Ag Hub',
                    icon: Icons.storefront_outlined,
                  ),
                  const InfoRow(
                    label: 'Active Listings',
                    value: '4 Batches',
                    icon: Icons.inventory_2_outlined,
                    showDivider: false,
                  ),
                ],
              ),
            ),
            const SizedBox(height: AppSpacing.base),

            // ── Guidelines & Information ───────────────────────────────
            AppCard(
              padding: const EdgeInsets.all(AppSpacing.base),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Quality & Standards',
                    style: GoogleFonts.inter(
                      fontSize: 15,
                      fontWeight: FontWeight.w700,
                      color: AppColors.textPrimary,
                    ),
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  InkWell(
                    onTap: () {
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(
                          content: Text('Grading guide: Grade A (export/premium), Grade B (standard market), Reject (unsellable).'),
                        ),
                      );
                    },
                    borderRadius: BorderRadius.circular(AppRadius.control),
                    child: Padding(
                      padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
                      child: Row(
                        children: [
                          const Icon(Icons.menu_book_rounded, color: AppColors.primaryGreen, size: 20),
                          const SizedBox(width: AppSpacing.md),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  'Grading Standard Guide',
                                  style: GoogleFonts.inter(fontSize: 14, fontWeight: FontWeight.w600, color: AppColors.textPrimary),
                                ),
                                Text(
                                  'Grade A, B, and Reject criteria',
                                  style: GoogleFonts.inter(fontSize: 12, color: AppColors.textSecondary),
                                ),
                              ],
                            ),
                          ),
                          const Icon(Icons.chevron_right_rounded, color: AppColors.textMuted, size: 20),
                        ],
                      ),
                    ),
                  ),
                  const Divider(height: 1, color: AppColors.borderLight),
                  InkWell(
                    onTap: () {
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(
                          content: Text('To appeal a grade discrepancy, contact your local collection center officer within 24 hours of inspection.'),
                        ),
                      );
                    },
                    borderRadius: BorderRadius.circular(AppRadius.control),
                    child: Padding(
                      padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
                      child: Row(
                        children: [
                          const Icon(Icons.help_outline_rounded, color: AppColors.primaryGreen, size: 20),
                          const SizedBox(width: AppSpacing.md),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  'Discrepancy Resolution Support',
                                  style: GoogleFonts.inter(fontSize: 14, fontWeight: FontWeight.w600, color: AppColors.textPrimary),
                                ),
                                Text(
                                  'How to appeal grade differences',
                                  style: GoogleFonts.inter(fontSize: 12, color: AppColors.textSecondary),
                                ),
                              ],
                            ),
                          ),
                          const Icon(Icons.chevron_right_rounded, color: AppColors.textMuted, size: 20),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: AppSpacing.base),

            // ── App Version ───────────────────────────────────────────
            Center(
              child: Text(
                'AgriConnect Mobile v1.0.0\nComponent C — Quality Grading & Inspection Module',
                textAlign: TextAlign.center,
                style: GoogleFonts.inter(
                  fontSize: 11,
                  color: AppColors.textMuted,
                  height: 1.4,
                ),
              ),
            ),
            const SizedBox(height: AppSpacing.xl),
          ],
        ),
      ),
    );
  }
}
