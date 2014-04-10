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
    public static class SMSProviderOperation
    {
        public static readonly ExecuteSymbol<SMSMessageDN> SendSMSMessage = OperationSymbol.Execute<SMSMessageDN>();
        public static readonly ExecuteSymbol<SMSMessageDN> SendSMSMessagesFromTemplate = OperationSymbol.Execute<SMSMessageDN>();
    }

    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class SMSMessageDN : Entity, IProcessLineDataDN
    {
        Lite<SMSTemplateDN> template;
        public Lite<SMSTemplateDN> Template
        {
            get { return template; }
            set { Set(ref template, value); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string message;
        [NotNullValidator]
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

        Lite<SMSSendPackageDN> sendpackage;
        public Lite<SMSSendPackageDN> SendPackage
        {
            get { return sendpackage; }
            set { Set(ref sendpackage, value); }
        }

        Lite<SMSUpdatePackageDN> updatePackage;
        public Lite<SMSUpdatePackageDN> UpdatePackage
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
        Lite<IdentifiableEntity> referred;
        public Lite<IdentifiableEntity> Referred
        {
            get { return referred; }
            set { Set(ref referred, value); }
        }

        Lite<ExceptionDN> exception;
        public Lite<ExceptionDN> Exception
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
        public static readonly ExecuteSymbol<SMSMessageDN> Send = OperationSymbol.Execute<SMSMessageDN>();
        public static readonly ExecuteSymbol<SMSMessageDN> UpdateStatus = OperationSymbol.Execute<SMSMessageDN>();
        public static readonly ExecuteSymbol<SMSMessageDN> CreateUpdateStatusPackage = OperationSymbol.Execute<SMSMessageDN>();
        public static readonly ConstructFromSymbol<SMSTemplateDN, SMSMessageDN> CreateSMSFromSMSTemplate = OperationSymbol.ConstructFrom<SMSTemplateDN, SMSMessageDN>();
        public static readonly ConstructFromSymbol<IdentifiableEntity, SMSMessageDN> CreateSMSWithTemplateFromEntity = OperationSymbol.ConstructFrom<IdentifiableEntity, SMSMessageDN>();
        public static readonly ConstructFromSymbol<IdentifiableEntity, SMSMessageDN> CreateSMSFromEntity = OperationSymbol.ConstructFrom<IdentifiableEntity, SMSMessageDN>();
    }

    public static class SMSMessageProcess
    {
        public static readonly ProcessAlgorithmSymbol Send = new ProcessAlgorithmSymbol();
        public static readonly ProcessAlgorithmSymbol UpdateStatus = new ProcessAlgorithmSymbol();
    }
}
