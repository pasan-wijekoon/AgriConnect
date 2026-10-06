/// Mirrors `backend/src/dtos/NotificationDtos.cs`'s `NotificationResponse`.
class NotificationItem {
  final String id;
  final String type;
  final String? title;
  final String message;
  final DateTime? readAt;
  final DateTime createdAt;

  NotificationItem({
    required this.id,
    required this.type,
    this.title,
    required this.message,
    this.readAt,
    required this.createdAt,
  });

  bool get isRead => readAt != null;

  factory NotificationItem.fromJson(Map<String, dynamic> json) => NotificationItem(
        id: json['id'] as String,
        type: json['type'] as String,
        title: json['title'] as String?,
        message: json['message'] as String,
        readAt: json['readAt'] == null ? null : DateTime.parse(json['readAt'] as String),
        createdAt: DateTime.parse(json['createdAt'] as String),
      );

  NotificationItem asRead() => NotificationItem(
        id: id,
        type: type,
        title: title,
        message: message,
        readAt: readAt ?? DateTime.now(),
        createdAt: createdAt,
      );
}
