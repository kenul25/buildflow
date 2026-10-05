import 'package:flutter/material.dart';

import '../../services/scheduling_service.dart';
import '../home/operations_navigation.dart';

class SchedulingActionSheet extends StatefulWidget {
  const SchedulingActionSheet({
    required this.issue,
    required this.service,
    required this.activities,
    required this.equipment,
    super.key,
  });
  final bool issue;
  final SchedulingService service;
  final List<Map<String, dynamic>> activities, equipment;
  @override
  State<SchedulingActionSheet> createState() => _SchedulingActionSheetState();
}

class _SchedulingActionSheetState extends State<SchedulingActionSheet> {
  final form = GlobalKey<FormState>();
  final errorAnchor = GlobalKey();
  final title = TextEditingController();
  final notes = TextEditingController();
  String? activityId, equipmentId, error;
  bool saving = false;
  bool get canSubmit =>
      widget.activities.isNotEmpty &&
      (widget.issue || widget.equipment.isNotEmpty);

  @override
  void dispose() {
    title.dispose();
    notes.dispose();
    super.dispose();
  }

  Future<void> submit() async {
    if (saving || !canSubmit || !form.currentState!.validate()) return;
    FocusScope.of(context).unfocus();
    setState(() {
      saving = true;
      error = null;
    });
    try {
      final body = <String, dynamic>{
        'name': title.text.trim(),
        'notes': notes.text.trim(),
        'activityId': activityId,
        'equipmentId': equipmentId,
      };
      if (widget.issue) {
        await widget.service.report(body);
      } else {
        await widget.service.requestEquipment(body);
      }
      if (mounted) Navigator.pop(context, true);
    } catch (exception) {
      if (mounted) {
        setState(() {
          error = exception.toString();
          saving = false;
        });
        WidgetsBinding.instance.addPostFrameCallback((_) {
          if (mounted && errorAnchor.currentContext != null) {
            Scrollable.ensureVisible(
              errorAnchor.currentContext!,
              duration: const Duration(milliseconds: 250),
            );
          }
        });
      }
    }
  }

