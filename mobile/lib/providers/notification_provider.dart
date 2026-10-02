import 'package:flutter/foundation.dart';

import '../models/notification_item.dart';
import '../services/notification_service.dart';

/// Notifications inbox + unread badge (FR22). Failures are surfaced on the
/// inbox screen but never block the rest of the app — the badge just stays at
/// its last known value.
class NotificationProvider extends ChangeNotifier {
  final NotificationService _service;
  final String? ownerId;

  NotificationProvider(this._service, {this.ownerId});

  List<NotificationItem> _items = [];
  int _unread = 0;
  bool _loading = false;
  String? _error;

  List<NotificationItem> get items => _items;
  int get unread => _unread;
  bool get loading => _loading;
  String? get error => _error;

  Future<void> refreshUnread() async {
    try {
      _unread = await _service.unreadCount();
      notifyListeners();
    } catch (_) {
      // Badge is best-effort; the inbox screen reports real errors.
    }
  }

  Future<void> load() async {
    _loading = true;
    _error = null;
    notifyListeners();
    try {
      _items = await _service.list();
      _unread = _items.where((n) => !n.isRead).length;
    } catch (e) {
      _error = e.toString();
    } finally {
      _loading = false;
      notifyListeners();
    }
  }

  Future<void> markRead(NotificationItem item) async {
    if (item.isRead) return;
    try {
      await _service.markRead(item.id);
      _items = _items.map((n) => n.id == item.id ? n.asRead() : n).toList();
      _unread = _items.where((n) => !n.isRead).length;
      notifyListeners();
    } catch (e) {
      _error = e.toString();
      notifyListeners();
    }
  }

  Future<void> markAllRead() async {
    try {
      await _service.markAllRead();
      _items = _items.map((n) => n.asRead()).toList();
      _unread = 0;
      notifyListeners();
    } catch (e) {
      _error = e.toString();
      notifyListeners();
    }
  }
}
