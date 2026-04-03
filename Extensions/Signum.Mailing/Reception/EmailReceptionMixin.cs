namespace Signum.Mailing.Reception;

public class EmailReceptionMixin : MixinEntity
{
    protected EmailReceptionMixin(ModifiableEntity mainEntity, MixinEntity next) : base(mainEntity, next)
    {
        this.BindParent();
    }

    [BindParent]
    public EmailReceptionInfoEmbedded? ReceptionInfo { get; set; }
}

public class EmailReceptionInfoEmbedded : EmbeddedEntity
{
    public EmailReceptionInfoEmbedded()
    {
        this.BindParent();
    }

    [UniqueIndex]
    [StringLengthValidator(Min = 1, Max = 100)]
    public string UniqueId { get; set; }


    public Lite<EmailReceptionEntity> Reception { get; set; }

    [BindParent]
    public BigStringEmbedded RawContent { get; set; } = new BigStringEmbedded();

    [DbTypeAttribute(DateTimeKind = DateTimeKind.Utc)]
    public DateTime SentDate { get; set; }

    public DateTime ReceivedDate { get; set; }

    public DateTime? DeletionDate { get; set; }
}
