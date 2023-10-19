import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import React from "react";
import { OverlayTrigger, Popover } from "react-bootstrap";
import { TypeReference, getQueryNiceName, getTypeInfo, isTypeEntity } from "../Reflection";
import { getTypeNiceName } from "../Finder";
import MessageModal from "../Modals/MessageModal";
import { AggregateFunction, CollectionAnyAllType, CollectionElementType, QueryTokenMessage } from "../Signum.DynamicQuery.Tokens";
import { SearchMessage } from "../Signum.Entities";

export function FieldPopover(p: { queryKey: string, type?: string }) {
  const [expressionExpanded, setExpressionExpanded] = React.useState(false);
  const isDefaultQuery = isTypeEntity(p.queryKey);
  const popover = (
    <Popover id="popover-basic" style={{ minWidth: expressionExpanded ? 900 : 600 }}>
      <Popover.Header as="h3"><strong>{QueryTokenMessage.FilterField.niceToString()}</strong></Popover.Header>
      <Popover.Body>
        <ul>
          <li>{QueryTokenMessage.AFilterConsistsOfA0AComparison1AndAConstant2.niceToString()
            .formatHtml(<strong>{QueryTokenMessage.Field.niceToString()}</strong>, <strong>{QueryTokenMessage.Operator.niceToString()}</strong>, <strong>{QueryTokenMessage.Value.niceToString()}</strong>)}</li>
          {isDefaultQuery && <li>{QueryTokenMessage.FieldCanBeAnyFieldOfThe0OrAnyRelatedEntity.niceToString().formatHtml(<strong>{p.type}</strong>)}</li>}
          {!isDefaultQuery && <li>{QueryTokenMessage.FieldCanBeAnyColumnOfTheQuery0OrAnyFieldOf1.niceToString().formatHtml(<strong>{getQueryNiceName(p.queryKey)}</strong>, <strong>{p.type}</strong>)}</li>}
        </ul>
        <LearnMoreAboutFieldExpressions expanded={expressionExpanded} onSetExpanded={setExpressionExpanded} />
        <strong>{QueryTokenMessage.AndOrGroups.niceToString()}</strong>
        <div className="my-2">{QueryTokenMessage.UsingAddOrGroupYouCanGroupAFewFiltersTogether.niceToString()}</div>
        <div className="my-2">{QueryTokenMessage.FilterGroupsCanAlsoBeUsedToCombineFiltersThatShouldBeSatisfiedByTheSameElement.niceToString()}</div>
      </Popover.Body>
    </Popover>
  );

  return (
    <OverlayTrigger trigger="click" placement="bottom" overlay={popover} >
      <button className="btn sf-line-button p-0 m-0 b-0 ms-1" onClick={e => setExpressionExpanded(false)}><FontAwesomeIcon icon="question-circle" title="help" /></button>
    </OverlayTrigger>
  );
}

