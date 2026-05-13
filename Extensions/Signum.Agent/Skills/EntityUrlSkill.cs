namespace Signum.Agent.Skills;

public class EntityUrlSkill : SkillCode
{
    public EntityUrlSkill()
    {
        ShortDescription = "Explains how to construct local URLs to navigate to entities in the application";
        IsAllowed = () => true;
        Replacements = new Dictionary<string, Func<object?, string>>()
        {
            { "UrlLeft", obj => CurrentServerContextSkill.UrlLeft?.Invoke() ?? "" },
        };
    }
}
