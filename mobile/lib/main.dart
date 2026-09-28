import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  runApp(const BuildFlowApp());
}

class BuildFlowApp extends StatelessWidget {
  const BuildFlowApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'BuildFlow AI',
      theme: ThemeData(
        useMaterial3: true,
        colorSchemeSeed: Colors.indigo,
      ),
      home: const HomePage(),
    );
  }
}

/* =========================================================
   API SERVICE
========================================================= */

class ApiService {
  static const String baseUrl = 'http://10.0.2.2:5144/api';

  Future<String?> getToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString('token');
  }

  Future<void> setToken(String token) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('token', token);
  }

  Future<Map<String, String>> headers() async {
    final token = await getToken();

    return {
      'Content-Type': 'application/json',
      if (token != null && token.isNotEmpty)
        'Authorization': 'Bearer $token',
    };
  }

  Future<dynamic> get(
    String path, {
    Map<String, String>? params,
  }) async {
    final uri = Uri.parse('$baseUrl$path').replace(
      queryParameters: params,
    );

    final response = await http.get(
      uri,
      headers: await headers(),
    );

    return _handle(response);
  }

  Future<dynamic> post(
    String path, {
    Map<String, dynamic>? body,
  }) async {
    final response = await http.post(
      Uri.parse('$baseUrl$path'),
      headers: await headers(),
      body: body == null ? null : jsonEncode(body),
    );

    return _handle(response);
  }

  Future<dynamic> put(
    String path, {
    Map<String, dynamic>? body,
  }) async {
    final response = await http.put(
      Uri.parse('$baseUrl$path'),
      headers: await headers(),
      body: body == null ? null : jsonEncode(body),
    );

    return _handle(response);
  }

  Future<dynamic> delete(String path) async {
    final response = await http.delete(
      Uri.parse('$baseUrl$path'),
      headers: await headers(),
    );

    return _handle(response);
  }

  dynamic _handle(http.Response response) {
    dynamic data;

    if (response.body.isNotEmpty) {
      try {
        data = jsonDecode(response.body);
      } catch (_) {
        data = response.body;
      }
    }

    if (response.statusCode < 200 || response.statusCode >= 300) {
      String message = 'Request failed (${response.statusCode})';

      if (data is Map<String, dynamic>) {
        message =
            data['message']?.toString() ??
            data['title']?.toString() ??
            data['error']?.toString() ??
            message;
      }

      throw Exception(message);
    }

    return data;
  }
}

final api = ApiService();

/* =========================================================
   COMMON HELPERS
========================================================= */

List<dynamic> extractItems(dynamic data) {
  if (data is List) return data;

  if (data is Map<String, dynamic>) {
    final items = data['items'];

    if (items is List) {
      return items;
    }
  }

  return [];
}

String textValue(dynamic value) {
  return value?.toString() ?? '';
}

/* =========================================================
   HOME
========================================================= */

class HomePage extends StatefulWidget {
  const HomePage({super.key});

  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  int index = 0;

  final pages = const [
    AssignmentsPage(),
    EquipmentPage(),
    SchedulesPage(),
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('BuildFlow AI'),
        actions: [
          IconButton(
            icon: const Icon(Icons.qr_code_scanner),
            onPressed: () {
              Navigator.push(
                context,
                MaterialPageRoute(
                  builder: (_) => const QRScannerPage(),
                ),
              );
            },
          ),
        ],
      ),
      body: pages[index],
      bottomNavigationBar: NavigationBar(
        selectedIndex: index,
        onDestinationSelected: (value) {
          setState(() {
            index = value;
          });
        },
        destinations: const [
          NavigationDestination(
            icon: Icon(Icons.assignment),
            label: 'Assignments',
          ),
          NavigationDestination(
            icon: Icon(Icons.construction),
            label: 'Equipment',
          ),
          NavigationDestination(
            icon: Icon(Icons.calendar_month),
            label: 'Schedules',
          ),
        ],
      ),
    );
  }
}

/* =========================================================
   ASSIGNMENTS
========================================================= */

class AssignmentsPage extends StatefulWidget {
  const AssignmentsPage({super.key});

