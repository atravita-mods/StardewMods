namespace SpecialOrdersExtended.Integrations;

/// <summary>
/// API to interface with Spacecore's custom skills.
/// </summary>
internal interface ISpaceCoreAPI
{
    /// <summary>
    /// Gets a list of custom skills.
    /// </summary>
    /// <returns>An array of skill IDs, one for each registered skill.</returns>
    string[] GetCustomSkills();

    /// <summary>
    /// Gets the level for a custom skill.
    /// </summary>
    /// <param name="farmer"></param>
    /// <param name="skill"></param>
    /// <returns></returns>
    int GetLevelForCustomSkill(Farmer farmer, string skill);

    /// <summary>
    /// Gets the integer professionID for a specific skill and profession.
    /// </summary>
    /// <param name="skill"></param>
    /// <param name="profession"></param>
    /// <returns></returns>
    int GetProfessionId(string skill, string profession);
}