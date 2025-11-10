using StockQuoteAlert.Domain;

namespace StockQuoteAlert.ServiceLayer;

/// <summary>
/// Publish/subscribe abstraction for domain events.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Register a handler for the given event type.
    /// </summary>
    /// <typeparam name="TEvent">Domain event type.</typeparam>
    /// <param name="handler">Async handler invoked when the event is published.</param>
    void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent;

    /// <summary>
    /// Dispatch the event to all subscribers.
    /// </summary>
    /// <typeparam name="TEvent">Domain event type.</typeparam>
    /// <param name="event">Event payload.</param>
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent;
}

/// <summary>
/// In-memory message bus for handling domain events
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private readonly object _lock = new();

    /// <summary>
    /// Subscribe to an event type
    /// </summary>
    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent
    {
        lock (_lock)
        {
            var eventType = typeof(TEvent);
            if (!_handlers.ContainsKey(eventType))
            {
                _handlers[eventType] = new List<Delegate>();
            }

            _handlers[eventType].Add(handler);
        }
    }

    /// <summary>
    /// Publish an event to all subscribers
    /// </summary>
    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent
    {
        List<Delegate>? handlers;

        lock (_lock)
        {
            var eventType = typeof(TEvent);
            if (!_handlers.TryGetValue(eventType, out handlers))
            {
                return; // No handlers registered
            }

            // Create a copy to avoid issues with concurrent modifications
            handlers = new List<Delegate>(handlers);
        }

        // Execute all handlers
        var tasks = handlers
            .Cast<Func<TEvent, Task>>()
            .Select(handler => ExecuteHandlerSafely(handler, @event));

        await Task.WhenAll(tasks);
    }

    private async Task ExecuteHandlerSafely<TEvent>(Func<TEvent, Task> handler, TEvent @event) where TEvent : IEvent
    {
        try
        {
            await handler(@event);
        }
        catch (Exception ex)
        {
            // Log the error but don't let one handler failure affect others
            Console.Error.WriteLine($"Error in event handler: {ex.Message}");
        }
    }
}
