namespace SinZsEventTester.Framework;

/// <summary>
/// A record that represents an event in a location.
/// </summary>
/// <param name="location">Name of the location.</param>
/// <param name="eventKey">The event's key.</param>
public readonly record struct EventRecord(string location, string eventKey);
