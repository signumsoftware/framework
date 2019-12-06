using System;
using Signum.Entities.Processes;
using Signum.Entities.Basics;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Entities.Scheduler;
using System.ComponentModel;

namespace Signum.Entities.SMS
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class SMSMessageEntity : Entity, IProcessLineDataEntity
    {
        public Lite<SMSTemplateEntity>? Template { get; set; }

        [StringLengthValidator(MultiLine = true)]
        public string Message { get; set; }

        public bool EditableMessage { get; set; } = true;

        [StringLengthValidator(Max = 200)]
        public string? From { get; set; }

        [SecondsPrecisionValidator]
        public DateTime? SendDate { get; set; }

        public SMSMessageState State { get; set; } = SMSMessageState.Created;

        [StringLengthValidator(Min = 9), MultipleTelephoneValidator]
        public string DestinationNumber { get; set; }

        [StringLengthValidator(Max = 100)]
        public string? MessageID { get; set; }

        public bool Certified { get; set; }

        public Lite<SMSSendPackageEntity>? SendPackage { get; set; }

        Lite<SMSUpdatePackageEntity>? updatePackage;
        public Lite<SMSUpdatePackageEntity>? UpdatePackage
        {
            get { return updatePackage; }
            set
            {
                if (Set(ref updatePackage, value))
                    UpdatePackageProcessed = false;
            }
        }

        public bool UpdatePackageProcessed { get; set; }

        [ImplementedByAll()]
        public Lite<ISMSOwnerEntity>? Referred { get; set; }

        public Lite<ExceptionEntity>? Exception { get; set; }

        public override string ToString()
        {
            return "SMS " + MessageID;
        }
    }

    public enum SMSMessageState
    {
        Created,
        Sent,
        SendFailed,
        Delivered,
        DeliveryFailed,
    }

    [AutoInit]
    public static class SMSMessageOperation
    {
        public static ExecuteSymbol<SMSMessageEntity> Send;
        public static ExecuteSymbol<SMSMessageEntity> UpdateStatus;
        public static ConstructSymbol<ProcessEntity>.FromMany<SMSMessageEntity> CreateUpdateStatusPackage;
        public static ConstructSymbol<SMSMessageEntity>.From<SMSTemplateEntity> CreateSMSFromTemplate;
        public static ConstructSymbol<ProcessEntity>.FromMany<Entity> SendMultipleSMSMessages;
    }

    [Serializable]
    public class MultipleSMSModel : ModelEntity
    {
        [StringLengthValidator(Max = SMSCharacters.SMSMaxTextLength)]
        public string Message { get; set; }

        public string From { get; set; }

        public bool Certified { get; set; }
    }

    [AutoInit]
    public static class SMSMessageProcess
    {
        public static ProcessAlgorithmSymbol Send;
        public static ProcessAlgorithmSymbol UpdateStatus;
    }

    [AutoInit]
    public static class SMSMessageTask
    {
        public static SimpleTaskSymbol UpdateSMSStatus;
    }

}
