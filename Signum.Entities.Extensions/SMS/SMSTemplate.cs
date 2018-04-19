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
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.SMS
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class SMSTemplateEntity : Entity
    {
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        public bool Certified { get; set; }

        public bool EditableMessage { get; set; } = AllowEditMessages;

        public TypeEntity AssociatedType { get; set; }

        [NotifyCollectionChanged]
        public MList<SMSTemplateMessageEmbedded> Messages { get; set; } = new MList<SMSTemplateMessageEmbedded>();

        [StringLengthValidator(AllowNulls = false, Max = 200)]
        public string From { get; set; }

        public MessageLengthExceeded MessageLengthExceeded { get; set; } = MessageLengthExceeded.NotAllowed;

        public bool RemoveNoSMSCharacters { get; set; } = true;

        public bool Active { get; set; }

        [MinutesPrecissionValidator]
        public DateTime StartDate { get; set; } = TimeZoneManager.Now.TrimToMinutes();

        [MinutesPrecissionValidator]
        public DateTime? EndDate { get; set; }

        static Expression<Func<SMSTemplateEntity, bool>> IsActiveNowExpression =
            (mt) => mt.Active && TimeZoneManager.Now.IsInInterval(mt.StartDate, mt.EndDate);
        [ExpressionField]
        public bool IsActiveNow()
        {
            return IsActiveNowExpression.Evaluate(this);
        }

        protected override string PropertyValidation(System.Reflection.PropertyInfo pi)
        {
            if (pi.Name == nameof(StartDate) || pi.Name == nameof(EndDate))
            {
                if (EndDate != null && StartDate >= EndDate)
                    return SMSTemplateMessage.EndDateMustBeHigherThanStartDate.NiceToString();
            }

            if (pi.Name == nameof(Messages))
            {
                if (Messages == null || !Messages.Any())
                    return SMSTemplateMessage.ThereAreNoMessagesForTheTemplate.NiceToString();

                if (Messages.GroupCount(m => m.CultureInfo).Any(c => c.Value > 1))
                    return SMSTemplateMessage.TheresMoreThanOneMessageForTheSameLanguage.NiceToString();
            }

            return base.PropertyValidation(pi);
        }

        static readonly Expression<Func<SMSTemplateEntity, string>> ToStringExpression = e => e.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        protected override void ChildCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (sender == Messages)
            {
                if (args.OldItems != null)
                    foreach (var item in args.OldItems.Cast<SMSTemplateMessageEmbedded>())
                        item.Template = null;

                if (args.NewItems != null)
                    foreach (var item in args.NewItems.Cast<SMSTemplateMessageEmbedded>())
                        item.Template = this;
            }
        }

        protected override void PreSaving(PreSavingContext ctx)
        {
            base.PreSaving(ctx);

            Messages.ForEach(e => e.Template = this);
        }

        public static bool AllowEditMessages = true;
    }

    [AutoInit]
    public static class SMSTemplateOperation
    {
        public static ConstructSymbol<SMSTemplateEntity>.Simple Create;
        public static ExecuteSymbol<SMSTemplateEntity> Save;
    }

    public enum MessageLengthExceeded
    {
        NotAllowed,
        Allowed,
        TextPruning,
    }

    [Serializable]
    public class SMSTemplateMessageEmbedded : EmbeddedEntity
    {
        public SMSTemplateMessageEmbedded() { }

        public SMSTemplateMessageEmbedded(CultureInfoEntity culture)
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

        [NotNullValidator]
        public CultureInfoEntity CultureInfo { get; set; }

        [StringLengthValidator(AllowNulls = false, MultiLine = true)]
        public string Message { get; set; }

        public override string ToString()
        {
            return CultureInfo?.ToString() ?? SMSTemplateMessage.NewCulture.NiceToString();
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
