using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Signum.Entities.Basics;
using static System.Int32;

namespace Signum.Entities.Rest
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional), InTypeScript(Undefined = false)]
    public class RestLogEntity : Entity
    {
        [StringLengthValidator(Max = 100)]
        public string? HttpMethod { get; set; }

        [ForceNotNullable, SqlDbType(Size = MaxValue)]
        public string Url { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public DateTime? ReplayDate { get; set; }

        [SqlDbType(Size = MaxValue)]
        public string? RequestBody { get; set; }

        [PreserveOrder]
        public MList<QueryStringValueEmbedded> QueryString { get; set; } = new MList<QueryStringValueEmbedded>();

        public Lite<IUserEntity>? User { get; set; }

        [SqlDbType(Size = int.MaxValue)]
        public string? UserHostAddress { get; set; }

        [SqlDbType(Size = int.MaxValue)]
        public string? UserHostName { get; set; }

        [SqlDbType(Size = int.MaxValue)]
        public string? Referrer { get; set; }

        [SqlDbType(Size = 100)]
        public string Controller { get; set; }

        [SqlDbType(Size = 100)]
        public string? ControllerName { get; set; }

        [SqlDbType(Size = 100)]
        public string Action { get; set; }

        [SqlDbType(Size = 100)]
        public string? MachineName { get; set; }

        [SqlDbType(Size = 100)]
        public string? ApplicationName { get; set; }

        public Lite<ExceptionEntity>? Exception { get; set; }

        [SqlDbType(Size = MaxValue)]
        public string? ResponseBody { get; set; }

        public RestLogReplayState? ReplayState { get; set; }

        public double? ChangedPercentage { get; set; }

        public bool AllowReplay { get; set; }

        static Expression<Func<RestLogEntity, double?>> DurationExpression =
          log => (double?)(log.EndDate - log.StartDate).TotalMilliseconds;

        [Unit("ms"), ExpressionField("DurationExpression")]
        public double? Duration => DurationExpression.Evaluate(this);
    }

    [Serializable]
    public class QueryStringValueEmbedded : EmbeddedEntity
    {
        [ForceNotNullable, SqlDbType(Size = MaxValue)]
        public string Key { get; set; }

        [SqlDbType(Size = MaxValue)]
        public string Value { get; set; }
    }

    public enum RestLogReplayState
    {
        NoChanges,
        WithChanges
    }

    public class RestDiffResult
    {
        public string? previous { get; set; }
        public string current { get; set; }
        public List<StringDistance.DiffPair<List<StringDistance.DiffPair<string>>>> diff { get; set; }
    }
}
