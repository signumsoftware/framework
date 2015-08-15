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
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.Mailing
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class NewsletterEntity : Entity, IProcessDataEntity
    {
        [NotNullable, SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        public NewsletterState State { get; set; } = NewsletterState.Created;

        [NotNullable, SqlDbType(Size = 100)]
        [EMailValidator, StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string From { get; set; }

        [NotNullable, SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string DisplayFrom { get; set; }

        [SqlDbType(Size = 300)]
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 300)]
        public string Subject { get; set; }

        [Ignore]
        internal object SubjectParsedNode;

        [SqlDbType(Size = int.MaxValue)]
        [StringLengthValidator(AllowNulls = true, Min = 3, MultiLine = true)]
        public string Text { get; set; }

        [Ignore]
        internal object TextParsedNode;

        static StateValidator<NewsletterEntity, NewsletterState> stateValidator = new StateValidator<NewsletterEntity, NewsletterState>
            (n => n.State, n => n.Subject, n => n.Text)
            {
                { NewsletterState.Created, null,           null },
                { NewsletterState.Saved,   null,           null },
                { NewsletterState.Sent,    true,           true },
            };

        protected override string PropertyValidation(PropertyInfo pi)
        {
            return stateValidator.Validate(this, pi) ?? base.PropertyValidation(pi);
        }

        static readonly Expression<Func<NewsletterEntity, string>> ToStringExpression = e => e.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }


        public QueryEntity Query { get; set; }
    }

    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class NewsletterDeliveryEntity : Entity, IProcessLineDataEntity
    {
        public bool Sent { get; set; }

        [DateTimePrecissionValidator(DateTimePrecision.Seconds)]
        public DateTime? SendDate { get; set; }

        public Lite<IEmailOwnerEntity> Recipient { get; set; }

        public Lite<NewsletterEntity> Newsletter { get; set; }
    }

    public static class NewsletterProcess
    {
        public static ProcessAlgorithmSymbol SendNewsletter;
    }

    [AutoInit]
    public static class NewsletterOperation
    {
        public static ExecuteSymbol<NewsletterEntity> Save;
        public static ConstructSymbol<ProcessEntity>.From<NewsletterEntity> Send;
        public static ExecuteSymbol<NewsletterEntity> AddRecipients;
        public static ExecuteSymbol<NewsletterEntity> RemoveRecipients;
        public static ConstructSymbol<NewsletterEntity>.From<NewsletterEntity> Clone;
    }

    public enum NewsletterState
    {
        Created,
        Saved,
        Sent
    }
}
