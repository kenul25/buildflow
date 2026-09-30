import 'package:flutter/foundation.dart';

import '../core/storage/secure_storage.dart';
import '../models/api_exception.dart';
import '../models/auth_session.dart';
import '../services/auth_service.dart';

enum AuthStatus { initializing, unauthenticated, authenticated }

class AuthProvider extends ChangeNotifier {
  AuthProvider(this._authService, this._storage);

  final AuthService _authService;
  final SecureStorage _storage;
  AuthStatus status = AuthStatus.initializing;
  AppUser? user;
  String? errorMessage;

  Future<void> initialize() async {
    status = AuthStatus.initializing;
    notifyListeners();
    try {
      if (await _storage.readRefreshToken() == null) {
        status = AuthStatus.unauthenticated;
      } else {
        user = await _authService.me();
        status = AuthStatus.authenticated;
      }
    } catch (_) {
      await _storage.clearTokens();
      status = AuthStatus.unauthenticated;
    }
    notifyListeners();
  }

  Future<bool> login({required String email, required String password}) =>
      _authenticate(() => _authService.login(email: email, password: password));

  Future<bool> register({
    required String fullName,
    required String email,
    required String password,
    required String confirmPassword,
  }) => _authenticate(
    () => _authService.register(
      fullName: fullName,
      email: email,
      password: password,
      confirmPassword: confirmPassword,
    ),
  );

  Future<bool> _authenticate(Future<AuthSession> Function() operation) async {
    errorMessage = null;
    notifyListeners();
    try {
      final session = await operation();
      await _storage.saveTokens(
        accessToken: session.accessToken,
        refreshToken: session.refreshToken,
      );
      user = session.user;
      status = AuthStatus.authenticated;
      notifyListeners();
      return true;
    } on ApiException catch (error) {
      errorMessage = error.message;
    } catch (_) {
      errorMessage = 'Something went wrong. Please try again.';
    }
    notifyListeners();
    return false;
  }

  Future<void> logout() async {
    try {
      await _authService.logout();
    } finally {
      await _storage.clearTokens();
      user = null;
      status = AuthStatus.unauthenticated;
      notifyListeners();
    }
  }

  void clearError() {
    if (errorMessage == null) return;
    errorMessage = null;
    notifyListeners();
  }
}
