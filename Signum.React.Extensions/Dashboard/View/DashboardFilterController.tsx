import { DashboardEntity, InteractionGroup, PanelPartEmbedded, UserChartPartEntity, UserQueryPartEntity } from '../Signum.Entities.Dashboard';
import { FilterConditionOptionParsed, FilterGroupOptionParsed, FilterOption, FilterOptionParsed, FindOptions, isActive, isFilterGroupOptionParsed, QueryToken, tokenStartsWith } from '@framework/FindOptions';
import { FilterGroupOperation } from '@framework/Signum.Entities.DynamicQuery';
import { ChartRequestModel, UserChartEntity } from '../../Chart/Signum.Entities.Chart';
import { ChartRow } from '../../Chart/ChartClient';
import { Entity, is, Lite } from '@framework/Signum.Entities';
import * as Finder from '../../../Signum.React/Scripts/Finder';
import { getQueryKey } from '@framework/Reflection';
import { Dic, softCast } from '../../../Signum.React/Scripts/Globals';
import { UserQueryEntity } from '../../UserQueries/Signum.Entities.UserQueries';


export class DashboardController {

  forceUpdate: () => void;

  filters: Map<PanelPartEmbedded, DashboardFilter> = new Map();
  pinnedFilters: Map<PanelPartEmbedded, DashboardPinnedFilters> = new Map();
  lastChange: Map<string /*queryKey*/, number> = new Map();
  dashboard: DashboardEntity;
  queriesWithEquivalences: string/*queryKey*/[];

  invalidationMap: Map<PanelPartEmbedded, () => void> = new Map();

  isLoading: boolean;

  constructor(forceUpdate: () => void, dashboard: DashboardEntity) {
    this.forceUpdate = forceUpdate;
    this.dashboard = dashboard;

    this.queriesWithEquivalences = dashboard.tokenEquivalencesGroups.flatMap(a => a.element.tokenEquivalences.map(a => a.element.query.key)).distinctBy(a => a);

    this.isLoading = true;
  }

  setIsLoading() {
    this.isLoading = !this.dashboard.parts
      .filter(p => UserQueryPartEntity.isInstance(p.element.content) || UserChartPartEntity.isInstance(p.element.content))
      .every(p => this.invalidationMap.has(p.element));
  }

  registerInvalidations(embedded: PanelPartEmbedded, invalidation: () => void) {
    this.invalidationMap.set(embedded, invalidation);
  }

  invalidate(source: PanelPartEmbedded, interactionGroup: InteractionGroup | null | undefined) {
    Array.from(this.invalidationMap.keys())
      .filter(p => p != source && (interactionGroup == null || p.interactionGroup == interactionGroup))
      .forEach(p => this.invalidationMap.get(p)!());

  }

  setFilter(filter: DashboardFilter) {
    this.lastChange.set(filter.queryKey, new Date().getTime());
    this.filters.set(filter.partEmbedded, filter);
    this.forceUpdate();
  }

  clearFilters(partEmbedded: PanelPartEmbedded) {
    var current = this.filters.get(partEmbedded);
    if (current)
      this.lastChange.set(current.queryKey, new Date().getTime());
    this.filters.delete(partEmbedded);
    this.forceUpdate();
  }

  setPinnedFilter(filter: DashboardPinnedFilters) {
    this.lastChange.set(filter.queryKey, new Date().getTime());
    this.pinnedFilters.set(filter.partEmbedded, filter);
    this.forceUpdate();
  }       

  clearPinnesFilter(partEmbedded: PanelPartEmbedded) {
    var current = this.pinnedFilters.get(partEmbedded);
    if (current)
      this.lastChange.set(current.queryKey, new Date().getTime());

    this.pinnedFilters.delete(partEmbedded);
    this.forceUpdate();
  }

  getLastChange(queryKey: string) {
    if (this.queriesWithEquivalences.contains(queryKey))
      return this.queriesWithEquivalences.max(qk => this.lastChange.get(qk));

    return this.lastChange.get(queryKey);
  }

