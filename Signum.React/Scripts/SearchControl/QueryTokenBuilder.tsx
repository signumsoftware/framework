import * as React from 'react'
import { areEqual, classes } from '../Globals'
import * as Finder from '../Finder'
import { QueryToken, SubTokensOptions, getTokenParents, isPrefix } from '../FindOptions'
import * as PropTypes from "prop-types";
import "./QueryTokenBuilder.css"
import { DropdownList } from 'react-widgets'
import { StyleContext } from '../Lines';
import { useAPI } from '../Hooks';

interface QueryTokenBuilderProps {
  prefixQueryToken?: QueryToken | undefined;
  queryToken: QueryToken | undefined | null;
  onTokenChange: (newToken: QueryToken | undefined) => void;
  queryKey: string;
  subTokenOptions: SubTokensOptions;
  readOnly: boolean;
  className?: string;
}

let copiedToken: { fullKey: string, queryKey: string } | undefined;

export default function QueryTokenBuilder(p: QueryTokenBuilderProps) {
  var [expanded, setExpanded] = React.useState(false);
  var lastTokenChanged = React.useRef<string | undefined>(undefined);

  React.useEffect(() => {
    setExpanded(false);
  }, [p.queryKey, p.prefixQueryToken])

  function handleExpandButton(e: React.MouseEvent<any>) {
    setExpanded(true);
  }

  let tokenList: (QueryToken | undefined)[] = [...getTokenParents(p.queryToken)];

  var initialIndex = !expanded && p.prefixQueryToken && p.queryToken && isPrefix(p.prefixQueryToken, p.queryToken) ?
    tokenList.findIndex(a => a!.fullKey == p.prefixQueryToken!.fullKey) + 1 : 0;

  if (!p.readOnly)
    tokenList.push(undefined);

  return (
    <div className={classes("sf-query-token-builder", p.className)} onKeyDown={handleKeyDown}>
      {initialIndex != 0 && <button onClick={handleExpandButton} className="btn btn-sm sf-prefix-btn">…</button>}
      {tokenList.map((a, i) => i < initialIndex ? null : <QueryTokenPart key={i == 0 ? "__first__" : tokenList[i - 1]!.fullKey}
        queryKey={p.queryKey}
        readOnly={p.readOnly}
        onTokenSelected={qt => {
          lastTokenChanged.current = qt?.fullKey;
          p.onTokenChange && p.onTokenChange(qt);
        }}
        defaultOpen={lastTokenChanged.current && i > 0 && lastTokenChanged.current == tokenList[i - 1]!.fullKey ? true : false}
        subTokenOptions={p.subTokenOptions}
        parentToken={i == 0 ? undefined : tokenList[i - 1]}
        selectedToken={a} />)}
    </div>
  );

  function handleKeyDown(e: React.KeyboardEvent<HTMLDivElement>) {

    if (e.ctrlKey) {
      if (e.key == "c") {
        copiedToken = p.queryToken ? {
          fullKey: p.queryToken.fullKey,
          queryKey: p.queryKey
        } : undefined;
        e.preventDefault();
      }
      else if (e.key == "v" && copiedToken?.queryKey == p.queryKey) {
        Finder.parseSingleToken(p.queryKey, copiedToken.fullKey, p.subTokenOptions)
          .then(a => p.onTokenChange(a))
          .done();
        e.preventDefault();
      }
    }
  }
}


interface QueryTokenPartProps{
  parentToken: QueryToken | undefined;
  selectedToken: QueryToken | undefined;
  onTokenSelected: (newToken: QueryToken | undefined) => void;
  queryKey: string;
  subTokenOptions: SubTokensOptions;
  readOnly: boolean;
  defaultOpen: boolean;
}

const ParentTokenContext = React.createContext<QueryToken | undefined>(undefined);

export function QueryTokenPart(p: QueryTokenPartProps) {

  const subTokens = useAPI(() => {
    if (p.readOnly)
      return Promise.resolve(undefined);

    return Finder.API.getSubTokens(p.queryKey, p.parentToken, p.subTokenOptions)
      .then(tokens => tokens.length == 0 ? tokens : [null, ...tokens])
  }, [p.readOnly, p.parentToken && p.parentToken.fullKey, p.subTokenOptions, p.queryKey])

  if (subTokens != undefined && subTokens.length == 0)
    return null;

  return (
    <ParentTokenContext.Provider value={p.parentToken}>
      <div className="sf-query-token-part" onKeyUp={handleKeyUp} onKeyDown={handleKeyUp}>
        <DropdownList
          disabled={p.readOnly}
          filter="contains"
          autoComplete="off"
          focusFirstItem={true}
          data={subTokens ?? []}
          placeholder={p.selectedToken == null ? "..." : undefined}
          value={p.selectedToken}
          onChange={handleOnChange}
          dataKey="fullKey"
          textField="toStr"
          renderValue={a => <QueryTokenItem item={a.item} />}
          renderListItem={a => <QueryTokenOptionalItem item={a.item} />}
          defaultOpen={p.defaultOpen}
          busy={!p.readOnly && subTokens == undefined}
        />
      </div>
      </ParentTokenContext.Provider>
      );

 
  function handleOnChange (value: any) {
    p.onTokenSelected(value ?? p.parentToken);
  }

  function handleKeyUp (e: React.KeyboardEvent<any>) {
    if (e.key == "Enter") {
      e.preventDefault();
      e.stopPropagation();
    }
  }
}

export function QueryTokenItem(p: { item: QueryToken | null }) {

  const item = p.item;

  if (item == null)
    return null;

  return (
    <span
      style={{ color: item.typeColor }}
      title={StyleContext.default.titleLabels ? item.niceTypeName : undefined}>
      {item.toStr}
    </span>
  );
}


export function QueryTokenOptionalItem(p: { item: QueryToken | null }) {

  const item = p.item;

  if (item == null)
    return <span> - </span>;

  var parentToken = React.useContext(ParentTokenContext);

  return (
    <span data-token={item.key}
      style={{ color: item.typeColor }}
      title={StyleContext.default.titleLabels ? item.niceTypeName : undefined}>
      {((item.parent && !parentToken) ? " > " : "") + item.toStr}
    </span>
  );
}
