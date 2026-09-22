import 'package:flutter/material.dart';

void main() {
  runApp(const BuildFlowApp());
}

class BuildFlowApp extends StatelessWidget {
  const BuildFlowApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'BuildFlow AI',
      home: Scaffold(
        appBar: AppBar(title: const Text('BuildFlow AI')),
        body: const SafeArea(
          child: Padding(
            padding: EdgeInsets.all(24),
            child: Text(
              'Construction site operations. Feature development is coming next.',
            ),
          ),
        ),
      ),
    );
  }
}
