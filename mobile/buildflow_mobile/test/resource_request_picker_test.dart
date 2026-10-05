import 'package:buildflow_mobile/screens/projects/site_engineer_projects_screen.dart';
import 'package:buildflow_mobile/services/project_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

class CatalogService extends Fake implements ProjectService {
  Map<String, dynamic>? submitted;
  @override
  Future<List<Map<String, dynamic>>> progressHistory(String id) async => [];
  @override
  Future<List<Map<String, dynamic>>> requests() async => [];
  @override
  Future<List<Map<String, dynamic>>> resourceOptions(String kind) async =>
      kind == 'Material'
      ? [
          {'name': 'Cement', 'unit': 'bags'},
          {'name': 'Steel', 'unit': 'metric tons'},
        ]
      : kind == 'Equipment'
      ? [
          {'name': 'Excavator', 'category': 'Earthmoving'},
        ]
      : [
          {'name': 'Masonry'},
        ];
  @override
  Future<Map<String, dynamic>> submitRequest(Map<String, dynamic> body) async {
    submitted = body;
    return {'id': 'request'};
  }

  @override
  Future<Map<String, dynamic>> startPlanning(String id) async => {
    'id': 'workflow',
    'status': 'Failed',
  };
}

Finder field(String label) => find.byWidgetPredicate(
  (widget) => widget is TextField && widget.decoration?.labelText == label,
);
Finder dropdown(String label) => find.byWidgetPredicate(
  (widget) =>
      widget is DropdownButtonFormField<String> &&
      widget.decoration.labelText == label,
);

Future<void> open(WidgetTester tester, CatalogService service) async {
  await tester.pumpWidget(
    MaterialApp(
      home: SiteActivityScreen(
        service: service,
        project: const {'id': 'project'},
        siteId: 'site',
        activity: const {
          'id': 'activity',
          'name': 'Clearing',
          'progressPercent': 0,
        },
        requestOnly: true,
      ),
    ),
  );
  await tester.pumpAndSettle();
}

Future<void> choose(WidgetTester tester, String label, String value) async {
  await tester.ensureVisible(dropdown(label));
  await tester.tap(dropdown(label));
  await tester.pumpAndSettle();
  await tester.tap(find.text(value).last);
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('material search locks the master unit and submits it', (
    tester,
  ) async {
    final service = CatalogService();
    await open(tester, service);
    await tester.ensureVisible(field('Resource name'));
    await tester.tap(field('Resource name'));
    await tester.pumpAndSettle();
    await tester.enterText(field('Search resources'), 'cem');
    await tester.pumpAndSettle();
    expect(find.text('Steel'), findsNothing);
    await tester.tap(find.text('Cement'));
    await tester.pumpAndSettle();
    final unit = tester.widget<TextField>(field('Unit'));
    expect(unit.readOnly, isTrue);
    expect(unit.controller!.text, 'bags');
    await tester.ensureVisible(field('Objective'));
    await tester.enterText(field('Objective'), 'Materials for site clearing');
    await tester.ensureVisible(find.text('Submit and start planning'));
    await tester.tap(find.text('Submit and start planning'));
    await tester.pumpAndSettle();
    expect(service.submitted!['items'], [
      {'kind': 'Material', 'name': 'Cement', 'quantity': 1.0, 'unit': 'bags'},
    ]);
    expect(
      find.textContaining('Request saved, but planning failed'),
      findsOneWidget,
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'equipment submits count separately and workforce has labor units',
    (tester) async {
      final service = CatalogService();
      await open(tester, service);
      await choose(tester, 'Resource type', 'Equipment');
      expect(
        tester.widget<TextField>(field('Resource name')).controller!.text,
        isEmpty,
      );
      await tester.ensureVisible(field('Resource name'));
      await tester.tap(field('Resource name'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Excavator'));
      await tester.pumpAndSettle();
      await tester.ensureVisible(field('Number of machines'));
      await tester.enterText(field('Number of machines'), '2');
      await choose(tester, 'Unit', 'hours');
      await tester.ensureVisible(field('Total usage / effort'));
      await tester.enterText(field('Total usage / effort'), '6');
      await tester.ensureVisible(field('Objective'));
      await tester.enterText(field('Objective'), 'Equipment for site clearing');
      await tester.ensureVisible(find.text('Submit and start planning'));
      await tester.tap(find.text('Submit and start planning'));
      await tester.pumpAndSettle();
      expect(service.submitted!['items'], [
        {
          'kind': 'Equipment',
          'name': 'Excavator',
          'quantity': 6.0,
          'unit': 'hours',
          'resourceCount': 2,
        },
      ]);
      await choose(tester, 'Resource type', 'Workforce');
      expect(
        tester.widget<TextField>(field('Worker skill')).controller!.text,
        isEmpty,
      );
      final units = tester
          .widget<DropdownButton<String>>(
            find.descendant(
              of: dropdown('Unit'),
              matching: find.byType(DropdownButton<String>),
            ),
          )
          .items!
          .map((item) => item.value)
          .toList();
      expect(units, ['mandays', 'workers', 'hours', 'shifts']);
      await choose(tester, 'Unit', 'workers');
      expect(field('Number of workers'), findsNothing);
      expect(field('Quantity'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );
}
