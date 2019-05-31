using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Signum.Entities.Dynamic
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class DynamicViewEntity : Entity
    {
        [StringLengthValidator(Min = 3, Max = 100)]
        public string ViewName { get; set; } = "Default";

        public TypeEntity EntityType { get; set; }

        [PreserveOrder]
        public MList<DynamicViewPropEmbedded> Props { get; set; } = new MList<DynamicViewPropEmbedded>();

        [StringLengthValidator(Max = int.MaxValue, MultiLine = true)]
        public string? Locals { get; set; }

        [StringLengthValidator(Min = 3)]
        public string ViewContent { get; set; }


        static Expression<Func<DynamicViewEntity, string>> ToStringExpression = @this => @this.ViewName + ": " + @this.EntityType;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        protected override string? PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(Props))
                return NoRepeatValidatorAttribute.ByKey(Props, a => a.Name);

            return base.PropertyValidation(pi);
        }
    }

    [Serializable]
    public class DynamicViewPropEmbedded : EmbeddedEntity
    {
        [StringLengthValidator(Max = 100), IdentifierValidator(IdentifierType.Ascii)]
        public string Name { get; set; }

        [StringLengthValidator(Max = 100)]
        public string Type { get; set; }

        static string[] ForbiddenNames = new string[]
        {
            "ctx",
            "initialDynamicView",
            "ref",
            "key",
            "children"
        };

        protected override string? PropertyValidation(PropertyInfo pi)
        {

            if(pi.Name == nameof(Name) && Name.HasText())
            {
                if(Name != Name.FirstLower())
                    return DynamicViewValidationMessage._0ShouldStartByLowercase.NiceToString(pi.NiceName());

                if (ForbiddenNames.Contains(Name))
                    return DynamicViewValidationMessage._0CanNotBe1.NiceToString(pi.NiceName(), Name);
            }

            return base.PropertyValidation(pi);
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
        [UniqueIndex]
        public TypeEntity EntityType { get; set; }

        [StringLengthValidator(Min = 3, MultiLine = true)]
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
        
        public TypeEntity EntityType { get; set; }

        [StringLengthValidator(Min = 3, Max = 100)]
        public string? ViewName { get; set; }

        [StringLengthValidator(Min = 3, MultiLine = true)]
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
        _0ShouldStartByLowercase,
        _0CanNotBe1,
    }
}
