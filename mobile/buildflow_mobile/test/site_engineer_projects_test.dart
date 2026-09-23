import 'package:buildflow_mobile/screens/projects/site_engineer_projects_screen.dart';
import 'package:buildflow_mobile/services/project_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

class EmptyProjectsService extends Fake implements ProjectService {
  @override
  Future<List<Map<String, dynamic>>> list(String kind, {String? parentId}) async => [];
}

void main() {
  testWidgets('assigned projects show a clear empty state', (tester) async {
    await tester.pumpWidget(MaterialApp(home: Scaffold(body: SiteEngineerProjectsScreen(service: EmptyProjectsService()))));
    await tester.pumpAndSettle();
    expect(find.text('No projects assigned yet.'), findsOneWidget);
  });
}
