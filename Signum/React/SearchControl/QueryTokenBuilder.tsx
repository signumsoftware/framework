import * as React from 'react'
import { areEqual, classes, Dic } from '../Globals'
import { Finder } from '../Finder'
import { QueryToken, SubTokensOptions, getTokenParents, isPrefix, ManualToken, QueryDescription, getQueryTokenColor } from '../FindOptions'
import * as PropTypes from "prop-types";
import "./QueryTokenBuilder.css"
import { DropdownList } from 'react-widgets-up'
import { StyleContext } from '../Lines';
import { useAPI, useForceUpdate } from '../Hooks';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

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

export default function QueryTokenBuilder(p: QueryTokenBuilderProps): React.ReactElement {

  var [expanded, setExpanded] = React.useState(false);
  const [lastTokenChanged, setLastTokenChanged] = React.useState<string | undefined>(undefined);


  React.useEffect(() => {
    setExpanded(false);
  }, [p.queryKey, p.prefixQueryToken]);

  const qd = useAPI(() => Finder.getQueryDescription(p.queryKey), [p.queryKey]);

  function handleExpandButton(e: React.MouseEvent<any>) {
    setExpanded(true);
  }

  let tokenList: (QueryToken | undefined)[] = [...getTokenParents(p.queryToken)];

  var initialIndex = !expanded && p.prefixQueryToken && p.queryToken && isPrefix(p.prefixQueryToken, p.queryToken) ?
    tokenList.findIndex(a => a!.fullKey == p.prefixQueryToken!.fullKey) + 1 : 0;

  if (!p.readOnly)
    tokenList.push(undefined);

  return (
    <div className={classes("sf-query-token-builder", p.className)} onKeyDown={handleKeyDown} data-token={p.queryToken?.fullKey}>
      {initialIndex != 0 && <button type="button" onClick={handleExpandButton} className="btn btn-sm sf-prefix-btn">…</button>}
      {qd && tokenList.map((a, i) => {
        if (i < initialIndex)
          return null;

        var parentToken = i == 0 ? undefined : tokenList[i - 1]!;

        return (
          <QueryTokenPart key={i == 0 ? "__first__" : parentToken!.fullKey}
            queryDescription={qd}
            queryKey={p.queryKey}
            readOnly={p.readOnly}
            setLastTokenChange={(fullKey) => { setLastTokenChanged(fullKey); }}
            onTokenSelected={async (qt, keyboard) => {
              var nqt = (await tryApplyToken(p.queryToken, qt)) ?? qt;
              setLastTokenChanged(keyboard ? nqt?.fullKey : undefined);
              p.onTokenChange && p.onTokenChange(nqt);
            }}
            defaultOpen={lastTokenChanged && i > 0 && lastTokenChanged == parentToken!.fullKey
          /*&& (tokenList[i - 1]!.type.isCollection)*/ ? true : false}
            subTokenOptions={p.subTokenOptions}
            parentToken={parentToken}
            selectedToken={a} />
        );
      })}
    </div>
  );

  async function tryApplyToken(token: QueryToken | null | undefined, newToken: QueryToken | undefined): Promise<QueryToken | undefined> {
    if (newToken == undefined)
      return undefined;

    if (token == null)
      return newToken;

    if (token.fullKey == newToken.fullKey)
      return newToken;

    if (token.fullKey.startsWith(newToken.fullKey + "."))
      return newToken;

    if (newToken.parent == null || token.fullKey.startsWith(newToken.parent.fullKey + ".")) {
      var tokenParents = getTokenParents(token);
      var newTokenParents = getTokenParents(newToken);

      var extraTokens = tokenParents.slice(newTokenParents.length);

      var tempToken = newToken;
      var tokenCompleter = new Finder.TokenCompleter(qd!);
      for (var i = 0; i < extraTokens.length; i++) {
        var key = extraTokens[i].key;
        var t = (await tokenCompleter.getSubTokens(tempToken, p.subTokenOptions, false)).singleOrNull(a => a.key == key);
        if (t == null)
          return newToken;

        tempToken = t;
      }

      return tempToken;

    } else {
      return newToken;
    }
  }

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
          .then(a => p.onTokenChange(a));
        e.preventDefault();
      }
    }
  }
}


interface QueryTokenPartProps {
  queryDescription: QueryDescription;
  parentToken: QueryToken | undefined;
  selectedToken: QueryToken | undefined;
  onTokenSelected: (newToken: QueryToken | undefined, keyboard: boolean) => void;
  queryKey: string;
  subTokenOptions: SubTokensOptions;
  readOnly: boolean;
  defaultOpen: boolean;
  setLastTokenChange: (fullKey: string | undefined) => void;
}

//const ParentTokenContext = React.createContext<QueryToken | undefined>(undefined);