export function ColumnPopover(p: { queryKey: string, type?: string }) {
  const [expressionExpanded, setExpressionExpanded] = React.useState(false);
  const isDefaultQuery = isTypeEntity(p.queryKey);
  const popover = (
    <Popover id="popover-basic" style={{ minWidth: 800 }}>
      <Popover.Header as="h3"><strong>{QueryTokenMessage.FilterField.niceToString()}</strong></Popover.Header>
      <Popover.Body>
        <ul>
          <li>{QueryTokenMessage.AFilterConsistsOfA0AComparison1AndAConstant2.niceToString()
            .formatHtml(<strong>{QueryTokenMessage.Field.niceToString()}</strong>, <strong>{QueryTokenMessage.Operator.niceToString()}</strong>, <strong>{QueryTokenMessage.Value.niceToString()}</strong>)}</li>
          {isDefaultQuery && <li>{QueryTokenMessage.FieldCanBeAnyFieldOfThe0OrAnyRelatedEntity.niceToString().formatHtml(<strong>{p.type}</strong>)}</li>}
          {!isDefaultQuery && <li>{QueryTokenMessage.FieldCanBeAnyColumnOfTheQuery0OrAnyFieldOf1.niceToString().formatHtml(<strong>{getQueryNiceName(p.queryKey)}</strong>, <strong>{p.type}</strong>)}</li>}
        </ul>
        <LearnMoreAboutFieldExpressions expanded={expressionExpanded} onSetExpanded={setExpressionExpanded} />
        <strong>{QueryTokenMessage.AndOrGroups.niceToString()}</strong>
        <div className="my-2">{QueryTokenMessage.UsingAddOrGroupYouCanGroupAFewFiltersTogether.niceToString()}</div>
        <div className="my-2">{QueryTokenMessage.FilterGroupsCanAlsoBeUsedToCombineFiltersThatShouldBeSatisfiedByTheSameElement.niceToString()}</div>
      </Popover.Body>
    </Popover>
  );

  return (
    <OverlayTrigger trigger="click" placement="bottom" overlay={popover} >
      <button className="btn sf-line-button p-0 m-0 b-0 ms-1" onClick={e => setExpressionExpanded(false)}><FontAwesomeIcon icon="question-circle" title="help" /></button>
    </OverlayTrigger>  
  );
}
function LearnMoreAboutFieldExpressions(p: { expanded: boolean, onSetExpanded: (a: any) => void} ) {
  return (
    <div className="mb-4">
      <a href="#" onClick={e => {
        e.preventDefault();
        p.onSetExpanded(!p.expanded);
      }}><FontAwesomeIcon icon={p.expanded ? "chevron-up" : "chevron-down"} /> {QueryTokenMessage.LearnMoreAboutFieldExpressions.niceToString()}</a>
      {p.expanded == true && <div className="ms-4">
        <div className="my-2">{QueryTokenMessage.YouCanNavigateDatabaseRelationshipsByContinuingTheExpressionWithMoreItems.niceToString()}</div>
        <ul>
          <li><strong>{QueryTokenMessage.Strings.niceToString()}: </strong>{QueryTokenMessage.ASequenceOfCharactersContinuingTheExpressionAllowsSimpleOperationsLike0.niceToString().formatHtml(<em>{QueryTokenMessage.Length.niceToString()}</em>)}</li>
          <li><strong>{QueryTokenMessage.Numbers.niceToString()}: </strong>{QueryTokenMessage.AnyNumericValueCanContinueWithSimpleExpressionLike0Or1ForHistograms.niceToString().formatHtml(<em>{QueryTokenMessage.Modulo0.niceToString("")}</em>, <em>{QueryTokenMessage.Step0.niceToString("")}</em>)}</li>
          <li><strong style={{ color: '#5100a1' }}>{QueryTokenMessage.Dates.niceToString()}: </strong>{QueryTokenMessage._0And1YouCanExtractsPartsOfTheDateByContinuingTheExpressionWith345returnANumberOr678ReturnADate.niceToString()
            .formatHtml(<em>{QueryTokenMessage.Date.niceToString()}</em>, <em>{QueryTokenMessage.DateTime.niceToString()}</em>, <em>{QueryTokenMessage.Month.niceToString()}</em>, <em>{QueryTokenMessage.WeekNumber.niceToString()}</em>, <em>{QueryTokenMessage.Day.niceToString()}</em>,
              <em>{QueryTokenMessage.MonthStart.niceToString()}</em>, <em>{QueryTokenMessage.WeekStart.niceToString()}</em>, <em>{QueryTokenMessage.Date.niceToString()}</em>)}
          </li>
          <li><strong style={{ color: '#2b91af' }}>{QueryTokenMessage.EntityRelationships.niceToString()}: </strong> {QueryTokenMessage.EntityRelationshipsAllowYouToNavigateToOtherTablesToGetFields.niceToString()} (<code>LEFT JOIN</code> {QueryTokenMessage.InSql.niceToString()})</li>
          <li><strong style={{ color: '#ce6700' }}>{QueryTokenMessage.Collections.niceToString()}: </strong> {QueryTokenMessage.CollectionOfEntitiesOrRelationships.niceToString()}</li>
          <li><strong style={{ color: 'blue' }}>{QueryTokenMessage.CollectionOperators.niceToString()}:</strong>
            <ul>
              <li><strong>{CollectionElementType.niceToString("Element")}: </strong> {QueryTokenMessage.MultipliesTheNumberOfRowsByAllTheElementsInTheCollection012.niceToString()
                .formatHtml(<code>OUTER APPLY</code>, <code>LEFT JOIN LATERAL</code>, QueryTokenMessage.InSql.niceToString(), <em>{CollectionElementType.niceToString("Element")}</em>, <em>{CollectionElementType.niceToString("Element2")}</em>, <em>{CollectionElementType.niceToString("Element3")}</em>)}
              </li>
              <li><strong>{CollectionAnyAllType.niceToString("Any")} / {CollectionAnyAllType.niceToString("NotAny")} / {CollectionAnyAllType.niceToString("All")} / {CollectionAnyAllType.niceToString("NotAll")}:</strong> {QueryTokenMessage.AllowsToAddFiltersThatUseConditionsOnTheCollectionElemens.niceToString()
                .formatHtml(<code>EXISTS</code>, QueryTokenMessage.InSql.niceToString(), <code>AND</code>, <code>OR</code>)}
              </li>
            </ul>
          </li>
          <li><strong style={{ color: 'green' }}>{QueryTokenMessage.Aggregates.niceToString()}:</strong> {QueryTokenMessage.WhenGroupingAllowsToCollapseManyValuesInOneValue.niceToString()}
            <ul>
              <li><strong>{AggregateFunction.niceToString("Count")}:</strong> Can be used as the first item, counts the number of rows on each group.</li>
              <li><strong>{AggregateFunction.niceToString("Min")}, {AggregateFunction.niceToString("Max")}, {AggregateFunction.niceToString("Average")}, {QueryTokenMessage.CountNotNull.niceToString()}, {QueryTokenMessage.CountDistinct.niceToString()} ..:</strong> {QueryTokenMessage.CanOnlyBeUsedAfterAnotherField.niceToString()}</li>
            </ul>
          </li>
        </ul>
        <div>{QueryTokenMessage.FinallyRememberThatYouCan01FullFieldExpression.niceToString().formatHtml(<code>COPY</code>, <code>PASTE</code>, <kbd>Ctrl+C</kbd>, <kbd>Ctrl+V</kbd>)}</div>
      </div>}
    </div>
  );
}
