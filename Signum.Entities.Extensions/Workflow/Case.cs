using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Workflow
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional), InTypeScript(Undefined = false)]
    public class CaseEntity : Entity
    {
        [NotNullable]
        [NotNullValidator]
        public WorkflowEntity Workflow { get; set; }

        public CaseEntity ParentCase { get; set; }

        [NotNullable, SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Description { get; set; }

        [NotNullable, ImplementedByAll]
        [NotNullValidator]
        public ICaseMainEntity MainEntity { get; set; }

        public DateTime StartDate { get; set; } = TimeZoneManager.Now;
        public DateTime? FinishDate { get; set; }

        [NotNullable]
        [NotNullValidator, NoRepeatValidator]
        public MList<CaseTagEntity> Tags { get; set; } = new MList<CaseTagEntity>();

        static Expression<Func<CaseEntity, string>> ToStringExpression = @this => @this.Description;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [AutoInit]
    public static class CaseOperation
    {
        public static readonly ExecuteSymbol<CaseEntity> SetTags;
    }

    public interface ICaseMainEntity : IEntity
    {

    }

    [Serializable]
    public class CaseTagsModel : ModelEntity
    {
        [NotNullable, PreserveOrder]
        [NotNullValidator, NoRepeatValidator]
        public MList<CaseTagEntity> CaseTags { get; set; } = new MList<CaseTagEntity>();
    }


    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class CaseTagEntity : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        [StringLengthValidator(AllowNulls = false, Min = 2, Max = 100)]
        public string Name { get; set; }

        [NotNullable, SqlDbType(Size = 12)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 12)]
        public string Color { get; set; }

        static Expression<Func<CaseTagEntity, string>> ToStringExpression = @this => @this.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [AutoInit]
    public static class CaseTagOperation
    {
        public static readonly ExecuteSymbol<CaseTagEntity> Save;
    }
}
