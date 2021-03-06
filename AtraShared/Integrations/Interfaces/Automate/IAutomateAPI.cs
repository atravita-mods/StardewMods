using Microsoft.Xna.Framework;

namespace AtraShared.Integrations.Interfaces.Automate;

/// <summary>
/// The API for Automate.
/// </summary>
public interface IAutomateAPI
{
    /// <summary>Add an automation factory.</summary>
    /// <param name="factory">An automation factory which construct machines, containers, and connectors.</param>
    void AddFactory(IAutomationFactory factory);
}
