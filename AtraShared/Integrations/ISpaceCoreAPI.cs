namespace AtraShared.Integrations;

/// <summary>
/// API to interface with Spacecore's custom skills.
/// </summary>
public interface ISpaceCoreAPI
{
    /// <summary>
    /// Gets a list of custom skills.
    /// </summary>
    /// <returns>An array of skill IDs, one for each registered skill.</returns>
    string[] GetCustomSkills();

    /// <summary>
    /// Gets the level for a custom skill.
    /// </summary>
    /// <param name="farmer">Farmer to look in.</param>
    /// <param name="skill">String name of skill.</param>
    /// <returns>Level for the custom skill if found, 0 otherwise.</returns>
    int GetLevelForCustomSkill(Farmer farmer, string skill);

    /// <summary>
    /// Gets the integer professionID for a specific skill and profession.
    /// </summary>
    /// <param name="skill">string name of skill.</param>
    /// <param name="profession">string name for profession.</param>
    /// <exception cref="InvalidOperationException">LINQ single failed, likely profession not found.</exception>
    /// <exception cref="NullReferenceException">Search for skill failed, likely skill not found.</exception>
    /// <returns>integer profession ID.</returns>
    int GetProfessionId(string skill, string profession);
}