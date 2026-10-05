import 'package:buildflow_mobile/screens/scheduling/scheduling_screen.dart';
import 'package:buildflow_mobile/screens/scheduling/scheduling_action_sheet.dart';
import 'package:buildflow_mobile/services/api_service.dart';
import 'package:buildflow_mobile/services/scheduling_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

class FormApi extends Fake implements ApiService {
  @override
  Future<dynamic> request(
    String method,
    String path, {
    Object? body,
    bool authenticated = true,
  }) async => {
    'items': [
      {'id': 'activity-1', 'name': 'Site Clearing'},
    ],
    'total': 1,
  };
}

class FormScheduling extends Fake implements SchedulingService {
  @override
  final ApiService api = FormApi();
  bool fail = false;
  bool equipmentAvailable = true;
  Map<String, dynamic>? requestBody, reportBody;
  @override
  Future<List<Map<String, dynamic>>> list(String kind) async =>
      kind == 'equipment' && equipmentAvailable
      ? [
          {'id': 'equipment-1', 'name': 'Excavator'},
        ]
      : [];
  @override
  Future<void> requestEquipment(Map<String, dynamic> body) async {
    if (fail) throw Exception('Server unavailable');
    requestBody = body;
  }

  @override
  Future<void> report(Map<String, dynamic> body) async {
    reportBody = body;
  }
}

Future<void> openForm(
  WidgetTester tester,
  FormScheduling service,
  String action, {
  double scale = 1,
}) async {
  await tester.pumpWidget(
    MaterialApp(
      builder: (context, child) => MediaQuery(
        data: MediaQuery.of(context)
            .copyWith(textScaler: TextScaler.linear(scale)),
        child: child!,
      ),
      home: Scaffold(body: SchedulingScreen(service: service)),
    ),
  );
  await tester.pumpAndSettle();
  await tester.tap(find.text(action));
  await tester.pumpAndSettle();
}

Future<void> chooseFormOption(
  WidgetTester tester,
  int index,
  String name,
) async {
  final finder = find.byType(DropdownButtonFormField<String>).at(index);
  FocusManager.instance.primaryFocus?.unfocus();
  await tester.pumpAndSettle();
  await tester.ensureVisible(finder);
  await tester.pumpAndSettle();
  await tester.tap(finder);
  await tester.pumpAndSettle();
  await tester.tap(find.text(name).last);
  await tester.pumpAndSettle();
}

void main() {
  testWidgets(
    'equipment form validates required fields and keeps values after a failed save',
    (tester) async {
      final service = FormScheduling();
      await openForm(tester, service, 'Request');
      expect(find.byType(SchedulingActionSheet), findsOneWidget);
      await tester.tap(find.text('Save request'));
      await tester.pumpAndSettle();
      expect(service.requestBody, isNull);
      expect(
        find.text('Enter a title with at least 2 characters'),
        findsOneWidget,
      );
      await tester.enterText(
        find.byType(TextFormField).first,
        'Excavator for clearing',
      );
      await chooseFormOption(tester, 0, 'Site Clearing');
      await chooseFormOption(tester, 1, 'Excavator');
      service.fail = true;
      await tester.tap(find.text('Save request'));
      await tester.pumpAndSettle();
      expect(find.byType(SchedulingActionSheet), findsOneWidget);
      expect(find.text('Excavator for clearing'), findsOneWidget);
      await tester.ensureVisible(find.textContaining('Server unavailable'));
      await tester.pumpAndSettle();
      service.fail = false;
      await tester.tap(find.text('Save request'));
      await tester.pumpAndSettle();
      expect(service.requestBody!['activityId'], 'activity-1');
      expect(service.requestBody!['equipmentId'], 'equipment-1');
      expect(find.byType(SchedulingActionSheet), findsNothing);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'issue form fits a narrow screen with keyboard and allows no equipment',
    (tester) async {
      tester.view.devicePixelRatio = 1;
      tester.view.physicalSize = const Size(320, 720);
      addTearDown(tester.view.resetDevicePixelRatio);
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetViewInsets);
      final service = FormScheduling();
      await openForm(tester, service, 'Report', scale: 1.4);
      tester.view.viewInsets = const FakeViewPadding(bottom: 260);
      await tester.pumpAndSettle();
      expect(tester.takeException(), isNull);
      await tester.ensureVisible(find.byType(TextFormField).first);
      await tester.pumpAndSettle();
      await tester.enterText(
        find.byType(TextFormField).first,
        'Road access blocked',
      );
      await chooseFormOption(tester, 0, 'Site Clearing');
      await tester.tap(find.text('Report issue'));
      await tester.pumpAndSettle();
      expect(service.reportBody!['name'], 'Road access blocked');
      expect(service.reportBody!['equipmentId'], isNull);
      expect(find.byType(SchedulingActionSheet), findsNothing);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'missing equipment gives guidance and prevents an invalid request',
    (tester) async {
      final service = FormScheduling()..equipmentAvailable = false;
      await openForm(tester, service, 'Request');
      expect(
        tester
            .widget<FilledButton>(
              find.widgetWithText(FilledButton, 'Save request'),
            )
            .onPressed,
        isNull,
      );
      expect(
        find.textContaining('No equipment available. Add equipment'),
        findsOneWidget,
      );
      await tester.tap(find.text('Cancel'));
      await tester.pumpAndSettle();
      expect(find.byType(SchedulingActionSheet), findsNothing);
      expect(service.requestBody, isNull);
    },
  );
}