  @override
  State<AssignmentsPage> createState() => _AssignmentsPageState();
}

class _AssignmentsPageState extends State<AssignmentsPage> {
  bool loading = true;
  String error = '';
  List<dynamic> assignments = [];

  @override
  void initState() {
    super.initState();
    loadAssignments();
  }

  Future<void> loadAssignments() async {
    setState(() {
      loading = true;
      error = '';
    });

    try {
      final data = await api.get(
        '/worker-assignments',
        params: {
          'page': '1',
          'pageSize': '100',
        },
      );

      assignments = extractItems(data);
    } catch (e) {
      error = e.toString();
    }

    if (mounted) {
      setState(() {
        loading = false;
      });
    }
  }

  Future<void> updateStatus(
    String id,
    String status,
  ) async {
    try {
      final current = assignments.firstWhere(
        (item) => item['id'].toString() == id,
      );

      await api.put(
        '/worker-assignments/$id',
        body: {
          'workerId': current['workerId'],
          'activityId': current['activityId'],
          'startTime': current['startTime'],
          'endTime': current['endTime'],
          'status': status,
          'notes': current['notes'],
        },
      );

      await loadAssignments();
    } catch (e) {
      showMessage(e.toString());
    }
  }

  void showMessage(String message) {
    if (!mounted) return;

    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(message)),
    );
  }

  String worker(dynamic item) {
    return textValue(item['workerName']).isNotEmpty
        ? textValue(item['workerName'])
        : textValue(item['workerId']);
  }

  String activity(dynamic item) {
    return textValue(item['activityName']).isNotEmpty
        ? textValue(item['activityName'])
        : textValue(item['activityId']);
  }

  @override
  Widget build(BuildContext context) {
    if (loading) {
      return const Center(
        child: CircularProgressIndicator(),
      );
    }

    if (error.isNotEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(error),
              const SizedBox(height: 12),
              FilledButton(
                onPressed: loadAssignments,
                child: const Text('Retry'),
              ),
            ],
          ),
        ),
      );
    }

    if (assignments.isEmpty) {
      return RefreshIndicator(
        onRefresh: loadAssignments,
        child: ListView(
          children: const [
            SizedBox(height: 220),
            Center(
              child: Text('No worker assignments found.'),
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: loadAssignments,
      child: ListView.builder(
        padding: const EdgeInsets.all(12),
        itemCount: assignments.length,
        itemBuilder: (context, index) {
          final item = assignments[index];

          final status =
              textValue(item['status']).isEmpty
                  ? 'Planned'
                  : textValue(item['status']);

          return Card(
            child: Padding(
              padding: const EdgeInsets.all(14),
              child: Column(
                crossAxisAlignment:
                    CrossAxisAlignment.start,
                children: [
                  Text(
                    worker(item),
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 17,
                    ),
                  ),
                  const SizedBox(height: 6),
                  Text('Activity: ${activity(item)}'),
                  Text(
                    'Start: ${textValue(item['startTime'])}',
                  ),
                  Text(
                    'End: ${textValue(item['endTime'])}',
                  ),
                  const SizedBox(height: 8),
                  Chip(label: Text(status)),
                  const SizedBox(height: 8),
                  Wrap(
                    spacing: 8,
                    children: [
                      if (status == 'Planned')
                        FilledButton.tonal(
                          onPressed: () {
                            updateStatus(
                              item['id'].toString(),
                              'InProgress',
                            );
                          },
                          child: const Text('Start'),
                        ),
                      if (status == 'InProgress')
                        FilledButton(
                          onPressed: () {
                            updateStatus(
                              item['id'].toString(),
                              'Completed',
                            );
                          },
                          child: const Text('Complete'),
                        ),
                      if (status != 'Completed' &&
                          status != 'Cancelled')
                        OutlinedButton(
                          onPressed: () {
                            showDelayDialog(
                              item['id'].toString(),
                            );
                          },
                          child: const Text('Report Delay'),
                        ),
                    ],
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }

  void showDelayDialog(String assignmentId) {
    final controller = TextEditingController();

    showDialog(
      context: context,
      builder: (context) {
        return AlertDialog(
          title: const Text('Report Delay'),
          content: TextField(
            controller: controller,
            maxLines: 4,
            decoration: const InputDecoration(
              hintText: 'Describe the delay...',
              border: OutlineInputBorder(),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () {
                Navigator.pop(context);
              },
              child: const Text('Cancel'),
            ),
            FilledButton(
              onPressed: () {
                Navigator.pop(context);

                showMessage(
                  'Delay reported for assignment $assignmentId.',
                );
              },
              child: const Text('Report'),
            ),
          ],
        );
      },
    );
  }
}

/* =========================================================
   EQUIPMENT
========================================================= */

class EquipmentPage extends StatefulWidget {
  const EquipmentPage({super.key});

  @override
  State<EquipmentPage> createState() => _EquipmentPageState();
}

class _EquipmentPageState extends State<EquipmentPage> {
  bool loading = true;
  String error = '';
  List<dynamic> equipment = [];

  @override
  void initState() {
    super.initState();
    loadEquipment();
  }

  Future<void> loadEquipment() async {
    setState(() {
      loading = true;
      error = '';
    });

    try {
      final data = await api.get(
        '/equipment',
        params: {
          'page': '1',
          'pageSize': '100',
          'includeArchived': 'false',
        },
      );

      equipment = extractItems(data);
    } catch (e) {
      error = e.toString();
    }

    if (mounted) {
      setState(() {
        loading = false;
      });
    }
  }

  Future<void> createReservation(
    dynamic item,
  ) async {
    final activityController = TextEditingController();
    final startController = TextEditingController();
    final endController = TextEditingController();

    await showDialog(
      context: context,
      builder: (context) {
        return AlertDialog(
          title: Text('Reserve ${item['name']}'),
          content: SingleChildScrollView(
            child: Column(
              children: [
                TextField(
                  controller: activityController,
                  decoration: const InputDecoration(
                    labelText: 'Activity ID',
                  ),
                ),
                TextField(
                  controller: startController,
                  decoration: const InputDecoration(
                    labelText: 'Start time',
                  ),
                ),
                TextField(
                  controller: endController,
                  decoration: const InputDecoration(
                    labelText: 'End time',
                  ),
                ),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: () {
                Navigator.pop(context);
              },
              child: const Text('Cancel'),
            ),
            FilledButton(
              onPressed: () async {
                try {
                  await api.post(
                    '/equipment-reservations',
                    body: {
                      'equipmentId':
                          item['id'],
                      'activityId':
                          activityController.text.trim(),
                      'startTime':
                          startController.text.trim(),
                      'endTime':
                          endController.text.trim(),
                      'status': 'Planned',
                      'notes': 'Mobile equipment request',
                    },
                  );

                  if (context.mounted) {
                    Navigator.pop(context);
                  }

                  showMessage(
                    'Equipment reservation created.',
                  );
                } catch (e) {
                  showMessage(e.toString());
                }
              },
              child: const Text('Reserve'),
            ),
          ],
        );
      },
    );
  }

  void showMessage(String message) {
    if (!mounted) return;

    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(message)),
    );
  }

  @override
  Widget build(BuildContext context) {
    if (loading) {
      return const Center(
        child: CircularProgressIndicator(),
      );
    }

    if (error.isNotEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(error),
              const SizedBox(height: 12),
              FilledButton(
                onPressed: loadEquipment,
                child: const Text('Retry'),
              ),
            ],
          ),
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: loadEquipment,
      child: ListView.builder(
        padding: const EdgeInsets.all(12),
        itemCount: equipment.length,
        itemBuilder: (context, index) {
          final item = equipment[index];

          final status = textValue(item['status']);

          return Card(
            child: ListTile(
              leading: const CircleAvatar(
                child: Icon(Icons.precision_manufacturing),
              ),
              title: Text(
                textValue(item['name']),
              ),
              subtitle: Text(
                '${item['equipmentCode'] ?? ''}\n'
                '${item['type'] ?? ''}\n'
                'Status: $status',
              ),
              isThreeLine: true,
              trailing: IconButton(
                icon: const Icon(Icons.add_box),
                onPressed:
                    status == 'Available'
                        ? () => createReservation(item)
                        : null,
              ),
            ),
          );
        },
      ),
    );
  }
}

/* =========================================================
   SCHEDULES
========================================================= */

class SchedulesPage extends StatefulWidget {
  const SchedulesPage({super.key});

  @override
  State<SchedulesPage> createState() =>
      _SchedulesPageState();
}

class _SchedulesPageState extends State<SchedulesPage> {
  bool loading = true;
  String error = '';
  List<dynamic> schedules = [];

  @override
  void initState() {
    super.initState();
    loadSchedules();
  }

  Future<void> loadSchedules() async {
    setState(() {
      loading = true;
      error = '';
    });

    try {
      final data = await api.get(
        '/schedules',
        params: {
          'page': '1',
          'pageSize': '100',
          'includeArchived': 'false',
        },
      );

      schedules = extractItems(data);
    } catch (e) {
      error = e.toString();
    }

    if (mounted) {
      setState(() {
        loading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    if (loading) {
      return const Center(
        child: CircularProgressIndicator(),
      );
    }

    if (error.isNotEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(error),
              const SizedBox(height: 12),
              FilledButton(
                onPressed: loadSchedules,
                child: const Text('Retry'),
              ),
            ],
          ),
        ),
      );
    }

    if (schedules.isEmpty) {
      return const Center(
        child: Text('No schedules found.'),
      );
    }

    return RefreshIndicator(
      onRefresh: loadSchedules,
      child: ListView.builder(
        padding: const EdgeInsets.all(12),
        itemCount: schedules.length,
        itemBuilder: (context, index) {
          final item = schedules[index];

          return Card(
            child: ListTile(
              leading: const Icon(
                Icons.calendar_month,
                size: 34,
              ),
              title: Text(
                textValue(item['activityName']).isNotEmpty
                    ? textValue(item['activityName'])
                    : textValue(item['activityId']),
              ),
              subtitle: Text(
                'Start: ${textValue(item['startTime'])}\n'
                'End: ${textValue(item['endTime'])}\n'
                'Status: ${textValue(item['status'])}\n'
                'Approval: ${textValue(item['approvalStatus'])}',
              ),
              isThreeLine: true,
            ),
          );
        },
      ),
    );
  }
}

/* =========================================================
   QR SCANNER
========================================================= */

class QRScannerPage extends StatefulWidget {
  const QRScannerPage({super.key});

  @override
  State<QRScannerPage> createState() =>
      _QRScannerPageState();
}

class _QRScannerPageState extends State<QRScannerPage> {
  bool scanned = false;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Scan Equipment QR'),
      ),
      body: MobileScanner(
        onDetect: (capture) {
          if (scanned) return;

          final code = capture.barcodes.isNotEmpty
              ? capture.barcodes.first.rawValue
              : null;

          if (code == null || code.isEmpty) return;

          scanned = true;

          Navigator.pop(
            context,
            code,
          );

          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(
                'Scanned equipment: $code',
              ),
            ),
          );
        },
      ),
    );
  }
}

/* =========================================================
   TOKEN SETUP
========================================================= */

class TokenSetupPage extends StatefulWidget {
  const TokenSetupPage({super.key});

  @override
  State<TokenSetupPage> createState() =>
      _TokenSetupPageState();
}

class _TokenSetupPageState extends State<TokenSetupPage> {
  final controller = TextEditingController();

  @override
  void dispose() {
    controller.dispose();
    super.dispose();
  }

  Future<void> save() async {
    await api.setToken(controller.text.trim());

    if (!mounted) return;

    Navigator.pop(context);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('API Token'),
      ),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            TextField(
              controller: controller,
              maxLines: 4,
              decoration: const InputDecoration(
                labelText: 'Bearer JWT token',
                border: OutlineInputBorder(),
              ),
            ),
            const SizedBox(height: 16),
            FilledButton(
              onPressed: save,
              child: const Text('Save Token'),
            ),
          ],
        ),
      ),
    );
  }
}