using System;
using Signum.Utilities;
using Signum.Entities.Basics;
using System.Linq.Expressions;
using System.ComponentModel;

namespace Signum.Entities.Alerts
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class AlertEntity : Entity
    {
        [ImplementedByAll]
        public Lite<Entity>? Target { get; set; }

        public DateTime CreationDate { get; private set; } = TimeZoneManager.Now;

        [NotNullValidator]
        public DateTime? AlertDate { get; set; }

        public DateTime? AttendedDate { get; set; }

        [StringLengthValidator(Max = 100)]
        public string? Title { get; set; }

        [StringLengthValidator(Min = 1, MultiLine = true)]
        public string Text { get; set; }

        public Lite<IUserEntity>? CreatedBy { get; set; }

        public Lite<IUserEntity>? Recipient { get; set; }

        public Lite<IUserEntity>? AttendedBy { get; set; }

        public AlertTypeEntity? AlertType { get; set; }

        public AlertState State { get; set; }

        public override string ToString()
        {
            return Text.FirstNonEmptyLine()?.Etc(100)!;
        }

        [AutoExpressionField]
        public bool Attended => As.Expression(() => AttendedDate.HasValue);

        [AutoExpressionField]
        public bool NotAttended => As.Expression(() => AttendedDate == null);

        [AutoExpressionField]
        public bool Alerted => As.Expression(() => !AttendedDate.HasValue && AlertDate <= TimeZoneManager.Now);

        [AutoExpressionField]
        public bool Future => As.Expression(() => !AttendedDate.HasValue && AlertDate > TimeZoneManager.Now);

        [AutoExpressionField]
        public AlertCurrentState CurrentState => As.Expression(() =>
            AttendedDate.HasValue ? AlertCurrentState.Attended :
            AlertDate <= TimeZoneManager.Now ? AlertCurrentState.Alerted :
            AlertCurrentState.Future);
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

    [Serializable, EntityKind(EntityKind.String, EntityData.Master)]
    public class AlertTypeEntity : SemiSymbol
    {
        public AlertTypeEntity()
        {
        }

        public AlertTypeEntity(Type declaringType, string fieldName) : base(declaringType, fieldName)
        {
        }
    }

    [AutoInit]
    public static class AlertTypeOperation
    {
        public static ExecuteSymbol<AlertTypeEntity> Save;
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
    }
}
