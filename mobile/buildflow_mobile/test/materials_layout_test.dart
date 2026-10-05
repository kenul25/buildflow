import 'package:buildflow_mobile/screens/inventory/inventory_screen.dart';
import 'package:buildflow_mobile/services/inventory_service.dart';
import 'package:buildflow_mobile/services/api_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

class MaterialsService extends Fake implements InventoryService {
  String? savedAction;
  double? savedQuantity;
  @override
  Future<List<Map<String, dynamic>>> materials({String? search}) async => [
    {
      'id': 'm1',
      'name': 'Concrete M20',
      'category': 'Concrete',
      'unit': 'bags',
      'currentStock': 100,
      'reservedStock': 20,
      'availableStock': 80,
      'warehouseId': 'w1',
      'warehouseName': 'North warehouse',
    },
    {
      'id': 'm2',
      'name': 'Steel Fe500',
      'category': 'Steel',
      'unit': 'metric tons',
      'currentStock': 10,
      'reservedStock': 2,
      'availableStock': 8,
      'warehouseId': 'w1',
      'warehouseName': 'North warehouse',
    },
    {
      'id': 'm3',
      'name': 'Steel Fe415',
      'category': 'Steel',
      'unit': 'metric tons',
      'currentStock': 0,
      'reservedStock': 0,
      'availableStock': 0,
      'warehouseId': 'w2',
      'warehouseName': 'South warehouse',
    },
  ];
  @override
  Future<List<Map<String, dynamic>>> warehouses() async => [
    {'id': 'w1', 'name': 'North warehouse'},
    {'id': 'w2', 'name': 'South warehouse'},
  ];
  @override
  Future<List<Map<String, dynamic>>> reservations() async => [
    {
      'id': 'r1',
      'materialId': 'm2',
      'materialName': 'Steel Fe500',
      'quantity': 2,
      'status': 'Active',
      'expiresAt': '2026-10-06T10:00:00Z',
    },
  ];
  @override
  Future<List<Map<String, dynamic>>> movements() async => [
    {
      'id': 'move1',
      'materialId': 'm2',
      'materialName': 'Steel Fe500',
      'quantity': 10,
      'type': 'Receive',
      'stockAfter': 10,
      'createdAt': '2026-10-05T10:00:00Z',
    },
  ];
  @override
  Future<List<Map<String, dynamic>>> alerts() async => [];
  @override
  Future<Map<String, dynamic>> receive(
    String materialId,
    double quantity, {
    String? reference,
  }) async {
    savedAction = 'Receive';
    savedQuantity = quantity;
    return {};
  }
}

Future<void> openMaterials(
  WidgetTester tester,
  MaterialsService service, {
  bool manage = false,
  double scale = 1,
}) async {
  await tester.pumpWidget(
    MaterialApp(
      builder: (context, child) => MediaQuery(
        data: MediaQuery.of(context)
            .copyWith(textScaler: TextScaler.linear(scale)),
        child: child!,
      ),
      home: Scaffold(
        body: InventoryScreen(service: service, canManageStock: manage),
      ),
    ),
  );
  await tester.pumpAndSettle();
}

class PagedInventoryApi extends Fake implements ApiService {
  final paths = <String>[];
  @override
  Future<dynamic> request(
    String method,
    String path, {
    Object? body,
    bool authenticated = true,
  }) async {
    paths.add(path);
    return {
      'items': [
        {'id': path.contains('page=1&') ? 'first' : 'second'},
      ],
      'total': 2,
    };
  }
}

void main() {
  testWidgets(
    'material filters apply to stock reservations and movements on a narrow screen',
    (tester) async {
      tester.view.devicePixelRatio = 1;
      tester.view.physicalSize = const Size(320, 720);
      addTearDown(tester.view.resetDevicePixelRatio);
      addTearDown(tester.view.resetPhysicalSize);
      await openMaterials(tester, MaterialsService(), scale: 1.4);
      expect(find.byType(SegmentedButton<int>), findsOneWidget);
      expect(find.byType(ChoiceChip), findsNothing);
      expect(find.text('Stock actions'), findsNothing);
      await tester.enterText(find.byType(TextField), 'Fe500');
      await tester.pumpAndSettle();
      expect(find.text('Steel Fe500'), findsOneWidget);
      expect(find.text('Concrete M20'), findsNothing);
      FocusManager.instance.primaryFocus?.unfocus();
      await tester.tap(find.text('Reservations'));
      await tester.pumpAndSettle();
      expect(find.text('Steel Fe500'), findsOneWidget);
      await tester.tap(find.text('Movements'));
      await tester.pumpAndSettle();
      expect(find.text('Steel Fe500'), findsOneWidget);
      await tester.tap(find.byTooltip('Clear search'));
      await tester.pumpAndSettle();
      await tester.tap(find.byType(DropdownButtonFormField<String>));
      await tester.pumpAndSettle();
      await tester.tap(find.text('South warehouse').last);
      await tester.pumpAndSettle();
      expect(find.text('No matching records'), findsOneWidget);
      await tester.tap(find.text('Stock'));
      await tester.pumpAndSettle();
      expect(find.text('Steel Fe415'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('category filtering is independent of the search text', (
    tester,
  ) async {
    await openMaterials(tester, MaterialsService());
    await tester.tap(find.byTooltip('Filter materials'));
    await tester.pumpAndSettle();
    await tester.tap(find.byType(DropdownButtonFormField<String>).last);
    await tester.pumpAndSettle();
    await tester.tap(find.text('In stock').last);
    await tester.pumpAndSettle();
    await tester.tap(find.byType(DropdownButtonFormField<String>).at(1));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Steel').last);
    await tester.pumpAndSettle();
    await tester.tap(find.text('Apply filters'));
    await tester.pumpAndSettle();
    expect(find.text('Steel Fe500'), findsOneWidget);
    expect(find.text('Concrete M20'), findsNothing);
    expect(find.text('Steel Fe415'), findsNothing);
  });

  testWidgets(
    'stock changes require choosing an action and confirming a valid quantity',
    (tester) async {
      final service = MaterialsService();
      await openMaterials(tester, service, manage: true);
      expect(find.text('Receive'), findsNothing);
      await tester.ensureVisible(find.text('Stock actions').first);
      await tester.pumpAndSettle();
      await tester.tap(find.text('Stock actions').first);
      await tester.pumpAndSettle();
      await tester.tap(find.text('Receive'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Confirm receive'));
      await tester.pumpAndSettle();
      expect(service.savedAction, isNull);
      expect(find.text('Enter a positive quantity'), findsOneWidget);
      await tester.enterText(find.byType(TextFormField), '2.5');
      await tester.tap(find.text('Confirm receive'));
      await tester.pumpAndSettle();
      expect(service.savedAction, 'Receive');
      expect(service.savedQuantity, 2.5);
      expect(find.text('Confirm receive'), findsNothing);
      expect(tester.takeException(), isNull);
    },
  );

  test(
    'inventory search loads every page instead of stopping at 100 records',
    () async {
      final api = PagedInventoryApi();
      final rows = await InventoryService(api).materials(search: 'Fe500');
      expect(rows.length, 2);
      expect(api.paths.length, 2);
      expect(api.paths.last, contains('page=2&'));
      expect(api.paths.last, contains('search=Fe500'));
    },
  );
}
