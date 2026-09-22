import 'package:flutter/material.dart';

import 'core/storage/preference_storage.dart';
import 'core/storage/secure_storage.dart';
import 'core/theme/app_theme.dart';
import 'providers/auth_provider.dart';
import 'providers/theme_provider.dart';
import 'screens/auth/login_screen.dart';
import 'screens/home/home_screen.dart';
import 'screens/splash/splash_screen.dart';
import 'services/api_service.dart';
import 'services/auth_service.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  final secureStorage = SecureStorage();
  final authProvider = AuthProvider(
    AuthService(ApiService(secureStorage)),
    secureStorage,
  );
  final themeProvider = ThemeProvider(PreferenceStorage());
  runApp(
    BuildFlowApp(authProvider: authProvider, themeProvider: themeProvider),
  );
}

class BuildFlowApp extends StatefulWidget {
  const BuildFlowApp({
    required this.authProvider,
    required this.themeProvider,
    super.key,
  });
  final AuthProvider authProvider;
  final ThemeProvider themeProvider;

  @override
  State<BuildFlowApp> createState() => _BuildFlowAppState();
}

class _BuildFlowAppState extends State<BuildFlowApp> {
  @override
  void initState() {
    super.initState();
    widget.themeProvider.initialize();
    widget.authProvider.initialize();
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: Listenable.merge([widget.authProvider, widget.themeProvider]),
      builder: (context, _) => MaterialApp(
        title: 'BuildFlow AI',
        debugShowCheckedModeBanner: false,
        theme: buildLightTheme(),
        darkTheme: buildDarkTheme(),
        themeMode: widget.themeProvider.mode,
        home: switch (widget.authProvider.status) {
          AuthStatus.initializing => const SplashScreen(),
          AuthStatus.unauthenticated => LoginScreen(
            authProvider: widget.authProvider,
          ),
          AuthStatus.authenticated => HomeScreen(
            authProvider: widget.authProvider,
            themeProvider: widget.themeProvider,
          ),
        },
      ),
    );
  }
}
