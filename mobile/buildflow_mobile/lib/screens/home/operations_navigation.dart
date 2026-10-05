import 'dart:math' as math;

import 'package:flutter/material.dart';

class OperationsNavigation extends StatelessWidget {
  const OperationsNavigation({
    required this.selectedIndex,
    required this.onSelect,
    super.key,
  });
  final int selectedIndex;
  final ValueChanged<int> onSelect;

  static const blue = Color(0xFF1E6BFF);
  static const slate = Color(0xFF64748B);

  @override
  Widget build(BuildContext context) => BottomAppBar(
    color: Colors.white,
    surfaceTintColor: Colors.transparent,
    elevation: 8,
    shadowColor: const Color(0x260F172A),
    shape: const RaisedActionCradle(),
    notchMargin: 6,
    clipBehavior: Clip.antiAlias,
    padding: EdgeInsets.zero,
    height: 72,
    child: Row(
      children: [
        _tab(0, 'Home', Icons.home_outlined, Icons.home_rounded),
        _tab(1, 'Projects', Icons.apartment_outlined, Icons.apartment_rounded),
        const SizedBox(width: 72),
        _tab(
          2,
          'Schedule',
          Icons.calendar_month_outlined,
          Icons.calendar_month_rounded,
        ),
        _tab(3, 'More', Icons.grid_view_outlined, Icons.grid_view_rounded),
      ],
    ),
  );
  Widget _tab(int index, String label, IconData icon, IconData selectedIcon) {
    final selected = selectedIndex == index;
    return Expanded(
      child: Semantics(
        selected: selected,
        button: true,
        label: label,
        child: ExcludeSemantics(
          child: Material(
            color: Colors.transparent,
            child: InkWell(
              onTap: () => onSelect(index),
              child: SizedBox(
                height: 72,
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(
                      selected ? selectedIcon : icon,
                      size: 24,
                      color: selected ? blue : slate,
                    ),
                    const SizedBox(height: 5),
                    Text(
                      label,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        fontSize: 11,
                        fontWeight: selected
                            ? FontWeight.w700
                            : FontWeight.w500,
                        color: selected ? blue : slate,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/// Extends the bar upward behind the FAB, leaving a white rim around it.
class RaisedActionCradle extends NotchedShape {
  const RaisedActionCradle();

  @override
  Path getOuterPath(Rect host, Rect? guest) {
    if (guest == null || !host.overlaps(guest)) {
      return Path()..addRect(host);
    }
    final center = guest.center;
    final radius = guest.width / 2;
    // Keep the shoulder joins above the bar when the button sits lower.
    final inset = ((center.dy - host.top) / radius).clamp(0.0, 0.9);
    final blendAngle = (math.asin(inset) + 0.25).clamp(0.25, 1.3);
    final shoulder = radius + 16;
    final dx = radius * math.cos(blendAngle);
    final dy = radius * math.sin(blendAngle);
    final left = center + Offset(-dx, -dy);
    final right = center + Offset(dx, -dy);
    const tangentLength = 8.0;
    final tangentX = tangentLength * math.sin(blendAngle);
    final tangentY = tangentLength * math.cos(blendAngle);
    return Path()
      ..moveTo(host.left, host.top)
      ..lineTo(center.dx - shoulder, host.top)
      ..cubicTo(
        center.dx - shoulder + 12,
        host.top,
        left.dx - tangentX,
        left.dy + tangentY,
        left.dx,
        left.dy,
      )
      ..arcTo(
        Rect.fromCircle(center: center, radius: radius),
        math.pi + blendAngle,
        math.pi - 2 * blendAngle,
        false,
      )
      ..cubicTo(
        right.dx + tangentX,
        right.dy + tangentY,
        center.dx + shoulder - 12,
        host.top,
        center.dx + shoulder,
        host.top,
      )
      ..lineTo(host.right, host.top)
      ..lineTo(host.right, host.bottom)
      ..lineTo(host.left, host.bottom)
      ..close();
  }
}

class RequestActionButton extends StatelessWidget {
  const RequestActionButton({required this.onPressed, super.key});
  final VoidCallback onPressed;
  @override
  Widget build(BuildContext context) => FloatingActionButton(
    onPressed: onPressed,
    tooltip: 'Create new request',
    backgroundColor: OperationsNavigation.blue,
    foregroundColor: Colors.white,
    elevation: 6,
    highlightElevation: 8,
    shape: const CircleBorder(),
    child: const Icon(Icons.add_rounded, size: 32),
  );
}

class RequestActionLocation extends FloatingActionButtonLocation {
  const RequestActionLocation();
  static const downwardOffset = 14.0;

  @override
  Offset getOffset(ScaffoldPrelayoutGeometry scaffoldGeometry) =>
      FloatingActionButtonLocation.centerDocked.getOffset(scaffoldGeometry) +
      const Offset(0, downwardOffset);
}
