import 'package:flutter/material.dart';

import '../core/storage/preference_storage.dart';

class ThemeProvider extends ChangeNotifier {
  ThemeProvider(this._storage);
  final PreferenceStorage _storage;
  ThemeMode mode = ThemeMode.system;

  Future<void> initialize() async {
    final saved = await _storage.readThemeMode();
    mode = ThemeMode.values.firstWhere(
      (value) => value.name == saved,
      orElse: () => ThemeMode.system,
    );
    notifyListeners();
  }

  Future<void> setMode(ThemeMode value) async {
    mode = value;
    notifyListeners();
    await _storage.saveThemeMode(value.name);
  }
}
