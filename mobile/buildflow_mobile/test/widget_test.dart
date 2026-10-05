import 'package:flutter_test/flutter_test.dart';
import 'package:buildflow_mobile/core/storage/secure_storage.dart';
import 'package:buildflow_mobile/providers/auth_provider.dart';
import 'package:buildflow_mobile/screens/auth/login_screen.dart';
import 'package:buildflow_mobile/screens/auth/register_screen.dart';
import 'package:buildflow_mobile/core/theme/app_theme.dart';
import 'package:buildflow_mobile/services/api_service.dart';
import 'package:buildflow_mobile/services/auth_service.dart';
import 'package:flutter/material.dart';

void main() {
  testWidgets('login validates required credentials', (
    WidgetTester tester,
  ) async {
    final storage = SecureStorage();
    final provider = AuthProvider(AuthService(ApiService(storage)), storage);
    await tester.pumpWidget(
      MaterialApp(home: LoginScreen(authProvider: provider)),
    );
    await tester.ensureVisible(find.text('Sign in'));
    await tester.tap(find.text('Sign in'));
    await tester.pump();
    expect(find.text('Enter a valid email address.'), findsOneWidget);
    expect(find.text('Enter your password.'), findsOneWidget);
  });

  for (final dark in [false, true]) {
    testWidgets(
      'registration fits a narrow screen with keyboard (${dark ? 'dark' : 'light'})',
      (tester) async {
        tester.view.physicalSize = const Size(320, 640);
        tester.view.devicePixelRatio = 1;
        addTearDown(tester.view.resetPhysicalSize);
        addTearDown(tester.view.resetDevicePixelRatio);
        final storage = SecureStorage();
        final provider = AuthProvider(
          AuthService(ApiService(storage)),
          storage,
        );
        await tester.pumpWidget(
          MaterialApp(
            theme: dark ? buildDarkTheme() : buildLightTheme(),
            builder: (context, child) => MediaQuery(
              data: MediaQuery.of(context).copyWith(
                viewInsets: const EdgeInsets.only(bottom: 240),
                textScaler: const TextScaler.linear(1.3),
              ),
              child: child!,
            ),
            home: RegisterScreen(authProvider: provider),
          ),
        );
        expect(
          find.text(
            'Project Manager and officer roles are assigned by an administrator.',
          ),
          findsNothing,
        );
        final toggle = find.byTooltip('Show password');
        await tester.ensureVisible(toggle);
        await tester.tap(toggle);
        await tester.pump();
        expect(find.byTooltip('Hide password'), findsOneWidget);
        final password = tester.widget<TextField>(find.byType(TextField).at(2));
        expect(password.obscureText, isFalse);
        final confirmToggle = find.byTooltip('Show confirm password');
        await tester.ensureVisible(confirmToggle);
        await tester.tap(confirmToggle);
        await tester.pump();
        expect(
          tester.widget<TextField>(find.byType(TextField).at(3)).obscureText,
          isFalse,
        );
        await tester.ensureVisible(find.text('Create account'));
        await tester.tap(find.text('Create account'));
        await tester.pump();
        expect(find.text('Enter your full name.'), findsOneWidget);
        expect(tester.takeException(), isNull);
      },
    );
  }
}
