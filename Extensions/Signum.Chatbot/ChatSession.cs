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

    public bool IsToolCall { get; set; }

    [StringLengthValidator(MultiLine = true)]
    public string? Message { get; set; }

    [StringLengthValidator(MultiLine = true)]
    public string? ToolDescriptions { get; set; }

    [StringLengthValidator(MultiLine = true)]
    public string? ToolID { get; set; }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(IsToolCall) && IsToolCall && Role != ChatMessageRole.Assistant)
            return ValidationMessage._0ShouldBe1.NiceToString(pi.NiceName(), false);

        if(pi.Name == nameof(ToolID) && ToolID != null && Role != ChatMessageRole.Tool)
            return ValidationMessage._0ShouldBeNull.NiceToString(pi.NiceName());

        if (pi.Name == nameof(ToolDescriptions) && ToolID != null && (Role != ChatMessageRole.System || Role != ChatMessageRole.Tool))
            return ValidationMessage._0ShouldBeNull.NiceToString(pi.NiceName());

        return base.PropertyValidation(pi);
    }
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
    UsingInternalTool,
    ReceivingInstructions,
}
