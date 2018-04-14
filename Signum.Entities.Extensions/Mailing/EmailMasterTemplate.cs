using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Signum.Entities.Basics;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.Mailing
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class EmailMasterTemplateEntity : Entity
    {
        [UniqueIndex]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        [NotifyCollectionChanged, NotNullValidator]
        public MList<EmailMasterTemplateMessageEmbedded> Messages { get; set; } = new MList<EmailMasterTemplateMessageEmbedded>();

        public static readonly Regex MasterTemplateContentRegex = new Regex(@"\@\[content\]");

        static Expression<Func<EmailMasterTemplateEntity, string>> ToStringExpression = e => e.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        protected override string PropertyValidation(System.Reflection.PropertyInfo pi)
        {
            if (pi.Name == nameof(Messages))
            {
                if (Messages == null || !Messages.Any())
                    return EmailTemplateMessage.ThereAreNoMessagesForTheTemplate.NiceToString();

                if (Messages.GroupBy(m => m.CultureInfo).Any(g => g.Count() > 1))
                    return EmailTemplateMessage.TheresMoreThanOneMessageForTheSameLanguage.NiceToString();
            }

            return base.PropertyValidation(pi);
        }

        protected override void ChildCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (sender == Messages)
            {
                if (args.OldItems != null)
                    foreach (var item in args.OldItems.Cast<EmailMasterTemplateMessageEmbedded>())
                        item.MasterTemplate = null;

                if (args.NewItems != null)
                    foreach (var item in args.NewItems.Cast<EmailMasterTemplateMessageEmbedded>())
                        item.MasterTemplate = this;
            }
        }

        protected override void PreSaving(PreSavingContext ctx)
        {
            base.PreSaving(ctx);

            Messages.ForEach(e => e.MasterTemplate = this);
        }
    }

    [AutoInit]
    public static class EmailMasterTemplateOperation
    {
        public static ConstructSymbol<EmailMasterTemplateEntity>.Simple Create;
        public static ExecuteSymbol<EmailMasterTemplateEntity> Save;
    }

    [Serializable]
    public class EmailMasterTemplateMessageEmbedded : EmbeddedEntity
    {
        private EmailMasterTemplateMessageEmbedded() { }

        public EmailMasterTemplateMessageEmbedded(CultureInfoEntity culture)
        {
            this.CultureInfo = culture;
        }

        [Ignore]
        internal EmailMasterTemplateEntity masterTemplate;
        [InTypeScript(false)]
        public EmailMasterTemplateEntity MasterTemplate
        {
            get { return masterTemplate; }
            set { masterTemplate = value; }
        }

        [NotNullValidator]
        public CultureInfoEntity CultureInfo { get; set; }

        [StringLengthValidator(AllowNulls = false, MultiLine = true)]
        public string Text { get; set; }

        public override string ToString()
        {
            return CultureInfo?.ToString();
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(Text) && !EmailMasterTemplateEntity.MasterTemplateContentRegex.IsMatch(Text))
            {
                return EmailTemplateMessage.TheTextMustContain0IndicatingReplacementPoint.NiceToString().FormatWith("@[content]");
            }

            return base.PropertyValidation(pi);
        }
    }
}
