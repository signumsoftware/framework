using Signum.Entities.Basics;
using static System.Int32;

namespace Signum.Entities.Rest;

[EntityKind(EntityKind.System, EntityData.Transactional)]
public class RestLogEntity : Entity
{
    [StringLengthValidator(Max = 100)]
    public string? HttpMethod { get; set; }

    [ForceNotNullable, DbType(Size = MaxValue)]
    public string Url { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime? ReplayDate { get; set; }

    [DbType(Size = MaxValue)]
    public string? RequestBody { get; set; }

    [PreserveOrder]
    public MList<QueryStringValueEmbedded> QueryString { get; set; } = new MList<QueryStringValueEmbedded>();

    public Lite<IUserEntity>? User { get; set; }

    [DbType(Size = int.MaxValue)]
    public string? UserHostAddress { get; set; }

    [DbType(Size = int.MaxValue)]
    public string? UserHostName { get; set; }

    [DbType(Size = int.MaxValue)]
    public string? Referrer { get; set; }

    [DbType(Size = 100)]
    public string Controller { get; set; }

    [DbType(Size = 100)]
    public string? ControllerName { get; set; }

    [DbType(Size = 100)]
    public string Action { get; set; }

    [DbType(Size = 100)]
    public string? MachineName { get; set; }

    [DbType(Size = 100)]
    public string? ApplicationName { get; set; }

    public Lite<ExceptionEntity>? Exception { get; set; }

    [DbType(Size = MaxValue)]
    public string? ResponseBody { get; set; }

    public RestLogReplayState? ReplayState { get; set; }

    public double? ChangedPercentage { get; set; }

    public bool AllowReplay { get; set; }

    static Expression<Func<RestLogEntity, double?>> DurationExpression =
      log => (double?)(log.EndDate - log.StartDate).TotalMilliseconds;
    [Unit("ms"), ExpressionField("DurationExpression")]
    public double? Duration => DurationExpression.Evaluate(this);
}

public class QueryStringValueEmbedded : EmbeddedEntity
{
    [ForceNotNullable, DbType(Size = MaxValue)]
    public string Key { get; set; }

    [DbType(Size = MaxValue)]
    public string Value { get; set; }
}

public enum RestLogReplayState
{
    NoChanges,
    WithChanges
}
