import 'dart:async';
import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_unity_widget/flutter_unity_widget.dart';

import '../state/best_score.dart';
import '../theme/app_theme.dart';

class ARViewScreen extends StatefulWidget {
  const ARViewScreen({super.key});

  @override
  State<ARViewScreen> createState() => _ARViewScreenState();
}

class _ARViewScreenState extends State<ARViewScreen>
    with WidgetsBindingObserver {
  UnityWidgetController? _unityController;
  Timer? _feedbackTimer;
  Timer? _unitySyncTimer;
  bool _unityReady = false;
  int _currentScore = 0;
  String _progressText = '1 / 8';
  _ArFeedback? _feedback;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _feedbackTimer?.cancel();
    _unitySyncTimer?.cancel();
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    final controller = _unityController;
    if (controller == null) {
      return;
    }

    if (state == AppLifecycleState.resumed) {
      controller.resume();
      _scheduleGameStateSync();
    } else if (state == AppLifecycleState.inactive ||
        state == AppLifecycleState.paused ||
        state == AppLifecycleState.hidden) {
      controller.pause();
    }
  }

  void _onUnityCreated(UnityWidgetController controller) {
    _unityController = controller;
    controller.resume();
    _scheduleGameStateSync();
    if (!mounted) {
      return;
    }
    setState(() => _unityReady = true);
  }

  void _scheduleGameStateSync() {
    _unitySyncTimer?.cancel();
    _unitySyncTimer = Timer(
      const Duration(milliseconds: 900),
      _requestGameStateSync,
    );
  }

  void _requestGameStateSync() {
    final controller = _unityController;
    if (controller == null) {
      return;
    }

    controller.postMessage(
      'UnityFlutterBridge',
      'OnReceiveFlutterCommand',
      jsonEncode({'command': 'syncState'}),
    );
  }

  void _onUnityMessage(dynamic message) {
    // ignore: avoid_print
    print('UNITY MESSAGE RECEIVED: $message');

    final data = _decodeMessage(message);
    if (data == null) {
      return;
    }

    final type = data['type']?.toString();

    if (type == 'gameUpdate') {
      final score = int.tryParse('${data['score']}');
      final progress = int.tryParse('${data['progress']}');
      final total = int.tryParse('${data['total']}');
      if (score == null || progress == null || total == null) {
        return;
      }

      if (score > bestScore.value) {
        bestScore.value = score;
      }

      if (mounted) {
        setState(() {
          _currentScore = score;
          _progressText = '$progress / $total';
        });
      }
      return;
    }

    if (type == 'score') {
      final score = int.tryParse('${data['score']}');
      if (score == null) {
        return;
      }

      if (score > bestScore.value) {
        bestScore.value = score;
      }

      if (mounted) {
        setState(() => _currentScore = score);
      }
      return;
    }

    if (type == 'progress') {
      final current = int.tryParse('${data['current']}');
      final total = int.tryParse('${data['total']}');
      if (current != null && total != null && mounted) {
        setState(() => _progressText = '$current / $total');
      }
      return;
    }

    if (type == 'feedback') {
      final text = data['message']?.toString();
      if (text == null || text.isEmpty) {
        return;
      }

      _feedbackTimer?.cancel();
      if (!mounted) {
        return;
      }
      setState(
        () => _feedback = _ArFeedback(
          text: text,
          correct: data['correct'] == true,
        ),
      );
      _feedbackTimer = Timer(const Duration(seconds: 4), () {
        if (mounted) {
          setState(() => _feedback = null);
        }
      });
    }
  }

  Map<String, dynamic>? _decodeMessage(dynamic message) {
    if (message is Map) {
      return message.map((key, value) => MapEntry(key.toString(), value));
    }

    if (message is String) {
      try {
        final decoded = jsonDecode(message);
        if (decoded is Map) {
          return decoded.map((key, value) => MapEntry(key.toString(), value));
        }
      } on FormatException {
        return null;
      }
    }

    return null;
  }

  @override
  Widget build(BuildContext context) {
    final media = MediaQuery.of(context);

    return Scaffold(
      backgroundColor: Colors.black,
      body: Stack(
        fit: StackFit.expand,
        children: [
          UnityWidget(
            onUnityCreated: _onUnityCreated,
            onUnityMessage: _onUnityMessage,
            fullscreen: false,
            unloadOnDispose: false,
            printSetupLog: false,
            placeholder: const Center(
              child: CircularProgressIndicator(color: AppColors.emerald),
            ),
          ),
          if (!_unityReady) const _UnityLoader(),
          Positioned(
            top: media.padding.top + 16,
            left: 16,
            right: 16,
            child: _buildTopBar(context),
          ),
          Positioned(
            left: 24,
            right: 24,
            bottom: media.padding.bottom + 24,
            child: _buildBottomArea(context),
          ),
        ],
      ),
    );
  }

  Widget _buildTopBar(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 6),
      decoration: BoxDecoration(
        color: AppColors.navy.withValues(alpha: 0.85),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.border),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.28),
            blurRadius: 20,
            offset: const Offset(0, 8),
          ),
        ],
      ),
      child: Row(
        children: [
          IconButton(
            onPressed: () => Navigator.of(context).pop(),
            tooltip: 'Kembali ke menu',
            icon: const Icon(Icons.arrow_back_rounded),
            color: AppColors.textMain,
            visualDensity: VisualDensity.compact,
          ),
          const Spacer(),
          _HudChip(
            icon: Icons.stars_rounded,
            color: const Color(0xFFFBBF24),
            label: 'Skor: $_currentScore',
          ),
          const SizedBox(width: 8),
          _HudChip(
            icon: Icons.checklist_rounded,
            color: const Color(0xFF4ADE80),
            label: _progressText,
          ),
        ],
      ),
    );
  }

  Widget _buildBottomArea(BuildContext context) {
    final theme = Theme.of(context);

    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        AnimatedSwitcher(
          duration: const Duration(milliseconds: 220),
          transitionBuilder: (child, animation) =>
              FadeTransition(opacity: animation, child: child),
          child: _feedback == null
              ? const SizedBox.shrink()
              : Padding(
                  key: ValueKey('${_feedback!.text}-${_feedback!.correct}'),
                  padding: const EdgeInsets.only(bottom: 10),
                  child: _FeedbackBanner(feedback: _feedback!),
                ),
        ),
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 13),
          decoration: BoxDecoration(
            color: AppColors.navy.withValues(alpha: 0.9),
            borderRadius: BorderRadius.circular(999),
            border: Border.all(color: AppColors.border),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withValues(alpha: 0.28),
                blurRadius: 18,
                offset: const Offset(0, 8),
              ),
            ],
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(
                Icons.touch_app_rounded,
                size: 20,
                color: Color(0xFF4ADE80),
              ),
              const SizedBox(width: 10),
              Flexible(
                child: Text(
                  'Sentuh & seret sampah ke wadah yang tepat',
                  textAlign: TextAlign.center,
                  style: theme.textTheme.bodyMedium?.copyWith(
                    color: AppColors.textMain,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _HudChip extends StatelessWidget {
  const _HudChip({
    required this.icon,
    required this.label,
    required this.color,
  });

  final IconData icon;
  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      decoration: BoxDecoration(
        color: AppColors.surface.withValues(alpha: 0.9),
        borderRadius: BorderRadius.circular(11),
        border: Border.all(color: AppColors.border.withValues(alpha: 0.8)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 16, color: color),
          const SizedBox(width: 6),
          Text(
            label,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: Theme.of(context).textTheme.titleMedium?.copyWith(
              fontSize: 13,
              fontWeight: FontWeight.w700,
            ),
          ),
        ],
      ),
    );
  }
}

class _ArFeedback {
  const _ArFeedback({required this.text, required this.correct});

  final String text;
  final bool correct;
}

class _FeedbackBanner extends StatelessWidget {
  const _FeedbackBanner({required this.feedback});

  final _ArFeedback feedback;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final color = feedback.correct ? AppColors.organik : AppColors.b3;

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 13),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: color, width: 1.5),
      ),
      child: Row(
        children: [
          Icon(
            feedback.correct
                ? Icons.check_circle_rounded
                : Icons.cancel_rounded,
            color: color,
            size: 24,
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Text(
              feedback.text,
              style: theme.textTheme.bodyLarge?.copyWith(
                color: AppColors.textMain,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _UnityLoader extends StatelessWidget {
  const _UnityLoader();

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return ColoredBox(
      color: AppColors.navy,
      child: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const SizedBox(
              width: 34,
              height: 34,
              child: CircularProgressIndicator(
                strokeWidth: 3,
                color: Color(0xFF4ADE80),
              ),
            ),
            const SizedBox(height: 16),
            Text('Menyiapkan mesin Unity...', style: theme.textTheme.bodyLarge),
          ],
        ),
      ),
    );
  }
}
