namespace Signum.Agent.Skills;

public class EntityUrlSkill : ChatbotSkill
{
    public EntityUrlSkill()
    {
        ShortDescription = "Explains how to construct local URLs to navigate to entities in the application";
        IsAllowed = () => true;
    }
}
