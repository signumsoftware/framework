import * as React from 'react'
import { RouteObject } from 'react-router'
import * as AppContext from '@framework/AppContext'
import { Finder } from '@framework/Finder'
import { Navigator } from '@framework/Navigator'
import * as OmniboxSpecialAction from '@framework/OmniboxSpecialAction'
import { QueryEntitiesRequest } from "@framework/FindOptions";
import { ajaxPost } from "@framework/Services";
import { Entity, Lite } from "@framework/Signum.Entities";
import { StyleContext } from '@framework/TypeContext'
import { FindOptions, FilterConditionOption, ColumnOption } from '@framework/FindOptions';
import { Type, QueryTokenString } from '@framework/Reflection';
import { SearchValueLine } from '@framework/Search';
import { expandNumbers } from '../Signum.DiffLog/Templates/DiffDocument';
import { EvalPanelPermission } from './Signum.Eval';
import { AuthClient } from '../Signum.Authorization/AuthClient'
import { ImportComponent } from '@framework/ImportComponent'
import { ChangeLogClient } from '@framework/Basics/ChangeLogClient'
import { LinkButton } from '@framework/Basics/LinkButton'

export namespace EvalClient {
  
  export function start(options: { routes: RouteObject[] }): void {
  
    ChangeLogClient.registerChangeLogModule("Signum.Eval", () => import("./Changelog"));
  
    OmniboxSpecialAction.registerSpecialAction({
      allowed: () => AppContext.isPermissionAuthorized(EvalPanelPermission.ViewDynamicPanel),
      key: "DynamicPanel",
      onClick: () => Promise.resolve("/dynamic/panel")
    });
  
    options.routes.push({ path: "/dynamic/panel", element: <ImportComponent onImport={() => import("./EvalPanelPage")} /> });
  }
  
  
  export type FormatColumnType = "Text" | "Code" | "JSon";
  
  export namespace Options {
  
    export let onGetDynamicPanelSearch: ((ctx: StyleContext, search: string) => React.ReactNode)[] = [];
  
    export function registerDynamicPanelSearch<T extends Entity>(type: Type<T>, getColumns: (token: QueryTokenString<T & { entity: T }>) => { token: QueryTokenString<any>, type: FormatColumnType }[]): void {
      onGetDynamicPanelSearch.push((ctx, search) => {
        var columns = getColumns(type.token());
  
        var findOptions = {
          queryName: type,
          filterOptions: [{
            groupOperation: "Or",
            value: search,
            pinned: { splitValue: true },
            filters: columns.map(t => ({ token: t.token, operation: "Contains" } as FilterConditionOption))
          }],
          columnOptionsMode: "Add",
          columnOptions: columns.filter(c => c.token.toString().startsWith("Entity.")).map(c => ({ token: c.token }) as ColumnOption)
        } as FindOptions;
  
  
        return (
          <SearchValueLine ctx={ctx} findOptions={findOptions} searchControlProps={{
            formatters: columns.toObjectDistinct(a => a.token.toString(), a => new Finder.CellFormatter((cell, cfc) => cell && <HighlightText search={search} text={cell} type={a.type} />, true))
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
        return <>{line}<br /></>;
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
          <LinkButton title={undefined} className="text-muted" onClick={e => { setShowAll(false) }}>Show less</LinkButton>
        </div>
      );
    }
    else {
      var changes = lines.map((str, i) => searchParts.some(p => str.search(p) != -1) ? i : null).notNull();
  
      if (changes.length == 0)
        changes = [0];
  
      var result = expandNumbers(changes, lines.length, 4);
  
      return (
        <div>
          {
            React.createElement("pre", undefined, ...result.map(n => typeof n == "number" ? makeLine(mark(lines[n], searchParts.length - 1)) :
              makeLine(` ----- ${n.numLines} Lines Removed ----- `)
            ))}
          <LinkButton title={undefined} className="text-muted" onClick={e => { setShowAll(true) }}>Show all</LinkButton>
        </div>
      );
    }
  }
  
  
  
  
  export interface CompilationError {
    fileName: string;
    line: number;
    column: number;
    errorNumber: string;
    errorText: string;
    fileContent: string;
  }
  
  export namespace API {
  
    export function getEvalErrors(request: QueryEntitiesRequest): Promise<EvalEntityError[]> {
      return ajaxPost({ url: `/api/eval/evalErrors` }, request);
    }
  
  }
  
  export interface EvalEntityError {
    lite: Lite<Entity>;
    error: string;
  }
}
