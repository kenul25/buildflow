import 'package:flutter_test/flutter_test.dart';
import 'package:buildflow_mobile/main.dart';

void main() {
  testWidgets('BuildFlow application starts', (WidgetTester tester) async {
    await tester.pumpWidget(const BuildFlowApp());
    expect(find.text('BuildFlow AI'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
