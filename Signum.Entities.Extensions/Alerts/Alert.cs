using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.Authorization;
using Signum.Entities.Patterns;
using Signum.Entities.Basics;
using System.Linq.Expressions;
using System.ComponentModel;
using System.ServiceModel;
using Signum.Services;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Signum.Entities.Alerts
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class AlertDN : IdentifiableEntity
    {
        [ImplementedByAll]
        Lite<IdentifiableEntity> target;
        public Lite<IdentifiableEntity> Target
        {
            get { return target; }
            set { Set(ref target, value); }
        }

        DateTime creationDate = TimeZoneManager.Now;
        public DateTime CreationDate
        {
            get { return creationDate; }
            private set { Set(ref creationDate, value); }
        }

        [NotNullable]
        DateTime? alertDate;
        [NotNullValidator]
        public DateTime? AlertDate
        {
            get { return alertDate; }
            set { Set(ref alertDate, value); }
        }

        DateTime? attendedDate;
        public DateTime? AttendedDate
        {
            get { return attendedDate; }
            set { Set(ref attendedDate, value); }
        }

        [SqlDbType(Size = 100)]
        string title;
        [StringLengthValidator(AllowNulls = true, Max = 100)]
        public string Title
        {
            get { return title; }
            set { SetToStr(ref title, value); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string text;
        [StringLengthValidator(Min = 1)]
        public string Text
        {
            get { return text; }
            set { SetToStr(ref text, value); }
        }

        Lite<IUserDN> createdBy;
        public Lite<IUserDN> CreatedBy
        {
            get { return createdBy; }
            set { Set(ref createdBy, value); }
        }

        Lite<IUserDN> attendedBy;
        public Lite<IUserDN> AttendedBy
        {
            get { return attendedBy; }
            set { Set(ref attendedBy, value); }
        }

        [NotNullable]
        AlertTypeDN alertType;
        [NotNullValidator]
        public AlertTypeDN AlertType
        {
            get { return alertType; }
            set { Set(ref alertType, value); }
        }

        AlertState state;
        public AlertState State
        {
            get { return state; }
            set { Set(ref state, value); }
        }

        public override string ToString()
        {
            return text.FirstNonEmptyLine().Etc(100);
        }

        static Expression<Func<AlertDN, bool>> AttendedExpression =
           a => a.AttendedDate.HasValue;
        public bool Attended
        {
            get { return AttendedExpression.Evaluate(this); }
        }

        static Expression<Func<AlertDN, bool>> NotAttendedExpression =
           a => a.AttendedDate == null;
        public bool NotAttended
        {
            get { return NotAttendedExpression.Evaluate(this); }
        }

        static Expression<Func<AlertDN, bool>> AlertedExpression =
            a => !a.AttendedDate.HasValue && a.AlertDate <= TimeZoneManager.Now;
        public bool Alerted
        {
            get{ return AlertedExpression.Evaluate(this); }
        }

        static Expression<Func<AlertDN, bool>> FutureExpression =
            a => !a.AttendedDate.HasValue && a.AlertDate > TimeZoneManager.Now; 
        public bool Future
        {
            get{ return FutureExpression.Evaluate(this); }
        }

        static Expression<Func<AlertDN, AlertCurrentState>> CurrentStateExpression = 
            a =>a.attendedDate.HasValue ? AlertCurrentState.Attended: 
                a.alertDate <= TimeZoneManager.Now ? AlertCurrentState.Alerted:  AlertCurrentState.Future;
        public AlertCurrentState CurrentState
        {
            get{ return CurrentStateExpression.Evaluate(this); }
        }
    }

    public enum AlertState
    {
        [Ignore]
        New,
        Saved,
        Attended
    }

    public enum AlertCurrentState
    {
        Attended,
        Alerted,
        Future,
    }

    public static class AlertOperation
    {
        public static readonly ConstructSymbol<AlertDN>.From<IdentifiableEntity> CreateAlertFromEntity = OperationSymbol.Construct<AlertDN>.From<IdentifiableEntity>();
        public static readonly ExecuteSymbol<AlertDN> SaveNew = OperationSymbol.Execute<AlertDN>();
        public static readonly ExecuteSymbol<AlertDN> Save = OperationSymbol.Execute<AlertDN>();
        public static readonly ExecuteSymbol<AlertDN> Attend = OperationSymbol.Execute<AlertDN>();
        public static readonly ExecuteSymbol<AlertDN> Unattend = OperationSymbol.Execute<AlertDN>();
    }

    [Serializable, EntityKind(EntityKind.String, EntityData.Master)]
    public class AlertTypeDN : SemiSymbol
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public AlertTypeDN MakeSymbol([CallerMemberName]string memberName = null)
        {
            base.MakeSymbol(new StackFrame(1, false), memberName);
            return this;
        }
    }

    public static class AlertTypeOperation
    {
        public static readonly ExecuteSymbol<AlertTypeDN> Save = OperationSymbol.Execute<AlertTypeDN>();
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
        WarnedAlerts
    }
}
