import * as React from 'react'
import { ColumnOptionParsed, QueryDescription, QueryToken, SubTokensOptions } from '../FindOptions'
import { SearchMessage, JavascriptMessage } from '../Signum.Entities'
import QueryTokenBuilder from './QueryTokenBuilder'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import "./ColumnBuilder.css"
import { StyleContext } from '../Lines';
import { useForceUpdate } from '../Hooks';

export interface ColumnsBuilderProps {
  queryDescription: QueryDescription;
  columnOptions: ColumnOptionParsed[];
  subTokensOptions: SubTokensOptions;
  onColumnsChanged?: (columns: ColumnOptionParsed[]) => void;
  title?: React.ReactNode;
  readonly?: boolean;
}

export default function ColumnsBuilder(p: ColumnsBuilderProps): React.ReactElement {

  const forceUpdate = useForceUpdate();

  function handlerNewColumn() {

    p.columnOptions.push({
      token: undefined,
      displayName: undefined,
    });

    if (p.onColumnsChanged)
      p.onColumnsChanged(p.columnOptions);

    forceUpdate();
  };

  function handlerDeleteColumn(column: ColumnOptionParsed) {
    p.columnOptions.remove(column);
    if (p.onColumnsChanged)
      p.onColumnsChanged(p.columnOptions);

    forceUpdate();
  };

  function handleColumnChanged(column: ColumnOptionParsed) {
    if (p.onColumnsChanged)
      p.onColumnsChanged(p.columnOptions);

    forceUpdate();
  };


  return (
    <fieldset className="form-xs">
      {p.title && <legend>{p.title}</legend>}
      <div className="sf-columns-list table-responsive" style={{ overflowX: "visible" }}>
        <table className="table table-condensed">
          <thead>
            <tr>
              <th style={{ minWidth: "24px" }}></th>
              <th>{SearchMessage.ColumnField.niceToString()}</th>
            </tr>
          </thead>
          <tbody>
            {p.columnOptions.map((c, i) => <ColumnComponent column={c} key={i} readonly={Boolean(p.readonly)}
              onDeleteColumn={handlerDeleteColumn}
              subTokenOptions={p.subTokensOptions}
              queryDescription={p.queryDescription}
              onColumnChanged={handleColumnChanged}
            />)}
            {!p.readonly &&
              <tr>
                <td colSpan={4}>
                  <a title={StyleContext.default.titleLabels ? SearchMessage.AddColumn.niceToString() : undefined}
                    className="sf-line-button sf-create"
                    onClick={handlerNewColumn}>
                    <FontAwesomeIcon icon="plus" className="sf-create" />&nbsp;{SearchMessage.AddColumn.niceToString()}
                  </a>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </fieldset>
  );
}



export interface ColumnComponentProps {
  column: ColumnOptionParsed;
  onDeleteColumn: (fo: ColumnOptionParsed) => void;
  queryDescription: QueryDescription;
  subTokenOptions: SubTokensOptions;
  onTokenChanged?: (token: QueryToken | undefined) => void;
  onColumnChanged: (column: ColumnOptionParsed) => void;
  readonly: boolean;
}

export function ColumnComponent(p: ColumnComponentProps): React.ReactElement {

  const forceUpdate = useForceUpdate();
  function handleDeleteColumn() {
    p.onDeleteColumn(p.column);
  }

  function handleTokenChanged(newToken: QueryToken | null | undefined) {
    const c = p.column;
    c.displayName = undefined;
    c.token = newToken ?? undefined;

    if (p.onTokenChanged)
      p.onTokenChanged(newToken ?? undefined);

    p.onColumnChanged(p.column);

    forceUpdate();
  }

  const c = p.column;
  const readonly = p.readonly;
  return (
    <tr>
      <td>
        {!readonly &&
          <a title={StyleContext.default.titleLabels ? JavascriptMessage.removeColumn.niceToString() : undefined}
            className="sf-line-button sf-remove"
            onClick={handleDeleteColumn}>
            <FontAwesomeIcon icon="xmark" />
          </a>
        }
      </td>
      <td>
        <QueryTokenBuilder
          queryToken={c.token}
          onTokenChange={handleTokenChanged}
          queryKey={p.queryDescription.queryKey}
          subTokenOptions={p.subTokenOptions}
          readOnly={readonly} />
      </td>
    </tr>
  );
}