export function QueryTokenPart(p: QueryTokenPartProps): React.ReactElement | null {

  const doAutoExpand = !p.parentToken?.type.isCollection;

  const subTokens = useAPI(() => {
    if (p.readOnly)
      return Promise.resolve(undefined);

    const manuals = getManualSubTokens(p.parentToken);
    if (manuals)
      return manuals.then(tokens => tokens.length == 0 ? tokens : [null, ...tokens]);

    var tc = new Finder.TokenCompleter(p.queryDescription);
  
    return tc.getSubTokens(p.parentToken, p.subTokenOptions, doAutoExpand)
      .then(tokens => tokens.length == 0 ? tokens : [null, ...tokens])
  }, [p.readOnly, p.parentToken && p.parentToken.fullKey, p.subTokenOptions, p.queryKey])


  const [open, setOpen] = React.useState(p.defaultOpen)


  if (subTokens != undefined && subTokens.length == 0)
    return null;

  return (
    //<ParentTokenContext.Provider value={p.parentToken}>
    <div className="sf-query-token-part" onKeyUp={handleKeyUp} onKeyDown={handleKeyUp}>
      {p.selectedToken || p.parentToken == null || p.defaultOpen ?
        <DropdownList
          disabled={p.readOnly}
          selectIcon={open && doAutoExpand ? <FontAwesomeIcon aria-hidden={true} icon="magnifying-glass" /> : undefined}
          onToggle={isOpen => setOpen(isOpen)}
          filter={(item, searchTerm, idx) => item != null && searchTerm.toLowerCase().split(" ").filter(a => a != "").every(part => parentsUntil(item, p.parentToken).some(t => t.key.toLowerCase().contains(part) || t.toStr.toLowerCase().contains(part)))}
          autoComplete="off"
          focusFirstItem={true}
          data={subTokens?.orderBy(a => a?.parent != null) ?? []}
          placeholder={p.selectedToken == null ? "..." : undefined}
          value={p.selectedToken}
          onChange={(value, metadata) => p.onTokenSelected(value ?? p.parentToken, metadata.originalEvent?.nativeEvent instanceof KeyboardEvent)}
          dataKey="fullKey"
          textField="toStr"
          onBlur={() => {  p.selectedToken == null && p.setLastTokenChange(undefined); }}
          renderValue={a => <QueryTokenItem item={a.item} />}
          renderListItem={a => <QueryTokenListItem item={a.item} ancestor={p.parentToken} />}
          defaultOpen={p.defaultOpen}
          busy={!p.readOnly && subTokens == undefined}
        /> : <button type="button" className="btn btn-sm sf-query-token-plus" onClick={e => { e.preventDefault(); p.setLastTokenChange(p.parentToken!.fullKey); }}>
          <FontAwesomeIcon aria-hidden={true} icon="plus" />
        </button>}
    </div>
    //</ParentTokenContext.Provider>
  );

  function handleKeyUp(e: React.KeyboardEvent<any>) {
    if (e.key == "Enter") {
      e.preventDefault();
      e.stopPropagation();
    }
  }
}

export function QueryTokenItem(p: { item: QueryToken | null }): React.ReactElement | null {

  const item = p.item;

  if (item == null)
    return null;



  return (
    <span
      data-full-token={item.fullKey}
      style={{ color: getQueryTokenColor(item) }}
      title={StyleContext.default.titleLabels ? item.niceTypeName : undefined}>
      {item.toStr}
    </span>
  );
}


export function QueryTokenListItem(p: { item: QueryToken | null, ancestor: QueryToken | undefined }): React.ReactElement {

  const item = p.item;

  if (item == null)
    return <span> - </span>;

  return (
    <span data-full-token={item.fullKey} style={{ whiteSpace: "nowrap" }} className="sf-token-list-item">
      {parentsUntil(item, p.ancestor)
        .map(a => <span style={{ color: getQueryTokenColor(a) }} title={StyleContext.default.titleLabels ? a.niceTypeName : undefined}>{a.toStr}</span>)
        .joinHtml(" › ")}
    </span>
  );
}

function parentsUntil(token: QueryToken, ancestor?: QueryToken) {
  const tokens: QueryToken[] = [];

  for (let t: QueryToken | undefined = token; t != null && t.fullKey != ancestor?.fullKey; t = t?.parent) {
    tokens.push(t);
  }

  tokens.reverse();

  return tokens;
}


export function clearManualSubTokens(): void {
  Dic.clear(manualSubTokens);
}

export const manualSubTokens: { [key: string]: (entityType: string) => Promise<ManualToken[]> } = {};

export function registerManualSubTokens(key: string, func: (entityType: string) => Promise<ManualToken[]>): void {
  Dic.addOrThrow(manualSubTokens, key, func);
}

function getManualSubTokens(token?: QueryToken) {

  if (token?.parent && token.queryTokenType == 'Manual' && token.parent.queryTokenType == 'Manual')//it is a Manual sub token
    return Promise.resolve([]); //prevents sending to server

  const container = token?.parent && manualSubTokens[token.key] && token;
  if (container) {
    const entityType = container.parent!.type.name;
    const manuals = manualSubTokens[container.key] && manualSubTokens[container.key](entityType);
    const tokens = manuals.then(ms => ms.map(m =>
    ({
      ...m, parent: container,
      fullKey: (container.fullKey + "." + m.key),
      type: { name: "ManualCellDTO" },
      queryTokenType: "Manual",
      isGroupable: false
    } as QueryToken)));

    return tokens;
  }
}
