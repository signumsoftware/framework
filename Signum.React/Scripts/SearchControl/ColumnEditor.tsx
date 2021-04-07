
import * as React from 'react'
import { classes } from '../Globals';
import { ColumnOptionParsed, QueryDescription, QueryToken, SubTokensOptions } from '../FindOptions'
import { SearchMessage } from '../Signum.Entities'
import QueryTokenBuilder from './QueryTokenBuilder'
import { StyleContext } from '../Lines';

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
    p.onChange(undefined);
  }

  const co = p.columnOption;

  const isCollection = co.token && co.token.type.isCollection;

  const summaryNotAggregate = co.summaryToken != null && co.summaryToken.queryTokenType != "Aggregate";

  return (
    <div className="sf-column-editor">
      <button type="button" className="close" aria-label="Close" onClick={p.close} ><span aria-hidden="true">×</span></button>
      <div className={classes("d-flex", isCollection ? "error" : undefined)}
        title={StyleContext.default.titleLabels && isCollection ? SearchMessage.CollectionsCanNotBeAddedAsColumns.niceToString() : undefined}>
        <label className="col-form-label col-form-label-xs mr-2" style={{ minWidth: "140px" }}>{SearchMessage.Field.niceToString()}</label>
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
      <div className="d-flex">
        <label className="col-form-label col-form-label-xs mr-2" style={{ minWidth: "140px" }}>{SearchMessage.DisplayName.niceToString()}</label>
        <div className="flex-grow-1">
          <input className="form-control form-control-xs"
            value={co.displayName || ""} disabled={co.hiddenColumn}
            onChange={handleOnChange} />
          <label className="col-form-label col-form-label-xs mr-2" style={{ minWidth: "140px" }}>
            <input type="checkbox" disabled={co.token == null} checked={co.hiddenColumn} onChange={handleHiddenColumnClick} className="mr-1" />
            {SearchMessage.HiddenColumn.niceToString()}
          </label>
        </div>
      </div>
      <div className={classes("d-flex", co.summaryToken && summaryNotAggregate ? "error" : undefined)}
        title={StyleContext.default.titleLabels && summaryNotAggregate ? SearchMessage.SummaryHeaderMustBeAnAggregate.niceToString() : undefined}>
        <label className="col-form-label col-form-label-xs mr-2" style={{ minWidth: "140px" }}>
          <input type="checkbox" disabled={co.token ==null} checked={co.summaryToken != null} onChange={handleSummaryCheck} className="mr-1" />
          {SearchMessage.SummaryHeader.niceToString()}
        </label>
        <div className="flex-grow-1">
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
      </div>
    </div>
  );
}



