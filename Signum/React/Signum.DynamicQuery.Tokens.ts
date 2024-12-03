//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from './Reflection'
import * as Entities from './Signum.Entities'


export const AggregateFunction: EnumType<AggregateFunction> = new EnumType<AggregateFunction>("AggregateFunction");
export type AggregateFunction =
  "Count" |
  "Average" |
  "Sum" |
  "Min" |
  "Max";

export const CollectionAnyAllType: EnumType<CollectionAnyAllType> = new EnumType<CollectionAnyAllType>("CollectionAnyAllType");
export type CollectionAnyAllType =
  "Any" |
  "All" |
  "NotAny" |
  "NotAll";

export const CollectionElementType: EnumType<CollectionElementType> = new EnumType<CollectionElementType>("CollectionElementType");
export type CollectionElementType =
  "Element" |
  "Element2" |
  "Element3";

export namespace ColumnFieldMessage {
  export const ColumnsHelp: MessageKey = new MessageKey("ColumnFieldMessage", "ColumnsHelp");
  export const YouCanSelectAFieldExpressionToPointToAnyColumnOfTheQuery0OrAnyFieldOf1OrAnyRelatedEntity: MessageKey = new MessageKey("ColumnFieldMessage", "YouCanSelectAFieldExpressionToPointToAnyColumnOfTheQuery0OrAnyFieldOf1OrAnyRelatedEntity");
  export const YouCanSelectAFieldExpressionToPointToAnyFieldOfThe0OrAnyRelatedEntity: MessageKey = new MessageKey("ColumnFieldMessage", "YouCanSelectAFieldExpressionToPointToAnyFieldOfThe0OrAnyRelatedEntity");
  export const TheColumnHeaderTextIsTypicallyAutomaticallySetDependingOnTheFieldExpression: MessageKey = new MessageKey("ColumnFieldMessage", "TheColumnHeaderTextIsTypicallyAutomaticallySetDependingOnTheFieldExpression");
  export const YouCanAddOneNumericValueToTheColumnHeaderLikeTheTotalSumOfTheInvoices: MessageKey = new MessageKey("ColumnFieldMessage", "YouCanAddOneNumericValueToTheColumnHeaderLikeTheTotalSumOfTheInvoices");
  export const WhenATableHasManyRepeatedValuesInAColumnYouCanCombineThemVertically01: MessageKey = new MessageKey("ColumnFieldMessage", "WhenATableHasManyRepeatedValuesInAColumnYouCanCombineThemVertically01");
  export const NoteTheAggregationIncludesRowsThatMayNotBeVisibleDueToPagination: MessageKey = new MessageKey("ColumnFieldMessage", "NoteTheAggregationIncludesRowsThatMayNotBeVisibleDueToPagination");
}

export const ContainerTokenKey: EnumType<ContainerTokenKey> = new EnumType<ContainerTokenKey>("ContainerTokenKey");
export type ContainerTokenKey =
  "Operations" |
  "QuickLinks";

