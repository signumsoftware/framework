using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Entities.Processes;
using Signum.Entities.Mailing;
using Signum.Utilities;
using Signum.Entities.Basics;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Entities.UserQueries;

namespace Signum.Entities.Mailing
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class NewsletterDN : Entity, IProcessDataDN
    {
        [NotNullable, SqlDbType(Size = 100)]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, () => Name); }
        }

        NewsletterState state = NewsletterState.Created;
        public NewsletterState State
        {
            get { return state; }
            set { Set(ref state, value, () => State); }
        }

        Lite<SmtpConfigurationDN> smtpConfig = DefaultSmtpConfig;
        public Lite<SmtpConfigurationDN> SmtpConfig
        {
            get { return smtpConfig; }
            set { Set(ref smtpConfig, value, () => SmtpConfig); }
        }

        [NotNullable, SqlDbType(Size = 50)]
        string from;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 50)]
        public string From
        {
            get { return from; }
            set { Set(ref from, value, () => From); }
        }

        [NotNullable, SqlDbType(Size = 50)]
        string displayFrom;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 50)]
        public string DisplayFrom
        {
            get { return displayFrom; }
            set { Set(ref displayFrom, value, () => DisplayFrom); }
        }

        [SqlDbType(Size = 300)]
        string subject;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 300)]
        public string Subject
        {
            get { return subject; }
            set { Set(ref subject, value, () => Subject); }
        }

        [Ignore]
        internal object SubjectParsedNode;

        [SqlDbType(Size = int.MaxValue)]
        string text;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = int.MaxValue)]
        public string Text
        {
            get { return text; }
            set { Set(ref text, value, () => Text); }
        }

        [Ignore]
        internal object TextParsedNode;

        static StateValidator<NewsletterDN, NewsletterState> stateValidator = new StateValidator<NewsletterDN, NewsletterState>
            (     n => n.State,            n => n.Subject, n => n.Text)
            {
                { NewsletterState.Created, null,           null },
                { NewsletterState.Saved,   null,           null },
                { NewsletterState.Sent,    true,           true },
            };

        protected override string PropertyValidation(PropertyInfo pi)
        {
            return stateValidator.Validate(this, pi) ?? base.PropertyValidation(pi);
        }

        static readonly Expression<Func<NewsletterDN, string>> ToStringExpression = e => e.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public static Lite<SmtpConfigurationDN> DefaultSmtpConfig;

        QueryDN query;
        public QueryDN Query
        {
            get { return query; }
            set { Set(ref query, value, () => Query); }
        }

        MList<QueryTokenDN> tokens = new MList<QueryTokenDN>();
        public MList<QueryTokenDN> Tokens
        {
            get { return tokens; }
            set { Set(ref tokens, value, () => Tokens); }
        }

        internal void ParseData(QueryDescription queryDescription)
        {
            if (Tokens != null)
                foreach (var t in Tokens)
                    t.ParseData(this, queryDescription, false);
        }
    }

    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class NewsletterDeliveryDN : Entity, IProcessLineDataDN
    {
        bool sent;
        public bool Sent
        {
            get { return sent; }
            set { Set(ref sent, value, () => Sent); }
        }

        DateTime? sendDate;
        [DateTimePrecissionValidator(DateTimePrecision.Seconds)]
        public DateTime? SendDate
        {
            get { return sendDate; }
            set { Set(ref sendDate, value, () => SendDate); }
        }

        Lite<IEmailOwnerDN> recipient;
        public Lite<IEmailOwnerDN> Recipient
        {
            get { return recipient; }
            set { Set(ref recipient, value, () => Recipient); }
        }

        Lite<NewsletterDN> newsletter;
        public Lite<NewsletterDN> Newsletter
        {
            get { return newsletter; }
            set { Set(ref newsletter, value, () => Newsletter); }
        }
    }


    public enum NewsletterOperation
    {
        Save,
        Send,
        AddRecipients,
        RemoveRecipients,
        Clone,
    }

    public enum NewsletterState
    { 
        Created,
        Saved,
        Sent
    }
}
