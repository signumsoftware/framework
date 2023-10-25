//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from './Reflection'
import * as Entities from './Signum.Entities'


export const AggregateFunction = new EnumType<AggregateFunction>("AggregateFunction");
export type AggregateFunction =
  "Count" |
  "Average" |
  "Sum" |
  "Min" |
  "Max";

export const CollectionAnyAllType = new EnumType<CollectionAnyAllType>("CollectionAnyAllType");
export type CollectionAnyAllType =
  "Any" |
  "All" |
  "NotAny" |
  "NotAll";

export const CollectionElementType = new EnumType<CollectionElementType>("CollectionElementType");
export type CollectionElementType =
  "Element" |
  "Element2" |
  "Element3";

export module ColumnFieldMessage {
  export const ColumnsHelp = new MessageKey("ColumnFieldMessage", "ColumnsHelp");
  export const Rearrange = new MessageKey("ColumnFieldMessage", "Rearrange");
  export const RightClick = new MessageKey("ColumnFieldMessage", "RightClick");
  export const YouCanSelectAFieldExpressionToPointToAnyColumnOfTheQuery0OrAnyFieldOf1OrAnyRelatedEntity = new MessageKey("ColumnFieldMessage", "YouCanSelectAFieldExpressionToPointToAnyColumnOfTheQuery0OrAnyFieldOf1OrAnyRelatedEntity");
  export const YouCanSelectAFieldExpressionToPointToAnyFieldOfThe0OrAnyRelatedEntity = new MessageKey("ColumnFieldMessage", "YouCanSelectAFieldExpressionToPointToAnyFieldOfThe0OrAnyRelatedEntity");
  export const TheColumnHeaderTextIsTypicallyAutomaticallySetDependingOnTheFieldExpression = new MessageKey("ColumnFieldMessage", "TheColumnHeaderTextIsTypicallyAutomaticallySetDependingOnTheFieldExpression");
  export const YouCanAddOneNumericValueToTheColumnHeaderLikeTheTotalSumOfTheInvoices = new MessageKey("ColumnFieldMessage", "YouCanAddOneNumericValueToTheColumnHeaderLikeTheTotalSumOfTheInvoices");
  export const WhenATableHasManyRepeatedValuesInAColumnYouCanCombineThemVertically = new MessageKey("ColumnFieldMessage", "WhenATableHasManyRepeatedValuesInAColumnYouCanCombineThemVertically");
  export const SummaryHeaderField = new MessageKey("ColumnFieldMessage", "SummaryHeaderField");
  export const ActivateSummaryHeaderToAddAnAggregateForTheWholeQuery = new MessageKey("ColumnFieldMessage", "ActivateSummaryHeaderToAddAnAggregateForTheWholeQuery");
  export const NoteTheAggregationIncludesRowsThatMayNotBeVisibleDueToPagination = new MessageKey("ColumnFieldMessage", "NoteTheAggregationIncludesRowsThatMayNotBeVisibleDueToPagination");
}

export const ContainerTokenKey = new EnumType<ContainerTokenKey>("ContainerTokenKey");
export type ContainerTokenKey =
  "Operations" |
  "QuickLinks";

