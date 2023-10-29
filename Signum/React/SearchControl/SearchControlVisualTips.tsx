import React from "react";
import { OverlayTrigger, Popover } from "react-bootstrap";
import { getQueryNiceName, isTypeEntity } from "../Reflection";
import SearchControlLoaded, { CustomFontAwesomeIcon, getAddFilterIcon, getEditColumnIcon, getGroupByThisColumnIcon, getInsertColumnIcon, getRemoveColumnIcon, getResotreDefaultColumnsIcon } from "./SearchControlLoaded";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { Dic } from "../Globals";
import { CollectionMessage } from "../Signum.External";
import { getAllPinned } from "./PinnedFilterBuilder";
import { faFilter } from "@fortawesome/free-solid-svg-icons";
import { AggregateFunction, CollectionAnyAllType, CollectionElementType, ColumnFieldMessage, FieldExpressionMessage, FilterFieldMessage, QueryTokenMessage } from "../Signum.DynamicQuery.Tokens";
import { JavascriptMessage, SearchMessage } from "../Signum.Entities";
import { OverlayInjectedProps } from "react-bootstrap/esm/Overlay";
import { QueryDescription } from "../FindOptions";
import { getNiceTypeName } from "../Operations/MultiPropertySetter";

export function SearchHelp(p: { sc: SearchControlLoaded, injected: OverlayInjectedProps }) {
  var sc = p.sc;
  var query = getQueryNiceName(sc.props.queryDescription.queryKey);
  var type = sc.props.queryDescription.columns['Entity'].displayName;
  var searchMode = getSearchMode(sc);
  const fo = sc.props.findOptions;

  const tokensObj = fo.columnOptions.map(a => a.token)
    .concat(fo.orderOptions.map(a => a.token))
    .filter(a => a != undefined && a.queryTokenType != "Aggregate")
    .toObjectDistinct(a => a!.fullKey, a => a!);

  const tokens = Dic.getValues(tokensObj);

  return (
    <Popover id="popover-basic" {...p.injected} style={{ ...p.injected.style, minWidth: 900 }}>
      <Popover.Header as="h3"><strong>Search help</strong></Popover.Header>
      <Popover.Body>
        <div className="my-2">The <strong>search control</strong> is very powerfull, but can be intimidating. Take some time to learn how to use it... will be worth it!</div>
        <div className="pt-2"><strong>The basics</strong></div>
        {searchMode == "Search" && <p className="my-2">Currently we are in the query <strong><samp>{query}</samp></strong>, you can open a <strong><samp>{type}</samp></strong> by clicking the <FontAwesomeIcon icon="arrow-right" color="#b1bac4" /> icon, or doing <strong><samp style={{ whiteSpace: 'nowrap' }}>double-click</samp></strong> in the row (but not in a link!). </p>}
        {searchMode == "Group" && <p className="my-2">Currently we are in the query <strong><samp>{query}</samp></strong> grouped by {tokens.map(a => <strong><samp>{a.niceName}</samp></strong>).joinCommaHtml(CollectionMessage.And.niceToString())}, you can open a group by clicking in the <FontAwesomeIcon icon="layer-group" color="#b1bac4" /> icon, or doing <strong><samp style={{ whiteSpace: 'nowrap' }}>double-click</samp></strong> in the row (but not in a link!). </p>}
        {searchMode == "Find" && <p className="my-2">Doing <strong><samp style={{ whiteSpace: 'nowrap' }}>double-click</samp></strong> in the row will select the entity and close the modal automatically, alternatively you can select one entity and click OK.</p>}
        {getAllPinned(fo.filterOptions).length > 0 && <div className="my-2">You can use the prepared filters on the top to quickly find the <strong><samp>{type}</samp></strong> you are looking for. </div>}
        <div className="pt-2"><strong>Ordering results</strong></div>
        <p className="my-2">You can order results by clicking in a column header, default ordering is <samp>Ascending</samp> <FontAwesomeIcon icon="sort-up" /> and by clicking again it changes to <samp>Descending</samp> <FontAwesomeIcon icon="sort-down" />. You can order by more that one column if you keep <kbd>Shift</kbd> down when clicking on the columns header.</p>
        <div className="pt-2"><strong>Change columns</strong></div>
        <p className="my-2">You are not limited to the columns you see! The default columns can be changed at will by <strong><samp>right-clicking</samp></strong> in a column header and then select {getInsertColumnIcon()}<em>Insert Column</em>, {getEditColumnIcon()}<em>Edit Column</em> or {getRemoveColumnIcon()}<em>Remove Column</em>.</p>
        <p className="my-2">You can also <em>rearrange</em> the columns by dragging and dropping them to another position.</p>
        <p className="my-2">When inserting, the new column will be added before or after the selected column, depending where you <strong><samp style={{ whiteSpace: 'nowrap' }}>right-click</samp></strong>.</p>
        <div className="pt-2"><strong>Advanced Filters</strong></div>
        <p className="my-2">Click on the <CustomFontAwesomeIcon iconDefinition={faFilter} strokeWith={"40px"} stroke="currentColor" fill="transparent" /> button to open the Advanced filters, this will allow you create complex filters manually by selecting the <strong>field</strong> of the entity (or a related entities), a comparison <strong>operator</strong> and a <strong>value</strong> to compare.</p>
        <p className="my-2">Trick: You can <strong><samp style={{ whiteSpace: 'nowrap' }}>right-click</samp></strong> on a <strong>column header</strong> and choose {getAddFilterIcon()}<em>Add filter</em> to quickly filter by this column. Even more, you can <strong><samp style={{ whiteSpace: 'nowrap' }}>right-click</samp></strong> on a <strong>value</strong> to filter by this value directly.</p>
        <div className="pt-2"><strong>Grouping results by one (or more) column</strong></div>
        <p className="my-2">You can group results by <strong><samp style={{ whiteSpace: 'nowrap' }}>right-clicking</samp></strong> in a column header and selecting {getGroupByThisColumnIcon()}<em style={{ whiteSpace: 'nowrap' }}>Group by this column</em>. All the columns will disapear except the selected one and an agregation column (typically <em>Count</em>).</p>
      </Popover.Body>
    </Popover>
  );
}

