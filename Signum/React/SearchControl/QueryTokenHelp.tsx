import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import React from "react";
import { OverlayTrigger, Popover } from "react-bootstrap";
import { getQueryNiceName, isTypeEntity } from "../Reflection";
import { AggregateFunction, CollectionAnyAllType, CollectionElementType, ColumnFieldMessage, FieldExpressionMessage, FilterFieldMessage, QueryTokenMessage } from "../Signum.DynamicQuery.Tokens";
import { JavascriptMessage, SearchMessage } from "../Signum.Entities";

export function FieldHelp(p: { queryKey: string, type?: string }) {
  const [expressionExpanded, setExpressionExpanded] = React.useState(false);
  const isDefaultQuery = isTypeEntity(p.queryKey);
  const popover = (
    <Popover id="popover-basic" style={{ minWidth: expressionExpanded ? 900 : 600 }}>
      <Popover.Header as="h3"><strong>{FilterFieldMessage.FiltersHelp.niceToString()}</strong></Popover.Header>
      <Popover.Body>
        <div className="my-2">{FilterFieldMessage.AFilterConsistsOfA0AComparison1AndAConstant2.niceToString()
          .formatHtml(<strong>{FilterFieldMessage.Field.niceToString()}</strong>, <strong>{FilterFieldMessage.Operator.niceToString()}</strong>, <strong>{FilterFieldMessage.Value.niceToString()}</strong>)}</div>
        {isDefaultQuery && <div className="my-2">{FilterFieldMessage.FieldCanBeAnyFieldOfThe0OrAnyRelatedEntity.niceToString().formatHtml(<strong>{p.type}</strong>)}</div>}
        {!isDefaultQuery && <div className="my-2">{FilterFieldMessage.FieldCanBeAnyColumnOfTheQuery0OrAnyFieldOf1.niceToString().formatHtml(<strong>{getQueryNiceName(p.queryKey)}</strong>, <strong>{p.type}</strong>)}</div>}
        <LearnMoreAboutFieldExpressions expanded={expressionExpanded} onSetExpanded={setExpressionExpanded} showAny={true} />
        <strong>{FilterFieldMessage.AndOrGroups.niceToString()}</strong>
        <div className="my-2">{FilterFieldMessage.UsingAddOrGroupYouCanGroupAFewFiltersTogether.niceToString()}</div>
        <div className="my-2">{FilterFieldMessage.FilterGroupsCanAlsoBeUsedToCombineFiltersThatShouldBeSatisfiedByTheSameElement.niceToString()}</div>
      </Popover.Body>
    </Popover>
  );

  return (
    <OverlayTrigger trigger="click" rootClose placement="bottom" overlay={popover} >
      <button className="btn sf-line-button p-0 m-0 b-0" onClick={e => setExpressionExpanded(false)}><FontAwesomeIcon icon="question-circle" title="help" /></button>
    </OverlayTrigger>
  );
}