export namespace FieldExpressionMessage {
  export const LearnMoreAboutFieldExpressions: MessageKey = new MessageKey("FieldExpressionMessage", "LearnMoreAboutFieldExpressions");
  export const YouCanNavigateDatabaseRelationshipsByContinuingTheExpressionWithMoreItems: MessageKey = new MessageKey("FieldExpressionMessage", "YouCanNavigateDatabaseRelationshipsByContinuingTheExpressionWithMoreItems");
  export const SimpleValues: MessageKey = new MessageKey("FieldExpressionMessage", "SimpleValues");
  export const AStringLikeHelloANumberLike: MessageKey = new MessageKey("FieldExpressionMessage", "AStringLikeHelloANumberLike");
  export const Dates: MessageKey = new MessageKey("FieldExpressionMessage", "Dates");
  export const _0And1YouCanExtractsPartsOfTheDateByContinuingTheExpressionWith2ReturnANumberOr3ReturnADate: MessageKey = new MessageKey("FieldExpressionMessage", "_0And1YouCanExtractsPartsOfTheDateByContinuingTheExpressionWith2ReturnANumberOr3ReturnADate");
  export const EntityRelationships: MessageKey = new MessageKey("FieldExpressionMessage", "EntityRelationships");
  export const EntityRelationshipsAllowYouToNavigateToOtherTablesToGetFields: MessageKey = new MessageKey("FieldExpressionMessage", "EntityRelationshipsAllowYouToNavigateToOtherTablesToGetFields");
  export const InSql: MessageKey = new MessageKey("FieldExpressionMessage", "InSql");
  export const Collections: MessageKey = new MessageKey("FieldExpressionMessage", "Collections");
  export const CollectionOfEntitiesOrRelationships: MessageKey = new MessageKey("FieldExpressionMessage", "CollectionOfEntitiesOrRelationships");
  export const CollectionOperators: MessageKey = new MessageKey("FieldExpressionMessage", "CollectionOperators");
  export const MultipliesTheNumberOfRowsByAllTheElementsInTheCollection012: MessageKey = new MessageKey("FieldExpressionMessage", "MultipliesTheNumberOfRowsByAllTheElementsInTheCollection012");
  export const AllowsToAddFiltersThatUseConditionsOnTheCollectionElemens: MessageKey = new MessageKey("FieldExpressionMessage", "AllowsToAddFiltersThatUseConditionsOnTheCollectionElemens");
  export const Aggregates: MessageKey = new MessageKey("FieldExpressionMessage", "Aggregates");
  export const WhenGroupingAllowsToCollapseManyValuesInOneValue: MessageKey = new MessageKey("FieldExpressionMessage", "WhenGroupingAllowsToCollapseManyValuesInOneValue");
  export const CountNotNull: MessageKey = new MessageKey("FieldExpressionMessage", "CountNotNull");
  export const CountDistinct: MessageKey = new MessageKey("FieldExpressionMessage", "CountDistinct");
  export const CanOnlyBeUsedAfterAnotherField: MessageKey = new MessageKey("FieldExpressionMessage", "CanOnlyBeUsedAfterAnotherField");
  export const FinallyRememberThatYouCan01FullFieldExpression: MessageKey = new MessageKey("FieldExpressionMessage", "FinallyRememberThatYouCan01FullFieldExpression");
}

export namespace FilterFieldMessage {
  export const FiltersHelp: MessageKey = new MessageKey("FilterFieldMessage", "FiltersHelp");
  export const AFilterConsistsOfA0AComparison1AndAConstant2: MessageKey = new MessageKey("FilterFieldMessage", "AFilterConsistsOfA0AComparison1AndAConstant2");
  export const Field: MessageKey = new MessageKey("FilterFieldMessage", "Field");
  export const Operator: MessageKey = new MessageKey("FilterFieldMessage", "Operator");
  export const Value: MessageKey = new MessageKey("FilterFieldMessage", "Value");
  export const FieldCanBeAnyFieldOfThe0OrAnyRelatedEntity: MessageKey = new MessageKey("FilterFieldMessage", "FieldCanBeAnyFieldOfThe0OrAnyRelatedEntity");
  export const FieldCanBeAnyColumnOfTheQuery0OrAnyFieldOf1: MessageKey = new MessageKey("FilterFieldMessage", "FieldCanBeAnyColumnOfTheQuery0OrAnyFieldOf1");
  export const AndOrGroups: MessageKey = new MessageKey("FilterFieldMessage", "AndOrGroups");
  export const Using0YouCanGroupAFewFiltersTogether: MessageKey = new MessageKey("FilterFieldMessage", "Using0YouCanGroupAFewFiltersTogether");
  export const FilterGroupsCanAlsoBeUsedToCombineFiltersForTheSameElement012: MessageKey = new MessageKey("FilterFieldMessage", "FilterGroupsCanAlsoBeUsedToCombineFiltersForTheSameElement012");
  export const TheSameElement: MessageKey = new MessageKey("FilterFieldMessage", "TheSameElement");
}

