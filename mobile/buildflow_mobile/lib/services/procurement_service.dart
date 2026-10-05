import 'api_service.dart';

class ProcurementService {
  ProcurementService(this.api);
  final ApiService api;
  Future<List<Map<String, dynamic>>> _list(String path) async {
    final rows = <Map<String, dynamic>>[];
    for (var page = 1; ; page++) {
      final data =
          await api.request('GET', '$path?page=$page&pageSize=100') as List;
      rows.addAll(data.cast<Map<String, dynamic>>());
      if (data.length < 100) return rows;
    }
  }

  Future<List<Map<String, dynamic>>> purchaseRequests() =>
      _list('/PurchaseRequests');
  Future<List<Map<String, dynamic>>> purchaseOrders() =>
      _list('/PurchaseOrders');
  Future<List<Map<String, dynamic>>> deliveries() => _list('/deliveries');
  Future<dynamic> approvePurchaseRequest(String id) =>
      api.request('PUT', '/PurchaseRequests/$id/approve');
  Future<dynamic> updateDeliveryStatus(String id, String status) => api.request(
    'PUT',
    '/deliveries/$id/status?status=${Uri.encodeComponent(status)}',
  );
  Future<dynamic> uploadDeliveryEvidence(String path, String filePath) =>
      api.upload(path, filePath);
}