export module FieldExpressionMessage {
  export const LearnMoreAboutFieldExpressions = new MessageKey("FieldExpressionMessage", "LearnMoreAboutFieldExpressions");
  export const YouCanNavigateDatabaseRelationshipsByContinuingTheExpressionWithMoreItems = new MessageKey("FieldExpressionMessage", "YouCanNavigateDatabaseRelationshipsByContinuingTheExpressionWithMoreItems");
  export const SimpleValues = new MessageKey("FieldExpressionMessage", "SimpleValues");
  export const AStringLikeHelloANumberLike = new MessageKey("FieldExpressionMessage", "AStringLikeHelloANumberLike");
  export const Dates = new MessageKey("FieldExpressionMessage", "Dates");
  export const _0And1YouCanExtractsPartsOfTheDateByContinuingTheExpressionWith2ReturnANumberOr3ReturnADate = new MessageKey("FieldExpressionMessage", "_0And1YouCanExtractsPartsOfTheDateByContinuingTheExpressionWith2ReturnANumberOr3ReturnADate");
  export const EntityRelationships = new MessageKey("FieldExpressionMessage", "EntityRelationships");
  export const EntityRelationshipsAllowYouToNavigateToOtherTablesToGetFields = new MessageKey("FieldExpressionMessage", "EntityRelationshipsAllowYouToNavigateToOtherTablesToGetFields");
  export const InSql = new MessageKey("FieldExpressionMessage", "InSql");
  export const Collections = new MessageKey("FieldExpressionMessage", "Collections");
  export const CollectionOfEntitiesOrRelationships = new MessageKey("FieldExpressionMessage", "CollectionOfEntitiesOrRelationships");
  export const CollectionOperators = new MessageKey("FieldExpressionMessage", "CollectionOperators");
  export const MultipliesTheNumberOfRowsByAllTheElementsInTheCollection012 = new MessageKey("FieldExpressionMessage", "MultipliesTheNumberOfRowsByAllTheElementsInTheCollection012");
  export const AllowsToAddFiltersThatUseConditionsOnTheCollectionElemens = new MessageKey("FieldExpressionMessage", "AllowsToAddFiltersThatUseConditionsOnTheCollectionElemens");
  export const Aggregates = new MessageKey("FieldExpressionMessage", "Aggregates");
  export const WhenGroupingAllowsToCollapseManyValuesInOneValue = new MessageKey("FieldExpressionMessage", "WhenGroupingAllowsToCollapseManyValuesInOneValue");
  export const CountNotNull = new MessageKey("FieldExpressionMessage", "CountNotNull");
  export const CountDistinct = new MessageKey("FieldExpressionMessage", "CountDistinct");
  export const CanOnlyBeUsedAfterAnotherField = new MessageKey("FieldExpressionMessage", "CanOnlyBeUsedAfterAnotherField");
  export const FinallyRememberThatYouCan01FullFieldExpression = new MessageKey("FieldExpressionMessage", "FinallyRememberThatYouCan01FullFieldExpression");
}

export module FilterFieldMessage {
  export const FiltersHelp = new MessageKey("FilterFieldMessage", "FiltersHelp");
  export const AFilterConsistsOfA0AComparison1AndAConstant2 = new MessageKey("FilterFieldMessage", "AFilterConsistsOfA0AComparison1AndAConstant2");
  export const Field = new MessageKey("FilterFieldMessage", "Field");
  export const Operator = new MessageKey("FilterFieldMessage", "Operator");
  export const Value = new MessageKey("FilterFieldMessage", "Value");
  export const FieldCanBeAnyFieldOfThe0OrAnyRelatedEntity = new MessageKey("FilterFieldMessage", "FieldCanBeAnyFieldOfThe0OrAnyRelatedEntity");
  export const FieldCanBeAnyColumnOfTheQuery0OrAnyFieldOf1 = new MessageKey("FilterFieldMessage", "FieldCanBeAnyColumnOfTheQuery0OrAnyFieldOf1");
  export const AndOrGroups = new MessageKey("FilterFieldMessage", "AndOrGroups");
  export const Using0YouCanGroupAFewFiltersTogether = new MessageKey("FilterFieldMessage", "Using0YouCanGroupAFewFiltersTogether");
  export const FilterGroupsCanAlsoBeUsedToCombineFiltersForTheSameElement012 = new MessageKey("FilterFieldMessage", "FilterGroupsCanAlsoBeUsedToCombineFiltersForTheSameElement012");
  export const TheSameElement = new MessageKey("FilterFieldMessage", "TheSameElement");
}