export namespace QueryTokenDateMessage {
  export const TimeOfDay: MessageKey = new MessageKey("QueryTokenDateMessage", "TimeOfDay");
  export const Date: MessageKey = new MessageKey("QueryTokenDateMessage", "Date");
  export const Year: MessageKey = new MessageKey("QueryTokenDateMessage", "Year");
  export const Quarter: MessageKey = new MessageKey("QueryTokenDateMessage", "Quarter");
  export const Month: MessageKey = new MessageKey("QueryTokenDateMessage", "Month");
  export const WeekNumber: MessageKey = new MessageKey("QueryTokenDateMessage", "WeekNumber");
  export const DayOfYear: MessageKey = new MessageKey("QueryTokenDateMessage", "DayOfYear");
  export const Day: MessageKey = new MessageKey("QueryTokenDateMessage", "Day");
  export const Days: MessageKey = new MessageKey("QueryTokenDateMessage", "Days");
  export const DayOfWeek: MessageKey = new MessageKey("QueryTokenDateMessage", "DayOfWeek");
  export const Hour: MessageKey = new MessageKey("QueryTokenDateMessage", "Hour");
  export const Minute: MessageKey = new MessageKey("QueryTokenDateMessage", "Minute");
  export const Second: MessageKey = new MessageKey("QueryTokenDateMessage", "Second");
  export const Millisecond: MessageKey = new MessageKey("QueryTokenDateMessage", "Millisecond");
  export const UtcDateTime: MessageKey = new MessageKey("QueryTokenDateMessage", "UtcDateTime");
  export const DateTimePart: MessageKey = new MessageKey("QueryTokenDateMessage", "DateTimePart");
  export const TotalDays: MessageKey = new MessageKey("QueryTokenDateMessage", "TotalDays");
  export const TotalHours: MessageKey = new MessageKey("QueryTokenDateMessage", "TotalHours");
  export const TotalSeconds: MessageKey = new MessageKey("QueryTokenDateMessage", "TotalSeconds");
  export const TotalMinutes: MessageKey = new MessageKey("QueryTokenDateMessage", "TotalMinutes");
  export const TotalMilliseconds: MessageKey = new MessageKey("QueryTokenDateMessage", "TotalMilliseconds");
  export const MonthStart: MessageKey = new MessageKey("QueryTokenDateMessage", "MonthStart");
  export const QuarterStart: MessageKey = new MessageKey("QueryTokenDateMessage", "QuarterStart");
  export const WeekStart: MessageKey = new MessageKey("QueryTokenDateMessage", "WeekStart");
  export const Every0Hours: MessageKey = new MessageKey("QueryTokenDateMessage", "Every0Hours");
  export const HourStart: MessageKey = new MessageKey("QueryTokenDateMessage", "HourStart");
  export const Every0Minutes: MessageKey = new MessageKey("QueryTokenDateMessage", "Every0Minutes");
  export const MinuteStart: MessageKey = new MessageKey("QueryTokenDateMessage", "MinuteStart");
  export const Every0Seconds: MessageKey = new MessageKey("QueryTokenDateMessage", "Every0Seconds");
  export const SecondStart: MessageKey = new MessageKey("QueryTokenDateMessage", "SecondStart");
  export const Every0Milliseconds: MessageKey = new MessageKey("QueryTokenDateMessage", "Every0Milliseconds");
  export const Every01: MessageKey = new MessageKey("QueryTokenDateMessage", "Every01");
  export const _0Steps1Rows2TotalRowsAprox: MessageKey = new MessageKey("QueryTokenDateMessage", "_0Steps1Rows2TotalRowsAprox");
}

