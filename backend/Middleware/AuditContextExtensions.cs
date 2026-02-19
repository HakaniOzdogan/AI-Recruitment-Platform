namespace IkOtomasyon.Api.Middleware;

public static class AuditContextExtensions
{
    private const string ActionKey = "AuditAction";
    private const string EntityKey = "AuditEntity";
    private const string EntityIdKey = "AuditEntityId";
    private const string EventsKey = "AuditEvents";

    public record AuditEvent(string Action, string? Entity, string? EntityId);

    public static void SetAuditInfo(this HttpContext context, string action, string? entity = null, string? entityId = null)
    {
        context.Items[ActionKey] = action;
        context.Items[EntityKey] = entity;
        context.Items[EntityIdKey] = entityId;
        context.Items[EventsKey] = new List<AuditEvent> { new(action, entity, entityId) };
    }

    public static void AddAuditInfo(this HttpContext context, string action, string? entity = null, string? entityId = null)
    {
        if (!context.Items.TryGetValue(EventsKey, out var existing) || existing is not List<AuditEvent> events)
        {
            events = new List<AuditEvent>();
            context.Items[EventsKey] = events;
        }

        events.Add(new AuditEvent(action, entity, entityId));

        if (events.Count == 1)
        {
            context.Items[ActionKey] = action;
            context.Items[EntityKey] = entity;
            context.Items[EntityIdKey] = entityId;
        }
    }

    public static string? GetAuditAction(this HttpContext context) => context.Items.TryGetValue(ActionKey, out var value) ? value?.ToString() : null;
    public static string? GetAuditEntity(this HttpContext context) => context.Items.TryGetValue(EntityKey, out var value) ? value?.ToString() : null;
    public static string? GetAuditEntityId(this HttpContext context) => context.Items.TryGetValue(EntityIdKey, out var value) ? value?.ToString() : null;
    public static IReadOnlyCollection<AuditEvent> GetAuditEvents(this HttpContext context)
    {
        if (context.Items.TryGetValue(EventsKey, out var existing) && existing is List<AuditEvent> events && events.Count > 0)
        {
            return events;
        }

        var action = context.GetAuditAction();
        if (string.IsNullOrWhiteSpace(action))
        {
            return Array.Empty<AuditEvent>();
        }

        return [new AuditEvent(action, context.GetAuditEntity(), context.GetAuditEntityId())];
    }
}
