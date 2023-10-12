import * as React from 'react'
import { areEqual, classes, Dic } from '../Globals'
import * as Finder from '../Finder'
import { QueryToken, SubTokensOptions, getTokenParents, isPrefix, ManualToken } from '../FindOptions'
import * as PropTypes from "prop-types";
import "./QueryTokenBuilder.css"
import { DropdownList } from 'react-widgets'
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

export default function QueryTokenBuilder(p: QueryTokenBuilderProps) {
  var [expanded, setExpanded] = React.useState(false);
  const [lastTokenChanged, setLastTokenChanged] = React.useState<string | undefined>(undefined);


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
      {tokenList.map((a, i) => {
        if (i < initialIndex)
          return null;

        var parentToken = i == 0 ? undefined : tokenList[i - 1]!;

        return (
          <QueryTokenPart key={i == 0 ? "__first__" : parentToken!.fullKey}
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

  async function tryApplyToken(token: QueryToken | null | undefined, newToken: QueryToken | undefined) {
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
      for (var i = 0; i < extraTokens.length; i++) {
        var key = extraTokens[i].key;
        var t = (await Finder.API.getSubTokens(p.queryKey, tempToken, p.subTokenOptions)).singleOrNull(a => a.key == key);
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
  parentToken: QueryToken | undefined;
  selectedToken: QueryToken | undefined;
  onTokenSelected: (newToken: QueryToken | undefined, keyboard: boolean) => void;
  queryKey: string;
  subTokenOptions: SubTokensOptions;
  readOnly: boolean;
  defaultOpen: boolean;
  setLastTokenChange: (fullKey: string | undefined) => void;
}

const ParentTokenContext = React.createContext<QueryToken | undefined>(undefined);


export function QueryTokenPart(p: QueryTokenPartProps) {

  const subTokens = useAPI(() => {
    if (p.readOnly)
      return Promise.resolve(undefined);

    const manuals = getManualSubTokens(p.parentToken);
    if (manuals)
      return manuals.then(tokens => tokens.length == 0 ? tokens : [null, ...tokens]);

    return Finder.API.getSubTokens(p.queryKey, p.parentToken, p.subTokenOptions)
      .then(tokens => tokens.length == 0 ? tokens : [null, ...tokens])
  }, [p.readOnly, p.parentToken && p.parentToken.fullKey, p.subTokenOptions, p.queryKey])

  if (subTokens != undefined && subTokens.length == 0)
    return null;

  return (
    <ParentTokenContext.Provider value={p.parentToken}>
      <div className="sf-query-token-part" onKeyUp={handleKeyUp} onKeyDown={handleKeyUp}>
        {p.selectedToken || p.parentToken == null || p.defaultOpen ?
          <DropdownList
            disabled={p.readOnly}
            filter="contains"
            autoComplete="off"
            focusFirstItem={true}
            data={subTokens ?? []}
            placeholder={p.selectedToken == null ? "..." : undefined}
            value={p.selectedToken}
            onChange={(value, metadata) => p.onTokenSelected(value ?? p.parentToken, metadata.originalEvent?.nativeEvent instanceof KeyboardEvent)}
            dataKey="fullKey"
            textField="toStr"
            onBlur={() => { p.setLastTokenChange(undefined); }}
            renderValue={a => <QueryTokenItem item={a.item} />}
            renderListItem={a => <QueryTokenOptionalItem item={a.item} />}
            defaultOpen={p.defaultOpen}
            busy={!p.readOnly && subTokens == undefined}
          /> : <button className="btn btn-sm sf-query-token-plus" onClick={e => { e.preventDefault(); p.setLastTokenChange(p.parentToken!.fullKey); }}>
            <FontAwesomeIcon icon="plus" />
          </button>}
      </div>
    </ParentTokenContext.Provider>
  );

  function handleKeyUp(e: React.KeyboardEvent<any>) {
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


export function clearManualSubTokens() {
  Dic.clear(manualSubTokens);
}

export const manualSubTokens: { [key: string]: (entityType: string) => Promise<ManualToken[]> } = {};

export function registerManualSubTokens(key: string, func: (entityType: string) => Promise<ManualToken[]>) {
  Dic.addOrThrow(manualSubTokens, key, func);
}

function getManualSubTokens(token?: QueryToken) {

  //if (token?.type.name == "ManualCellDTO")
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