export function ColumnHelp(p: { queryKey: string, type?: string }) {
  const [expressionExpanded, setExpressionExpanded] = React.useState(false);
  const isDefaultQuery = isTypeEntity(p.queryKey);
  const popover = (
    <Popover id="popover-basic" style={{ minWidth: 800 }}>
      <Popover.Header as="h3"><strong>{ColumnFieldMessage.ColumnsHelp.niceToString()}</strong></Popover.Header>
      <Popover.Body>
        <div className="my-2">You are editing a column, let me explain what each field does:</div>
        <strong>{ColumnFieldMessage.ModifyingColumns.niceToString()}</strong>
        <div className="my-2">{ColumnFieldMessage.TheDefaultColumnsCanBeChangedAtWillBy0InAColumnHeaderAndThenSelect1Or2Or3.niceToString()
          .formatHtml(<em>{ColumnFieldMessage.RightClicking.niceToString()}</em>, <em>{JavascriptMessage.insertColumn.niceToString()}</em>,
            <em>{JavascriptMessage.editColumn.niceToString()}</em>, <em>{JavascriptMessage.removeColumn.niceToString()}</em>, <em>{ColumnFieldMessage.Rearrange.niceToString()}</em>)}</div>
        <div className="my-2">{ColumnFieldMessage.WhenInsertingTheNewColumnWillBeAddedBeforeOrAfterTheSelectedColumn.niceToString().formatHtml(<em>{ColumnFieldMessage.RightClick.niceToString()}</em>)}</div>
        <div className="my-2">{ColumnFieldMessage.OnceEditingAColumnTheFollowingFieldsAreAvailable.niceToString()}</div>
        <ul>
          <li><strong>{FilterFieldMessage.Field.niceToString()}</strong></li>
          <ul>
            {!isDefaultQuery && <li>{ColumnFieldMessage.YouCanSelectAFieldExpressionToPointToAnyColumnOfTheQuery0OrAnyFieldOf1OrAnyRelatedEntity.niceToString().formatHtml(<strong>{getQueryNiceName(p.queryKey)}</strong>, <strong>{p.type}</strong>)}</li>}
          {isDefaultQuery && <li>{ColumnFieldMessage.YouCanSelectAFieldExpressionToPointToAnyFieldOfThe0OrAnyRelatedEntity.niceToString().formatHtml(<strong>{p.type}</strong>)}</li>}
          <LearnMoreAboutFieldExpressions expanded={expressionExpanded} onSetExpanded={setExpressionExpanded} showAny={false} />
          </ul>
          <li><strong>{SearchMessage.DisplayName.niceToString()}: </strong>{ColumnFieldMessage.TheColumnHeaderTextIsTypicallyAutomaticallySetDependingOnTheFieldExpression.niceToString()
            .formatHtml(<em>{SearchMessage.DisplayName.niceToString()}</em>)}</li>
          <li><strong>{SearchMessage.SummaryHeader.niceToString()} (Ʃ): </strong>{ColumnFieldMessage.YouCanAddOneNumericValueToTheColumnHeaderLikeTheTotalSumOfTheInvoices.niceToString()
            .formatHtml(<span><em>{AggregateFunction.niceToString("Count")}</em>, <em>{AggregateFunction.niceToString("Sum")}</em></span>)}</li>
          <li><strong>{ColumnFieldMessage.CombineValues.niceToString()}: </strong>{ColumnFieldMessage.WhenATableHasManyRepeatedValuesInAColumnYouCanCombineThemVertically.niceToString()
            .formatHtml(<code>rowSpan</code>)}</li>
        </ul>
        <strong>{ColumnFieldMessage.GroupingResultsByOneOrMoreColumn.niceToString()} </strong><FontAwesomeIcon icon="layer-group" />
        <div className="my-2">{ColumnFieldMessage.YouCanGroupResultsBy0InAColumnHeaderAndSelecting1.niceToString()
          .formatHtml(<em>{ColumnFieldMessage.RightClicking.niceToString()}</em>, <em>{JavascriptMessage.groupByThisColumn.niceToString()}</em>, <em>{AggregateFunction.niceToString("Count")}</em>)}</div>
        <div className="my-2">{ColumnFieldMessage.AnyNewColumnShouldEitherBeAnAggregateOrItWillBeConsideredANew0.niceToString()
          .formatHtml(<span><em>{SearchMessage.GroupKey.niceToString()}</em> <FontAwesomeIcon icon="key" /></span>)} </div>
        <div className="my-2">{ColumnFieldMessage.OnceGroupingYouCanFilterNormallyOrUsingAggregatesInYourFields.niceToString().formatHtml(<code>HAVING</code>, FieldExpressionMessage.InSql.niceToString())}</div>
        <div className="my-2">{ColumnFieldMessage.FinallyYouCanStopGroupingBy0InAColumnHeaderAndSelect1.niceToString()
          .formatHtml(<em>{ColumnFieldMessage.RightClicking.niceToString()}</em>, <em>{JavascriptMessage.restoreDefaultColumns.niceToString()}</em>)}</div>
        <strong>{ColumnFieldMessage.OrderingResults.niceToString()} </strong><FontAwesomeIcon icon="sort-up" />
        <div className="my-2">{ColumnFieldMessage.YouCanOrderResultsByClickingInAColumnHeaderDefualtOrderingIsAscending.niceToString().formatHtml(<kbd>Shift</kbd>)}</div>
      </Popover.Body>
    </Popover>
  );

  return (
    <OverlayTrigger trigger="click" rootClose placement="bottom" overlay={popover} >
      <button className="btn sf-line-button p-0 m-0 b-0 ms-1" onClick={e => setExpressionExpanded(false)}><FontAwesomeIcon icon="question-circle" title="help" /></button>
    </OverlayTrigger>  
  );
}
function LearnMoreAboutFieldExpressions(p: { expanded: boolean, onSetExpanded: (a: any) => void, showAny: boolean} ) {
  return (
    <div className="mb-4">
      <a href="#" onClick={e => {
        e.preventDefault();
        p.onSetExpanded(!p.expanded);
      }}><FontAwesomeIcon icon={p.expanded ? "chevron-up" : "chevron-down"} /> {FieldExpressionMessage.LearnMoreAboutFieldExpressions.niceToString()}</a>
      {p.expanded == true && <div className="ms-4">
        <div className="my-2">{FieldExpressionMessage.YouCanNavigateDatabaseRelationshipsByContinuingTheExpressionWithMoreItems.niceToString()}</div>
        <ul>
          <li><strong>{FieldExpressionMessage.SimpleValues.niceToString()}: </strong>{FieldExpressionMessage.AStringLikeHelloANumberLike.niceToString().formatHtml(<em>{QueryTokenMessage.Length.niceToString()}</em>, <em>{QueryTokenMessage.Modulo0.niceToString("")}</em>, <em>{QueryTokenMessage.Step0.niceToString("")}</em>)}</li>
          <li><strong style={{ color: '#5100a1' }}>{FieldExpressionMessage.Dates.niceToString()}: </strong>{FieldExpressionMessage._0And1YouCanExtractsPartsOfTheDateByContinuingTheExpressionWith2ReturnANumberOr3ReturnADate.niceToString()
            .formatHtml(<em>{QueryTokenMessage.Date.niceToString()}</em>, <em>{QueryTokenMessage.DateTime.niceToString()}</em>, <span><em>{QueryTokenMessage.Month.niceToString()}</em>, <em>{QueryTokenMessage.WeekNumber.niceToString()}</em>, <em>{QueryTokenMessage.Day.niceToString()}</em></span>,
              <span><em>{QueryTokenMessage.MonthStart.niceToString()}</em>, <em>{QueryTokenMessage.WeekStart.niceToString()}</em>, <em>{QueryTokenMessage.Date.niceToString()}</em></span>)}
          </li>
          <li><strong style={{ color: '#2b91af' }}>{FieldExpressionMessage.EntityRelationships.niceToString()}: </strong> {FieldExpressionMessage.EntityRelationshipsAllowYouToNavigateToOtherTablesToGetFields.niceToString()} (<code>LEFT JOIN</code> {FieldExpressionMessage.InSql.niceToString()})</li>
          <li><strong style={{ color: '#ce6700' }}>{FieldExpressionMessage.Collections.niceToString()}: </strong> {FieldExpressionMessage.CollectionOfEntitiesOrRelationships.niceToString()}</li>
          <li><strong style={{ color: 'blue' }}>{FieldExpressionMessage.CollectionOperators.niceToString()}:</strong>
            <ul>
              <li><strong>{CollectionElementType.niceToString("Element")}: </strong> {FieldExpressionMessage.MultipliesTheNumberOfRowsByAllTheElementsInTheCollection012.niceToString()
                .formatHtml(<code>OUTER APPLY</code>, <code>LEFT JOIN LATERAL</code>, FieldExpressionMessage.InSql.niceToString(), <em>{CollectionElementType.niceToString("Element")}</em>, <em>{CollectionElementType.niceToString("Element2")}</em>, <em>{CollectionElementType.niceToString("Element3")}</em>)}
              </li>
              {p.showAny && <li><strong>{CollectionAnyAllType.niceToString("Any")} / {CollectionAnyAllType.niceToString("NotAny")} / {CollectionAnyAllType.niceToString("All")} / {CollectionAnyAllType.niceToString("NotAll")}:</strong> {FieldExpressionMessage.AllowsToAddFiltersThatUseConditionsOnTheCollectionElemens.niceToString()
                .formatHtml(<code>EXISTS</code>, FieldExpressionMessage.InSql.niceToString(), <code>AND</code>, <code>OR</code>)}
              </li>}
            </ul>
          </li>
          <li><strong style={{ color: 'green' }}>{FieldExpressionMessage.Aggregates.niceToString()}:</strong> {FieldExpressionMessage.WhenGroupingAllowsToCollapseManyValuesInOneValue.niceToString()}
            <ul>
              <li><strong>{AggregateFunction.niceToString("Count")}:</strong> Can be used as the first item, counts the number of rows on each group.</li>
              <li><strong>{AggregateFunction.niceToString("Min")}, {AggregateFunction.niceToString("Max")}, {AggregateFunction.niceToString("Average")}, {FieldExpressionMessage.CountNotNull.niceToString()}, {FieldExpressionMessage.CountDistinct.niceToString()} ..:</strong> {FieldExpressionMessage.CanOnlyBeUsedAfterAnotherField.niceToString()}</li>
            </ul>
          </li>
        </ul>
        <div>{FieldExpressionMessage.FinallyRememberThatYouCan01FullFieldExpression.niceToString().formatHtml(<code>COPY</code>, <code>PASTE</code>, <kbd>Ctrl+C</kbd>, <kbd>Ctrl+V</kbd>)}</div>
      </div>}
    </div>
  );
}
