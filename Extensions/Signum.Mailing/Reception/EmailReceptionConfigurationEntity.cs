using Signum.Mailing;
using Signum.Scheduler;

namespace Signum.Mailing.Reception;

[EntityKind(EntityKind.Shared, EntityData.Master)]
public class EmailReceptionConfigurationEntity : Entity, ITaskEntity
{
    public bool Active { get; set; }

    [StringLengthValidator(Max = 100)]
    public string EmailAddress { get; set; }

    [Unit("d")]
    public int? DeleteMessagesAfter { get; set; } = 14;

    public CompareInbox CompareInbox { get; set; }

    [ImplementedBy()]
    public EmailReceptionServiceEntity Service { get; set; }
}


public enum CompareInbox
{
    Full,
    LastNEmails,
}


[EntityKind(EntityKind.Part, EntityData.Master)]
public abstract class EmailReceptionServiceEntity : Entity
{

}



[AutoInit]
public static class EmailReceptionConfigurationOperation
{
    public static ExecuteSymbol<EmailReceptionConfigurationEntity> Save;
    public static ConstructSymbol<EmailReceptionEntity>.From<EmailReceptionConfigurationEntity> ReceiveEmails;
    public static ConstructSymbol<EmailReceptionEntity>.From<EmailReceptionConfigurationEntity> ReceiveLastEmails;

}

[AutoInit]
public static class EmailReceptionAction
{
    public static SimpleTaskSymbol ReceiveAllActiveEmailConfigurations;
}

[EntityKind(EntityKind.System, EntityData.Transactional)]
public class EmailReceptionEntity : Entity
{   
    public Lite<EmailReceptionConfigurationEntity> EmailReceptionConfiguration { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int NewEmails { get; set; }
    public int ServerEmails { get; set; }
    public string? LastServerMessageUID { get; set; }
    public bool MailsFromDifferentAccounts { get; set; }
    public Lite<ExceptionEntity>? Exception { get; set; }
}


[EntityKind(EntityKind.System, EntityData.Transactional)]
public class EmailReceptionExceptionEntity : Entity
{
    
    public Lite<EmailReceptionEntity> Reception { get; set; }

    //[UniqueIndex]
    public Lite<ExceptionEntity> Exception { get; set; }
}
