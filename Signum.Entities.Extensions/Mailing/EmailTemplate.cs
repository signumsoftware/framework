using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Entities.UserQueries;
using Signum.Entities.DynamicQuery;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.ComponentModel;
using Signum.Entities.Translation;
using System.Reflection;
using Signum.Entities.UserAssets;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities;
using Signum.Entities.Templating;

namespace Signum.Entities.Mailing
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class EmailTemplateEntity : Entity
    {
        public EmailTemplateEntity()
        {
            RebindEvents();
        }

        public EmailTemplateEntity(object queryName) : this()
        {
            this.queryName = queryName;
        }

        [Ignore]
        internal object queryName;

        [UniqueIndex]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        public bool EditableMessage { get; set; } = true;

        public bool DisableAuthorization { get; set; }

        [NotNullValidator]
        public QueryEntity Query { get; set; }

        public SystemEmailEntity SystemEmail { get; set; }

        public bool SendDifferentMessages { get; set; }

        public EmailTemplateContactEmbedded From { get; set; }

        [NotNullValidator, NoRepeatValidator]
        public MList<EmailTemplateRecipientEntity> Recipients { get; set; } = new MList<EmailTemplateRecipientEntity>();

        [PreserveOrder]
        [NotNullValidator, NoRepeatValidator, ImplementedBy(typeof(ImageAttachmentEntity)), NotifyChildProperty]
        public MList<IAttachmentGeneratorEntity> Attachments { get; set; } = new MList<IAttachmentGeneratorEntity>();

        public Lite<EmailMasterTemplateEntity> MasterTemplate { get; set; }

        public bool IsBodyHtml { get; set; } = true;

        [NotNullValidator, NotifyCollectionChanged, NotifyChildProperty]
        public MList<EmailTemplateMessageEmbedded> Messages { get; set; } = new MList<EmailTemplateMessageEmbedded>();

        [NotifyChildProperty]
        public TemplateApplicableEval Applicable { get; set; }
        
        protected override string PropertyValidation(System.Reflection.PropertyInfo pi)
        {
            if (pi.Name == nameof(Messages))
            {
                if (Messages == null || !Messages.Any())
                    return EmailTemplateMessage.ThereAreNoMessagesForTheTemplate.NiceToString();

                if (Messages.GroupCount(m => m.CultureInfo).Any(c => c.Value > 1))
                    return EmailTemplateMessage.TheresMoreThanOneMessageForTheSameLanguage.NiceToString();
            }

            return base.PropertyValidation(pi);
        }

        static readonly Expression<Func<EmailTemplateEntity, string>> ToStringExpression = e => e.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        internal void ParseData(QueryDescription queryDescription)
        {
            if (Recipients != null)
                foreach (var r in Recipients.Where(r => r.Token != null))
                    r.Token.ParseData(this, queryDescription, SubTokensOptions.CanElement);

            if (From != null && From.Token != null)
                From.Token.ParseData(this, queryDescription, SubTokensOptions.CanElement);
        }

        public bool IsApplicable(Entity entity)
        {
            if (Applicable == null)
                return true;

            try
            {
                return Applicable.Algorithm.ApplicableUntyped(entity);
            }
            catch (Exception e)
            {
                throw new ApplicationException($"Error evaluating Applicable for EmailTemplate '{Name}' with entity '{entity}': " + e.Message, e);
            }
        }
    }

    [Serializable]
    public class EmailTemplateContactEmbedded : EmbeddedEntity
    {
        public QueryTokenEmbedded Token { get; set; }

        public string EmailAddress { get; set; }

        public string DisplayName { get; set; }

        public override string ToString()
        {
            return "{0} <{1}>".FormatWith(DisplayName, EmailAddress);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(Token))
            {
                if (Token == null && EmailAddress.IsNullOrEmpty())
                    return EmailTemplateMessage.TokenOrEmailAddressMustBeSet.NiceToString();

                if (Token != null && !EmailAddress.IsNullOrEmpty())
                    return EmailTemplateMessage.TokenAndEmailAddressCanNotBeSetAtTheSameTime.NiceToString();

                if (Token != null && Token.Token.Type != typeof(EmailOwnerData))
                    return EmailTemplateMessage.TokenMustBeA0.NiceToString(typeof(EmailOwnerData).NiceName());
            }

            return null;
        }
    }

    [Serializable]
    public class EmailTemplateRecipientEntity : EmailTemplateContactEmbedded
    {
        public EmailRecipientKind Kind { get; set; }

        public override string ToString()
        {
            return "{0} {1} <{2}>".FormatWith(Kind.NiceToString(), DisplayName, EmailAddress);
        }
    }

    [Serializable]
    public class EmailTemplateMessageEmbedded : EmbeddedEntity
    {
        private EmailTemplateMessageEmbedded() { }

        public EmailTemplateMessageEmbedded(CultureInfoEntity culture)
        {
            this.CultureInfo = culture;
        }

        [NotNullValidator]
        public CultureInfoEntity CultureInfo { get; set; }

        [SqlDbType(Size = int.MaxValue)]
        string text;
        [StringLengthValidator(AllowNulls = false, MultiLine=true)]
        public string Text
        {
            get { return text; }
            set
            {
                if (Set(ref text, value))
                    TextParsedNode = null;
            }
        }

        [Ignore]
        internal object TextParsedNode;

        string subject;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 200)]
        public string Subject
        {
            get { return subject; }
            set
            {
                if (Set(ref subject, value))
                    SubjectParsedNode = null;
            }
        }

        [Ignore]
        internal object SubjectParsedNode;

        public override string ToString()
        {
            return CultureInfo?.ToString() ?? EmailTemplateMessage.NewCulture.NiceToString();
        }
    }

    public interface IAttachmentGeneratorEntity : IEntity
    {
    }

    [AutoInit]
    public static class EmailTemplateOperation
    {
        public static ConstructSymbol<EmailTemplateEntity>.From<SystemEmailEntity> CreateEmailTemplateFromSystemEmail;
        public static ConstructSymbol<EmailTemplateEntity>.Simple Create;
        public static ExecuteSymbol<EmailTemplateEntity> Save;
        public static DeleteSymbol<EmailTemplateEntity> Delete;
    }

    public enum EmailTemplateMessage
    {
        [Description("End date must be higher than start date")]
        EndDateMustBeHigherThanStartDate,
        [Description("There are no messages for the template")]
        ThereAreNoMessagesForTheTemplate,
        [Description("There must be a message for {0}")]
        ThereMustBeAMessageFor0,
        [Description("There's more than one message for the same language")]
        TheresMoreThanOneMessageForTheSameLanguage,
        [Description("The text must contain {0} indicating replacement point")]
        TheTextMustContain0IndicatingReplacementPoint,
        [Description("Impossible to access {0} because the template has no {1}")]
        ImpossibleToAccess0BecauseTheTemplateHAsNo1,
        NewCulture,
        TokenOrEmailAddressMustBeSet,
        TokenAndEmailAddressCanNotBeSetAtTheSameTime,
        [Description("Token must be a {0}")]
        TokenMustBeA0,
        ShowPreview,
        HidePreview
    }

    public enum EmailTemplateViewMessage
    {
        [Description("Insert message content")]
        InsertMessageContent,
        [Description("Insert")]
        Insert,
        [Description("Language")]
        Language
    }

    [InTypeScript(true)]
    public enum EmailTemplateVisibleOn
    {
        Single = 1,
        Multiple = 2,
        Query = 4
    }
}
