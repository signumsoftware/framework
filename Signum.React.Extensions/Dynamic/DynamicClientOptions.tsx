
import * as React from 'react'
import { StyleContext } from '@framework/TypeContext'
import { FindOptions, FilterConditionOption, ColumnOption } from '@framework/FindOptions';
import { Type, QueryTokenString } from '@framework/Reflection';
import { Entity } from '@framework/Signum.Entities';
import { ValueSearchControlLine } from '../../../Framework/Signum.React/Scripts/Search';
import { CellFormatter } from '../../../Framework/Signum.React/Scripts/Finder';
import { expandNumbers } from '../DiffLog/Templates/DiffDocument';

export type FormatColumnType = "Text" | "Code" | "JSon";

export namespace Options {

  export let onGetDynamicPanelSearch: ((ctx: StyleContext, search: string) => React.ReactNode)[] = [];

  export function registerDynamicPanelSearch<T extends Entity>(type: Type<T>, getColumns: (token: QueryTokenString<T>) => { token: QueryTokenString<any>, type: FormatColumnType }[]) {
    onGetDynamicPanelSearch.push((ctx, search) => {
      var columns = getColumns(type.token());

      var findOptions = {
        queryName: type,
        filterOptions: [{
          groupOperation: "Or",
          value: search,
          pinned: { splitText: true },
          filters: columns.map(t => ({ token: t.token, operation: "Contains" } as FilterConditionOption))
        }],
        columnOptionsMode: "Add",
        columnOptions: columns.filter(c => c.token.toString().startsWith("Entity.")).map(c => ({ token: c.token }) as ColumnOption)
      } as FindOptions;


      return (
        <ValueSearchControlLine ctx={ctx} findOptions={findOptions} searchControlProps={{
          formatters: columns.toObjectDistinct(a => a.token.toString(), a => new CellFormatter((cell, cfc) => cell && <HighlightText search={search} text={cell} type={a.type} />))
        }} />
      );
    });
  }

  export let onGetDynamicLineForPanel: ((ctx: StyleContext) => React.ReactNode)[] = [];
  export let onGetDynamicLineForType: ((ctx: StyleContext, type: string) => React.ReactNode)[] = [];
  export let checkEvalFindOptions: FindOptions[] = [];

  export let getDynaicMigrationsStep: (() => React.ReactElement<any>) | undefined = undefined;
}


function HighlightText({ text, search, type }: { text: string, search: string, type: FormatColumnType }) {

  if (type == "JSon")
    text = JSON.stringify(JSON.parse(text), undefined, 2);

  var [showAll, setShowAll] = React.useState(type == "Text");

  var lines = text.split(/\r?\n/g);

  var searchParts = search.split(/\s+/).filter(s => s.length > 0).orderBy(a => a.length).map(p => new RegExp(RegExp.escape(p), "gi"));
  function mark(line: string, index: number): React.ReactNode {

    if (index == -1)
      return line;

    var s = searchParts[index];

    if (line.search(s) == -1)
      return mark(line, index - 1);

    var parts = line.split(s);
    var matches = line.match(s)!;

    return React.createElement(React.Fragment, undefined, ...parts.flatMap((p, i) => i != parts.length - 1 ?
      [mark(p, index - 1), <mark>{matches[i]}</mark>] :
      [mark(p, index - 1)]));
  }

  function makeLine(line: React.ReactNode) {
    if (type == "Text")
      return <>{line}<br/></>;
    else
      return <><code>{line}</code>{"\n"}</>;
  }


  if (type == "Text") {
    return (React.createElement("div", undefined, ...lines.map(line => makeLine(mark(line, searchParts.length - 1)))));
  }
  else if (showAll) {
    return (
      <div>
        {React.createElement("pre", undefined, ...lines.map(line => makeLine(mark(line, searchParts.length - 1))))}
        <a href="#" className="text-muted" onClick={e => { e.preventDefault(); setShowAll(false) }}>Show less</a>
      </div>
    );
  }
  else {
    var changes = lines.map((str, i) => searchParts.some(p => str.search(p) != -1) ? i : null).notNull();

    if (changes.length == 0)
      changes = [0];

    var result = expandNumbers(changes, 4);

    return (
      <div>
        {
          React.createElement("pre", undefined, ...result.map(n => typeof n == "number" ? makeLine(mark(lines[n], searchParts.length - 1)) :
            makeLine(` ----- ${n.numLines} Lines Removed ----- `)
          ))}
        <a href="#" className="text-muted" onClick={e => { e.preventDefault(); setShowAll(true) }}>Show all</a>
      </div>
    );
  }
}


