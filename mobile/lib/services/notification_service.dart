import '../models/notification_item.dart';
import 'backend_client.dart';

/// FR22 — the buyer/farmer's own notifications (`/api/notifications`).
class NotificationService {
  final BackendClient _api;

  NotificationService(this._api);

  Future<List<NotificationItem>> list() => _api.request(
        'GET',
        '/api/notifications',
        decode: (json) => (json as List)
            .map((e) => NotificationItem.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

  Future<int> unreadCount() => _api.request(
        'GET',
        '/api/notifications/unread-count',
        decode: (json) => (json as Map<String, dynamic>)['count'] as int,
      );

  Future<void> markRead(String id) => _api.request(
        'PUT',
        '/api/notifications/$id/read',
        decode: (_) {},
      );

  Future<void> markAllRead() => _api.request(
        'PUT',
        '/api/notifications/read-all',
        decode: (_) {},
      );
}