  InputDecoration decoration(String hint, {IconData? icon}) => InputDecoration(
    hintText: hint,
    filled: true,
    fillColor: Theme.of(context).brightness == Brightness.dark
        ? const Color(0xFF1E293B)
        : const Color(0xFFF8FAFC),
    contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 16),
    prefixIcon: icon == null
        ? null
        : Icon(icon, color: OperationsNavigation.blue, size: 20),
    border: OutlineInputBorder(
      borderRadius: BorderRadius.circular(12),
      borderSide: const BorderSide(color: Color(0xFFE2E8F0)),
    ),
    enabledBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(12),
      borderSide: const BorderSide(color: Color(0xFFE2E8F0)),
    ),
    focusedBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(12),
      borderSide: const BorderSide(
        color: OperationsNavigation.blue,
        width: 1.5,
      ),
    ),
    errorMaxLines: 2,
  );

  Widget field(String label, Widget input, {String? guidance}) => Padding(
    padding: const EdgeInsets.only(bottom: 20),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          label,
          style: Theme.of(context).textTheme.bodyMedium
              ?.copyWith(fontWeight: FontWeight.w600),
        ),
        const SizedBox(height: 8),
        input,
        if (guidance != null) ...[
          const SizedBox(height: 6),
          Text(
            guidance,
            style: Theme.of(context).textTheme.bodySmall
                ?.copyWith(color: const Color(0xFF64748B), height: 1.4),
          ),
        ],
      ],
    ),
  );

  @override
  Widget build(BuildContext context) {
    final media = MediaQuery.of(context);
    return PopScope(
      canPop: !saving,
      child: AnimatedPadding(
        duration: const Duration(milliseconds: 180),
        padding: EdgeInsets.only(bottom: media.viewInsets.bottom),
        child: ConstrainedBox(
          constraints: BoxConstraints(
            maxHeight: (media.size.height * .9 - media.viewInsets.bottom).clamp(
              220,
              media.size.height,
            ),
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Padding(
                padding: const EdgeInsets.fromLTRB(20, 20, 8, 16),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Container(
                      width: 40,
                      height: 40,
                      decoration: BoxDecoration(
                        color: OperationsNavigation.blue.withValues(alpha: .09),
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: Icon(
                        widget.issue
                            ? Icons.flag_outlined
                            : Icons.construction_rounded,
                        color: OperationsNavigation.blue,
                        size: 22,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            widget.issue
                                ? 'Report site issue'
                                : 'Request equipment',
                            style: Theme.of(context).textTheme.titleLarge
                                ?.copyWith(fontWeight: FontWeight.w700),
                          ),
                          if (media.viewInsets.bottom == 0) ...[
                            const SizedBox(height: 6),
                            Text(
                              widget.issue
                                  ? 'Share a delay, blocker, or equipment problem.'
                                  : 'Save an equipment request for your site activity.',
                              style: Theme.of(context).textTheme.bodySmall
                                  ?.copyWith(
                                    color: const Color(0xFF64748B),
                                    height: 1.4,
                                  ),
                            ),
                          ],
                        ],
                      ),
                    ),
                    IconButton(
                      tooltip: 'Close form',
                      onPressed: saving
                          ? null
                          : () => Navigator.pop(context, false),
                      icon: const Icon(Icons.close_rounded, size: 20),
                    ),
                  ],
                ),
              ),
              const Divider(height: 1, color: Color(0xFFE2E8F0)),
              Flexible(
                child: SingleChildScrollView(
                  keyboardDismissBehavior:
                      ScrollViewKeyboardDismissBehavior.onDrag,
                  padding: const EdgeInsets.fromLTRB(20, 20, 20, 0),
                  child: Form(
                    key: form,
                    autovalidateMode: AutovalidateMode.onUserInteraction,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        Text(
                          '* Required fields',
                          style: Theme.of(context).textTheme.bodySmall
                              ?.copyWith(color: const Color(0xFF64748B)),
                        ),
                        const SizedBox(height: 16),
                        if (!canSubmit) ...[
                          Text(
                            widget.activities.isEmpty
                                ? 'No activities available. An activity must be assigned before you can submit.'
                                : 'No equipment available. Add equipment to the catalog before requesting it.',
                            style: Theme.of(context).textTheme.bodyMedium,
                          ),
                          const SizedBox(height: 16),
                        ],
                        field(
                          widget.issue ? 'Issue title *' : 'Request title *',
                          TextFormField(
                            controller: title,
                            enabled: !saving,
                            maxLength: 160,
                            textCapitalization: TextCapitalization.sentences,
                            textInputAction: TextInputAction.next,
                            decoration: decoration(
                              widget.issue
                                  ? 'e.g. Access road blocked'
                                  : 'e.g. Excavator for site clearing',
                            ).copyWith(counterText: ''),
                            validator: (value) =>
                                (value?.trim().length ?? 0) < 2
                                ? 'Enter a title with at least 2 characters'
                                : null,
                          ),
                        ),
                        field(
                          'Activity *',
                          DropdownButtonFormField<String>(
                            isExpanded: true,
                            decoration: decoration(
                              'Select an activity',
                              icon: Icons.layers_outlined,
                            ),
                            items: widget.activities
                                .map(
                                  (item) => DropdownMenuItem(
                                    value: item['id'] as String,
                                    child: Text(
                                      '${item['name']}',
                                      maxLines: 1,
                                      overflow: TextOverflow.ellipsis,
                                    ),
                                  ),
                                )
                                .toList(),
                            onChanged: saving || widget.activities.isEmpty
                                ? null
                                : (value) => setState(() => activityId = value),
                            validator: (value) => value == null
                                ? 'Select the affected activity'
                                : null,
                          ),
                        ),
                        field(
                          widget.issue ? 'Equipment (optional)' : 'Equipment *',
                          DropdownButtonFormField<String>(
                            isExpanded: true,
                            decoration: decoration(
                              widget.equipment.isEmpty
                                  ? 'No equipment available'
                                  : 'Select equipment',
                              icon: Icons.construction_outlined,
                            ),
                            items: [
                              if (widget.issue)
                                const DropdownMenuItem<String>(
                                  value: '',
                                  child: Text('No specific equipment'),
                                ),
                              ...widget.equipment.map(
                                (item) => DropdownMenuItem(
                                  value: item['id'] as String,
                                  child: Text(
                                    '${item['name']}',
                                    maxLines: 1,
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                ),
                              ),
                            ],
                            onChanged: saving || widget.equipment.isEmpty
                                ? null
                                : (value) => setState(
                                    () => equipmentId = value == ''
                                        ? null
                                        : value,
                                  ),
                            validator: (value) => !widget.issue && value == null
                                ? 'Select the equipment you need'
                                : null,
                          ),
                          guidance: widget.issue
                              ? 'Leave blank for delays or issues unrelated to equipment.'
                              : 'Choose the equipment needed for this activity.',
                        ),
                        field(
                          widget.issue
                              ? 'Details & expected impact'
                              : 'Request details',
                          TextFormField(
                            controller: notes,
                            enabled: !saving,
                            minLines: 3,
                            maxLines: 5,
                            maxLength: 2000,
                            textCapitalization: TextCapitalization.sentences,
                            decoration: decoration(
                              widget.issue
                                  ? 'Describe what happened and how it affects site work.'
                                  : 'Describe the work, preferred timing, and equipment needs.',
                            ).copyWith(counterText: ''),
                          ),
                          guidance: 'Optional. Add context to help the site team respond.',
                        ),
                        if (error != null)
                          Padding(
                            key: errorAnchor,
                            padding: const EdgeInsets.only(bottom: 20),
                            child: Semantics(
                              liveRegion: true,
                              child: Text(
                                error!,
                                style: TextStyle(
                                  color: Theme.of(context).colorScheme.error,
                                ),
                              ),
                            ),
                          ),
                      ],
                    ),
                  ),
                ),
              ),
              const Divider(height: 1, color: Color(0xFFE2E8F0)),
              SafeArea(
                top: false,
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(20, 16, 20, 16),
                  child: Row(
                    children: [
                      Expanded(
                        child: OutlinedButton(
                          onPressed: saving
                              ? null
                              : () => Navigator.pop(context, false),
                          style: OutlinedButton.styleFrom(
                            foregroundColor: const Color(0xFF64748B),
                            side: const BorderSide(color: Color(0xFFE2E8F0)),
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(12),
                            ),
                            minimumSize: const Size.fromHeight(48),
                          ),
                          child: const Text('Cancel'),
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        flex: 2,
                        child: FilledButton(
                          onPressed: saving || !canSubmit ? null : submit,
                          style: FilledButton.styleFrom(
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(12),
                            ),
                            backgroundColor: OperationsNavigation.blue,
                            foregroundColor: Colors.white,
                            minimumSize: const Size.fromHeight(48),
                          ),
                          child: saving
                              ? const SizedBox(
                                  width: 20,
                                  height: 20,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                    color: Colors.white,
                                  ),
                                )
                              : Text(
                                  widget.issue
                                      ? 'Report issue'
                                      : 'Save request',
                                  textAlign: TextAlign.center,
                                ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
