
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

  function handleTokenChanged(newToken: QueryToken | undefined) {
    p.columnOption.token = newToken;
    p.columnOption.displayName = newToken && newToken.niceName;
    p.onChange(newToken);
  }

  function handleOnChange(event: React.ChangeEvent<HTMLInputElement>) {
    p.columnOption.displayName = event.currentTarget.value || undefined;
    p.onChange(undefined);
  }

  const co = p.columnOption;

  const isCollection = co.token && co.token.type.isCollection;

  return (
    <div className={classes("sf-column-editor", isCollection ? "error" : undefined)}
      title={StyleContext.default.titleLabels && isCollection ? SearchMessage.CollectionsCanNotBeAddedAsColumns.niceToString() : undefined}>
      <button type="button" className="close" aria-label="Close" onClick={p.close} ><span aria-hidden="true">×</span></button>
      <div className="rw-widget-xs">
        <QueryTokenBuilder
          queryToken={co.token!}
          onTokenChange={handleTokenChanged}
          queryKey={p.queryDescription.queryKey}
          subTokenOptions={p.subTokensOptions}
          readOnly={false} />
      </div>
      <input className="form-control form-control-xs"
        value={co.displayName || ""}
        onChange={handleOnChange} />
    </div>
  );
}



