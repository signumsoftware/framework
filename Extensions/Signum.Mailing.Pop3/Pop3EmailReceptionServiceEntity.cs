using Signum.Mailing.Reception;

namespace Signum.Mailing.Pop3;

[EntityKind(EntityKind.Part, EntityData.Master)]
public class Pop3EmailReceptionServiceEntity : EmailReceptionServiceEntity
{
    public int Port { get; set; } = 110;

    [StringLengthValidator(Min = 3, Max = 100)]
    public string Host { get; set; }

    [StringLengthValidator(Max = 100)]
    public string? Username { get; set; }

    [StringLengthValidator(Max = 100)]
    public string? Password { get; set; }

    bool enableSSL;
    public bool EnableSSL
    {
        get { return enableSSL; }
        set
        {
            if (Set(ref enableSSL, value))
            {
                Port = enableSSL ? 995 : 110;
            }
        }
    }

    [NumberIsValidator(ComparisonType.GreaterThanOrEqualTo, -1), Unit("ms")]
    public int ReadTimeout { get; set; } = 60000;

    public MList<ClientCertificationFileEmbedded> ClientCertificationFiles { get; set; } = new MList<ClientCertificationFileEmbedded>();

    public override string ToString()
    {
        return "{0} ({1})".FormatWith(Username, Host);
    }
}
