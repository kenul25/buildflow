import 'api_service.dart';

class ProcurementService {
  ProcurementService(this.api);

  final ApiService api;

  // -----------------------------
  // Purchase Requests
  // -----------------------------

  Future<List<Map<String, dynamic>>> purchaseRequests() async {
    final data = await api.request(
      'GET',
      '/purchase-requests',
    );

    return (data as List).cast<Map<String, dynamic>>();
  }

  Future<Map<String, dynamic>> purchaseRequest(String id) async {
    return (await api.request(
      'GET',
      '/purchase-requests/$id',
    )) as Map<String, dynamic>;
  }

  Future<void> cancelPurchaseRequest(String id) async {
    await api.request(
      'DELETE',
      '/purchase-requests/$id',
    );
  }

  Future<Map<String, dynamic>> createPurchaseRequest({
  required String materialName,
  required double quantity,
  required String requiredByDate,
}) async {
  return (await api.request(
    'POST',
    '/purchase-requests',
    body: {
      'materialName': materialName,
      'quantity': quantity,
      'requiredByDate': requiredByDate,
    },
  )) as Map<String, dynamic>;
}

Future<Map<String, dynamic>> updatePurchaseRequest(
  String id, {
  required String materialName,
  required double quantity,
  required String requiredByDate,
}) async {
  return (await api.request(
    'PUT',
    '/purchase-requests/$id',
    body: {
      'materialName': materialName,
      'quantity': quantity,
      'requiredByDate': requiredByDate,
    },
  )) as Map<String, dynamic>;
}

Future<Map<String, dynamic>> approvePurchaseRequest(
  String id,
) async {
  return (await api.request(
    'PUT',
    '/purchase-requests/$id/approve',
  )) as Map<String, dynamic>;
}

  // -----------------------------
  // Purchase Orders
  // -----------------------------

  Future<List<Map<String, dynamic>>> purchaseOrders() async {
    final data = await api.request(
      'GET',
      '/purchase-orders',
    );

    return (data as List).cast<Map<String, dynamic>>();
  }

  Future<Map<String, dynamic>> purchaseOrder(String id) async {
    return (await api.request(
      'GET',
      '/purchase-orders/$id',
    )) as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> createPurchaseOrder({
  required String purchaseRequestId,
  required String supplierId,
  required double unitPrice,
  required String deliveryDate,
}) async {
  return (await api.request(
    'POST',
    '/purchase-orders',
    body: {
      'purchaseRequestId': int.parse(purchaseRequestId),
      'supplierId': int.parse(supplierId),
      'unitPrice': unitPrice,
      'deliveryDate': deliveryDate,
    },
  )) as Map<String, dynamic>;
}

Future<Map<String, dynamic>> updatePurchaseOrder(
  String id, {
  required String purchaseRequestId,
  required String supplierId,
  required double unitPrice,
  required String deliveryDate,
}) async {
  return (await api.request(
    'PUT',
    '/purchase-orders/$id',
    body: {
      'purchaseRequestId': int.parse(purchaseRequestId),
      'supplierId': int.parse(supplierId),
      'unitPrice': unitPrice,
      'deliveryDate': deliveryDate,
    },
  )) as Map<String, dynamic>;
}

Future<void> cancelPurchaseOrder(String id) async {
  await api.request(
    'DELETE',
    '/purchase-orders/$id',
  );
}

  // -----------------------------
  // Deliveries
  // -----------------------------

  Future<List<Map<String, dynamic>>> deliveries() async {
    final data = await api.request(
      'GET',
      '/deliveries',
    );

    return (data as List).cast<Map<String, dynamic>>();
  }

  Future<Map<String, dynamic>> delivery(String id) async {
    return (await api.request(
      'GET',
      '/deliveries/$id',
    )) as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> createDelivery({
  required String purchaseOrderId,
  required double quantity,
  required String deliveryDate,
  String evidenceUrl = '',
}) async {
  return (await api.request(
    'POST',
    '/deliveries',
    body: {
      'purchaseOrderId': int.parse(purchaseOrderId),
      'quantity': quantity,
      'deliveryDate': deliveryDate,
      'evidenceUrl': evidenceUrl,
    },
  )) as Map<String, dynamic>;
}

  Future<Map<String, dynamic>> updateDeliveryStatus(
    String id,
    String status,
  ) async {
    return (await api.request(
      'PUT',
      '/deliveries/$id/status?status=${Uri.encodeComponent(status)}',
    )) as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> updateDelivery(
    String id,
    Map<String, dynamic> body,
  ) async {
    return (await api.request(
      'PUT',
      '/deliveries/$id',
      body: body,
    )) as Map<String, dynamic>;
  }

  Future<void> cancelDelivery(String id) async {
    await api.request(
      'DELETE',
      '/deliveries/$id',
    );
  }

  // -----------------------------
  // Delivery Evidence
  // -----------------------------

  Future<dynamic> uploadDeliveryEvidence(
    String path,
    String filePath,
  ) async {
    return await api.upload(
      path,
      filePath,
    );
  }
}