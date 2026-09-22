import 'package:flutter_test/flutter_test.dart';
import 'package:buildflow_mobile/core/storage/secure_storage.dart';
import 'package:buildflow_mobile/providers/auth_provider.dart';
import 'package:buildflow_mobile/screens/auth/login_screen.dart';
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
    await tester.tap(find.text('Sign in'));
    await tester.pump();
    expect(find.text('Enter a valid email address.'), findsOneWidget);
    expect(find.text('Enter your password.'), findsOneWidget);
  });
}
