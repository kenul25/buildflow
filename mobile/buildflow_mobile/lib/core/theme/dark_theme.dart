import 'package:flutter/material.dart';

ThemeData buildDarkTheme() => ThemeData(
  useMaterial3: true,
  colorScheme: ColorScheme.fromSeed(
    seedColor: const Color(0xFF60A5FA),
    brightness: Brightness.dark,
    surface: const Color(0xFF111827),
  ),
  scaffoldBackgroundColor: const Color(0xFF0B1220),
  inputDecorationTheme: const InputDecorationTheme(
    filled: true,
    fillColor: Color(0xFF172033),
    border: OutlineInputBorder(
      borderRadius: BorderRadius.all(Radius.circular(12)),
    ),
  ),
);
