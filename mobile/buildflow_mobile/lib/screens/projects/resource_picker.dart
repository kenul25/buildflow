import 'package:flutter/material.dart';

import '../../services/project_service.dart';

Future<Map<String, dynamic>?> pickResource(
  BuildContext context,
  ProjectService service,
  String kind,
) => showModalBottomSheet<Map<String, dynamic>>(
  context: context,
  isScrollControlled: true,
  useSafeArea: true,
  showDragHandle: true,
  builder: (_) => _ResourcePicker(service: service, kind: kind),
);

class _ResourcePicker extends StatefulWidget {
  const _ResourcePicker({required this.service, required this.kind});
  final ProjectService service;
  final String kind;
  @override
  State<_ResourcePicker> createState() => _ResourcePickerState();
}

class _ResourcePickerState extends State<_ResourcePicker> {
  late Future<List<Map<String, dynamic>>> options;
  String search = '';
  @override
  void initState() {
    super.initState();
    options = widget.service.resourceOptions(widget.kind);
  }

  @override
  Widget build(BuildContext context) => Padding(
    padding: EdgeInsets.only(bottom: MediaQuery.viewInsetsOf(context).bottom),
    child: SizedBox(
      height: MediaQuery.sizeOf(context).height * .65,
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 20),
            child: Row(
              children: [
                Expanded(
                  child: Text(
                    widget.kind == 'Workforce'
                        ? 'Choose worker skill'
                        : 'Choose ${widget.kind.toLowerCase()}',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                ),
                IconButton(
                  tooltip: 'Close resource search',
                  onPressed: () => Navigator.pop(context),
                  icon: const Icon(Icons.close),
                ),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(16),
            child: TextField(
              decoration: const InputDecoration(
                labelText: 'Search resources',
                prefixIcon: Icon(Icons.search),
              ),
              onChanged: (value) =>
                  setState(() => search = value.toLowerCase().trim()),
            ),
          ),
          Expanded(
            child: FutureBuilder<List<Map<String, dynamic>>>(
              future: options,
              builder: (context, snapshot) {
                if (snapshot.hasError) {
                  return Center(
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        const Text('Could not load resources.'),
                        TextButton(
                          onPressed: () {
                            final retry = widget.service.resourceOptions(
                              widget.kind,
                            );
                            setState(() {
                              options = retry;
                            });
                          },
                          child: const Text('Retry'),
                        ),
                      ],
                    ),
                  );
                }
                if (!snapshot.hasData) {
                  return const Center(child: CircularProgressIndicator());
                }
                final matches = snapshot.data!
                    .where(
                      (row) =>
                          '${row['name']} ${row['category'] ?? ''} ${row['unit'] ?? ''}'
                              .toLowerCase()
                              .contains(search),
                    )
                    .toList();
                if (matches.isEmpty) {
                  return Center(
                    child: Padding(
                      padding: const EdgeInsets.all(24),
                      child: Text(
                        snapshot.data!.isEmpty
                            ? 'No ${widget.kind.toLowerCase()} records available. Ask your manager to add them first.'
                            : 'No matching resources.',
                        textAlign: TextAlign.center,
                      ),
                    ),
                  );
                }
                return ListView.builder(
                  itemCount: matches.length,
                  itemBuilder: (_, index) {
                    final row = matches[index];
                    return ListTile(
                      title: Text(row['name'] as String),
                      subtitle: widget.kind == 'Material'
                          ? Text('Master unit: ${row['unit']}')
                          : widget.kind == 'Equipment'
                          ? Text(
                              '${row['category'] ?? 'Equipment'} · Operational',
                            )
                          : const Text('Qualified worker skill'),
                      trailing: const Icon(Icons.chevron_right),
                      onTap: () => Navigator.pop(context, row),
                    );
                  },
                );
              },
            ),
          ),
        ],
      ),
    ),
  );
}
