using SpaceCore;

namespace QualityRings.Framework;
internal abstract class AbstractCraftingProfession : Skills.Skill.Profession
{

    protected AbstractCraftingProfession(CraftingSkill skill, string id)
        : base(skill, id)
    {
    }

    public override string GetDescription() => throw new NotImplementedException();

    public override string GetName() => throw new NotImplementedException();
}
