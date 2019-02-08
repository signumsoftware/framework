using System;
using System.Linq;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Entities.Basics;
using System.ComponentModel;
using System.Collections.Specialized;

namespace Signum.Entities.SMS
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class SMSTemplateEntity : Entity
    {
        [StringLengthValidator(Min = 3, Max = 100)]
        public string Name { get; set; }

        public bool Certified { get; set; }

        public bool EditableMessage { get; set; } = AllowEditMessages;

        public TypeEntity? AssociatedType { get; set; }

        [NotifyCollectionChanged]
        public MList<SMSTemplateMessageEmbedded> Messages { get; set; } = new MList<SMSTemplateMessageEmbedded>();

        [StringLengthValidator(Max = 200)]
        public string From { get; set; }

        public MessageLengthExceeded MessageLengthExceeded { get; set; } = MessageLengthExceeded.NotAllowed;

        public bool RemoveNoSMSCharacters { get; set; } = true;

        public bool Active { get; set; }

        [MinutesPrecisionValidator]
        public DateTime StartDate { get; set; } = TimeZoneManager.Now.TrimToMinutes();

        [MinutesPrecisionValidator]
        public DateTime? EndDate { get; set; }


        static Expression<Func<SMSTemplateEntity, bool>> IsActiveNowExpression =
            (mt) => mt.Active && TimeZoneManager.Now.IsInInterval(mt.StartDate, mt.EndDate);
        [ExpressionField]
        public bool IsActiveNow()
        {
            return IsActiveNowExpression.Evaluate(this);
        }

        public static bool AllowEditMessages = true;

        protected override string? PropertyValidation(System.Reflection.PropertyInfo pi)
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
        
        public CultureInfoEntity CultureInfo { get; set; }

        [StringLengthValidator(MultiLine = true)]
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
