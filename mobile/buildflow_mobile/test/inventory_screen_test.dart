import 'package:buildflow_mobile/screens/inventory/inventory_screen.dart';
import 'package:buildflow_mobile/services/inventory_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

class EmptyInventoryService extends Fake implements InventoryService {
  @override
  Future<List<Map<String, dynamic>>> materials({String? search}) async => [];
}

void main() {
  testWidgets('inventory screen shows a clear empty state', (tester) async {
    await tester.pumpWidget(
      MaterialApp(home: Scaffold(body: InventoryScreen(service: EmptyInventoryService()))),
    );
    await tester.pumpAndSettle();
    expect(find.text('No inventory is available yet.'), findsOneWidget);
  });
}