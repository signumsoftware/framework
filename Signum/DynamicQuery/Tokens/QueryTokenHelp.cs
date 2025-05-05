using System.ComponentModel;

namespace Signum.DynamicQuery.Tokens;

public enum FilterFieldMessage
{
    FiltersHelp,
    [Description("A filter consists of a {0}, a comparison {1} and a constant {2}.")]
    AFilterConsistsOfA0AComparison1AndAConstant2,
    Field,
    Operator,
    Value,
    [Description("Field can be any field of the {0}, or any related entity.")]
    FieldCanBeAnyFieldOfThe0OrAnyRelatedEntity,
    [Description("Field can be any column of the query {0}, or any field of {1}.")]
    FieldCanBeAnyColumnOfTheQuery0OrAnyFieldOf1,
    [Description("AND / OR Groups")]
    AndOrGroups,
    [Description("Using {0} you can group a few filters together so that only one condition needs to be satisfied. Inside an {1} you can create a nested {2} and so on. ")]
    Using0YouCanGroupAFewFiltersTogether,
    [Description("Filter groups can also be used to combine filters for {0} of a collection when using operator like {1} or {2} in the prefix field.")]
    FilterGroupsCanAlsoBeUsedToCombineFiltersForTheSameElement012,
    [Description("the same element")]
    TheSameElement,
}

public enum FieldExpressionMessage
{
    LearnMoreAboutFieldExpressions,

    [Description("You can navigate database relationships by continuing the expression with more items.")]
    YouCanNavigateDatabaseRelationshipsByContinuingTheExpressionWithMoreItems,

    SimpleValues,
    [Description("A string (like \"Hello\") a number (like 3.14) or a boolean (true). Sometimes you will be able to continue the expression, like the {0} of a string or calculating the {1} or {2} of a number (for histograms).")]
    AStringLikeHelloANumberLike,

    Dates,
    [Description("{0} and {1}, you can extracts parts of the date by continuing the expression with {2} (return a number) or {3} (return a date)")]
    _0And1YouCanExtractsPartsOfTheDateByContinuingTheExpressionWith2ReturnANumberOr3ReturnADate,
    [Description("Entity Relationships")]
    EntityRelationships,
    EntityRelationshipsAllowYouToNavigateToOtherTablesToGetFields,
    [Description("in SQL")]
    InSql,

    Collections,
    [Description("Collection of entities or relationships.")]
    CollectionOfEntitiesOrRelationships,

    CollectionOperators,
    [Description("Multiplies the number of rows by all the elements in the collection. ({0} / {1} {2}). All the field expressions using the same {3} reuse the same {4}, to avoid this use {5} / {6}.")]
    MultipliesTheNumberOfRowsByAllTheElementsInTheCollection012,
    [Description("Allows to add filters that use conditions on the collection elemens (without multiplying the number of rows) ({0} {1}). To combine different conditons use {2} / {3} groups with a prefix (see below).")]
    AllowsToAddFiltersThatUseConditionsOnTheCollectionElemens,
    Aggregates,
    [Description("When allows to collapse many values in one value")]
    WhenGroupingAllowsToCollapseManyValuesInOneValue,
    [Description("Count Not Null")]
    CountNotNull,
    [Description("Count Distinct")]
    CountDistinct,
    [Description("Can only be used after another field.")]
    CanOnlyBeUsedAfterAnotherField,
    [Description("Finally, remember that you can {0} / {1} full field expression to other filters or columns by opening the drop-down-list and using {2} / {3}.")]
    FinallyRememberThatYouCan01FullFieldExpression,
}

public enum ColumnFieldMessage
{
    ColumnsHelp,
    

    [Description("You can select a field expression to point to any column of the query {0}, or any field of {1} or any related entity.")]
    YouCanSelectAFieldExpressionToPointToAnyColumnOfTheQuery0OrAnyFieldOf1OrAnyRelatedEntity,
    
    [Description("You can select a field expression to point to any field of the {0}, or any related entity.")]
    YouCanSelectAFieldExpressionToPointToAnyFieldOfThe0OrAnyRelatedEntity,
    
    [Description("The column header text is typically automatically set depending on the field expression, but can be customized by setting {0} manually.")]
    TheColumnHeaderTextIsTypicallyAutomaticallySetDependingOnTheFieldExpression,
    
    [Description("You can add one numeric value to the column header (like the total sum of the invoices), using a field expression ending in an aggregate (like {0},...). Note: The aggregation includes rows that may not be visible due to pagination!")]
    YouCanAddOneNumericValueToTheColumnHeaderLikeTheTotalSumOfTheInvoices,
    
    [Description("When a table has many repeated values in a column you can combine them vertically ({0}) either when the value is the same, or when is the same and belongs to the same {1}.")]
    WhenATableHasManyRepeatedValuesInAColumnYouCanCombineThemVertically01,

    
    [Description("Note: The aggregation includes rows that may not be visible due to pagination.")]
    NoteTheAggregationIncludesRowsThatMayNotBeVisibleDueToPagination,

}
