using Signum.Entities.Basics;
using System.ComponentModel;

namespace Signum.Entities.Alerts;

[EntityKind(EntityKind.Main, EntityData.Transactional)]
public class AlertEntity : Entity
{
    [ImplementedByAll]
    public Lite<Entity>? Target { get; set; }

    [ImplementedByAll]
    public Lite<Entity>? LinkTarget { get; set; }

    [ImplementedByAll]
    public Lite<Entity>? GroupTarget { get; set; }

    public DateTime CreationDate { get; private set; } = Clock.Now;

    [NotNullValidator]
    public DateTime? AlertDate { get; set; }

    public DateTime? AttendedDate { get; set; }


    [AutoExpressionField]
    public string Title => As.Expression(() => TitleField! ?? (AlertType != null ? AlertType.NiceToString() : CreationDate.ToString())); //Replaced in Logic

    [StringLengthValidator(Max = 100)]
    public string? TitleField { get; set; }

    [AutoExpressionField]
    public string? Text => As.Expression(() => TextField); //Replaced in Logic

    [StringLengthValidator(MultiLine = true)]
    public string? TextArguments { get; set; }

    [StringLengthValidator(Min = 1, MultiLine = true)]
    public string? TextField { get; set; }

    [Ignore]
    public string? TextFromAlertType { get; set; }

    public Lite<IUserEntity>? CreatedBy { get; set; }

    public Lite<IUserEntity>? Recipient { get; set; }

    public Lite<IUserEntity>? AttendedBy { get; set; }

    public AlertTypeSymbol? AlertType { get; set; }

    public AlertState State { get; set; }

    public bool EmailNotificationsSent { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Title);


    [AutoExpressionField]
    public bool Attended => As.Expression(() => AttendedDate.HasValue);

    [AutoExpressionField]
    public bool NotAttended => As.Expression(() => AttendedDate == null);

    [AutoExpressionField]
    public bool Alerted => As.Expression(() => !AttendedDate.HasValue && AlertDate <= Clock.Now);

    [AutoExpressionField]
    public bool Future => As.Expression(() => !AttendedDate.HasValue && AlertDate > Clock.Now);

    [AutoExpressionField]
    public AlertCurrentState CurrentState => As.Expression(() =>
        AttendedDate.HasValue ? AlertCurrentState.Attended :
        AlertDate <= Clock.Now ? AlertCurrentState.Alerted :
        AlertCurrentState.Future);

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(TitleField) && TitleField == null && AlertType == null)
            return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());

        return base.PropertyValidation(pi);
    }
}

public enum AlertState
{
    [Ignore]
    New,
    Saved,
    Attended
}

[InTypeScript(true), DescriptionOptions(DescriptionOptions.Members | DescriptionOptions.Description)]
public enum AlertCurrentState
{
    Attended,
    Alerted,
    Future,
}

[AutoInit]
public static class AlertOperation
{
    public static ConstructSymbol<AlertEntity>.From<Entity> CreateAlertFromEntity;
    public static ConstructSymbol<AlertEntity>.Simple Create;
    public static ExecuteSymbol<AlertEntity> Save;
    public static ExecuteSymbol<AlertEntity> Delay;
    public static ExecuteSymbol<AlertEntity> Attend;
    public static ExecuteSymbol<AlertEntity> Unattend;
}

[DescriptionOptions(DescriptionOptions.Members), InTypeScript(true)]
public enum DelayOption
{
    _5Mins,
    _15Mins,
    _30Mins,
    _1Hour,
    _2Hours,
    _1Day,
    Custom
}

[EntityKind(EntityKind.String, EntityData.Master, IsLowPopulation = true)]
public class AlertTypeSymbol : SemiSymbol
{
    public AlertTypeSymbol()
    {
    }

    public AlertTypeSymbol(Type declaringType, string fieldName) : base(declaringType, fieldName)
    {
    }
}

[AutoInit]
public static class AlertTypeOperation
{
    public static ExecuteSymbol<AlertTypeSymbol> Save;
    public static DeleteSymbol<AlertTypeSymbol> Delete;
}

public enum AlertMessage
{
    Alert,
    [Description("New Alert")]
    NewAlert,
    Alerts,
    [Description("Attended")]
    Alerts_Attended,
    [Description("Future")]
    Alerts_Future,
    [Description("Not attended")]
    Alerts_NotAttended,
    [Description("Checked")]
    CheckedAlerts,
    CreateAlert,
    [Description("Futures")]
    FutureAlerts,
    [Description("Warned")]
    WarnedAlerts,
    CustomDelay,
    DelayDuration,
    MyActiveAlerts,
    YouDoNotHaveAnyActiveAlert,
    [Description("{0} similar alerts")]
    _0SimilarAlerts,
    [Description("{0} hidden alerts")]
    _0HiddenAlerts,
    ViewMore,
    CloseAll,
    AllMyAlerts,
    NewUnreadNotifications,
    Title,
    Text,
    [Description("Hi {0},")]
    Hi0,
    [Description("You have some pending alerts:")]
    YouHaveSomePendingAlerts,
    [Description("Please visit {0}")]
    PleaseVisit0,
    OtherNotifications,
    Expand,
    Collapse,
    [Description("Show {0} alerts more")]
    Show0AlertsMore,

    [Description("Show {0} groups more ({1} remaining)")]
    Show0GroupsMore1Remaining,
}

[InTypeScript(true)]
[DescriptionOptions(DescriptionOptions.Members)]
public enum AlertDropDownGroup
{
    ByType,
    ByUser,
    ByTypeAndUser,
}

