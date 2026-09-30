import 'package:flutter/material.dart';

ThemeData buildLightTheme() => ThemeData(
  useMaterial3: true,
  colorScheme: ColorScheme.fromSeed(
    seedColor: const Color(0xFF2563EB),
    brightness: Brightness.light,
    surface: const Color(0xFFF8FAFC),
  ),
  scaffoldBackgroundColor: const Color(0xFFF8FAFC),
  inputDecorationTheme: const InputDecorationTheme(
    filled: true,
    fillColor: Colors.white,
    border: OutlineInputBorder(
      borderRadius: BorderRadius.all(Radius.circular(12)),
    ),
  ),
  cardTheme: const CardThemeData(
    color: Colors.white,
    elevation: 0,
    shape: RoundedRectangleBorder(
      side: BorderSide(color: Color(0xFFE2E8F0)),
      borderRadius: BorderRadius.all(Radius.circular(16)),
    ),
  ),
);
