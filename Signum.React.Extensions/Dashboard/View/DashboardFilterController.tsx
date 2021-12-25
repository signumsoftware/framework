import { DashboardEntity, PanelPartEmbedded } from '../Signum.Entities.Dashboard';
import { FilterConditionOptionParsed, FilterGroupOptionParsed, FilterOptionParsed, FindOptions, QueryToken } from '@framework/FindOptions';
import { FilterGroupOperation } from '@framework/Signum.Entities.DynamicQuery';
import { ChartRequestModel } from '../../Chart/Signum.Entities.Chart';
import { ChartRow } from '../../Chart/ChartClient';
import { Entity, is, Lite } from '@framework/Signum.Entities';
import * as Finder from '../../../Signum.React/Scripts/Finder';
import { getQueryKey } from '@framework/Reflection';
import { Dic, softCast } from '../../../Signum.React/Scripts/Globals';


export class DashboardFilterController {

  forceUpdate: () => void;

  filters: Map<PanelPartEmbedded, DashboardFilter> = new Map();
  lastChange: Map<string /*queryKey*/, number> = new Map();
  dashboard: DashboardEntity;

  constructor(forceUpdate: () => void, dashboard: DashboardEntity) {
    this.forceUpdate = forceUpdate;
    this.dashboard = dashboard;
  }

  setFilter(filter: DashboardFilter) {
    this.lastChange.set(filter.queryKey, new Date().getTime());
    this.filters.set(filter.partEmbedded, filter);
    this.forceUpdate();
  }

  clear(partEmbedded: PanelPartEmbedded) {
    var current = this.filters.get(partEmbedded);
    if (current) {
      this.lastChange.set(current.queryKey, new Date().getTime());
    }
    this.filters.delete(partEmbedded);
    this.forceUpdate();
  }

  getFilterOptions(partEmbedded: PanelPartEmbedded, queryKey: string): FilterOptionParsed[] {

    if (partEmbedded.interactionGroup == null)
      return [];

    var otherFilters = Array.from(this.filters.values()).filter(f => f.partEmbedded != partEmbedded && f.partEmbedded.interactionGroup == partEmbedded.interactionGroup && f.rows?.length);

    debugger;

    var equivalences = this.dashboard.tokenEquivalencesGroups
      .filter(a => a.element.interactionGroup == partEmbedded.interactionGroup || a.element.interactionGroup == null)
      .flatMap(gr => {
        var target = gr.element.tokenEquivalences.filter(a => a.element.query.key == queryKey)

        return gr.element.tokenEquivalences.flatMap(f => target.filter(t => t != f).map(t => softCast<TokenEquivalenceTuple>({
          fromQueryKey: f.element.query.key,
          fromToken: f.element.token.token!,
          toQuery: t.element.query.key,
          toToken: t.element.token.token!
        })));
      }).groupToObject(a => a.fromQueryKey)

    var result = otherFilters.map(
      df => {

        
        var tokenEquivalences = equivalences[df.queryKey]?.groupToObject(a => a.fromToken!.fullKey);

        if (df.queryKey != queryKey && tokenEquivalences == undefined)
          return null;

        return groupFilter("Or", df.rows.map(
          r => groupFilter("And", r.filters.map(
            f => {
              var token = df.queryKey == queryKey ? f.token : translateToken(f.token, tokenEquivalences);
              if (token == null)
                return undefined;

              return ({ token: token, operation: "EqualTo", value: f.value, frozen: false }) as FilterConditionOptionParsed;
            }
          ).notNull())
        ).notNull())
      }).notNull();

    return result;
  }

 

  applyToFindOptions(partEmbedded: PanelPartEmbedded, fo: FindOptions): FindOptions {

    var fops = this.getFilterOptions(partEmbedded, getQueryKey(fo.queryName));
    if (fops.length == 0)
      return fo;

    var newFilters = Finder.toFilterOptions(fops);
    return {
      ...fo,
      filterOptions: [
        ...fo.filterOptions ?? [],
        ...newFilters
      ]
    };
  }
}

function translateToken(token: QueryToken, tokenEquivalences: { [token: string]: TokenEquivalenceTuple[] }) {

  var toAdd: QueryToken[] = [];

  for (var t = token; t != null; t = t.parent!) {

    var equivalence = tokenEquivalences[t.fullKey];

    if (equivalence != null) {
      return toAdd.reduce((t, nt) => ({ ...nt, parent: t, fullKey: t.fullKey + "." + nt.key }) as QueryToken, equivalence.first().toToken)
    }

    toAdd.insertAt(0, t);

    if (t.parent == null) {//Is a Column, like 'Supplier', but maybe 'Entity' is mapped to we can interpret it as 'Entity.Supplier'  
      debugger;
      equivalence = tokenEquivalences["Entity"];

      if (equivalence != null) {
        return toAdd.reduce((t, nt) => ({ ...nt, parent: t, fullKey: t.fullKey + "." + nt.key }) as QueryToken, equivalence.first().toToken)
      }
    }
  }

  return null;
}


export function groupFilter(groupOperation: FilterGroupOperation, filters: FilterOptionParsed[]): FilterOptionParsed | undefined {

  if (filters.length == 0)
    return undefined;

  if (filters.length == 1)
    return filters[0];

  return ({
    groupOperation: groupOperation,
    filters: filters
  }) as FilterGroupOptionParsed;
}

interface TokenEquivalenceTuple {
  fromQueryKey: string;
  fromToken: QueryToken;
  toQuery: string;
  toToken: QueryToken;
}

export class DashboardFilter {
  partEmbedded: PanelPartEmbedded;
  queryKey: string;
  rows: DashboardFilterRow[] = [];

  constructor(partEmbedded: PanelPartEmbedded, queryKey: string) {
    this.partEmbedded = partEmbedded;
    this.queryKey = queryKey;
  }


  getActiveDetector(request: ChartRequestModel): ((row: ChartRow) => boolean) | undefined {

    if (this.rows.length == 0)
      return undefined;

    var tokenToColumn = request.columns
      .map((mle, i) => ({ colName: "c" + i, tokenString: mle.element.token?.tokenString }))
      .filter(a => a.tokenString != null)
      .groupBy(a => a.tokenString)
      .toObject(gr => gr.key!, gr => gr.elements.first().colName);

    return row => this.rows.some(r => {
      return r.filters.every(f => {
        var rowVal = (row as any)[tokenToColumn[f.token.fullKey]];
        return f.value == rowVal || is(f.value, rowVal, false, false);
      });
    });
  }
}

export interface DashboardFilterRow {
  filters: { token: QueryToken, value: unknown }[];
}

export function equalsDFR(row1: DashboardFilterRow, row2: DashboardFilterRow): boolean {
  if (row1.filters.length != row2.filters.length)
    return false;

  for (var i = 0; i < row1.filters.length; i++) {
    var f1 = row1.filters[i];
    var f2 = row2.filters[i];

    if (!(f1.token.fullKey == f2.token.fullKey &&
      (f1.value === f2.value || is(f1.value as Lite<Entity>, f2.value as Lite<Entity>, false, false))))
      return false;
  }

  return true;
}
