import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:http/http.dart' as http;
import 'package:http_parser/http_parser.dart';

import '../core/config/app_config.dart';
import '../core/storage/secure_storage.dart';
import '../models/api_exception.dart';

class ApiService {
  ApiService(this._storage, {http.Client? client})
    : _client = client ?? http.Client();

  final SecureStorage _storage;
  final http.Client _client;

  Future<dynamic> request(
    String method,
    String path, {
    Object? body,
    bool authenticated = true,
  }) async {
    try {
      var response = await _send(
        method,
        path,
        body: body,
        authenticated: authenticated,
      );
      if (response.statusCode == 401 && authenticated && await _refresh()) {
        response = await _send(method, path, body: body, authenticated: true);
      }
      return _decode(response);
    } on SocketException {
      throw const ApiException(
        'No internet connection. Check your network and try again.',
      );
    } on http.ClientException {
      throw const ApiException('Unable to reach the BuildFlow server.');
    } on TimeoutException {
      throw const ApiException(
        'The BuildFlow server did not respond. Check that the backend is running '
        'and that this device can reach it.',
      );
    }
  }

  Future<dynamic> upload(String path, String filePath) async {
    try {
      final token = await _storage.readAccessToken();
      final request = http.MultipartRequest('POST', Uri.parse('${AppConfig.apiBaseUrl}$path'));
      if (token != null) request.headers['Authorization'] = 'Bearer $token';
      final extension = filePath.toLowerCase().split('.').last;
      final subtype = extension == 'png' ? 'png' : extension == 'webp' ? 'webp' : 'jpeg';
      request.files.add(await http.MultipartFile.fromPath('file', filePath, contentType: MediaType('image', subtype)));
      final response = await http.Response.fromStream(await _client.send(request).timeout(const Duration(seconds: 30)));
      return _decode(response);
    } on SocketException {
      throw const ApiException('No internet connection. Check your network and try again.');
    } on TimeoutException {
      throw const ApiException('Photo upload timed out. Please try again.');
    }
  }

  Future<http.Response> _send(
    String method,
    String path, {
    Object? body,
    required bool authenticated,
  }) async {
    final request =
        http.Request(method, Uri.parse('${AppConfig.apiBaseUrl}$path'))
          ..headers['Accept'] = 'application/json'
          ..headers['Content-Type'] = 'application/json';
    if (authenticated) {
      final token = await _storage.readAccessToken();
      if (token != null) request.headers['Authorization'] = 'Bearer $token';
    }
    if (body != null) request.body = jsonEncode(body);
    final streamed = await _client
        .send(request)
        .timeout(const Duration(seconds: 15));
    return http.Response.fromStream(streamed);
  }

  Future<bool> _refresh() async {
    final refreshToken = await _storage.readRefreshToken();
    if (refreshToken == null) return false;
    try {
      final response = await _client
          .post(
            Uri.parse('${AppConfig.apiBaseUrl}/auth/refresh'),
            headers: {
              'Content-Type': 'application/json',
              'Accept': 'application/json',
            },
            body: jsonEncode({'refreshToken': refreshToken}),
          )
          .timeout(const Duration(seconds: 15));
      if (response.statusCode != 200) {
        await _storage.clearTokens();
        return false;
      }
      final payload = jsonDecode(response.body) as Map<String, dynamic>;
      await _storage.saveTokens(
        accessToken: payload['accessToken'] as String,
        refreshToken: payload['refreshToken'] as String,
      );
      return true;
    } catch (_) {
      return false;
    }
  }

  dynamic _decode(http.Response response) {
    final dynamic payload = response.body.isEmpty
        ? null
        : jsonDecode(response.body);
    if (response.statusCode >= 200 && response.statusCode < 300) return payload;
    final message = payload is Map<String, dynamic>
        ? payload['detail'] as String? ?? 'The request could not be completed.'
        : 'The request could not be completed.';
    throw ApiException(message, statusCode: response.statusCode);
  }
}
