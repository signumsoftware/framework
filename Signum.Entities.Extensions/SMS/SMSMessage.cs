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
    public enum SMSProviderOperation
    {
        SendSMSMessage,
        SendSMSMessagesFromTemplate
    }

    [Serializable, EntityKind(EntityKind.Main)]
    public class SMSMessageDN : Entity, IProcessLineDN
    {
        public static string DefaultFrom;

        Lite<SMSTemplateDN> template;
        public Lite<SMSTemplateDN> Template
        {
            get { return template; }
            set { Set(ref template, value, () => Template); }
        }

        string message;
        [StringLengthValidator(AllowNulls = false, Max = SMSCharacters.SMSMaxTextLength)]
        public string Message
        {
            get { return message; }
            set { Set(ref message, value, () => Message); }
        }

        bool editableMessage = true;
        public bool EditableMessage
        {
            get { return editableMessage; }
            set { Set(ref editableMessage, value, () => EditableMessage); }
        }

        string from = DefaultFrom;
        [StringLengthValidator(AllowNulls = false)]
        public string From
        {
            get { return from; }
            set { Set(ref from, value, () => From); }
        }

        DateTime? sendDate;
        [SecondsPrecissionValidator]
        public DateTime? SendDate
        {
            get { return sendDate; }
            set { Set(ref sendDate, value, () => SendDate); }
        }

        SMSMessageState state = SMSMessageState.Created;
        public SMSMessageState State
        {
            get { return state; }
            set { Set(ref state, value, () => State); }
        }

        [NotNullable]
        string destinationNumber;
        [StringLengthValidator(AllowNulls = false, Min = 9, Max = 20), TelephoneValidator]
        public string DestinationNumber
        {
            get { return destinationNumber; }
            set { Set(ref destinationNumber, value, () => DestinationNumber); }
        }

        [SqlDbType(Size = 100)]
        string messageID;
        [StringLengthValidator(AllowNulls = true, Max = 100)]
        public string MessageID
        {
            get { return messageID; }
            set { Set(ref messageID, value, () => MessageID); }
        }

        bool certified;
        public bool Certified
        {
            get { return certified; }
            set { Set(ref certified, value, () => Certified); }
        }

        Lite<SMSSendPackageDN> sendpackage;
        public Lite<SMSSendPackageDN> SendPackage
        {
            get { return sendpackage; }
            set { Set(ref sendpackage, value, () => SendPackage); }
        }

        Lite<SMSUpdatePackageDN> updatePackage;
        public Lite<SMSUpdatePackageDN> UpdatePackage
        {
            get { return updatePackage; }
            set { Set(ref updatePackage, value, () => UpdatePackage); }
        }

        [ImplementedBy()]
        Lite<IdentifiableEntity> referred;
        public Lite<IdentifiableEntity> Referred
        {
            get { return referred; }
            set { Set(ref referred, value, () => Referred); }
        }

        public override string ToString()
        {
            return "SMS " + MessageID;
        }

        Lite<ExceptionDN> exception;
        public Lite<ExceptionDN> Exception
        {
            get { return exception; }
            set { Set(ref exception, value, () => Exception); }
        }
    }

    public enum SMSMessageState
    {
        Created,
        Sent,
        Delivered,
        Failed,
    }

    public enum SMSMessageOperation
    {
        Send,
        UpdateStatus,
        CreateUpdateStatusPackage,
        CreateSMSFromSMSTemplate,
        CreateSMSWithTemplateFromEntity,
        CreateSMSFromEntity,
    }

    public enum SMSMessageProcess
    {
        Send,
        UpdateStatus
    }
}
