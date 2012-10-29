using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Entities.Extensions.Properties;
using Signum.Entities.UserQueries;
using Signum.Entities.DynamicQuery;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Signum.Entities.Mailing
{
    [Serializable]
    public class EmailTemplateOldDN : IdentifiableEntity
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

        static readonly Expression<Func<EmailTemplateOldDN, string>> ToStringExpression = e => e.friendlyName;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public enum EmailTemplateState
    {
        Created,
        Modified
    }

    public enum EmailTemplateOperations
    {
        Crear,
        Modificar
    }

    [Serializable]
    public class EmailTemplateDN : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, () => Name); }
        }

        bool editableMessage = true;
        public bool EditableMessage
        {
            get { return editableMessage; }
            set { Set(ref editableMessage, value, () => EditableMessage); }
        }

        [NotNullable]
        TypeDN associatedType;
        [NotNullValidator]
        public TypeDN AssociatedType
        {
            get { return associatedType; }
            set { Set(ref associatedType, value, () => AssociatedType); }
        }

        TemplateQueryTokenDN recipient;
        public TemplateQueryTokenDN Recipient
        {
            get { return recipient; }
            set { Set(ref recipient, value, () => Recipient); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string text;
        [StringLengthValidator(AllowNulls = false, Max = int.MaxValue)]
        public string Text
        {
            get { return text; }
            set { Set(ref text, value, () => Text); }
        }

        bool isBodyHtml = true;
        public bool IsBodyHtml
        {
            get { return isBodyHtml; }
            set { Set(ref isBodyHtml, value, () => IsBodyHtml); }
        }

        MList<TemplateQueryTokenDN> tokens;
        public MList<TemplateQueryTokenDN> Tokens
        {
            get { return tokens; }
            set { Set(ref tokens, value, () => Tokens); }
        }

        string from;
        [StringLengthValidator(AllowNulls = false)]
        public string From
        {
            get { return from; }
            set { Set(ref from, value, () => From); }
        }

        string displayFrom;
        public string DisplayFrom
        {
            get { return displayFrom; }
            set { Set(ref displayFrom, value, () => DisplayFrom); }
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

        Lite<EmailMasterTemplateDN> masterTemplate;
        public Lite<EmailMasterTemplateDN> MasterTemplate
        {
            get { return masterTemplate; }
            set { Set(ref masterTemplate, value, () => MasterTemplate); }
        }

        Lite<SMTPConfigurationDN> smtpConfiguration;
        public Lite<SMTPConfigurationDN> SMTPConfiguration
        {
            get { return smtpConfiguration; }
            set { Set(ref smtpConfiguration, value, () => SMTPConfiguration); }
        }

        SystemTemplateDN systemTemplate;
        public SystemTemplateDN SystemTemplate
        {
            get { return systemTemplate; }
            set { Set(ref systemTemplate, value, () => SystemTemplate); }
        }

        string cultureInfo;
        public string CultureInfo
        {
            get { return cultureInfo; }
            set { Set(ref cultureInfo, value, () => CultureInfo); }
        }

        public CultureInfo GetCultureInfo
        {
            get { return CultureInfo.HasText() ? new CultureInfo(CultureInfo) : System.Globalization.CultureInfo.InvariantCulture; }
        }

        EmailTemplateState state = EmailTemplateState.Created;
        public EmailTemplateState State
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

        static Expression<Func<EmailTemplateDN, bool>> IsActiveNowExpression =
            (mt) => mt.active && TimeZoneManager.Now.IsInInterval(mt.StartDate, mt.EndDate);
        public bool IsActiveNow()
        {
            return IsActiveNowExpression.Evaluate(this);
        }

        public static Func<TypeDN, bool> AssociatedTypeIsEmailOwner;

        protected override string PropertyValidation(System.Reflection.PropertyInfo pi)
        {
            if (pi.Is(() => StartDate) || pi.Is(() => EndDate))
            {
                if (EndDate != null && EndDate >= StartDate)
                    return Resources.EndDateMustBeHigherThanStartDate;
            }

            if (pi.Is(() => Recipient) && AssociatedType != null && !AssociatedTypeIsEmailOwner(AssociatedType))
            {
                return Resources.RouteToGetRecipientNotSet;
            }

            return base.PropertyValidation(pi);
        }

        static readonly Expression<Func<EmailTemplateDN, string>> ToStringExpression = e => e.Name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [Serializable]
    public class SystemTemplateDN : EnumDN
    {
    }

    [Serializable]
    public class TemplateQueryTokenDN : QueryTokenDN
    {
        public override void ParseData(Func<DynamicQuery.QueryToken, List<DynamicQuery.QueryToken>> subTokens)
        {
            throw new InvalidOperationException("ParseData is ambiguous on {0}".Formato(GetType().NiceName()));
        }
    }

    [Serializable]
    public class EmailMasterTemplateDN : Entity
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

        EmailTemplateState state = EmailTemplateState.Created;
        public EmailTemplateState State
        {
            get { return state; }
            set { Set(ref state, value, () => State); }
        }

        static Expression<Func<EmailMasterTemplateDN, string>> ToStringExpression = e => e.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        [Ignore]
        public static readonly Regex MasterTemplateContentRegex = new Regex(@"\@\[content\]");

        protected override string PropertyValidation(System.Reflection.PropertyInfo pi)
        {
            if (pi.Is(() => Text) && !MasterTemplateContentRegex.IsMatch(Text))
            {
                throw new ApplicationException(Resources.TheTextMustContaintThe0IndicatingReplacementPoint.Formato("@[content]"));
            }

            return base.PropertyValidation(pi);
        }
    }
}
