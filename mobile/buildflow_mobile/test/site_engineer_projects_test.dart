import 'package:buildflow_mobile/screens/projects/site_engineer_projects_screen.dart';
import 'package:buildflow_mobile/screens/projects/site_requests_screen.dart';
import 'package:buildflow_mobile/services/project_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

class EmptyProjectsService extends Fake implements ProjectService {
  @override
  Future<List<Map<String, dynamic>>> list(
    String kind, {
    String? parentId,
  }) async => [];
}

class LayoutService extends Fake implements ProjectService {
  LayoutService(this.projectName);
  final String projectName;

  @override
  Future<List<Map<String, dynamic>>> list(
    String kind, {
    String? parentId,
  }) async => kind == 'projects'
      ? [
          {
            'id': 'project-1',
            'name': projectName,
            'code': 'P-01',
            'status': 'Active',
          },
        ]
      : [];

  @override
  Future<List<Map<String, dynamic>>> requests() async => [
    {
      'id': 'request-1',
      'objective': 'Request concrete, reinforcement materials, equipment and a site crew for the northern foundation work area',
      'workflowStatus': 'AwaitingAgents',
      'workflowId': 'workflow-1',
    },
  ];
}

void setScreenSize(WidgetTester tester, Size size) {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = size;
  addTearDown(tester.view.resetDevicePixelRatio);
  addTearDown(tester.view.resetPhysicalSize);
}

void main() {
  testWidgets('assigned projects show a clear empty state', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: SiteEngineerProjectsScreen(service: EmptyProjectsService()),
        ),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('No projects assigned yet.'), findsOneWidget);
  });

  testWidgets('long project title wraps on a narrow screen', (tester) async {
    setScreenSize(tester, const Size(320, 640));
    const title =
        'Northern Foundation and Site Clearing Project With Extended Descriptive Name';
    final service = LayoutService(title);
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(body: SiteEngineerProjectsScreen(service: service)),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text(title), findsOneWidget);
    expect(tester.widget<Text>(find.text(title)).maxLines, isNull);
    expect(tester.takeException(), isNull);

    await tester.pumpWidget(
      MaterialApp(
        home: SiteProjectDetailScreen(
          service: service,
          project: {'id': 'project-1', 'name': title},
        ),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text(title), findsNWidgets(2));
    expect(
      tester
          .widgetList<Text>(find.text(title))
          .any((text) => text.maxLines == null),
      isTrue,
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('activity fields and actions fit a narrow screen', (
    tester,
  ) async {
    setScreenSize(tester, const Size(320, 640));
    await tester.pumpWidget(
      MaterialApp(
        home: SiteActivityScreen(
          service: LayoutService('Project'),
          project: const {'id': 'project-1', 'name': 'Project'},
          siteId: 'site-1',
          activity: const {
            'id': 'activity-1',
            'name': 'Site Clearing',
            'progressPercent': 31,
          },
        ),
      ),
    );
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    final fields = find.byType(TextField);
    final objectiveRect = tester.getRect(fields.at(2));
    final dropdownRect = tester.getRect(
      find.byType(DropdownButtonFormField<String>),
    );
    final quantityRect = tester.getRect(fields.at(4));
    final unitRect = tester.getRect(fields.at(5));
    expect(dropdownRect.top - objectiveRect.bottom, greaterThanOrEqualTo(12));
    expect(unitRect.top, greaterThan(quantityRect.bottom));
    await tester.ensureVisible(find.text('Submit and start planning'));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
  });

  testWidgets('quantity and unit share a row on a wider screen', (
    tester,
  ) async {
    setScreenSize(tester, const Size(600, 850));
    await tester.pumpWidget(
      MaterialApp(
        home: SiteActivityScreen(
          service: LayoutService('Project'),
          project: const {'id': 'project-1', 'name': 'Project'},
          siteId: 'site-1',
          activity: const {
            'id': 'activity-1',
            'name': 'Site Clearing',
            'progressPercent': 31,
          },
        ),
      ),
    );
    await tester.pumpAndSettle();
    final fields = find.byType(TextField);
    final quantityRect = tester.getRect(fields.at(4));
    final unitRect = tester.getRect(fields.at(5));
    expect(unitRect.top, quantityRect.top);
    expect(tester.takeException(), isNull);
  });

  testWidgets('long request objective wraps on a narrow screen', (
    tester,
  ) async {
    setScreenSize(tester, const Size(320, 640));
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: SiteRequestsScreen(service: LayoutService('Project')),
        ),
      ),
    );
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
  });

  testWidgets('activity actions remain reachable with larger text', (
    tester,
  ) async {
    setScreenSize(tester, const Size(320, 640));
    await tester.pumpWidget(
      MaterialApp(
        builder: (context, child) => MediaQuery(
          data: MediaQuery.of(context)
              .copyWith(textScaler: const TextScaler.linear(1.4)),
          child: child!,
        ),
        home: SiteActivityScreen(
          service: LayoutService('Project'),
          project: const {'id': 'project-1', 'name': 'Project'},
          siteId: 'site-1',
          activity: const {
            'id': 'activity-1',
            'name': 'Site Clearing',
            'progressPercent': 31,
          },
        ),
      ),
    );
    await tester.pumpAndSettle();
    await tester.ensureVisible(find.text('Submit and start planning'));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
  });
}
