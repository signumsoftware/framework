
import * as React from 'react'
import { classes } from '../Globals';
import { ColumnOption, ColumnOptionParsed, QueryDescription, QueryToken, SubTokensOptions } from '../FindOptions'
import { EntityControlMessage, SearchMessage } from '../Signum.Entities'
import QueryTokenBuilder from './QueryTokenBuilder'
import { StyleContext } from '../Lines';
import { CombineRows } from '../Signum.DynamicQuery';
import { VisualTipIcon } from '../Basics/VisualTipIcon';
import { SearchVisualTip } from '../Signum.Basics';
import { ColumnHelp, FilterHelp } from './SearchControlVisualTips';
import { getNiceTypeName } from '../Operations/MultiPropertySetter';
import { LinkButton } from '../Basics/LinkButton';

interface ColumnEditorProps {
  columnOption: ColumnOptionParsed
  subTokensOptions: SubTokensOptions;
  queryDescription: QueryDescription;
  onChange: (token?: QueryToken) => void;
  close: () => void;
}

export default function ColumnEditor(p: ColumnEditorProps): React.ReactElement {

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

  function handleCombineEqualsVertically(e: React.ChangeEvent<HTMLSelectElement>) {
    co.combineRows = (e.currentTarget.value as CombineRows) || undefined;
    p.onChange(undefined);
  }

  const co = p.columnOption;

  const tokenError = columnError(co.token);

  const [showMore, setShowMore] = React.useState<boolean>(false);

  React.useEffect(() => {
    if (co.summaryToken || co.displayName != co.token?.niceName || co.combineRows)
      setShowMore(true);
  }, [co.summaryToken, co.displayName != co.token?.niceName, co.combineRows]);

  const summaryError = columnSummaryError(co.summaryToken);

  return (
    <div className={classes("sf-column-editor", tokenError && "error")}>

      <div className="row">
        <div className={showMore ? "col-sm-7" : "col-sm-11" }>

          <div className="d-flex" title={!StyleContext.default.titleLabels ? undefined : tokenError}>
            <label className="col-form-label col-form-label-xs me-2 fw-bold">{SearchMessage.ColumnField.niceToString()}</label>
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
            
            {!showMore && <LinkButton title={undefined} onClick={e => { setShowMore(true); }}>{SearchMessage.ShowMore.niceToString()}</LinkButton>}
          </div>


          {showMore && < div className={classes("d-flex", summaryError ? "error" : undefined)}
            title={StyleContext.default.titleLabels ? summaryError : undefined}>
            <label className="col-form-label col-form-label-xs me-2">
              <input type="checkbox" disabled={co.token == null} className="form-check-input me-2" checked={co.summaryToken != null} onChange={handleSummaryCheck} />
              {SearchMessage.SummaryHeader.niceToString()} (Ʃ)
            </label>
            <div className="rw-widget-xs">
              {co.summaryToken && <QueryTokenBuilder
                queryToken={co.summaryToken!}
                onTokenChange={handleSummaryTokenChanged}
                queryKey={p.queryDescription.queryKey}
                subTokenOptions={p.subTokensOptions | SubTokensOptions.CanAggregate}
                readOnly={false} />
              }
            </div>
          </div>}
        </div>

        {showMore && <div className="col-sm-4">

          < div className="d-flex">
            <label className="col-form-label col-form-label-xs me-2">{SearchMessage.DisplayName.niceToString()}</label>
            <div className="flex-grow-1">
              <input className="form-control form-control-xs "
                value={co.displayName || ""} disabled={co.hiddenColumn}
                onChange={handleOnChange} />
            </div>
          </div>

          <div className="d-flex">
            <label htmlFor="combineRows" className="col-form-label col-form-label-xs me-2" style={{ minWidth: "140px" }}>  {SearchMessage.CombineRowsWith.niceToString()}</label>
            <div className="flex-grow-1">
              <select className="form-select form-select-xs" id="combineRows" value={co.combineRows ?? ""} onChange={handleCombineEqualsVertically}>
                <option value={""}>{" - "}</option>
                <option value={CombineRows.value("EqualEntity")}>{SearchMessage.Equal0.niceToString(getNiceTypeName(p.queryDescription.columns['Entity'].type))}</option>
                <option value={CombineRows.value("EqualValue")}>{CombineRows.niceToString("EqualValue")}</option>
              </select>
            </div>
          </div>

        </div>
        }

        <div className="col-sm-1">
          <button type="button" className="btn-close float-end" aria-label={EntityControlMessage.Close.niceToString()} onClick={p.close} />
          <VisualTipIcon visualTip={SearchVisualTip.ColumnHelp} content={props => <ColumnHelp queryDescription={p.queryDescription} injected={props} />} />
        </div>
      </div>
    </div >
  );
}

export function columnError(token?: QueryToken | undefined): string | undefined {
  if (token == null)
    return undefined;

  if (token.type.isCollection)
    return SearchMessage.CollectionsCanNotBeAddedAsColumns.niceToString();

  if (token.queryTokenType == "OperationContainer" || token.queryTokenType == "Manual" || token.queryTokenType == "IndexerContainer")
    return SearchMessage.InvalidColumnExpression.niceToString();
    
  return undefined;
}

export function columnSummaryError(summaryToken: QueryToken | undefined): string | undefined {
  if (summaryToken == null)
    return undefined;

  if (summaryToken.queryTokenType != "Aggregate")
    return SearchMessage.SummaryHeaderMustBeAnAggregate.niceToString();

  return undefined;
}

