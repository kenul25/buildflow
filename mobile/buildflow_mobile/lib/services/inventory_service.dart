import 'api_service.dart';

class InventoryService {
  InventoryService(this.api);
  final ApiService api;

  Future<List<Map<String, dynamic>>> materials({String? search}) async {
    final query = search == null || search.isEmpty ? '' : '&search=${Uri.encodeQueryComponent(search)}';
    final data = await api.request('GET', '/inventory/materials/page?page=1&pageSize=100$query') as Map<String, dynamic>;
    return (data['items'] as List).cast<Map<String, dynamic>>();
  }

  Future<List<Map<String, dynamic>>> alerts() async =>
      (await api.request('GET', '/inventory/alerts/low-stock?threshold=0') as List).cast<Map<String, dynamic>>();

  Future<Map<String, dynamic>> receive(String materialId, double quantity, {String? reference}) async =>
      (await api.request('POST', '/inventory/materials/$materialId/receive', body: {
        'quantity': quantity,
        'reference': reference ?? 'site receipt',
      })) as Map<String, dynamic>;

  Future<Map<String, dynamic>> checkAvailability(List<Map<String, dynamic>> requirements) async =>
      (await api.request('POST', '/inventory/reservations/check', body: requirements)) as Map<String, dynamic>;
}