import '../models/auth_session.dart';
import 'api_service.dart';

class AuthService {
  const AuthService(this._api);
  final ApiService _api;

  Future<AuthSession> login({
    required String email,
    required String password,
  }) async {
    final response = await _api.request(
      'POST',
      '/auth/login',
      authenticated: false,
      body: {'email': email, 'password': password},
    );
    return AuthSession.fromJson(response as Map<String, dynamic>);
  }

  Future<AuthSession> register({
    required String fullName,
    required String email,
    required String password,
    required String confirmPassword,
  }) async {
    final response = await _api.request(
      'POST',
      '/auth/register',
      authenticated: false,
      body: {
        'fullName': fullName,
        'email': email,
        'password': password,
        'confirmPassword': confirmPassword,
      },
    );
    return AuthSession.fromJson(response as Map<String, dynamic>);
  }

  Future<AppUser> me() async => AppUser.fromJson(
    await _api.request('GET', '/auth/me') as Map<String, dynamic>,
  );

  Future<void> logout() async => _api.request('POST', '/auth/logout');
}
