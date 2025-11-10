namespace StockQuoteAlert.Adapters;

/// <summary>
/// Abstraction for sending user-facing notifications (email, SMS, etc.).
/// </summary>
public interface INotificationSender
{
    Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the content of a notification being sent to the user.
/// </summary>
/// <param name="Subject">Short summary (used by channels that support subjects).</param>
/// <param name="Body">Full message text.</param>
/// <param name="Category">Semantic classification to let channels format accordingly.</param>
public sealed record NotificationMessage(
    string Subject,
    string Body,
    NotificationCategory Category);

/// <summary>
/// Lightweight channel-agnostic categories for routing/formatting decisions.
/// </summary>
public enum NotificationCategory
{
    BuyAlert,
    SellAlert,
    Error
}
