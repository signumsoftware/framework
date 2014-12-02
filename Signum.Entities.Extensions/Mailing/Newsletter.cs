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
    public class NewsletterEntity : Entity, IProcessDataEntity
    {
        [NotNullable, SqlDbType(Size = 100)]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value); }
        }

        NewsletterState state = NewsletterState.Created;
        public NewsletterState State
        {
            get { return state; }
            set { Set(ref state, value); }
        }

        Lite<SmtpConfigurationEntity> smtpConfig = DefaultSmtpConfig();
        public Lite<SmtpConfigurationEntity> SmtpConfig
        {
            get { return smtpConfig; }
            set { Set(ref smtpConfig, value); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string from;
        [EMailValidator, StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string From
        {
            get { return from; }
            set { Set(ref from, value); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string displayFrom;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string DisplayFrom
        {
            get { return displayFrom; }
            set { Set(ref displayFrom, value); }
        }

        [SqlDbType(Size = 300)]
        string subject;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 300)]
        public string Subject
        {
            get { return subject; }
            set { Set(ref subject, value); }
        }

        [Ignore]
        internal object SubjectParsedNode;

        [SqlDbType(Size = int.MaxValue)]
        string text;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = int.MaxValue)]
        public string Text
        {
            get { return text; }
            set { Set(ref text, value); }
        }

        [Ignore]
        internal object TextParsedNode;

        static StateValidator<NewsletterEntity, NewsletterState> stateValidator = new StateValidator<NewsletterEntity, NewsletterState>
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

        static readonly Expression<Func<NewsletterEntity, string>> ToStringExpression = e => e.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public static Func<Lite<SmtpConfigurationEntity>> DefaultSmtpConfig;

        QueryEntity query;
        public QueryEntity Query
        {
            get { return query; }
            set { Set(ref query, value); }
        }
    }

    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class NewsletterDeliveryEntity : Entity, IProcessLineDataEntity
    {
        bool sent;
        public bool Sent
        {
            get { return sent; }
            set { Set(ref sent, value); }
        }

        DateTime? sendDate;
        [DateTimePrecissionValidator(DateTimePrecision.Seconds)]
        public DateTime? SendDate
        {
            get { return sendDate; }
            set { Set(ref sendDate, value); }
        }

        Lite<IEmailOwnerEntity> recipient;
        public Lite<IEmailOwnerEntity> Recipient
        {
            get { return recipient; }
            set { Set(ref recipient, value); }
        }

        Lite<NewsletterEntity> newsletter;
        public Lite<NewsletterEntity> Newsletter
        {
            get { return newsletter; }
            set { Set(ref newsletter, value); }
        }
    }

    public static class NewsletterProcess
    {
        public static readonly ProcessAlgorithmSymbol SendNewsletter = new ProcessAlgorithmSymbol();
    }

    public static class NewsletterOperation
    {
        public static readonly ExecuteSymbol<NewsletterEntity> Save = OperationSymbol.Execute<NewsletterEntity>();
        public static readonly ConstructSymbol<ProcessEntity>.From<NewsletterEntity> Send = OperationSymbol.Construct<ProcessEntity>.From<NewsletterEntity>();
        public static readonly ExecuteSymbol<NewsletterEntity> AddRecipients = OperationSymbol.Execute<NewsletterEntity>();
        public static readonly ExecuteSymbol<NewsletterEntity> RemoveRecipients = OperationSymbol.Execute<NewsletterEntity>();
        public static readonly ConstructSymbol<NewsletterEntity>.From<NewsletterEntity> Clone = OperationSymbol.Construct<NewsletterEntity>.From<NewsletterEntity>();
    }

    public enum NewsletterState
    { 
        Created,
        Saved,
        Sent
    }
}
