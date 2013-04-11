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

namespace Signum.Entities.Alerts
{
    [Serializable, EntityKind(EntityKind.Main)]
    public class AlertDN : IdentifiableEntity
    {
        [ImplementedByAll]
        Lite<IdentifiableEntity> target;
        public Lite<IdentifiableEntity> Target
        {
            get { return target; }
            set { Set(ref target, value, () => Target); }
        }

        DateTime creationDate = TimeZoneManager.Now;
        public DateTime CreationDate
        {
            get { return creationDate; }
            private set { Set(ref creationDate, value, () => CreationDate); }
        }

        DateTime? alertDate;
        public DateTime? AlertDate
        {
            get { return alertDate; }
            set { Set(ref alertDate, value, () => AlertDate); }
        }

        DateTime? attendedDate;
        public DateTime? AttendedDate
        {
            get { return attendedDate; }
            set { Set(ref attendedDate, value, () => AttendedDate); }
        }

        [SqlDbType(Size = 100)]
        string title;
        [StringLengthValidator(AllowNulls = true, Max = 100)]
        public string Title
        {
            get { return title; }
            set { SetToStr(ref title, value, () => Title); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string text;
        [StringLengthValidator(Min = 1)]
        public string Text
        {
            get { return text; }
            set { SetToStr(ref text, value, () => Text); }
        }

        Lite<UserDN> createdBy;
        public Lite<UserDN> CreatedBy
        {
            get { return createdBy; }
            set { Set(ref createdBy, value, () => CreatedBy); }
        }

        Lite<UserDN> attendedBy;
        public Lite<UserDN> AttendedBy
        {
            get { return attendedBy; }
            set { Set(ref attendedBy, value, () => AttendedBy); }
        }

        [NotNullable]
        AlertTypeDN alertType;
        [NotNullValidator]
        public AlertTypeDN AlertType
        {
            get { return alertType; }
            set { Set(ref alertType, value, () => AlertType); }
        }

        AlertState state;
        public AlertState State
        {
            get { return state; }
            set { Set(ref state, value, () => State); }
        }

        public override string ToString()
        {
            return text.EtcLines(100);
        }

        static Expression<Func<AlertDN, bool>> AlertedExpression = 
            a => (a.AlertDate.HasValue && a.AlertDate <= TimeZoneManager.Now) && !a.AttendedDate.HasValue;
        public bool Alerted
        {
            get{ return AlertedExpression.Evaluate(this); }
        }

        static Expression<Func<AlertDN, bool>> AttendedExpression = 
            a => a.AttendedDate.HasValue; 
        public bool Attended
        {
            get{ return AttendedExpression.Evaluate(this); }
        }

        static Expression<Func<AlertDN, bool>> FutureExpression = 
            a => !a.AttendedDate.HasValue && (a.AlertDate == null || a.AlertDate > TimeZoneManager.Now); 
        public bool Future
        {
            get{ return FutureExpression.Evaluate(this); }
        }

        static Expression<Func<AlertDN, AlertCurrentState>> CurrentStateExpression = 
            a =>a.attendedDate.HasValue ? AlertCurrentState.Attended: 
                a.alertDate == null ? AlertCurrentState.None :  
                a.alertDate <= TimeZoneManager.Now ? AlertCurrentState.Alerted:  AlertCurrentState.Future;
        public AlertCurrentState CurrentState
        {
            get{ return CurrentStateExpression.Evaluate(this); }
        }

        [ImplementedBy()]
        Lite<IIdentifiable> aditionalData;
        public Lite<IIdentifiable> AditionalData
        {
            get { return aditionalData; }
            set { Set(ref aditionalData, value, () => AditionalData); }
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
        None,
        Attended,
        Alerted,
        Future,
    }

    public enum AlertOperation
    {
        SaveNew,
        Save,
        Attend,
        Unattend
    }

    [Serializable, EntityKind(EntityKind.String)]
    public class AlertTypeDN : MultiOptionalEnumDN
    {
      static AlertTypeDN()
        {
            DescriptionManager.DefaultDescriptionOptions += DescriptionManager_DefaultDescriptionOptions;
            DescriptionManager.Invalidate();
        }

        static DescriptionOptions? DescriptionManager_DefaultDescriptionOptions(Type type)
        {
            return type.IsEnum && type.Name.EndsWith("Alert") ? DescriptionOptions.Members : (DescriptionOptions?)null;
        }
    }

    public enum AlertTypeOperation
    {
        Save,
    }

    public enum AlertMessage
    {
        Alert,
        [Description("New Alert")]
        NewAlert,
        AlertCreated,
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
