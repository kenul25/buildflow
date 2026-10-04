import 'api_service.dart';

class SchedulingService {
  SchedulingService(this.api);
  final ApiService api;
  Future<List<Map<String, dynamic>>> list(String kind) async {
    final rows = <Map<String, dynamic>>[];
    for (var page = 1; ; page++) {
      final result = await api.request(
        'GET',
        '/scheduling/$kind?page=$page&pageSize=100',
      ) as Map<String, dynamic>;
      final items = (result['items'] as List).cast<Map<String, dynamic>>();
      rows.addAll(items);
      if (rows.length >= (result['total'] as num) || items.isEmpty) return rows;
    }
  }

  Future<void> status(String id, String value, String notes) async {
    await api.request(
      'PUT',
      '/scheduling/worker-assignments/$id/status',
      body: {'status': value, 'notes': notes},
    );
  }

  Future<void> scan(String code, String action) async {
    await api.request(
      'POST',
      '/scheduling/equipment/scan',
      body: {'code': code, 'action': action},
    );
  }

  Future<void> requestEquipment(Map<String, dynamic> body) async {
    await api.request('POST', '/scheduling/equipment-requests', body: body);
  }

  Future<void> cancelRequest(String id) async {
    await api.request('DELETE', '/scheduling/equipment-requests/$id');
  }

  Future<void> report(Map<String, dynamic> body) async {
    await api.request('POST', '/scheduling/issues', body: body);
  }
}