export namespace QueryTokenMessage {
  export const _0As1: MessageKey = new MessageKey("QueryTokenMessage", "_0As1");
  export const And: MessageKey = new MessageKey("QueryTokenMessage", "And");
  export const AnyEntity: MessageKey = new MessageKey("QueryTokenMessage", "AnyEntity");
  export const As0: MessageKey = new MessageKey("QueryTokenMessage", "As0");
  export const Check: MessageKey = new MessageKey("QueryTokenMessage", "Check");
  export const Column0NotFound: MessageKey = new MessageKey("QueryTokenMessage", "Column0NotFound");
  export const Count: MessageKey = new MessageKey("QueryTokenMessage", "Count");
  export const DecimalNumber: MessageKey = new MessageKey("QueryTokenMessage", "DecimalNumber");
  export const Embedded0: MessageKey = new MessageKey("QueryTokenMessage", "Embedded0");
  export const GlobalUniqueIdentifier: MessageKey = new MessageKey("QueryTokenMessage", "GlobalUniqueIdentifier");
  export const ListOf0: MessageKey = new MessageKey("QueryTokenMessage", "ListOf0");
  export const TimeOfDay: MessageKey = new MessageKey("QueryTokenMessage", "TimeOfDay");
  export const Date: MessageKey = new MessageKey("QueryTokenMessage", "Date");
  export const DateTime: MessageKey = new MessageKey("QueryTokenMessage", "DateTime");
  export const DateTimeOffset: MessageKey = new MessageKey("QueryTokenMessage", "DateTimeOffset");
  export const MoreThanOneColumnNamed0: MessageKey = new MessageKey("QueryTokenMessage", "MoreThanOneColumnNamed0");
  export const Number: MessageKey = new MessageKey("QueryTokenMessage", "Number");
  export const Text: MessageKey = new MessageKey("QueryTokenMessage", "Text");
  export const _0Steps1: MessageKey = new MessageKey("QueryTokenMessage", "_0Steps1");
  export const Step0: MessageKey = new MessageKey("QueryTokenMessage", "Step0");
  export const Length: MessageKey = new MessageKey("QueryTokenMessage", "Length");
  export const _0HasValue: MessageKey = new MessageKey("QueryTokenMessage", "_0HasValue");
  export const HasValue: MessageKey = new MessageKey("QueryTokenMessage", "HasValue");
  export const Modulo0: MessageKey = new MessageKey("QueryTokenMessage", "Modulo0");
  export const _0Mod1: MessageKey = new MessageKey("QueryTokenMessage", "_0Mod1");
  export const Null: MessageKey = new MessageKey("QueryTokenMessage", "Null");
  export const Not: MessageKey = new MessageKey("QueryTokenMessage", "Not");
  export const Distinct: MessageKey = new MessageKey("QueryTokenMessage", "Distinct");
  export const _0Of1: MessageKey = new MessageKey("QueryTokenMessage", "_0Of1");
  export const RowOrder: MessageKey = new MessageKey("QueryTokenMessage", "RowOrder");
  export const RowId: MessageKey = new MessageKey("QueryTokenMessage", "RowId");
  export const CellOperation: MessageKey = new MessageKey("QueryTokenMessage", "CellOperation");
  export const ContainerOfCellOperations: MessageKey = new MessageKey("QueryTokenMessage", "ContainerOfCellOperations");
  export const EntityType: MessageKey = new MessageKey("QueryTokenMessage", "EntityType");
  export const MatchRank: MessageKey = new MessageKey("QueryTokenMessage", "MatchRank");
  export const MatchRankFor0: MessageKey = new MessageKey("QueryTokenMessage", "MatchRankFor0");
  export const MatchSnippet: MessageKey = new MessageKey("QueryTokenMessage", "MatchSnippet");
  export const SnippetOf0: MessageKey = new MessageKey("QueryTokenMessage", "SnippetOf0");
  export const PartitionId: MessageKey = new MessageKey("QueryTokenMessage", "PartitionId");
  export const Nested: MessageKey = new MessageKey("QueryTokenMessage", "Nested");
}

export const RoundingType: EnumType<RoundingType> = new EnumType<RoundingType>("RoundingType");
export type RoundingType =
  "Floor" |
  "Ceil" |
  "Round" |
  "RoundMiddle";

