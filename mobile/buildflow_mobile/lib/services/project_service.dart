import 'api_service.dart';

class ProjectService {
  ProjectService(this.api);
  final ApiService api;

  Future<List<Map<String, dynamic>>> list(String kind, {String? parentId}) async {
    final path = '/$kind?pageSize=100${parentId == null ? '' : '&parentId=$parentId'}';
    final data = await api.request('GET', path) as Map<String, dynamic>;
    return (data['items'] as List).cast<Map<String, dynamic>>();
  }

  Future<Map<String, dynamic>> submitRequest(Map<String, dynamic> body) async =>
      (await api.request('POST', '/construction/resource-requests', body: body)) as Map<String, dynamic>;

  Future<List<Map<String, dynamic>>> requests() async =>
      (await api.request('GET', '/construction/resource-requests') as List).cast<Map<String, dynamic>>();

  Future<Map<String, dynamic>> startPlanning(String requestId) async =>
      (await api.request('POST', '/construction/resource-requests/$requestId/planning')) as Map<String, dynamic>;

  Future<Map<String, dynamic>> workflow(String id) async =>
      (await api.request('GET', '/construction/planning-workflows/$id')) as Map<String, dynamic>;

  Future<Map<String, dynamic>> updateProgress(String activityId, int percent, String work, String? blockers) async =>
      (await api.request('POST', '/construction/activities/$activityId/progress', body: {
        'progressPercent': percent, 'workCompleted': work, 'blockers': blockers,
      })) as Map<String, dynamic>;

  Future<void> uploadPhoto(String activityId, String path) async {
    await api.upload('/construction/activities/$activityId/photos', path);
  }
}
