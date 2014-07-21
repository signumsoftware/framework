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

namespace Signum.Entities.Mailing
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class EmailMasterTemplateDN : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value); }
        }

        [NotifyCollectionChanged, NotNullable]
        MList<EmailMasterTemplateMessageDN> messages = new MList<EmailMasterTemplateMessageDN>();
        public MList<EmailMasterTemplateMessageDN> Messages
        {
            get { return messages; }
            set { Set(ref messages, value); }
        }

        [Ignore]
        public static readonly Regex MasterTemplateContentRegex = new Regex(@"\@\[content\]");

        static Expression<Func<EmailMasterTemplateDN, string>> ToStringExpression = e => e.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        protected override string PropertyValidation(System.Reflection.PropertyInfo pi)
        {
            if (pi.Is(() => Messages))
            {
                if (Messages == null || !Messages.Any())
                    return EmailTemplateMessage.ThereAreNoMessagesForTheTemplate.NiceToString();

                if (Messages.GroupCount(m => m.CultureInfo).Any(c => c.Value > 1))
                    return EmailTemplateMessage.TheresMoreThanOneMessageForTheSameLanguage.NiceToString();
            }

            return base.PropertyValidation(pi);
        }

        protected override void ChildCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (sender == messages)
            {
                if (args.OldItems != null)
                    foreach (var item in args.OldItems.Cast<EmailMasterTemplateMessageDN>())
                        item.MasterTemplate = null;

                if (args.NewItems != null)
                    foreach (var item in args.NewItems.Cast<EmailMasterTemplateMessageDN>())
                        item.MasterTemplate = this;
            }
        }

        protected override void PreSaving(ref bool graphModified)
        {
            base.PreSaving(ref graphModified);

            messages.ForEach(e => e.MasterTemplate = this);
        }
    }

    public static class EmailMasterTemplateOperation
    {
        public static readonly ConstructSymbol<EmailMasterTemplateDN>.Simple Create = OperationSymbol.Construct<EmailMasterTemplateDN>.Simple();
        public static readonly ExecuteSymbol<EmailMasterTemplateDN> Save = OperationSymbol.Execute<EmailMasterTemplateDN>();
    }

    [Serializable]
    public class EmailMasterTemplateMessageDN : EmbeddedEntity
    {
        private EmailMasterTemplateMessageDN() { }

        public EmailMasterTemplateMessageDN(CultureInfoDN culture)
        {
            this.CultureInfo = culture;
        }

        [Ignore]
        internal EmailMasterTemplateDN masterTemplate;
        public EmailMasterTemplateDN MasterTemplate
        {
            get { return masterTemplate; }
            set { masterTemplate = value; }
        }

        [NotNullable]
        CultureInfoDN cultureInfo;
        [NotNullValidator]
        public CultureInfoDN CultureInfo
        {
            get { return cultureInfo; }
            set { Set(ref cultureInfo, value); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string text;
        [StringLengthValidator(AllowNulls = false, Max = int.MaxValue)]
        public string Text
        {
            get { return text; }
            set { Set(ref text, value); }
        }

        public override string ToString()
        {
            return cultureInfo.TryToString();
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => Text) && !EmailMasterTemplateDN.MasterTemplateContentRegex.IsMatch(Text))
            {
                throw new ApplicationException(EmailTemplateMessage.TheTextMustContain0IndicatingReplacementPoint.NiceToString().Formato("@[content]"));
            }

            return base.PropertyValidation(pi);
        }
    }
}