  getFilterOptions(partEmbedded: PanelPartEmbedded, queryKey: string): FilterOptionParsed[] {

    var otherFilters = partEmbedded.interactionGroup == null ? [] : Array.from(this.filters.values()).filter(f => f.partEmbedded != partEmbedded && f.partEmbedded.interactionGroup == partEmbedded.interactionGroup && f.rows?.length);
    var pinnedFilters = Array.from(this.pinnedFilters.values()).filter(a => a.pinnedFilters.length > 0);
    if (otherFilters.length == 0 && pinnedFilters.length == 0)
      return [];

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

    var resultFilters = otherFilters.map(
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

    var resultPinnedFilters = pinnedFilters.flatMap(a => {
      if (a.queryKey == queryKey)
        return a.pinnedFilters;

      var tokenEquivalences = equivalences[a.queryKey]?.groupToObject(a => a.fromToken!.fullKey);

      return a.pinnedFilters.map(fop => tokenEquivalences && translateFilterToken(fop, tokenEquivalences)).notNull();
    })

    return [...resultPinnedFilters, ...resultFilters];
  }

 

  applyToFindOptions(partEmbedded: PanelPartEmbedded, fo: FindOptions): FindOptions {

    var dashboardFilters = this.getFilterOptions(partEmbedded, getQueryKey(fo.queryName));
    if (dashboardFilters.length == 0)
      return fo;

    var dashboardFOs = Finder.toFilterOptions(dashboardFilters);

    function allTokens(fs: FilterOptionParsed[]): QueryToken[] {
      return fs.flatMap(f => isFilterGroupOptionParsed(f) ? [f.token, ...allTokens(f.filters)].notNull() : [f.token].notNull())
    }

    const simpleFilters = fo.filterOptions?.filter(a => a && a.dashboardBehaviour == null) ?? [];
    const useWhenNoFilters = fo.filterOptions?.filter(a => a && a.dashboardBehaviour == "UseWhenNoFilters") as FilterOption[] ?? [];


    var tokens = allTokens(dashboardFilters.filter(df => isActive(df)));

    return {
      ...fo,
      filterOptions: [
        ...simpleFilters!,
        ...useWhenNoFilters!.filter(a => !tokens.some(t => tokenStartsWith(a.token!, t))),
        ...dashboardFOs,
      ]
    };
  }
}

function translateFilterToken(fop: FilterOptionParsed, tokenEquivalences: { [token: string]: TokenEquivalenceTuple[] }): FilterOptionParsed | null {
  var newToken: QueryToken | null | undefined = fop.token;
  if (newToken != null) {
    newToken = translateToken(newToken, tokenEquivalences);
    if (newToken == null)
      return null;
  }

  if (isFilterGroupOptionParsed(fop)) {
    return ({ ...fop, token: newToken, filters: fop.filters.map(f => translateFilterToken(f, tokenEquivalences)).notNull() });
  }
  else
    return ({ ...fop, token: newToken });
}

function translateToken(token: QueryToken, tokenEquivalences: { [token: string]: TokenEquivalenceTuple[] }) {

  var toAdd: QueryToken[] = [];

  if (tokenEquivalences == null)
    return null;

  for (var t = token; t != null; t = t.parent!) {

    var equivalence = tokenEquivalences[t.fullKey];

    if (equivalence != null) {
      return toAdd.reduce((t, nt) => ({ ...nt, parent: t, fullKey: t.fullKey + "." + nt.key }) as QueryToken, equivalence.first().toToken)
    }

    toAdd.insertAt(0, t);

    if (t.parent == null) {//Is a Column, like 'Supplier', but maybe 'Entity' is mapped to we can interpret it as 'Entity.Supplier'  
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

export class DashboardPinnedFilters {
  partEmbedded: PanelPartEmbedded; 
  queryKey: string;
  pinnedFilters: FilterOptionParsed[];

  constructor(partEmbedded: PanelPartEmbedded, queryKey: string, pinnedFilters: FilterOptionParsed[]) {
    this.partEmbedded = partEmbedded;
    this.queryKey = queryKey;
    this.pinnedFilters = pinnedFilters;
  }
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
