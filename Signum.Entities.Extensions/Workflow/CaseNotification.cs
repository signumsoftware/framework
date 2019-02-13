using Signum.Entities.Authorization;
using System;

namespace Signum.Entities.Workflow
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class CaseNotificationEntity : Entity
    {
        
        public Lite<CaseActivityEntity> CaseActivity { get; set; }

        
        public Lite<UserEntity> User { get; set; }

        [ImplementedBy(typeof(UserEntity), typeof(RoleEntity))]
        public Lite<Entity> Actor { get; internal set; }

        [StringLengthValidator(MultiLine = true)]
        public string? Remarks { get; set; }

        public CaseNotificationState State { get; set; }
    }

    public enum CaseNotificationState
    {
        New,
        Opened,
        InProgress,
        Done,
        DoneByOther,
    }



    [AutoInit]
    public static class CaseNotificationOperation
    {
        public static readonly ExecuteSymbol<CaseNotificationEntity> SetRemarks;
    }

    [Serializable]
    public class InboxFilterModel : ModelEntity
    {
        public DateFilterRange Range { get; set; }
        public MList<CaseNotificationState> States { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

    }

    public enum InboxMessage
    {
        Clear,
        Activity,
        SenderNote,
        Sender,
        Filters,
    }

    public enum DateFilterRange
    {
        All,
        LastWeek,
        LastMonth,
        CurrentYear,
    }
}
