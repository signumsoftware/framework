using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Entities.Extensions.Properties;

namespace Signum.Entities.Mailing
{
    [Serializable]
    public class EmailTemplateDN : IdentifiableEntity
    {
        [NotNullable, UniqueIndex]
        string fullClassName;
        public string FullClassName
        {
            get { return fullClassName; }
            set { Set(ref fullClassName, value, () => FullClassName); }
        }

        [NotNullable]
        string friendlyName;
        [StringLengthValidator(Min = 1)]
        public string FriendlyName
        {
            get { return friendlyName; }
            set { Set(ref friendlyName, value, () => FriendlyName); }
        }

        static readonly Expression<Func<EmailTemplateDN, string>> ToStringExpression = e => e.friendlyName;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [Serializable]
    public class EmailMessageTemplateDN : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, () => Name); }
        }

        public static bool AllowEditMessages = true;

        bool editableMessage = AllowEditMessages;
        public bool EditableMessage
        {
            get { return editableMessage; }
            set { Set(ref editableMessage, value, () => EditableMessage); }
        }

        TypeDN associatedType;
        public TypeDN AssociatedType
        {
            get { return associatedType; }
            set { Set(ref associatedType, value, () => AssociatedType); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string text;
        [StringLengthValidator(AllowNulls = false, Max = int.MaxValue)]
        public string Text
        {
            get { return text; }
            set { Set(ref text, value, () => Text); }
        }

        public static string DefaultFrom;

        string from = DefaultFrom;
        [StringLengthValidator(AllowNulls = false)]
        public string From
        {
            get { return from; }
            set { Set(ref from, value, () => From); }
        }

        string bcc;
        public string Bcc
        {
            get { return bcc; }
            set { Set(ref bcc, value, () => Bcc); }
        }

        string cc;
        public string Cc
        {
            get { return cc; }
            set { Set(ref cc, value, () => Cc); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string subject;
        [StringLengthValidator(AllowNulls = false, Min = 3)]
        public string Subject
        {
            get { return subject; }
            set { Set(ref subject, value, () => Subject); }
        }

        Lite<VirtualEmailMessageTemplateDN> virtualTemplate;
        public Lite<VirtualEmailMessageTemplateDN> VirtualTemplate
        {
            get { return virtualTemplate; }
            set { Set(ref virtualTemplate, value, () => VirtualTemplate); }
        }

        EmailMessageTemplateStates state = EmailMessageTemplateStates.Created;
        public EmailMessageTemplateStates State
        {
            get { return state; }
            set { Set(ref state, value, () => State); }
        }

        bool active;
        public bool Active
        {
            get { return active; }
            set { Set(ref active, value, () => Active); }
        }

        DateTime startDate = TimeZoneManager.Now.TrimToMinutes();
        [MinutesPrecissionValidator]
        public DateTime StartDate
        {
            get { return startDate; }
            set { Set(ref startDate, value, () => StartDate); }
        }

        DateTime? endDate;
        [MinutesPrecissionValidator]
        public DateTime? EndDate
        {
            get { return endDate; }
            set { Set(ref endDate, value, () => EndDate); }
        }

        static Expression<Func<EmailMessageTemplateDN, bool>> IsActiveNowExpression =
            (mt) => mt.active && TimeZoneManager.Now.IsInInterval(mt.StartDate, mt.EndDate);
        public bool IsActiveNow()
        {
            return IsActiveNowExpression.Evaluate(this);
        }

        protected override string PropertyValidation(System.Reflection.PropertyInfo pi)
        {
            if (pi.Is(() => StartDate) || pi.Is(() => EndDate))
            {
                if (EndDate != null && EndDate >= StartDate)
                    return Resources.EndDateMustBeHigherThanStartDate;
            }

            return base.PropertyValidation(pi);
        }

        static readonly Expression<Func<EmailMessageTemplateDN, string>> ToStringExpression = e => e.Name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public static Func<EmailMessageTemplateDN, object, string> ComposeText;

        public EmailMessageDN CreateEmailMessage(Lite<IEmailOwnerDN> recipient, object arg)
        {
            return new EmailMessageDN 
            {
                Template = null,
                Subject = this.Subject,
                Bcc = this.Bcc,
                Cc = this.Cc,
                //From
                Recipient = recipient,
                Body = ComposeText(this, arg) //compose + virtual
            };
        }
    }

    public enum EmailMessageTemplateStates
    { 
        Created,
        Modified
    }

    [Serializable]
    public class VirtualEmailMessageTemplateDN : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, () => Name); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string text;
        [StringLengthValidator(AllowNulls = false, Max = int.MaxValue)]
        public string Text
        {
            get { return text; }
            set { Set(ref text, value, () => Text); }
        }

        EmailMessageTemplateStates state = EmailMessageTemplateStates.Created;
        public EmailMessageTemplateStates State
        {
            get { return state; }
            set { Set(ref state, value, () => State); }
        }

        static Expression<Func<VirtualEmailMessageTemplateDN, string>> ToStringExpression = e => e.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }
}
