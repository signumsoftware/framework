
namespace Signum.Chatbot.Skills;

public class QuestionSumarizerSkill : ChatbotSkill
{
    public QuestionSumarizerSkill()
    {
        ShortDescription = "Summarizes the user's questions in the conversation";
        IsAllowed = () => true;
        Replacements = new Dictionary<string, Func<object?, string>>()
        {
              { "<Conversation>", obj => ((ConversationHistory)obj!).GetMessages().Where(a=>a.Role == ChatMessageRole.User).Select((a, i) => $"#Question {(i +1)}:#\n{a.Content}").ToString("\n\n").Etc(500) }
        };
    }
}
