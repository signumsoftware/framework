import React from "react";
import { OverlayTrigger, Popover } from "react-bootstrap";
import { getQueryNiceName, isTypeEntity } from "../Reflection";
import SearchControlLoaded, { CustomFontAwesomeIcon, getAddFilterIcon, getEditColumnIcon, getGroupByThisColumnIcon, getInsertColumnIcon, getRemoveColumnIcon, getResotreDefaultColumnsIcon } from "./SearchControlLoaded";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { Dic } from "../Globals";
import { CollectionMessage } from "../Signum.External";
import { getAllPinned } from "./PinnedFilterBuilder";
import { faFilter } from "@fortawesome/free-solid-svg-icons";

export function SearchHelp(p: { sc: SearchControlLoaded}) {
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

  const popover = (
    <Popover id="popover-basic" style={{ minWidth: 900 }}>
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

  return (
    <OverlayTrigger trigger="click" rootClose placement="bottom" overlay={popover} >
      <button className="btn sf-line-button p-0 m-0 b-0 ms-1"><FontAwesomeIcon icon="question-circle" title="help" /></button>
    </OverlayTrigger>
  );
}

export function GroupHelp() {
  const popover = (
    <Popover id="popover-basic" style={{ minWidth: 900 }}>
      <Popover.Header as="h3"><strong>Group help</strong></Popover.Header>
      <Popover.Body>
        <p className="my-2">Any new column should either be an aggregate or it will be considered a new group key <FontAwesomeIcon icon="key" color="gray"/>.</p>
        <p className="my-2">Once grouping you can filter normally or using aggregates in your fields (<code>HAVING</code> in SQL).</p>
        <p className="my-2">Finally you can stop grouping by <strong><samp style={{ whiteSpace: 'nowrap' }}>right-clicking</samp></strong> in a column header and select {getResotreDefaultColumnsIcon()}<em style={{ whiteSpace: 'nowrap' }}>Restore default columns</em>.</p>
      </Popover.Body>
    </Popover>
  );

  return (
    <OverlayTrigger trigger="click" rootClose placement="bottom" overlay={popover} >
      <button className="btn sf-line-button p-0 m-0 b-0 ms-1"><FontAwesomeIcon icon="question-circle" title="help" /></button>
    </OverlayTrigger>
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
