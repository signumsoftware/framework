import * as React from 'react'
import { openModal, IModalProps } from '../Modals';
import { ColumnOptionParsed, FindOptionsParsed, OrderOptionParsed, QueryDescription } from '../FindOptions';
import { QueryToken, SubTokensOptions } from '../QueryToken';
import { JavascriptMessage, SearchMessage } from '../Signum.Entities';
import QueryTokenBuilder from './QueryTokenBuilder';
import { LinkButton } from '../Basics/LinkButton';
import { Modal } from 'react-bootstrap';
import { useForceUpdate } from '../Hooks';
import { DraggableTable, DraggableTableColumn } from './DraggableTable';
import { CombineRows, OrderType } from '../Signum.DynamicQuery';
import { getNiceTypeName } from '../Operations/MultiPropertySetter';
import { Finder } from '../Finder';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { classes } from '../Globals';

interface ColumnEditorModalProps extends IModalProps<boolean> {
  findOptions: FindOptionsParsed;
  queryDescription: QueryDescription;
  querySettings?: Finder.QuerySettings;
}

function ColumnEditorModal(p: ColumnEditorModalProps): React.ReactElement {
  const [show, setShow] = React.useState(true);
  const accepted = React.useRef<boolean>(false);
  const forceUpdate = useForceUpdate();

  const qd = p.queryDescription;

  const [groupResults, setGroupResults] = React.useState(p.findOptions.groupResults);
  const columns = React.useRef<ColumnOptionParsed[]>([...p.findOptions.columnOptions]);
  const orders = React.useRef<OrderOptionParsed[]>([...p.findOptions.orderOptions]);
  const [resetting, setResetting] = React.useState(false);

  const columnSubTokens = SubTokensOptions.CanElement | SubTokensOptions.CanToArray | SubTokensOptions.CanSnippet |
    (groupResults ? SubTokensOptions.CanAggregate : SubTokensOptions.CanOperation | SubTokensOptions.CanManual);
  const orderSubTokens = SubTokensOptions.CanElement | SubTokensOptions.CanSnippet |
    (groupResults ? SubTokensOptions.CanAggregate : 0);

  async function handleGroupResultsChange(newValue: boolean) {
    setResetting(true);
    if (newValue) {
      const defAggregate = p.querySettings?.defaultAggregates;
      if (defAggregate && defAggregate.length > 0) {
        columns.current = await Finder.parseColumnOptions(defAggregate, true, qd);
      } else {
        const [countCol] = await Finder.parseColumnOptions([{ token: "Count" }], true, qd);
        columns.current = [countCol];
      }
    } else {
      columns.current = Finder.getDefaultColumns(qd).map(token => ({ token, displayName: token.niceName }));
    }
    orders.current = [];
    setGroupResults(newValue);
    setResetting(false);
  }

  function handleColumnOrderClick(e: React.MouseEvent, token: QueryToken | undefined) {
    if (!token) return;
    const prev = orders.current.find(a => a.token.fullKey === token.fullKey);
    if (prev != null) {
      prev.orderType = prev.orderType === "Ascending" ? "Descending" : "Ascending";
      if (!e.shiftKey)
        orders.current = [prev];
    } else {
      const newOrder: OrderOptionParsed = { token, orderType: OrderType.value("Ascending") };
      if (e.shiftKey)
        orders.current.push(newOrder);
      else
        orders.current = [newOrder];
    }
    forceUpdate();
  }

  function handleOK() {
    accepted.current = true;
    setShow(false);
  }

  function handleCancel() {
    setShow(false);
  }

  function handleExited() {
    if (accepted.current) {
      p.findOptions.groupResults = groupResults;
      p.findOptions.columnOptions = columns.current.filter(co => co.token != null);
      p.findOptions.orderOptions = orders.current.filter(oo => oo.token != null);
    }
    p.onExited!(accepted.current);
  }

  const btnStyle: React.CSSProperties = { display: 'inline-flex', alignItems: 'center', justifyContent: 'center', minWidth: '1.2rem' };

  const columnColumns: DraggableTableColumn<ColumnOptionParsed>[] = [
    {
      header: SearchMessage.ColumnField.niceToString(),
      cellHtmlAttributes: { style: { minWidth: "280px" } },
      template: (co, _i, fu) => {
        const orderIndex = co.token ? orders.current.findIndex(a => a.token.fullKey === co.token!.fullKey) : -1;
        const order = orderIndex >= 0 ? orders.current[orderIndex] : undefined;
        const sortClass = order == null ? "asc" : order.orderType === "Ascending" ? "asc" : "desc";
        const lClass = orderIndex > 0 ? ` l${Math.min(orderIndex, 3)}` : "";
        return (
          <div>
            <div className="d-flex align-items-center gap-1">
              <LinkButton
                title={SearchMessage.Orders.niceToString()}
                className={classes("sf-line-button", order == null && "sf-shy-icon")}
                style={btnStyle}
                onClick={e => handleColumnOrderClick(e, co.token)}>
                <span className={`sf-header-sort ${sortClass}${lClass}`} />
              </LinkButton>
              <div className="rw-widget-xs flex-grow-1">
                <QueryTokenBuilder
                  queryToken={co.token}
                  onTokenChange={token => {
                    co.token = token;
                    co.displayName = token?.niceName;
                    co.summaryToken = undefined;
                    fu();
                  }}
                  queryKey={qd.queryKey}
                  subTokenOptions={columnSubTokens}
                  readOnly={false} />
              </div>
            </div>
            <div className="d-flex align-items-center gap-1 mt-1">
              <LinkButton
                title={SearchMessage.SummaryHeader.niceToString()}
                className={classes("sf-line-button", co.summaryToken == null && "sf-shy-icon")}
                style={btnStyle}
                onClick={() => { co.summaryToken = co.summaryToken ? undefined : co.token; fu(); }}>
                Ʃ
              </LinkButton>
              {co.summaryToken && (
                <div className="rw-widget-xs flex-grow-1">
                  <QueryTokenBuilder
                    queryToken={co.summaryToken}
                    onTokenChange={token => { co.summaryToken = token; fu(); }}
                    queryKey={qd.queryKey}
                    subTokenOptions={SubTokensOptions.CanElement | SubTokensOptions.CanAggregate}
                    readOnly={false} />
                </div>
              )}
            </div>
          </div>
        );
      },
    },
    {
      header: SearchMessage.DisplayName.niceToString(),
      cellHtmlAttributes: { style: { minWidth: "180px" } },
      template: (co, _i, fu) => (
        <div>
          <div className="d-flex align-items-center gap-1">
            {groupResults && co.token && co.token.queryTokenType !== "Aggregate" && (
              <FontAwesomeIcon icon="key" color="gray" title={SearchMessage.GroupKey.niceToString()} />
            )}
            <input className="form-control form-control-xs flex-grow-1"
              value={co.displayName ?? ""}
              disabled={co.hiddenColumn ?? false}
              placeholder={co.token?.niceName}
              onChange={e => { co.displayName = e.currentTarget.value || undefined; fu(); }} />
          </div>
          <label className="col-form-label col-form-label-xs mt-1 mb-0">
            <input type="checkbox" className="form-check-input me-1"
              checked={co.hiddenColumn ?? false}
              onChange={() => {
                co.hiddenColumn = !co.hiddenColumn || undefined;
                if (co.hiddenColumn) {
                  co.summaryToken = undefined;
                  co.displayName = undefined;
                  co.combineRows = undefined;
                }
                fu();
              }} />
            {SearchMessage.HiddenColumn.niceToString()}
          </label>
        </div>
      ),
    },
    {
      header: SearchMessage.CombineRowsWith.niceToString(),
      headerHtmlAttributes: { style: { width: "180px" } },
      cellHtmlAttributes: { style: { width: "180px" } },
      template: (co, _i, fu) => (
        <select className="form-select form-select-xs" value={co.combineRows ?? ""}
          disabled={co.hiddenColumn ?? false}
          onChange={e => { co.combineRows = (e.currentTarget.value as CombineRows) || undefined; fu(); }}>
          <option value="">{" - "}</option>
          <option value={CombineRows.value("EqualEntity")}>{SearchMessage.Equal0.niceToString(getNiceTypeName(qd.columns['Entity'].type))}</option>
          <option value={CombineRows.value("EqualValue")}>{CombineRows.niceToString("EqualValue")}</option>
        </select>
      ),
    },
  ];

  const orderColumns: DraggableTableColumn<OrderOptionParsed>[] = [
    {
      header: SearchMessage.ColumnField.niceToString(),
      cellHtmlAttributes: { style: { minWidth: "280px" } },
      template: (oo, _i, fu) => (
        <div className="rw-widget-xs">
          <QueryTokenBuilder
            queryToken={oo.token}
            onTokenChange={token => {
              if (token)
                oo.token = token;
              fu();
            }}
            queryKey={qd.queryKey}
            subTokenOptions={orderSubTokens}
            readOnly={false} />
        </div>
      ),
    },
    {
      header: OrderType.niceTypeName() ?? "Order",
      headerHtmlAttributes: { style: { width: "150px" } },
      cellHtmlAttributes: { style: { width: "150px" } },
      template: (oo, _i, fu) => (
        <select className="form-select form-select-xs" value={oo.orderType}
          onChange={e => { oo.orderType = e.currentTarget.value as OrderType; fu(); }}>
          <option value={OrderType.value("Ascending")}>{OrderType.niceToString("Ascending")}</option>
          <option value={OrderType.value("Descending")}>{OrderType.niceToString("Descending")}</option>
        </select>
      ),
    },
  ];

  return (
    <Modal show={show} onExited={handleExited} onHide={handleCancel} size="lg">
      <div className="modal-header bg-primary text-light">
        <h5 className="modal-title">{SearchMessage.EditAllColumns.niceToString()}</h5>
      </div>
      <div className="modal-body">
        {resetting ? <div className="text-center p-3">{JavascriptMessage.loading.niceToString()}</div> : (<>
          <h5 className="mb-3">
            <label>
              <input type="checkbox" className="form-check-input me-2"
                checked={groupResults}
                onChange={e => handleGroupResultsChange(e.currentTarget.checked)} />
              {JavascriptMessage.groupResults.niceToString()}
            </label>
          </h5>
          <fieldset className="my-3 p-3 pb-1 bg-body rounded shadow-sm border-0">
            <legend>{SearchMessage.Columns.niceToString()}</legend>
            <DraggableTable
              items={columns.current}
              columns={columnColumns}
              forceUpdate={forceUpdate}
              onCreate={() => ({ token: undefined, displayName: undefined })}
              className="w-auto"
            />
          </fieldset>
          <fieldset className="my-3 p-3 pb-1 bg-body rounded shadow-sm border-0">
            <legend>{SearchMessage.Orders.niceToString()}</legend>
            <DraggableTable
              items={orders.current}
              columns={orderColumns}
              forceUpdate={forceUpdate}
              onCreate={() => ({ token: undefined as unknown as QueryToken, orderType: OrderType.value("Ascending") })}
              className="w-auto"
            />
          </fieldset>
        </>)}
      </div>
      <div className="modal-footer">
        <button type="button" className="btn btn-primary ms-1" onClick={handleOK} disabled={resetting}>
          {JavascriptMessage.ok.niceToString()}
        </button>
        <button type="button" className="btn btn-secondary ms-1" onClick={handleCancel}>
          {JavascriptMessage.cancel.niceToString()}
        </button>
      </div>
    </Modal>
  );
}

namespace ColumnEditorModal {
  export function show(findOptions: FindOptionsParsed, queryDescription: QueryDescription, querySettings?: Finder.QuerySettings): Promise<boolean> {
    return openModal<boolean>(<ColumnEditorModal findOptions={findOptions} queryDescription={queryDescription} querySettings={querySettings} />);
  }
}

export default ColumnEditorModal;
