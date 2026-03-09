### Current Date and Time

The tool `getCurrentDate` returns the current date and time from the server:

- **LocalDateTime**: the server's current local date and time (ISO 8601, e.g. `2025-06-15T14:30:00.0000000+02:00`)
- **UtcDateTime**: the current UTC date and time (ISO 8601, e.g. `2025-06-15T12:30:00.0000000Z`)
- **TimeZoneId**: the server's time zone identifier (e.g. `Europe/Madrid`, `UTC`, `Eastern Standard Time`)
- **TimeZoneOffsetUtc**: the UTC offset of the server's local time zone (e.g. `02:00:00`)

IMPORTANT: Use `getCurrentDate` when the user asks about the current date, time, or time zone, or when you need to compute relative dates (e.g. "last month", "next week", "today").
