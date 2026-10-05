import 'package:buildflow_mobile/screens/projects/site_engineer_projects_screen.dart';
import 'package:buildflow_mobile/services/project_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

class HistoryService extends Fake implements ProjectService {
  bool pending = false;
  bool deleted = false;
  String requestObjective = 'Resources for site clearing';
  Map<String, dynamic>? editedRequest;
  final updates = <Map<String, dynamic>>[
    {
      'id': 'progress-1',
      'canEdit': true,
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
    if (!deleted)
      {
        'id': 'request-1',
        'activityId': 'activity-1',
        'objective': requestObjective,
        'workflowStatus': pending ? 'Queued' : 'Approved',
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
    'canEdit': pending,
    'workflowStatus': pending ? 'Queued' : 'Approved',
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
      'id': 'progress-${updates.length + 1}',
      'canEdit': true,
      'progressPercent': percent,
      'workCompleted': work,
      'blockers': blockers,
      'createdAt': '2026-10-04T09:00:00Z',
    };
    updates.insert(0, row);
    return row;
  }

  @override
  Future<void> editProgress(
    String activityId,
    String id,
    int percent,
    String work,
    String? blockers,
  ) async {
    final row = updates.singleWhere((row) => row['id'] == id);
    row['progressPercent'] = percent;
    row['workCompleted'] = work;
    row['blockers'] = blockers;
  }

  @override
  Future<void> removeProgress(String activityId, String id) async =>
      updates.removeWhere((row) => row['id'] == id);
  @override
  Future<void> editRequest(String id, Map<String, dynamic> body) async {
    editedRequest = body;
    requestObjective = body['objective'] as String;
  }

  @override
  Future<void> deleteRequest(String id) async {
    deleted = true;
  }

  @override
  Future<Map<String, dynamic>> startPlanning(String id) async {
    pending = false;
    return {'id': 'workflow', 'status': 'PendingProjectManagerApproval'};
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
  testWidgets(
    'progress edits use the same form and removal refreshes inline history',
    (tester) async {
      final service = HistoryService();
      await tester.pumpWidget(activity(service));
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.text('Edit'));
      await tester.tap(find.text('Edit'));
      await tester.pumpAndSettle();
      expect(find.text('Edit progress update'), findsOneWidget);
      await tester.enterText(
        find.byType(TextField).first,
        'Corrected clearing details',
      );
      await tester.ensureVisible(find.text('Save changes'));
      await tester.tap(find.text('Save changes'));
      await tester.pumpAndSettle();
      expect(
        service.updates.single['workCompleted'],
        'Corrected clearing details',
      );
      await tester.ensureVisible(find.text('Remove'));
      await tester.tap(find.text('Remove'));
      await tester.pumpAndSettle();
      await tester.tap(find.widgetWithText(FilledButton, 'Remove'));
      await tester.pumpAndSettle();
      expect(service.updates, isEmpty);
      expect(find.text('Recent progress (0)'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );
  testWidgets(
    'queued request edits preserve resource units and saved metadata',
    (tester) async {
      final service = HistoryService()..pending = true;
      await tester.pumpWidget(activity(service));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Requests'));
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.text('Resources for site clearing'));
      await tester.tap(find.text('Resources for site clearing'));
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.text('Edit'));
      await tester.tap(find.text('Edit'));
      await tester.pumpAndSettle();
      await tester.enterText(
        find.byType(TextField).first,
        'Revised resources for site clearing',
      );
      await tester.ensureVisible(find.text('Save and restart planning'));
      await tester.tap(find.text('Save and restart planning'));
      await tester.pumpAndSettle();
      expect(service.editedRequest!['notes'], 'Deliver to the east gate');
      expect(service.editedRequest!['budgetLimit'], 1000);
      expect((service.editedRequest!['items'] as List).single['unit'], 'bags');
      await tester.ensureVisible(
        find.text('Revised resources for site clearing'),
      );
      await tester.tap(find.text('Revised resources for site clearing'));
      await tester.pumpAndSettle();
      expect(find.text('Delete'), findsNothing);
      expect(tester.takeException(), isNull);
    },
  );
  testWidgets(
    'queued request delete requires confirmation and refreshes its history',
    (tester) async {
      final service = HistoryService()..pending = true;
      await tester.pumpWidget(activity(service));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Requests'));
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.text('Resources for site clearing'));
      await tester.tap(find.text('Resources for site clearing'));
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.text('Delete'));
      await tester.tap(find.text('Delete'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Cancel'));
      await tester.pumpAndSettle();
      expect(service.deleted, isFalse);
      await tester.tap(find.text('Delete'));
      await tester.pumpAndSettle();
      await tester.tap(find.widgetWithText(FilledButton, 'Remove'));
      await tester.pumpAndSettle();
      expect(service.deleted, isTrue);
      expect(find.text('Resources for site clearing'), findsNothing);
      expect(tester.takeException(), isNull);
    },
  );
  testWidgets('reopened activity shows saved progress and request contents', (
    tester,
  ) async {
    await tester.pumpWidget(activity(HistoryService()));
    await tester.pumpAndSettle();
    expect(find.text('Upload site photo'), findsNothing);
    expect(find.text('Unrelated request'), findsNothing);
    expect(find.text('31% complete'), findsWidgets);
    expect(find.text('Resources for site clearing'), findsNothing);
    await tester.ensureVisible(find.text('Recent progress (1)'));
    expect(find.text('31% · Cleared the eastern area'), findsOneWidget);
    expect(find.textContaining('Waiting for access'), findsOneWidget);
    await tester.tap(find.text('Requests'));
    await tester.pumpAndSettle();
    expect(find.text('Work completed today'), findsNothing);
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
    await tester.ensureVisible(find.text('Recent progress (2)'));
    await tester.pumpAndSettle();
    expect(find.text('31% · Cleared the western area'), findsOneWidget);
    await tester.pumpWidget(const SizedBox());
    await tester.pumpWidget(activity(service));
    await tester.pumpAndSettle();
    expect(find.text('Recent progress (2)'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
