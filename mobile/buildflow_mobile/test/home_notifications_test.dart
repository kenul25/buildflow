import 'package:buildflow_mobile/models/auth_session.dart';
import 'package:buildflow_mobile/providers/auth_provider.dart';
import 'package:buildflow_mobile/providers/theme_provider.dart';
import 'package:buildflow_mobile/screens/home/home_screen.dart';
import 'package:buildflow_mobile/services/api_service.dart';
import 'package:buildflow_mobile/services/inventory_service.dart';
import 'package:buildflow_mobile/services/procurement_service.dart';
import 'package:buildflow_mobile/services/project_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

class HomeApi extends Fake implements ApiService {
  bool read = false, fail = false;
  @override
  Future<dynamic> request(
    String method,
    String path, {
    Object? body,
    bool authenticated = true,
  }) async {
    if (path == '/dashboard') {
      return {
        'activeProjects': 2,
        'activeActivities': 4,
        'pendingRequests': 1,
        'approvedPlans': 3,
      };
    }
    if (path == '/notifications') {
      if (fail) throw Exception('offline');
      return {
        'unreadCount': read ? 0 : 1,
        'items': [
          {
            'id': 'notice',
            'title': 'Resource plan approved',
            'message': 'Demo site materials',
            'status': 'Approved',
            'requestId': 'request',
            'workflowId': 'workflow',
            'createdAt': '2026-10-05T01:00:00Z',
            'isRead': read,
          },
        ],
      };
    }
    if (path == '/notifications/notice/read' ||
        path == '/notifications/read-all') {
      read = true;
      return null;
    }
    if (path == '/construction/resource-requests/request') {
      return {
        'objective': 'Demo site materials',
        'items': [
          {'name': 'Cement', 'quantity': 6, 'unit': 'bags'},
        ],
      };
    }
    if (path == '/construction/planning-workflows/workflow') {
      return {'status': 'Approved'};
    }
    throw Exception('Unexpected request: $path');
  }
}

class HomeAuth extends Fake implements AuthProvider {
  @override
  AppUser get user => const AppUser(
    id: 'user',
    fullName: 'Kamal Perera',
    email: 'demo@example.invalid',
    roles: ['SiteEngineer'],
  );
}

class HomeTheme extends Fake implements ThemeProvider {}

Widget app(HomeApi api) => MaterialApp(
  home: HomeScreen(
    authProvider: HomeAuth(),
    themeProvider: HomeTheme(),
    projectService: ProjectService(api),
    inventoryService: InventoryService(api),
    procurementService: ProcurementService(api),
  ),
);

void main() {
  testWidgets(
    'home greeting uses initials and notification opens the approved request',
    (tester) async {
      final api = HomeApi();
      await tester.pumpWidget(app(api));
      await tester.pumpAndSettle();
      expect(find.text('KP'), findsOneWidget);
      expect(find.text('Hi, Kamal'), findsOneWidget);
      expect(find.text('Site Engineer'), findsOneWidget);
      expect(find.text('Home'), findsOneWidget); // Only the navigation label.
      await tester.tap(find.byTooltip('Notifications, 1 unread'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Resource plan approved'));
      await tester.pumpAndSettle();
      expect(find.text('Current status: Approved'), findsOneWidget);
      expect(find.text('6 bags'), findsOneWidget);
      expect(api.read, isTrue);
      await tester.pageBack();
      await tester.pumpAndSettle();
      await tester.pageBack();
      await tester.pumpAndSettle();
      expect(find.byTooltip('Notifications'), findsOneWidget);
      expect(tester.takeException(), isNull);
      await tester.pumpWidget(const SizedBox());
    },
  );
  testWidgets(
    'notification load errors are retryable and mark all updates the badge',
    (tester) async {
      final api = HomeApi()..fail = true;
      await tester.pumpWidget(app(api));
      await tester.pumpAndSettle();
      await tester.tap(find.byTooltip('Notifications'));
      await tester.pumpAndSettle();
      expect(
        find.text('Could not load notifications. Please try again.'),
        findsOneWidget,
      );
      api.fail = false;
      await tester.tap(find.text('Retry'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Mark all read'));
      await tester.pumpAndSettle();
      expect(api.read, isTrue);
      await tester.pageBack();
      await tester.pumpAndSettle();
      expect(find.byTooltip('Notifications'), findsOneWidget);
      await tester.pumpWidget(const SizedBox());
    },
  );
  testWidgets('home fits a narrow display with large text', (tester) async {
    tester.view.physicalSize = const Size(320, 720);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    await tester.pumpWidget(
      MaterialApp(
        builder: (context, child) => MediaQuery(
          data: MediaQuery.of(context)
              .copyWith(textScaler: const TextScaler.linear(1.5)),
          child: child!,
        ),
        home: HomeScreen(
          authProvider: HomeAuth(),
          themeProvider: HomeTheme(),
          projectService: ProjectService(HomeApi()),
          inventoryService: InventoryService(HomeApi()),
          procurementService: ProcurementService(HomeApi()),
        ),
      ),
    );
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    await tester.pumpWidget(const SizedBox());
  });
}
