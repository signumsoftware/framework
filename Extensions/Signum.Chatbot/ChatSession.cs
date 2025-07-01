using Signum.Authorization;
using Signum.Entities;
using Signum.Entities.Validation;
using Signum.Operations;
using Signum.Security;
using System;


namespace Signum.Chatbot;

[EntityKind(EntityKind.System, EntityData.Transactional)]
public class ChatSessionEntity : Entity
{
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

    public DateTime DateTime { get; set; }

    public ChatMessageRole Role { get; set; }

    [StringLengthValidator(MultiLine = true)]
    public string Message { get; set; }
}

public enum ChatMessageRole
{
    System,
    User,
    Assistant,
    Tool, 
}

[AutoInit]
public static class ChatMessageOperation
{
    public static readonly ExecuteSymbol<ChatMessageEntity> Save;
    public static readonly DeleteSymbol<ChatMessageEntity> Delete;
}
