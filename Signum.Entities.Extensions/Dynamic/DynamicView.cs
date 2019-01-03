using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Signum.Entities.Dynamic
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class DynamicViewEntity : Entity
    {
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string ViewName { get; set; } = "Default";

        [NotNullValidator]
        public TypeEntity EntityType { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 3)]
        public string ViewContent { get; set; }


        static Expression<Func<DynamicViewEntity, string>> ToStringExpression = @this => @this.ViewName + ": " + @this.EntityType;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [AutoInit]
    public static class DynamicViewOperation
    {
        public static readonly ConstructSymbol<DynamicViewEntity>.Simple Create;
        public static readonly ConstructSymbol<DynamicViewEntity>.From<DynamicViewEntity> Clone;
        public static readonly ExecuteSymbol<DynamicViewEntity> Save;
        public static readonly DeleteSymbol<DynamicViewEntity> Delete;
    }


    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class DynamicViewSelectorEntity : Entity
    {
        [NotNullValidator, UniqueIndex]
        public TypeEntity EntityType { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 3, MultiLine = true)]
        public string Script { get; set; }

        static Expression<Func<DynamicViewSelectorEntity, string>> ToStringExpression = @this => "ViewSelector " + @this.EntityType;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [AutoInit]
    public static class DynamicViewSelectorOperation
    {
        public static readonly ExecuteSymbol<DynamicViewSelectorEntity> Save;
        public static readonly DeleteSymbol<DynamicViewSelectorEntity> Delete;
    }



    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class DynamicViewOverrideEntity : Entity
    {
        [NotNullValidator]
        public TypeEntity EntityType { get; set; }

        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 100)]
        public string ViewName { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 3, MultiLine = true)]
        public string Script { get; set; }

        static Expression<Func<DynamicViewOverrideEntity, string>> ToStringExpression = @this => "DynamicViewOverride " + @this.EntityType;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [AutoInit]
    public static class DynamicViewOverrideOperation
    {
        public static readonly ExecuteSymbol<DynamicViewOverrideEntity> Save;
        public static readonly DeleteSymbol<DynamicViewOverrideEntity> Delete;
    }


    public enum DynamicViewMessage
    {
        AddChild,
        AddSibling,
        Remove,
        GenerateChildren,
        ClearChildren,
        SelectATypeOfComponent,
        SelectANodeFirst,
        UseExpression,
        SuggestedFindOptions,
        [Description("The following queries reference {0}:")]
        TheFollowingQueriesReference0,
        ChooseAView,
        [Description("Since there is no DynamicViewSelector you need to choose a view manually:")]
        SinceThereIsNoDynamicViewSelectorYouNeedToChooseAViewManually,
        ExampleEntity,
        ShowHelp,
        HideHelp
    }

    public enum DynamicViewValidationMessage
    {
        [Description("Only child nodes of type '{0}' allowed")]
        OnlyChildNodesOfType0Allowed,

        [Description("Type '{0}' does not contain field '{1}'")]
        Type0DoesNotContainsField1,

        [Description("Member '{0}' is mandatory for '{1}'")]
        Member0IsMandatoryFor1,

        [Description("{0} requires a {1}")]
        _0RequiresA1,

        Entity,
        CollectionOfEntities,
        Value,
        CollectionOfEnums,
        EntityOrValue,

        [Description("Filtering with new {0}. Consider changing visibility.")]
        FilteringWithNew0ConsiderChangingVisibility,


        [Description("Aggregate is mandatory for '{0}' ({1}).")]
        AggregateIsMandatoryFor01,

        [Description("ValueToken can not be use for '{0}' because is not an Entity.")]
        ValueTokenCanNotBeUseFor0BecauseIsNotAnEntity,

        ViewNameIsNotAllowedWhileHavingChildren,
    }
}
