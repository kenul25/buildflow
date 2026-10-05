import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:buildflow_mobile/screens/home/operations_navigation.dart';

void main() {
  testWidgets('four tabs fit a narrow screen with safe area and large text', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(320, 640);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    var selected = -1;
    await tester.pumpWidget(
      MaterialApp(
        home: MediaQuery(
          data: const MediaQueryData(
            size: Size(320, 640),
            padding: EdgeInsets.only(bottom: 34),
            textScaler: TextScaler.linear(1.5),
          ),
          child: Scaffold(
            bottomNavigationBar: OperationsNavigation(
              selectedIndex: 0,
              onSelect: (index) => selected = index,
            ),
          ),
        ),
      ),
    );
    for (final label in ['Home', 'Projects', 'Schedule', 'More']) {
      expect(find.text(label), findsOneWidget);
    }
    await tester.tap(find.text('Schedule'));
    expect(selected, 2);
    await tester.tap(find.text('More'));
    expect(selected, 3);
    expect(tester.takeException(), isNull);
  });

  testWidgets('central action is a 56 pixel button and opens request action', (
    tester,
  ) async {
    var creates = 0;
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          floatingActionButton: RequestActionButton(onPressed: () => creates++),
          floatingActionButtonLocation: const RequestActionLocation(),
          bottomNavigationBar: OperationsNavigation(
            selectedIndex: 0,
            onSelect: (_) {},
          ),
        ),
      ),
    );
    expect(
      tester.getSize(find.byType(FloatingActionButton)),
      const Size(56, 56),
    );
    final bar = tester.widget<BottomAppBar>(find.byType(BottomAppBar));
    expect(bar.shape, isA<RaisedActionCradle>());
    expect(bar.notchMargin, 6);
    final buttonRect = tester.getRect(find.byType(FloatingActionButton));
    final barRect = tester.getRect(find.byType(BottomAppBar));
    expect(buttonRect.center.dx, barRect.center.dx);
    const offset = RequestActionLocation.downwardOffset;
    expect(buttonRect.center.dy, barRect.top + offset);
    final outline = bar.shape!.getOuterPath(
      const Rect.fromLTWH(0, 0, 320, 72),
      Rect.fromLTWH(132, offset - 28, 56, 56).inflate(bar.notchMargin),
    );
    expect(outline.contains(const Offset(160, 1)), isTrue);
    expect(outline.contains(const Offset(160, offset - 33)), isTrue);
    expect(outline.contains(const Offset(160, offset - 35)), isFalse);
    // The circular cradle keeps the same six-pixel rim at its diagonals.
    expect(outline.contains(const Offset(183.33, offset - 23.33)), isTrue);
    expect(outline.contains(const Offset(184.75, offset - 24.75)), isFalse);
    expect(outline.contains(const Offset(136.67, offset - 23.33)), isTrue);
    expect(outline.contains(const Offset(135.25, offset - 24.75)), isFalse);
    expect(outline.contains(const Offset(126, 1)), isTrue);
    expect(outline.contains(const Offset(194, 1)), isTrue);
    expect(buttonRect.bottom, lessThan(barRect.top + 72));
    expect(outline.contains(const Offset(40, -1)), isFalse);
    expect(outline.contains(const Offset(40, 1)), isTrue);
    await tester.tap(find.byTooltip('Create new request'));
    expect(creates, 1);
    expect(tester.takeException(), isNull);
  });
}
