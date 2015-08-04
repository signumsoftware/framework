using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Entities.Basics;
using Signum.Entities.Translation;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Globalization;

namespace Signum.Entities.SMS
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class SMSTemplateEntity : Entity
    {
        [SqlDbType(Size = 100)]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { Set(ref name, value); }
        }

        bool certified;
        public bool Certified
        {
            get { return certified; }
            set { Set(ref certified, value); }
        }

        bool editableMessage = AllowEditMessages;
        public bool EditableMessage
        {
            get { return editableMessage; }
            set { Set(ref editableMessage, value); }
        }

        TypeEntity associatedType;
        public TypeEntity AssociatedType
        {
            get { return associatedType; }
            set { Set(ref associatedType, value); }
        }

        [NotifyCollectionChanged]
        MList<SMSTemplateMessageEntity> messages = new MList<SMSTemplateMessageEntity>();
        public MList<SMSTemplateMessageEntity> Messages
        {
            get { return messages; }
            set { Set(ref messages, value); }
        }

        string from;
        [StringLengthValidator(AllowNulls = false)]
        public string From
        {
            get { return from; }
            set { Set(ref from, value); }
        }

        MessageLengthExceeded messageLengthExceeded = MessageLengthExceeded.NotAllowed;
        public MessageLengthExceeded MessageLengthExceeded
        {
            get { return messageLengthExceeded; }
            set { Set(ref messageLengthExceeded, value); }
        }

        bool removeNoSMSCharacters = true;
        public bool RemoveNoSMSCharacters
        {
            get { return removeNoSMSCharacters; }
            set { Set(ref removeNoSMSCharacters, value); }
        }

        bool active;
        public bool Active
        {
            get { return active; }
            set { Set(ref active, value); }
        }

        DateTime startDate = TimeZoneManager.Now.TrimToMinutes();
        [MinutesPrecissionValidator]
        public DateTime StartDate
        {
            get { return startDate; }
            set { Set(ref startDate, value); }
        }

        DateTime? endDate;
        [MinutesPrecissionValidator]
        public DateTime? EndDate
        {
            get { return endDate; }
            set { Set(ref endDate, value); }
        }

        static Expression<Func<SMSTemplateEntity, bool>> IsActiveNowExpression =
            (mt) => mt.active && TimeZoneManager.Now.IsInInterval(mt.StartDate, mt.EndDate);
        public bool IsActiveNow()
        {
            return IsActiveNowExpression.Evaluate(this);
        }

        protected override string PropertyValidation(System.Reflection.PropertyInfo pi)
        {
            if (pi.Is(() => StartDate) || pi.Is(() => EndDate))
            {
                if (EndDate != null && StartDate >= EndDate)
                    return SMSTemplateMessage.EndDateMustBeHigherThanStartDate.NiceToString();
            }

            if (pi.Is(() => Messages))
            {
                if (Messages == null || !Messages.Any())
                    return SMSTemplateMessage.ThereAreNoMessagesForTheTemplate.NiceToString();

                if (Messages.GroupCount(m => m.CultureInfo).Any(c => c.Value > 1))
                    return SMSTemplateMessage.TheresMoreThanOneMessageForTheSameLanguage.NiceToString();
            }

            return base.PropertyValidation(pi);
        }

        static readonly Expression<Func<SMSTemplateEntity, string>> ToStringExpression = e => e.Name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        protected override void ChildCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (sender == messages)
            {
                if (args.OldItems != null)
                    foreach (var item in args.OldItems.Cast<SMSTemplateMessageEntity>())
                        item.Template = null;

                if (args.NewItems != null)
                    foreach (var item in args.NewItems.Cast<SMSTemplateMessageEntity>())
                        item.Template = this;
            }
        }

        protected override void PreSaving(ref bool graphModified)
        {
            base.PreSaving(ref graphModified);

            messages.ForEach(e => e.Template = this);
        }

        public static bool AllowEditMessages = true;
    }

    public static class SMSTemplateOperation
    {
        public static readonly ConstructSymbol<SMSTemplateEntity>.Simple Create = OperationSymbol.Construct<SMSTemplateEntity>.Simple();
        public static readonly ExecuteSymbol<SMSTemplateEntity> Save = OperationSymbol.Execute<SMSTemplateEntity>();
    }

    public enum MessageLengthExceeded
    {
        NotAllowed,
        Allowed,
        TextPruning,
    }

    [Serializable]
    public class SMSTemplateMessageEntity : EmbeddedEntity
    {
        public SMSTemplateMessageEntity() { }

        public SMSTemplateMessageEntity(CultureInfoEntity culture)
        {
            this.CultureInfo = culture;
        }

        [Ignore]
        internal SMSTemplateEntity template;
        public SMSTemplateEntity Template
        {
            get { return template; }
            set { template = value; }
        }

        [NotNullable]
        CultureInfoEntity cultureInfo;
        [NotNullValidator]
        public CultureInfoEntity CultureInfo
        {
            get { return cultureInfo; }
            set { Set(ref cultureInfo, value); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string message;
        [StringLengthValidator(AllowNulls = false, MultiLine = true)]
        public string Message
        {
            get { return message; }
            set { Set(ref message, value); }
        }

        public override string ToString()
        {
            return cultureInfo.TryToString() ?? SMSTemplateMessage.NewCulture.NiceToString();
        }
    }

    public enum SMSTemplateMessage
    {
        [Description("End date must be higher than start date")]
        EndDateMustBeHigherThanStartDate,
        [Description("There are no messages for the template")]
        ThereAreNoMessagesForTheTemplate,
        [Description("There must be a message for {0}")]
        ThereMustBeAMessageFor0,
        [Description("There's more than one message for the same language")]
        TheresMoreThanOneMessageForTheSameLanguage,
        NewCulture
    }
}
