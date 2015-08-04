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
        Lite<SMSTemplateEntity> template;
        public Lite<SMSTemplateEntity> Template
        {
            get { return template; }
            set { Set(ref template, value); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string message;
        [StringLengthValidator(AllowNulls=false, MultiLine = true)]
        public string Message
        {
            get { return message; }
            set { Set(ref message, value); }
        }

        bool editableMessage = true;
        public bool EditableMessage
        {
            get { return editableMessage; }
            set { Set(ref editableMessage, value); }
        }

        string from;
        [StringLengthValidator(AllowNulls = false)]
        public string From
        {
            get { return from; }
            set { Set(ref from, value); }
        }

        DateTime? sendDate;
        [SecondsPrecissionValidator]
        public DateTime? SendDate
        {
            get { return sendDate; }
            set { Set(ref sendDate, value); }
        }

        SMSMessageState state = SMSMessageState.Created;
        public SMSMessageState State
        {
            get { return state; }
            set { Set(ref state, value); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string destinationNumber;
        [StringLengthValidator(AllowNulls = false, Min = 9), MultipleTelephoneValidator]
        public string DestinationNumber
        {
            get { return destinationNumber; }
            set { Set(ref destinationNumber, value); }
        }

        [SqlDbType(Size = 100)]
        string messageID;
        [StringLengthValidator(AllowNulls = true, Max = 100)]
        public string MessageID
        {
            get { return messageID; }
            set { Set(ref messageID, value); }
        }

        bool certified;
        public bool Certified
        {
            get { return certified; }
            set { Set(ref certified, value); }
        }

        Lite<SMSSendPackageEntity> sendpackage;
        public Lite<SMSSendPackageEntity> SendPackage
        {
            get { return sendpackage; }
            set { Set(ref sendpackage, value); }
        }

        Lite<SMSUpdatePackageEntity> updatePackage;
        public Lite<SMSUpdatePackageEntity> UpdatePackage
        {
            get { return updatePackage; }
            set
            {
                if(Set(ref updatePackage, value))
                    UpdatePackageProcessed = false;
            }
        }

        bool updatePackageProcessed;
        public bool UpdatePackageProcessed
        {
            get { return updatePackageProcessed; }
            set { Set(ref updatePackageProcessed, value); }
        }

        [ImplementedBy()]
        Lite<Entity> referred;
        public Lite<Entity> Referred
        {
            get { return referred; }
            set { Set(ref referred, value); }
        }

        Lite<ExceptionEntity> exception;
        public Lite<ExceptionEntity> Exception
        {
            get { return exception; }
            set { Set(ref exception, value); }
        }

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

    public static class SMSMessageOperation
    {
        public static readonly ExecuteSymbol<SMSMessageEntity> Send = OperationSymbol.Execute<SMSMessageEntity>();
        public static readonly ExecuteSymbol<SMSMessageEntity> UpdateStatus = OperationSymbol.Execute<SMSMessageEntity>();
        public static readonly ConstructSymbol<ProcessEntity>.FromMany<SMSMessageEntity> CreateUpdateStatusPackage = OperationSymbol.Construct<ProcessEntity>.FromMany<SMSMessageEntity>();
        public static readonly ConstructSymbol<SMSMessageEntity>.From<SMSTemplateEntity> CreateSMSFromSMSTemplate = OperationSymbol.Construct<SMSMessageEntity>.From<SMSTemplateEntity>();
        public static readonly ConstructSymbol<SMSMessageEntity>.From<Entity> CreateSMSWithTemplateFromEntity = OperationSymbol.Construct<SMSMessageEntity>.From<Entity>();
        public static readonly ConstructSymbol<SMSMessageEntity>.From<Entity> CreateSMSFromEntity = OperationSymbol.Construct<SMSMessageEntity>.From<Entity>();

        public static readonly ConstructSymbol<ProcessEntity>.FromMany<Entity> SendSMSMessages = OperationSymbol.Construct<ProcessEntity>.FromMany<Entity>();
        public static readonly ConstructSymbol<ProcessEntity>.FromMany<Entity> SendSMSMessagesFromTemplate = OperationSymbol.Construct<ProcessEntity>.FromMany<Entity>();
    }

    [Serializable]
    public class MultipleSMSModel : ModelEntity
    {
        string message;
        [StringLengthValidator(AllowNulls = false, Max = SMSCharacters.SMSMaxTextLength)]
        public string Message
        {
            get { return message; }
            set { Set(ref message, value); }
        }

        string from;
        [StringLengthValidator(AllowNulls = false)]
        public string From
        {
            get { return from; }
            set { Set(ref from, value); }
        }

        bool certified;
        public bool Certified
        {
            get { return certified; }
            set { Set(ref certified, value); }
        }
    }

    public static class SMSMessageProcess
    {
        public static readonly ProcessAlgorithmSymbol Send = new ProcessAlgorithmSymbol();
        public static readonly ProcessAlgorithmSymbol UpdateStatus = new ProcessAlgorithmSymbol();
    }
}
