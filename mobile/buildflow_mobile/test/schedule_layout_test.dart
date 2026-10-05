import 'package:buildflow_mobile/screens/scheduling/scheduling_screen.dart';
import 'package:buildflow_mobile/services/scheduling_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

class ScheduleLayoutService extends Fake implements SchedulingService {
  bool filled = false;
  bool cancelled = false;
  int loads = 0;
  @override
  Future<List<Map<String, dynamic>>> list(String kind) async {
    loads++;
    if (!filled) return [];
    return switch (kind) {
      'worker-assignments' => [
        {'id': 'assignment', 'name': 'Foundation crew', 'status': 'Completed'},
      ],
      'equipment-reservations' => [
        {'id': 'booking', 'name': 'Crane booking', 'status': 'Approved'},
      ],
      'equipment' => [
        {'id': 'equipment', 'name': 'Mobile crane', 'status': 'Operational'},
      ],
      'equipment-requests' =>
        cancelled
            ? []
            : [
                {
                  'id': 'request',
                  'name': 'Draft crane request',
                  'status': 'Draft',
                },
              ],
      'schedules' => [
        {'id': 'schedule', 'name': 'Foundation schedule', 'status': 'Planned'},
      ],
      'issues' => [
        {'id': 'issue', 'name': 'Access road delay', 'status': 'Open'},
      ],
      _ => [],
    };
  }

  @override
  Future<void> cancelRequest(String id) async {
    cancelled = true;
  }
}

void main() {
  testWidgets(
    'fixed schedule tabs and quick actions fit a narrow screen with large text',
    (tester) async {
      tester.view.devicePixelRatio = 1;
      tester.view.physicalSize = const Size(320, 640);
      addTearDown(tester.view.resetDevicePixelRatio);
      addTearDown(tester.view.resetPhysicalSize);
      final service = ScheduleLayoutService();
      await tester.pumpWidget(
        MaterialApp(
          builder: (context, child) => MediaQuery(
            data: MediaQuery.of(context)
                .copyWith(textScaler: const TextScaler.linear(1.5)),
            child: child!,
          ),
          home: Scaffold(body: SchedulingScreen(service: service)),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.byType(SegmentedButton<int>), findsOneWidget);
      expect(find.byType(ChoiceChip), findsNothing);
      expect(find.text('No assignments yet'), findsOneWidget);
      expect(
        tester.getCenter(find.text('Scan')).dy,
        tester.getCenter(find.text('Request')).dy,
      );
      expect(
        tester.getCenter(find.text('Scan')).dy,
        tester.getCenter(find.text('Report')).dy,
      );
      expect(tester.takeException(), isNull);
      await tester.tap(find.text('Equipment'));
      await tester.pumpAndSettle();
      expect(find.text('No equipment activity yet'), findsOneWidget);
      await tester.tap(find.text('Timeline'));
      await tester.pumpAndSettle();
      expect(find.text('Your timeline is clear'), findsOneWidget);
      expect(tester.takeException(), isNull);
      service.filled = true;
      await tester.ensureVisible(find.text('Refresh'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Refresh'));
      await tester.pumpAndSettle();
      expect(service.loads, 12);
      expect(find.text('Foundation schedule'), findsOneWidget);
      expect(find.text('Access road delay'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'equipment tab keeps bookings catalog and cancellable draft requests accessible',
    (tester) async {
      final service = ScheduleLayoutService()..filled = true;
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(body: SchedulingScreen(service: service)),
        ),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.text('Equipment'));
      await tester.pumpAndSettle();
      expect(find.text('Crane booking'), findsOneWidget);
      await tester.ensureVisible(find.text('Cancel draft request'));
      await tester.tap(find.text('Cancel draft request'));
      await tester.pumpAndSettle();
      expect(service.cancelled, isTrue);
      expect(find.text('Draft crane request'), findsNothing);
      await tester.ensureVisible(find.text('Mobile crane'));
      expect(find.text('Mobile crane'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );
}
