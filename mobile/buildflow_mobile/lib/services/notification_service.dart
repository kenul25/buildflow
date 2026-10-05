import 'api_service.dart';

class NotificationService {
  NotificationService(this.api);
  final ApiService api;

  Future<Map<String, dynamic>> load() async =>
      await api.request('GET', '/notifications') as Map<String, dynamic>;
  Future<void> markRead(String id) async {
    await api.request('POST', '/notifications/$id/read');
  }

  Future<void> markAllRead() async {
    await api.request('POST', '/notifications/read-all');
  }
}
