using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Linq.Expressions;
using Signum.Utilities;

namespace Signum.Entities.SMS
{
    public enum SMSProviderOperation
    {
        SendSMSMessage,
        SendSMSMessagesFromTemplate
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
        CreateSMS,
        Send,
        UpdateStatus,
        CreateUpdateStatusPackage,
        CreateSMSMessageFromTemplate
    }

    public enum SMSMessageProcess
    {
        Send,
        UpdateStatus
    }

    [Serializable, EntityType(EntityType.Main)]
    public class SMSMessageDN : Entity
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

        static readonly Expression<Func<SMSMessageDN, string>> ToStringExpression = e => "SMS " + e.MessageID;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }
}
