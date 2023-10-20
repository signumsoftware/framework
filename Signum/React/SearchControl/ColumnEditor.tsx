
import * as React from 'react'
import { classes } from '../Globals';
import { ColumnOptionParsed, QueryDescription, QueryToken, SubTokensOptions } from '../FindOptions'
import { SearchMessage } from '../Signum.Entities'
import QueryTokenBuilder from './QueryTokenBuilder'
import { StyleContext } from '../Lines';
import { CombineRows } from '../Signum.DynamicQuery';
import { ColumnHelp } from './QueryTokenHelp';

interface ColumnEditorProps {
  columnOption: ColumnOptionParsed
  subTokensOptions: SubTokensOptions;
  queryDescription: QueryDescription;
  onChange: (token?: QueryToken) => void;
  close: () => void;
}

export default function ColumnEditor(p: ColumnEditorProps) {

  function handleSummaryTokenChanged(newToken: QueryToken | undefined) {
    p.columnOption.summaryToken = newToken;
    p.onChange(undefined);
  }

  function handleTokenChanged(newToken: QueryToken | undefined) {
    p.columnOption.token = newToken;
    p.columnOption.summaryToken = undefined;
    p.columnOption.displayName = newToken?.niceName;
    p.onChange(newToken);
  }

  function handleOnChange(event: React.ChangeEvent<HTMLInputElement>) {
    p.columnOption.displayName = event.currentTarget.value ?? undefined;
    p.onChange(undefined);
  }

  function handleSummaryCheck() {
    co.summaryToken = co.summaryToken ? undefined : co.token;
    p.onChange(undefined);
  }

  function handleHiddenColumnClick() {
    co.hiddenColumn = co.hiddenColumn ? undefined : true;
    co.displayName = co.token?.niceName;
    co.summaryToken = undefined;
    co.combineRows = undefined;
    p.onChange(undefined);
  }

  function handleCombineEqualsVertically(e: React.ChangeEvent<HTMLSelectElement>) {
    co.combineRows = (e.currentTarget.value as CombineRows) || undefined;
    p.onChange(undefined);
  }

  const co = p.columnOption;

  const isCollection = co.token && co.token.type.isCollection;
  const isInvalid = co.token && (co.token.queryTokenType == "Operation" || co.token.queryTokenType == "Manual");

  const summaryNotAggregate = co.summaryToken != null && co.summaryToken.queryTokenType != "Aggregate";

  return (
    <div className="sf-column-editor">

      <div className="row">
        <div className="col-sm-1">
          <label className="col-form-label col-form-label-xs me-2" style={{ minWidth: "140px" }}>{SearchMessage.Field.niceToString()}</label>
        </div>
        <div className="col-sm-6">
          <div className={classes("d-flex", isCollection || isInvalid ? "error" : undefined)}
            title={!StyleContext.default.titleLabels ? undefined :
              isCollection ? SearchMessage.CollectionsCanNotBeAddedAsColumns.niceToString() :
                isInvalid ? SearchMessage.InvalidColumnExpression.niceToString() : undefined}>
            <div className="flex-grow-1">
              <div className="rw-widget-xs">
                <QueryTokenBuilder
                  queryToken={co.token!}
                  onTokenChange={handleTokenChanged}
                  queryKey={p.queryDescription.queryKey}
                  subTokenOptions={p.subTokensOptions}
                  readOnly={false} />
              </div>
            </div>
          </div>
        </div>
        <div className="col-sm-3">
          <div className="d-flex">
            <label className="col-form-label col-form-label-xs" style={{ minWidth: "140px" }}>{SearchMessage.DisplayName.niceToString()}</label>
            <div className="flex-grow-1">
              <input className="form-control form-control-xs "
                value={co.displayName || ""} disabled={co.hiddenColumn}
                onChange={handleOnChange} />
            </div>
          </div>
        </div>

        <div className="col-sm-2">
            <label className="col-form-label col-form-label-xs">
              <input type="checkbox" disabled={co.token == null} className="form-check-input me-1" checked={co.hiddenColumn} onChange={handleHiddenColumnClick} />
              {SearchMessage.HiddenColumn.niceToString()}
            </label>
            <button type="button" className="btn-close float-end" aria-label="Close" onClick={p.close} />
        </div>
      </div>

      <div className="row">
        <div className="col-sm-1">
          <div className={classes("d-flex", co.summaryToken && summaryNotAggregate ? "error" : undefined)}
            title={StyleContext.default.titleLabels && summaryNotAggregate ? SearchMessage.SummaryHeaderMustBeAnAggregate.niceToString() : undefined}>
            <label className="col-form-label col-form-label-xs" style={{ minWidth: "140px" }}>
              <input type="checkbox" disabled={co.token == null} className="form-check-input me-1" checked={co.summaryToken != null} onChange={handleSummaryCheck} />
              {SearchMessage.SummaryHeader.niceToString()}
            </label>
          </div>
        </div>
        <div className="col-sm-6">
          <div className="rw-widget-xs">
            {co.summaryToken && <QueryTokenBuilder
              queryToken={co.summaryToken!}
              onTokenChange={handleSummaryTokenChanged}
              queryKey={p.queryDescription.queryKey}
              subTokenOptions={p.subTokensOptions | SubTokensOptions.CanAggregate}
              readOnly={false} />
            }
          </div>
        </div>
        <div className="col-sm-3">
          <div className="d-flex">
            <label htmlFor="combineRows" className="col-form-label col-form-label-xs" style={{ minWidth: "140px" }}>  {SearchMessage.CombineRowsWith.niceToString()}</label>
            <div className="flex-grow-1">
              <select className="form-select form-select-xs" id="combineRows" value={co.combineRows ?? ""} onChange={handleCombineEqualsVertically}>
                <option value={""}>{" - "}</option>
                <option value={CombineRows.value("EqualEntity")}>{CombineRows.niceToString("EqualEntity")}</option>
                <option value={CombineRows.value("EqualValue")}>{CombineRows.niceToString("EqualValue")}</option>
              </select>
            </div>
          </div>
        </div>
        <div className="col-sm-2">
          <ColumnHelp queryKey={p.queryDescription.queryKey} type={p.queryDescription.columns['Entity'].displayName} />
        </div>
      </div>
    </div>
  );
}