export function GroupHelp(p: { injected: OverlayInjectedProps }) {
  return (
    <Popover id="popover-basic" {...p.injected} style={{ ...p.injected.style, minWidth: 900 }}>
      <Popover.Header as="h3"><strong>Group help</strong></Popover.Header>
      <Popover.Body>
        <p className="my-2">Any new column should either be an aggregate (<samp>{AggregateFunction.niceToString("Count")}</samp>, <samp>{AggregateFunction.niceToString("Sum")}</samp>, <samp>{AggregateFunction.niceToString("Min")}</samp>...) or it will be considered a new group key <FontAwesomeIcon icon="key" color="gray" />.</p>
        <p className="my-2">Once grouping you can filter normally or using aggregates as the field (<code>HAVING</code> in SQL).</p>
        <p className="my-2">Finally you can stop grouping by <strong><samp style={{ whiteSpace: 'nowrap' }}>right-clicking</samp></strong> in a column header and select {getResotreDefaultColumnsIcon()}<em style={{ whiteSpace: 'nowrap' }}>Restore default columns</em>.</p>
      </Popover.Body>
    </Popover>
  );
}

type SearchMode = "Search" | "Group" | "Find";

function getSearchMode(sc: SearchControlLoaded): SearchMode {
  if (sc.props.findOptions.groupResults)
    return "Group";
  if (sc.props.onDoubleClick)
    return "Find";
  return "Search";
}

export function FilterHelp(p: { queryDescription: QueryDescription, injected: OverlayInjectedProps }) {
  const [expressionExpanded, setExpressionExpanded] = React.useState(false);
  var type = p.queryDescription.columns['Entity'].displayName;
  const isDefaultQuery = isTypeEntity(p.queryDescription.queryKey);
  var sampleColumns = Object.values(p.queryDescription.columns).filter(cd => cd.name != "Id" && cd.name != "Entity");
  return (
    <Popover id="popover-basic" {...p.injected} style={{ ...p.injected.style, minWidth: expressionExpanded ? 900 : 600 }}>
      <Popover.Header as="h3"><strong>{FilterFieldMessage.FiltersHelp.niceToString()}</strong></Popover.Header>
      <Popover.Body>
        <div className="my-2">{FilterFieldMessage.AFilterConsistsOfA0AComparison1AndAConstant2.niceToString()
          .formatHtml(<strong>{FilterFieldMessage.Field.niceToString()}</strong>, <strong>{FilterFieldMessage.Operator.niceToString()}</strong>, <strong>{FilterFieldMessage.Value.niceToString()}</strong>)}</div>
        <ul>
          <li>
            {isDefaultQuery && <div className="my-2"><strong>{FilterFieldMessage.Field.niceToString()}: </strong>A query expression could be any field of the <strong><samp>{type}</samp></strong> (like {sampleColumns.filter((c, i) => i < 3).map((c, i) => <strong><samp>{c.displayName}</samp></strong>).notNull().joinHtml(', ')} or any other field that you see in the <strong><samp>{type}</samp></strong> when you click <FontAwesomeIcon icon="arrow-right" color="#b1bac4" /> icon) or any related entity.</div>}
            {!isDefaultQuery && <div className="my-2"><strong>{FilterFieldMessage.Field.niceToString()}: </strong>A query expression could be any column of the <strong><samp>{getQueryNiceName(p.queryDescription.queryKey)}</samp></strong> (like {Object.values(p.queryDescription.columns).map((c, i) => i < 3 && <strong><samp>{c.displayName}</samp></strong>).joinCommaHtml(',')}, or any other field that you see in the Project when you click <FontAwesomeIcon icon="arrow-right" color="#b1bac4" /> icon) or any related entity.</div>}
            <LearnMoreAboutFieldExpressions expanded={expressionExpanded} onSetExpanded={setExpressionExpanded} showAny={true} />
          </li>
          <li><div className="my-2"><strong>{FilterFieldMessage.Operator.niceToString()}: </strong>The operation that will be used to compare the <strong><samp>field</samp></strong><samp></samp> with the <strong><samp>value</samp></strong>, like <samp>Equals, Distinct, GreaterThan</samp>, etc...</div></li>
          <li><div className="my-2"><strong>{FilterFieldMessage.Value.niceToString()}: </strong>The value that will be compared with the <strong><samp>field</samp></strong>, typically has the same type as the field, but some operators like <strong><samp>IsIn</samp></strong> and <strong><samp>IsNotIn</samp></strong> allow to select multiple values.</div></li>
        </ul>
        {/*{isDefaultQuery && <div className="my-2">{FilterFieldMessage.FieldCanBeAnyFieldOfThe0OrAnyRelatedEntity.niceToString().formatHtml(<strong>{type}</strong>)}</div>}*/}
        {/*{!isDefaultQuery && <div className="my-2">{FilterFieldMessage.FieldCanBeAnyColumnOfTheQuery0OrAnyFieldOf1.niceToString().formatHtml(<strong>{getQueryNiceName(queryKey)}</strong>, <strong>{type}</strong>)}</div>}*/}
        <strong>{FilterFieldMessage.AndOrGroups.niceToString()}</strong>
        <div className="my-2">{FilterFieldMessage.Using0YouCanGroupAFewFiltersTogether.niceToString()
          .formatHtml(<strong><samp>[+ {SearchMessage.AddOrGroup.niceToString()}]</samp></strong>, <strong><samp>{SearchMessage.OrGroup.niceToString()}</samp></strong>, <strong><samp>{SearchMessage.AndGroup.niceToString()}</samp></strong>)}</div>
        <div className="my-2">{FilterFieldMessage.FilterGroupsCanAlsoBeUsedToCombineFiltersForTheSameElement012.niceToString()
          .formatHtml(<strong>{FilterFieldMessage.TheSameElement.niceToString()}</strong>, <strong><samp>{CollectionAnyAllType.niceToString("Any")}</samp></strong>, <strong><samp>{CollectionAnyAllType.niceToString("All")}</samp></strong>)}</div>
      </Popover.Body>
    </Popover>);
}

