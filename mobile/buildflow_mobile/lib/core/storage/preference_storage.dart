import 'package:shared_preferences/shared_preferences.dart';

class PreferenceStorage {
  static const _themeModeKey = 'theme_mode';

  Future<String?> readThemeMode() async =>
      (await SharedPreferences.getInstance()).getString(_themeModeKey);

  Future<void> saveThemeMode(String mode) async =>
      (await SharedPreferences.getInstance()).setString(_themeModeKey, mode);
}
