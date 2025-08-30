using Signum.Authorization;
using Signum.Entities;
using Signum.Entities.Validation;
using Signum.Operations;
using Signum.Security;
using System;
using System.ComponentModel;


namespace Signum.Chatbot;

[EntityKind(EntityKind.System, EntityData.Transactional)]
public class ChatSessionEntity : Entity
{
    public string? Title { get; set; }

    public Lite<ChatbotLanguageModelEntity> LanguageModel { get; set; }

    public Lite<UserEntity> User { get; set; }

    public DateTime StartDate { get; set; }


    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Title ?? BaseToString());
}

[AutoInit]
public static class ChatSessionOperation
{
    public static readonly DeleteSymbol<ChatSessionEntity> Delete;
}


[EntityKind(EntityKind.System, EntityData.Transactional)]
public class ChatMessageEntity : Entity
{
    public Lite<ChatSessionEntity> ChatSession { get; set; }

    public DateTime CreationDate { get; set; } = Clock.Now;

    public ChatMessageRole Role { get; set; }

    [StringLengthValidator(MultiLine = true)] // The arguments of the tool call if role is Assistant and ToolID is not null
    public string? Content { get; set; }

    [PreserveOrder, NoRepeatValidator]
    public MList<ToolCallEmbedded> ToolCalls { get; set; } = new MList<ToolCallEmbedded>();

    // For Tool role responses
    [StringLengthValidator(Max = 100)]
    public string? ToolCallID { get; set; }

    [StringLengthValidator(Max = 100)]
    public string? ToolID { get; set; }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if(pi.Name == nameof(ToolID) && ToolID != null && Role != ChatMessageRole.Tool)
            return ValidationMessage._0ShouldBeNull.NiceToString(pi.NiceName());

        if (pi.Name == nameof(ToolCallID) && ToolCallID != null && Role != ChatMessageRole.Tool)
            return ValidationMessage._0ShouldBeNull.NiceToString(pi.NiceName());

        if (pi.Name == nameof(Content) && Content == null && Role != ChatMessageRole.Assistant)
            return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());

        return base.PropertyValidation(pi);
    }
}

public class ToolCallEmbedded : EmbeddedEntity
{
    [StringLengthValidator(Max = 100)]
    public string CallId { get; set; }

    [StringLengthValidator(Max = 100)]
    public string ToolID { get; set; }

    [StringLengthValidator(Max = 100)]
    public string Arguments { get; set; }
}

public enum ChatMessageRole
{
    System,    //Prompts
    User,      //Question user
    Assistant, //Answer LLM (maybe a command to a tool / prompt)
    Tool,      //Answer toool
}

[AutoInit]
public static class ChatMessageOperation
{
    public static readonly ExecuteSymbol<ChatMessageEntity> Save;
    public static readonly DeleteSymbol<ChatMessageEntity> Delete;
}

public enum ChatbotMessage
{
    OpenSession,
    NewSession,
    Send,
    [Description("Type a message...")]
    TypeAMessage,
    InitialInstruction,
}