export function ColumnHelp(p: { queryDescription: QueryDescription, injected: OverlayInjectedProps }) {
  const [expressionExpanded, setExpressionExpanded] = React.useState(false);
  const type = getNiceTypeName(p.queryDescription.columns['Entity'].type);
  const isDefaultQuery = isTypeEntity(p.queryDescription.queryKey);
  return (
    <Popover id="popover-basic" {...p.injected} style={{ ...p.injected.style, minWidth: 800 }}>
      <Popover.Header as="h3"><strong>{ColumnFieldMessage.ColumnsHelp.niceToString()}</strong></Popover.Header>
      <Popover.Body>
        <div className="my-2">You are editing a column, let me explain what each field does:</div>
        <ul>
          <li>
            <strong>{FilterFieldMessage.Field.niceToString()}: </strong>          
            {!isDefaultQuery && ColumnFieldMessage.YouCanSelectAFieldExpressionToPointToAnyColumnOfTheQuery0OrAnyFieldOf1OrAnyRelatedEntity.niceToString().formatHtml(<strong>{getQueryNiceName(p.queryDescription.queryKey)}</strong>, <strong>{type}</strong>)}
            {isDefaultQuery && ColumnFieldMessage.YouCanSelectAFieldExpressionToPointToAnyFieldOfThe0OrAnyRelatedEntity.niceToString().formatHtml(<strong>{type}</strong>)}
            <LearnMoreAboutFieldExpressions expanded={expressionExpanded} onSetExpanded={setExpressionExpanded} showAny={false} />
          </li>
          <li><strong>{SearchMessage.DisplayName.niceToString()}: </strong>{ColumnFieldMessage.TheColumnHeaderTextIsTypicallyAutomaticallySetDependingOnTheFieldExpression.niceToString()
            .formatHtml(<em>{SearchMessage.DisplayName.niceToString()}</em>)}</li>
          <li><strong>{SearchMessage.SummaryHeader.niceToString()} (Ʃ): </strong>
            {ColumnFieldMessage.YouCanAddOneNumericValueToTheColumnHeaderLikeTheTotalSumOfTheInvoices.niceToString().formatHtml(<span><em>{AggregateFunction.niceToString("Count")}</em>, <em>{AggregateFunction.niceToString("Sum")}</em></span>)}
            <br/>
            {ColumnFieldMessage.NoteTheAggregationIncludesRowsThatMayNotBeVisibleDueToPagination.niceToString()}
          </li>
          <li><strong>{SearchMessage.CombineRowsWith.niceToString()}: </strong>{ColumnFieldMessage.WhenATableHasManyRepeatedValuesInAColumnYouCanCombineThemVertically01.niceToString()
            .formatHtml(<code>rowSpan</code>, <strong><samp>{type}</samp></strong>)}</li>
        </ul>
      </Popover.Body>
    </Popover>
  );
}



export function LearnMoreAboutFieldExpressions(p: { expanded: boolean, onSetExpanded: (a: any) => void, showAny: boolean }) {
  return (
    <div className="mb-2">
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
