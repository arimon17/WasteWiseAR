import 'package:flutter/material.dart';

abstract final class AppColors {
  static const Color navy = Color(0xFF0F172A);
  static const Color surface = Color(0xFF1E293B);
  static const Color overlay = Color(0xF20F172A);
  static const Color border = Color(0xFF334155);
  static const Color primary = Color(0xFF1F8A5A);
  static const Color primaryActive = Color(0xFF156340);
  static const Color emerald = Color(0xFF4ADE80);
  static const Color primarySoft = Color(0xFFE8F5E9);
  static const Color organik = Color(0xFF2E7D32);
  static const Color anorganik = Color(0xFFF57F17);
  static const Color b3 = Color(0xFFC62828);
  static const Color textMain = Color(0xFFF8FAFC);
  static const Color textSub = Color(0xFF94A3B8);
  static const Color textMuted = Color(0xFF64748B);
}

abstract final class AppTheme {
  static ThemeData get dark {
    const scheme = ColorScheme.dark(
      primary: AppColors.primary,
      onPrimary: Colors.white,
      primaryContainer: AppColors.primaryActive,
      onPrimaryContainer: AppColors.primarySoft,
      secondary: Color(0xFF34D399),
      onSecondary: AppColors.navy,
      secondaryContainer: AppColors.surface,
      onSecondaryContainer: AppColors.textMain,
      surface: AppColors.navy,
      onSurface: AppColors.textMain,
      onSurfaceVariant: AppColors.textSub,
      surfaceContainerHighest: AppColors.surface,
      outline: AppColors.border,
      outlineVariant: AppColors.border,
      error: AppColors.b3,
      onError: Colors.white,
    );

    final base = ThemeData(
      useMaterial3: true,
      brightness: Brightness.dark,
      colorScheme: scheme,
      scaffoldBackgroundColor: AppColors.navy,
    );

    return base.copyWith(
      appBarTheme: const AppBarTheme(
        backgroundColor: AppColors.navy,
        foregroundColor: AppColors.textMain,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        centerTitle: false,
        titleTextStyle: TextStyle(
          fontSize: 20,
          fontWeight: FontWeight.w700,
          color: AppColors.textMain,
        ),
      ),
      cardTheme: CardThemeData(
        color: AppColors.surface,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        margin: EdgeInsets.zero,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
          side: const BorderSide(color: AppColors.border),
        ),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: AppColors.primary,
          foregroundColor: Colors.white,
          minimumSize: const Size.fromHeight(52),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: AppColors.textMain,
          side: const BorderSide(color: AppColors.border),
          minimumSize: const Size.fromHeight(52),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          textStyle: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
        ),
      ),
      textTheme: base.textTheme
          .apply(
            bodyColor: AppColors.textMain,
            displayColor: AppColors.textMain,
          )
          .copyWith(
            headlineSmall: const TextStyle(
              fontSize: 28,
              fontWeight: FontWeight.w800,
              letterSpacing: -0.5,
              color: AppColors.textMain,
            ),
            titleLarge: const TextStyle(
              fontSize: 22,
              fontWeight: FontWeight.w700,
              color: AppColors.textMain,
            ),
            titleMedium: const TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.w700,
              color: AppColors.textMain,
            ),
            bodyLarge: const TextStyle(
              fontSize: 15,
              height: 1.5,
              color: AppColors.textSub,
            ),
            bodyMedium: const TextStyle(
              fontSize: 14,
              height: 1.5,
              color: AppColors.textSub,
            ),
            labelSmall: const TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w600,
              letterSpacing: 0.4,
              color: AppColors.textMuted,
            ),
          ),
      dividerTheme: const DividerThemeData(
        color: AppColors.border,
        thickness: 1,
        space: 1,
      ),
      snackBarTheme: SnackBarThemeData(
        backgroundColor: AppColors.surface,
        contentTextStyle: const TextStyle(color: AppColors.textMain),
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      ),
    );
  }
}
