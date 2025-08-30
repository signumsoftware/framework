namespace Signum.Chatbot.Agents;

public class SearchSkill : ChatbotSkill
{
    public SearchSkill()
    {
        ShortDescription = "Explores the database schema to search any information in the database";
        IsAllowed = () => true;
    }
}
