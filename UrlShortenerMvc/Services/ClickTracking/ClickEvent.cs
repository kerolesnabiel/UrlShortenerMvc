public sealed record ClickEvent(
    Guid LinkId,
    DateTime Timestamp,
    string? Referrer,
    string? UserAgent,
    string? Country
);
