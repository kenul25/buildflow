import 'api_service.dart';

class ProjectService {
  ProjectService(this.api);
  final ApiService api;

  Future<List<Map<String, dynamic>>> resourceOptions(String kind) async {
    final path = kind == 'Material'
        ? '/inventory/materials/page'
        : kind == 'Equipment'
        ? '/scheduling/equipment'
        : '/scheduling/skills';
    final options = <Map<String, dynamic>>[];
    for (var page = 1; ; page++) {
      final result = await api.request(
        'GET',
        '$path?page=$page&pageSize=100',
      ) as Map<String, dynamic>;
      options.addAll((result['items'] as List).cast<Map<String, dynamic>>());
      if (options.length >= (result['total'] as num) ||
          (result['items'] as List).isEmpty) {
        break;
      }
    }
    final unique = <String, Map<String, dynamic>>{};
    for (final option in options) {
      if (kind == 'Equipment' && option['status'] != 'Operational') continue;
      unique.putIfAbsent(
        '${option['name']}|${kind == 'Material' ? option['unit'] : ''}',
        () => option,
      );
    }
    return unique.values.toList()
      ..sort((a, b) => (a['name'] as String).compareTo(b['name'] as String));
  }

  Future<List<Map<String, dynamic>>> list(
    String kind, {
    String? parentId,
  }) async {
    final rows = <Map<String, dynamic>>[];
    for (var page = 1; ; page++) {
      final path =
          '/$kind?page=$page&pageSize=100${parentId == null ? '' : '&parentId=$parentId'}';
      final data = await api.request('GET', path) as Map<String, dynamic>;
      final items = (data['items'] as List).cast<Map<String, dynamic>>();
      rows.addAll(items);
      if (items.length < 100 || rows.length >= (data['total'] as num)) {
        return rows;
      }
    }
  }

  Future<Map<String, dynamic>> submitRequest(Map<String, dynamic> body) async =>
      (await api.request('POST', '/construction/resource-requests', body: body))
          as Map<String, dynamic>;

  Future<List<Map<String, dynamic>>> requests() async =>
      (await api.request('GET', '/construction/resource-requests') as List)
          .cast<Map<String, dynamic>>();

  Future<List<Map<String, dynamic>>> progressHistory(String activityId) async =>
      (await api.request(
        'GET',
        '/construction/activities/$activityId/progress',
      ) as List).cast<Map<String, dynamic>>();

  Future<Map<String, dynamic>> requestDetails(String requestId) async =>
      await api.request('GET', '/construction/resource-requests/$requestId')
          as Map<String, dynamic>;

  Future<void> editRequest(String id, Map<String, dynamic> body) async =>
      api.request('PUT', '/construction/resource-requests/$id', body: body);
  Future<void> deleteRequest(String id) async =>
      api.request('DELETE', '/construction/resource-requests/$id');
  Future<void> editProgress(
    String activityId,
    String id,
    int percent,
    String work,
    String? blockers,
  ) async => api.request(
    'PUT',
    '/construction/activities/$activityId/progress/$id',
    body: {
      'progressPercent': percent,
      'workCompleted': work,
      'blockers': blockers,
    },
  );
  Future<void> removeProgress(String activityId, String id) async => api
      .request('DELETE', '/construction/activities/$activityId/progress/$id');

  Future<Map<String, dynamic>> startPlanning(String requestId) async {
    final workflow = await api.request(
      'POST',
      '/construction/resource-requests/$requestId/planning',
    ) as Map<String, dynamic>;
    if (workflow['status'] == 'AwaitingAgents') {
      return await api.request('POST', '/workflows/${workflow['id']}/execute')
          as Map<String, dynamic>;
    }
    return workflow;
  }

  Future<Map<String, dynamic>> workflow(String id) async =>
      (await api.request('GET', '/construction/planning-workflows/$id'))
          as Map<String, dynamic>;

  Future<Map<String, dynamic>> updateProgress(
    String activityId,
    int percent,
    String work,
    String? blockers,
  ) async => (await api.request(
    'POST',
    '/construction/activities/$activityId/progress',
    body: {
      'progressPercent': percent,
      'workCompleted': work,
      'blockers': blockers,
    },
  )) as Map<String, dynamic>;
}