export module QueryTokenMessage {
  export const _0As1 = new MessageKey("QueryTokenMessage", "_0As1");
  export const And = new MessageKey("QueryTokenMessage", "And");
  export const AnyEntity = new MessageKey("QueryTokenMessage", "AnyEntity");
  export const As0 = new MessageKey("QueryTokenMessage", "As0");
  export const Check = new MessageKey("QueryTokenMessage", "Check");
  export const Column0NotFound = new MessageKey("QueryTokenMessage", "Column0NotFound");
  export const Count = new MessageKey("QueryTokenMessage", "Count");
  export const Date = new MessageKey("QueryTokenMessage", "Date");
  export const DateTime = new MessageKey("QueryTokenMessage", "DateTime");
  export const DateTimeOffset = new MessageKey("QueryTokenMessage", "DateTimeOffset");
  export const Day = new MessageKey("QueryTokenMessage", "Day");
  export const DayOfWeek = new MessageKey("QueryTokenMessage", "DayOfWeek");
  export const DayOfYear = new MessageKey("QueryTokenMessage", "DayOfYear");
  export const DecimalNumber = new MessageKey("QueryTokenMessage", "DecimalNumber");
  export const Embedded0 = new MessageKey("QueryTokenMessage", "Embedded0");
  export const GlobalUniqueIdentifier = new MessageKey("QueryTokenMessage", "GlobalUniqueIdentifier");
  export const Hour = new MessageKey("QueryTokenMessage", "Hour");
  export const ListOf0 = new MessageKey("QueryTokenMessage", "ListOf0");
  export const Millisecond = new MessageKey("QueryTokenMessage", "Millisecond");
  export const TotalDays = new MessageKey("QueryTokenMessage", "TotalDays");
  export const TotalHours = new MessageKey("QueryTokenMessage", "TotalHours");
  export const TotalSeconds = new MessageKey("QueryTokenMessage", "TotalSeconds");
  export const TotalMinutes = new MessageKey("QueryTokenMessage", "TotalMinutes");
  export const TotalMilliseconds = new MessageKey("QueryTokenMessage", "TotalMilliseconds");
  export const Minute = new MessageKey("QueryTokenMessage", "Minute");
  export const Month = new MessageKey("QueryTokenMessage", "Month");
  export const MonthStart = new MessageKey("QueryTokenMessage", "MonthStart");
  export const Quarter = new MessageKey("QueryTokenMessage", "Quarter");
  export const QuarterStart = new MessageKey("QueryTokenMessage", "QuarterStart");
  export const WeekStart = new MessageKey("QueryTokenMessage", "WeekStart");
  export const HourStart = new MessageKey("QueryTokenMessage", "HourStart");
  export const MinuteStart = new MessageKey("QueryTokenMessage", "MinuteStart");
  export const SecondStart = new MessageKey("QueryTokenMessage", "SecondStart");
  export const TimeOfDay = new MessageKey("QueryTokenMessage", "TimeOfDay");
  export const MoreThanOneColumnNamed0 = new MessageKey("QueryTokenMessage", "MoreThanOneColumnNamed0");
  export const Number = new MessageKey("QueryTokenMessage", "Number");
  export const Second = new MessageKey("QueryTokenMessage", "Second");
  export const Text = new MessageKey("QueryTokenMessage", "Text");
  export const Year = new MessageKey("QueryTokenMessage", "Year");
  export const WeekNumber = new MessageKey("QueryTokenMessage", "WeekNumber");
  export const _0Steps1 = new MessageKey("QueryTokenMessage", "_0Steps1");
  export const Step0 = new MessageKey("QueryTokenMessage", "Step0");
  export const Length = new MessageKey("QueryTokenMessage", "Length");
  export const _0HasValue = new MessageKey("QueryTokenMessage", "_0HasValue");
  export const HasValue = new MessageKey("QueryTokenMessage", "HasValue");
  export const Modulo0 = new MessageKey("QueryTokenMessage", "Modulo0");
  export const _0Mod1 = new MessageKey("QueryTokenMessage", "_0Mod1");
  export const Null = new MessageKey("QueryTokenMessage", "Null");
  export const Not = new MessageKey("QueryTokenMessage", "Not");
  export const Distinct = new MessageKey("QueryTokenMessage", "Distinct");
  export const _0Of1 = new MessageKey("QueryTokenMessage", "_0Of1");
  export const RowOrder = new MessageKey("QueryTokenMessage", "RowOrder");
  export const RowId = new MessageKey("QueryTokenMessage", "RowId");
  export const CellOperation = new MessageKey("QueryTokenMessage", "CellOperation");
  export const ContainerOfCellOperations = new MessageKey("QueryTokenMessage", "ContainerOfCellOperations");
  export const EntityType = new MessageKey("QueryTokenMessage", "EntityType");
  export const UtcDateTime = new MessageKey("QueryTokenMessage", "UtcDateTime");
  export const DateTimePart = new MessageKey("QueryTokenMessage", "DateTimePart");
  export const MatchRank = new MessageKey("QueryTokenMessage", "MatchRank");
  export const MatchRankFor0 = new MessageKey("QueryTokenMessage", "MatchRankFor0");
  export const MatchSnippet = new MessageKey("QueryTokenMessage", "MatchSnippet");
  export const SnippetOf0 = new MessageKey("QueryTokenMessage", "SnippetOf0");
}

export const RoundingType = new EnumType<RoundingType>("RoundingType");
export type RoundingType =
  "Floor" |
  "Ceil" |
  "Round" |
  "RoundMiddle";

