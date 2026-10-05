import 'package:buildflow_mobile/screens/projects/site_engineer_projects_screen.dart';
import 'package:buildflow_mobile/services/project_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

class HistoryService extends Fake implements ProjectService {
  final updates = <Map<String, dynamic>>[
    {
      'progressPercent': 31,
      'workCompleted': 'Cleared the eastern area',
      'blockers': 'Waiting for access',
      'createdAt': '2026-10-04T08:00:00Z',
    },
  ];
  @override
  Future<List<Map<String, dynamic>>> progressHistory(String activityId) async =>
      [...updates];
  @override
  Future<List<Map<String, dynamic>>> requests() async => [
    {
      'id': 'request-1',
      'activityId': 'activity-1',
      'objective': 'Resources for site clearing',
      'workflowStatus': 'Approved',
      'createdAt': '2026-10-04T08:00:00Z',
    },
    {
      'id': 'other',
      'activityId': 'another-activity',
      'objective': 'Unrelated request',
    },
  ];
  @override
  Future<Map<String, dynamic>> requestDetails(String requestId) async => {
    'status': 'Draft',
    'notes': 'Deliver to the east gate',
    'budgetLimit': 1000,
    'items': [
      {'kind': 'Material', 'name': 'Cement', 'quantity': 2.5, 'unit': 'bags'},
    ],
  };
  @override
  Future<Map<String, dynamic>> updateProgress(
    String activityId,
    int percent,
    String work,
    String? blockers,
  ) async {
    final row = {
      'progressPercent': percent,
      'workCompleted': work,
      'blockers': blockers,
      'createdAt': '2026-10-04T09:00:00Z',
    };
    updates.insert(0, row);
    return row;
  }
}

Widget activity(HistoryService service) => MaterialApp(
  home: SiteActivityScreen(
    service: service,
    project: const {'id': 'project-1'},
    siteId: 'site-1',
    activity: const {
      'id': 'activity-1',
      'name': 'Site Clearing',
      'progressPercent': 0,
    },
  ),
);

void main() {
  testWidgets('reopened activity shows saved progress and request contents', (
    tester,
  ) async {
    await tester.pumpWidget(activity(HistoryService()));
    await tester.pumpAndSettle();
    expect(find.text('Upload site photo'), findsNothing);
    expect(find.text('Unrelated request'), findsNothing);
    expect(find.text('31% complete'), findsWidgets);
    await tester.tap(find.text('Progress history (1)'));
    await tester.pumpAndSettle();
    expect(find.text('31% · Cleared the eastern area'), findsOneWidget);
    expect(find.textContaining('Waiting for access'), findsOneWidget);
    await tester.ensureVisible(find.text('Resources for site clearing'));
    await tester.tap(find.text('Resources for site clearing'));
    await tester.pumpAndSettle();
    expect(find.text('Material: Cement · 2.5 bags'), findsOneWidget);
    expect(find.text('Notes: Deliver to the east gate'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('saving progress refreshes history and survives reopening', (
    tester,
  ) async {
    final service = HistoryService();
    await tester.pumpWidget(activity(service));
    await tester.pumpAndSettle();
    final workField = find.byType(TextField).first;
    await tester.ensureVisible(workField);
    await tester.enterText(workField, 'Cleared the western area');
    await tester.ensureVisible(find.text('Save progress'));
    await tester.tap(find.text('Save progress'));
    await tester.pumpAndSettle();
    expect(service.updates.first['workCompleted'], 'Cleared the western area');
    await tester.ensureVisible(find.text('Progress history (2)'));
    await tester.tap(find.text('Progress history (2)'));
    await tester.pumpAndSettle();
    expect(find.text('31% · Cleared the western area'), findsOneWidget);
    await tester.pumpWidget(const SizedBox());
    await tester.pumpWidget(activity(service));
    await tester.pumpAndSettle();
    expect(find.text('Progress history (2)'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
