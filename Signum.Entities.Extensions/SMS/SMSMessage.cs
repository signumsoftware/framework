using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Entities.Processes;
using Signum.Entities.Basics;

namespace Signum.Entities.SMS
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class SMSMessageEntity : Entity, IProcessLineDataEntity
    {
        public Lite<SMSTemplateEntity> Template { get; set; }

        [StringLengthValidator(AllowNulls=false, MultiLine = true)]
        public string Message { get; set; }

        public bool EditableMessage { get; set; } = true;

        [StringLengthValidator(AllowNulls = false, Max = 200)]
        public string From { get; set; }

        [SecondsPrecissionValidator]
        public DateTime? SendDate { get; set; }

        public SMSMessageState State { get; set; } = SMSMessageState.Created;

        [StringLengthValidator(AllowNulls = false, Min = 9), MultipleTelephoneValidator]
        public string DestinationNumber { get; set; }

        [StringLengthValidator(AllowNulls = true, Max = 100)]
        public string MessageID { get; set; }

        public bool Certified { get; set; }

        public Lite<SMSSendPackageEntity> SendPackage { get; set; }

        Lite<SMSUpdatePackageEntity> updatePackage;
        public Lite<SMSUpdatePackageEntity> UpdatePackage
        {
            get { return updatePackage; }
            set
            {
                if (Set(ref updatePackage, value))
                    UpdatePackageProcessed = false;
            }
        }

        public bool UpdatePackageProcessed { get; set; }

        [ImplementedBy()]
        public Lite<Entity> Referred { get; set; }

        public Lite<ExceptionEntity> Exception { get; set; }

        public override string ToString()
        {
            return "SMS " + MessageID;
        }
    }

    public enum SMSMessageState
    {
        Created,
        Sent,
        Delivered,
        Failed,
    }

    [AutoInit]
    public static class SMSMessageOperation
    {
        public static ExecuteSymbol<SMSMessageEntity> Send;
        public static ExecuteSymbol<SMSMessageEntity> UpdateStatus;
        public static ConstructSymbol<ProcessEntity>.FromMany<SMSMessageEntity> CreateUpdateStatusPackage;
        public static ConstructSymbol<SMSMessageEntity>.From<SMSTemplateEntity> CreateSMSFromSMSTemplate;
        public static ConstructSymbol<SMSMessageEntity>.From<Entity> CreateSMSWithTemplateFromEntity;
        public static ConstructSymbol<SMSMessageEntity>.From<Entity> CreateSMSFromEntity;

        public static ConstructSymbol<ProcessEntity>.FromMany<Entity> SendSMSMessages;
        public static ConstructSymbol<ProcessEntity>.FromMany<Entity> SendSMSMessagesFromTemplate;
    }

    [Serializable]
    public class MultipleSMSModel : ModelEntity
    {
        [StringLengthValidator(AllowNulls = false, Max = SMSCharacters.SMSMaxTextLength)]
        public string Message { get; set; }

        [StringLengthValidator(AllowNulls = false)]
        public string From { get; set; }

        public bool Certified { get; set; }
    }

    [AutoInit]
    public static class SMSMessageProcess
    {
        public static ProcessAlgorithmSymbol Send;
        public static ProcessAlgorithmSymbol UpdateStatus;
    }
}
