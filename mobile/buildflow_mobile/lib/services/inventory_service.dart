import 'api_service.dart';

class InventoryService {
  InventoryService(this.api);
  final ApiService api;

  Future<List<Map<String, dynamic>>> materials({String? search}) async {
    final query = search == null || search.isEmpty ? '' : '&search=${Uri.encodeQueryComponent(search)}';
    final data = await api.request('GET', '/inventory/materials/page?page=1&pageSize=100$query') as Map<String, dynamic>;
    return (data['items'] as List).cast<Map<String, dynamic>>();
  }

    Future<List<Map<String, dynamic>>> warehouses() async =>
      ((await api.request('GET', '/inventory/warehouses/page?page=1&pageSize=100') as Map<String, dynamic>)['items'] as List).cast<Map<String, dynamic>>();

    Future<List<Map<String, dynamic>>> reservations() async =>
      ((await api.request('GET', '/inventory/reservations?page=1&pageSize=100') as Map<String, dynamic>)['items'] as List).cast<Map<String, dynamic>>();

    Future<List<Map<String, dynamic>>> movements() async =>
      ((await api.request('GET', '/inventory/movements?page=1&pageSize=100') as Map<String, dynamic>)['items'] as List).cast<Map<String, dynamic>>();

  Future<List<Map<String, dynamic>>> alerts() async =>
      (await api.request('GET', '/inventory/alerts/low-stock?threshold=0') as List).cast<Map<String, dynamic>>();

  Future<Map<String, dynamic>> receive(String materialId, double quantity, {String? reference}) async =>
      (await api.request('POST', '/inventory/materials/$materialId/receive', body: {
        'quantity': quantity,
        'reference': reference ?? 'site receipt',
      })) as Map<String, dynamic>;

      Future<Map<String, dynamic>> issue(String materialId, double quantity) async =>
        (await api.request('POST', '/inventory/materials/$materialId/issue', body: {'quantity': quantity, 'reference': 'mobile issue'})) as Map<String, dynamic>;

      Future<Map<String, dynamic>> returnStock(String materialId, double quantity) async =>
        (await api.request('POST', '/inventory/materials/$materialId/return', body: {'quantity': quantity, 'reference': 'mobile return'})) as Map<String, dynamic>;

      Future<Map<String, dynamic>> reserve(String materialId, double quantity) async =>
        (await api.request('POST', '/inventory/materials/$materialId/reservations', body: {'quantity': quantity, 'durationMinutes': 60})) as Map<String, dynamic>;

      Future<void> release(String reservationId) async {
      await api.request('DELETE', '/inventory/reservations/$reservationId');
      }

  Future<Map<String, dynamic>> checkAvailability(List<Map<String, dynamic>> requirements) async =>
      (await api.request('POST', '/inventory/reservations/check', body: requirements)) as Map<String, dynamic>;
}