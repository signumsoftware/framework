using System;
using Signum.Entities.Basics;
using Signum.Entities.Scheduler;
using Signum.Utilities;

namespace Signum.Entities.Mailing
{
    [Serializable, EntityKind(EntityKind.Shared, EntityData.Master)]
    public class Pop3ConfigurationEntity : Entity, ITaskEntity
    {
        public bool Active { get; set; }

        public bool FullComparation { get; set; }

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

        [Unit("d")]
        public int? DeleteMessagesAfter { get; set; } = 14;

        
        public MList<ClientCertificationFileEmbedded> ClientCertificationFiles { get; set; } = new MList<ClientCertificationFileEmbedded>();

        public override string ToString()
        {
            return "{0} ({1})".FormatWith(Username, Host);
        }

    }

    [AutoInit]
    public static class Pop3ConfigurationOperation
    {
        public static ExecuteSymbol<Pop3ConfigurationEntity> Save;
        public static ConstructSymbol<Pop3ReceptionEntity>.From<Pop3ConfigurationEntity> ReceiveEmails;
    }

    [AutoInit]
    public static class Pop3ConfigurationAction
    {
        public static SimpleTaskSymbol ReceiveAllActivePop3Configurations;
    }

    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class Pop3ReceptionEntity : Entity
    {
        
        public Lite<Pop3ConfigurationEntity> Pop3Configuration { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public int NewEmails { get; set; }

        public Lite<ExceptionEntity>? Exception { get; set; }
    }


    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class Pop3ReceptionExceptionEntity : Entity
    {
        
        public Lite<Pop3ReceptionEntity> Reception { get; set; }

        [UniqueIndex]
        
        public Lite<ExceptionEntity> Exception { get; set; }
    }
}
