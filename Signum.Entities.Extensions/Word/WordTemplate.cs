using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Files;
using Signum.Utilities;
using System;
using System.ComponentModel;
using System.Linq.Expressions;
using Signum.Entities.Templating;

namespace Signum.Entities.Word
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class WordTemplateEntity : Entity
    {
        [UniqueIndex]
        [StringLengthValidator(Min = 3, Max = 200)]
        public string Name { get; set; }

        public QueryEntity Query { get; set; }

        public WordModelEntity? Model { get; set; }
        
        public CultureInfoEntity Culture { get; set; }

        [NotifyChildProperty]
        public TemplateApplicableEval? Applicable { get; set; }

        public bool DisableAuthorization { get; set; }

        public Lite<FileEntity>? Template { get; set; }

        [StringLengthValidator(Min = 3, Max = 100), FileNameValidator]
        public string FileName { get; set; }

        public WordTransformerSymbol? WordTransformer { get; set; }

        public WordConverterSymbol? WordConverter { get; set; }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => Name);

        public bool IsApplicable(Entity? entity)
        {
            if (Applicable == null)
                return true;

            try
            {
                return Applicable.Algorithm!.ApplicableUntyped(entity);
            }
            catch (Exception e)
            {
                throw new ApplicationException($"Error evaluating Applicable for WordTemplate '{Name}' with entity '{entity}': " + e.Message, e);
            }
        }
    }


    [AutoInit]
    public static class WordTemplateOperation
    {
        public static ExecuteSymbol<WordTemplateEntity> Save;
        public static DeleteSymbol<WordTemplateEntity> Delete;
        public static ExecuteSymbol<WordTemplateEntity> CreateWordReport;

        public static ConstructSymbol<WordTemplateEntity>.From<WordModelEntity> CreateWordTemplateFromWordModel;
    }

    public enum WordTemplateMessage
    {
        [Description("Model should be set to use model {0}")]
        ModelShouldBeSetToUseModel0,
        [Description("Type {0} does not have a property with name {1}")]
        Type0DoesNotHaveAPropertyWithName1,
        ChooseAReportTemplate,
        [Description("{0} {1} requires extra parameters")]
        _01RequiresExtraParameters,
        [Description("Select the source of data for your table or chart")]
        SelectTheSourceOfDataForYourTableOrChart,
        [Description("Write this key as Title in the 'Alternative text' of your table or chart")]
        WriteThisKeyAsTileInTheAlternativeTextOfYourTableOrChart,
        NoDefaultTemplateDefined,
        WordReport,
    }

    [Serializable]
    public class WordTransformerSymbol : Symbol
    {
        private WordTransformerSymbol() { }

        public WordTransformerSymbol(Type declaringType, string fieldName) :
            base(declaringType, fieldName)
        {
        }
    }

    [Serializable]
    public class WordConverterSymbol : Symbol
    {
        private WordConverterSymbol() { }

        public WordConverterSymbol(Type declaringType, string fieldName) :
            base(declaringType, fieldName)
        {
        }
    }

    [AutoInit]
    public static class WordTemplatePermission
    {
        public static PermissionSymbol GenerateReport;
    }

    [InTypeScript(true)]
    public enum WordTemplateVisibleOn
    {
        Single = 1,
        Multiple = 2,
        Query = 4
    }


}
