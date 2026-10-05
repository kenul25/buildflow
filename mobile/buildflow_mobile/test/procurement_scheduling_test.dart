import 'package:buildflow_mobile/screens/procurement/procurement_screen.dart';
import 'package:buildflow_mobile/screens/scheduling/scheduling_screen.dart';
import 'package:buildflow_mobile/services/procurement_service.dart';
import 'package:buildflow_mobile/services/scheduling_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

class SiteProcurement extends Fake implements ProcurementService {
  String deliveryStatus = 'Pending';
  @override
  Future<List<Map<String, dynamic>>> purchaseRequests() async => [];
  @override
  Future<List<Map<String, dynamic>>> purchaseOrders() async => [];
  @override
  Future<List<Map<String, dynamic>>> deliveries() async => [
    {
      'id': 1,
      'materialName': 'Cement',
      'quantity': 2.5,
      'status': deliveryStatus,
    },
  ];
  @override
  Future<dynamic> updateDeliveryStatus(String id, String status) async {
    deliveryStatus = status;
    return {};
  }
}

class SiteScheduling extends Fake implements SchedulingService {
  String assignmentStatus = 'Upcoming';
  @override
  Future<List<Map<String, dynamic>>> list(String kind) async =>
      kind == 'worker-assignments'
      ? [
          {
            'id': 'assignment',
            'name': 'Masonry work',
            'status': assignmentStatus,
          },
        ]
      : [];
  @override
  Future<void> status(String id, String status, String? notes) async {
    assignmentStatus = status;
  }
}

void main() {
  testWidgets('site delivery confirmation follows receipt then completion', (
    tester,
  ) async {
    final service = SiteProcurement();
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(body: ProcurementScreen(service: service)),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('Approve request'), findsNothing);
    await tester.tap(find.text('Deliveries'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Confirm received'));
    await tester.pumpAndSettle();
    expect(service.deliveryStatus, 'Received');
    expect(find.text('Complete delivery'), findsOneWidget);
    await tester.tap(find.text('Complete delivery'));
    await tester.pumpAndSettle();
    expect(service.deliveryStatus, 'Completed');
    expect(find.text('Confirm received'), findsNothing);
  });
  testWidgets(
    'site assignment can start and complete without exposing workforce CRUD',
    (tester) async {
      final service = SiteScheduling();
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(body: SchedulingScreen(service: service)),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.text('Masonry work'), findsOneWidget);
      await tester.tap(find.text('Start work'));
      await tester.pumpAndSettle();
      expect(service.assignmentStatus, 'InProgress');
      await tester.tap(find.text('Complete'));
      await tester.pumpAndSettle();
      expect(service.assignmentStatus, 'Completed');
      expect(find.text('Start work'), findsNothing);
    },
  );
}
