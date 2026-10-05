import 'api_service.dart';

class InventoryService {
  InventoryService(this.api);
  final ApiService api;

  Future<List<Map<String, dynamic>>> _all(
    String path, {
    String extra = '',
  }) async {
    final rows = <Map<String, dynamic>>[];
    for (var page = 1; ; page++) {
      final data = await api.request(
        'GET',
        '$path?page=$page&pageSize=100$extra',
      ) as Map<String, dynamic>;
      final items = (data['items'] as List).cast<Map<String, dynamic>>();
      rows.addAll(items);
      if (items.isEmpty || rows.length >= (data['total'] as num)) return rows;
    }
  }

  Future<List<Map<String, dynamic>>> materials({String? search}) async {
    final query = search == null || search.isEmpty
        ? ''
        : '&search=${Uri.encodeQueryComponent(search)}';
    return _all('/inventory/materials/page', extra: query);
  }

  Future<List<Map<String, dynamic>>> warehouses() async =>
      _all('/inventory/warehouses/page');

  Future<List<Map<String, dynamic>>> reservations() async =>
      _all('/inventory/reservations');

  Future<List<Map<String, dynamic>>> movements() async =>
      _all('/inventory/movements');

  Future<List<Map<String, dynamic>>> alerts() async => (await api.request(
    'GET',
    '/inventory/alerts/low-stock?threshold=0',
  ) as List).cast<Map<String, dynamic>>();

  Future<Map<String, dynamic>> receive(
    String materialId,
    double quantity, {
    String? reference,
  }) async => (await api.request(
    'POST',
    '/inventory/materials/$materialId/receive',
    body: {'quantity': quantity, 'reference': reference ?? 'site receipt'},
  )) as Map<String, dynamic>;

  Future<Map<String, dynamic>> issue(
    String materialId,
    double quantity,
  ) async => (await api.request(
    'POST',
    '/inventory/materials/$materialId/issue',
    body: {'quantity': quantity, 'reference': 'mobile issue'},
  )) as Map<String, dynamic>;

  Future<Map<String, dynamic>> returnStock(
    String materialId,
    double quantity,
  ) async => (await api.request(
    'POST',
    '/inventory/materials/$materialId/return',
    body: {'quantity': quantity, 'reference': 'mobile return'},
  )) as Map<String, dynamic>;

  Future<Map<String, dynamic>> reserve(
    String materialId,
    double quantity,
  ) async => (await api.request(
    'POST',
    '/inventory/materials/$materialId/reservations',
    body: {'quantity': quantity, 'durationMinutes': 60},
  )) as Map<String, dynamic>;

  Future<void> release(String reservationId) async {
    await api.request('DELETE', '/inventory/reservations/$reservationId');
  }

  Future<Map<String, dynamic>> checkAvailability(
    List<Map<String, dynamic>> requirements,
  ) async => (await api.request(
    'POST',
    '/inventory/reservations/check',
    body: requirements,
  )) as Map<String, dynamic>;
}
