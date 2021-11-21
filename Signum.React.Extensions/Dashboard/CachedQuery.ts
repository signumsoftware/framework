import * as Finder from '@framework/Finder'
import { ColumnRequest, FilterOperation, FilterRequest, isFilterGroupRequest, OrderRequest, Pagination, QueryRequest, QueryToken, ResultRow, ResultTable } from '@framework/FindOptions'
import { Entity, is, Lite } from '@framework/Signum.Entities';
import { TypeAllowedAndConditions } from '../Authorization/Signum.Entities.Authorization';


export interface CachedQuery {
  creationDate: string;
  queryRequest: QueryRequest;
  resultTable: ResultTable;
}

export function getCachedResultTable(cachedQuery: CachedQuery, request: QueryRequest, parsedTokens: { [token: string]: QueryToken }): ResultTable | string /*Error*/ {

  if (request.queryKey != cachedQuery.queryRequest.queryKey)
    return "Invalid queryKey";

  var pagProblems = paginatinProblems(request.pagination, cachedQuery.queryRequest.pagination);
  if (typeof pagProblems == "string")
    return pagProblems;

  const onlyIfExactFiltersAndOrders = typeof pagProblems == "symbol";

  const sameOrders = ordersEquals(cachedQuery.queryRequest.orders, request.orders)
  if (!sameOrders && onlyIfExactFiltersAndOrders)
    return "Incompatible pagination if the orders are not identical";

  const extraFilters = extractRequestedFilters(cachedQuery.queryRequest.filters, request.filters);
  if (typeof extraFilters == "string")
    return extraFilters;

  if (extraFilters.length && onlyIfExactFiltersAndOrders)
    return "Incompatible pagination if the filters are not identical";

  if (request.groupResults) {

    if (onlyIfExactFiltersAndOrders) {

      if (!cachedQuery.queryRequest.groupResults)
        return "Incompatible pagination if the request is grouping but the cached query is not";
      else {

        const requestKeyColumns = request.columns.map(a => parsedTokens[a.token].queryTokenType != "Aggregate");

        const cachedKeyColumns = cachedQuery.queryRequest.columns.map(a => parsedTokens[a.token].queryTokenType != "Aggregate");

        var extraColumns = cachedKeyColumns.filter(c => !requestKeyColumns.contains(c));
        if (extraColumns.length && onlyIfExactFiltersAndOrders)
          return "Incompatible pagination if the key columns are not identical";
      }
    }

    const aggregateFilters = extraFilters.extract(f => !isFilterGroupRequest(f) && parsedTokens[f.token].queryTokenType == "Aggregate");

    const filtered = filterRows(cachedQuery.resultTable, extraFilters);
    if (typeof filtered == "string")
      return filtered;

    const allColumns = [...request.columns.map(a => a.token), ...aggregateFilters.map(a => a.token!), ...sameOrders ? [] : request.orders.map(a => a.token)].distinctBy(a => a);

    const grouped = groupByRows(filtered, true, allColumns, parsedTokens);
    if (typeof grouped == "string")
      return grouped;

    const reFiltered = filterRows(grouped, aggregateFilters);
    if (typeof reFiltered == "string")
      return reFiltered;

    const ordered = sameOrders ? reFiltered : orderRows(reFiltered, request.orders, parsedTokens);
    if (typeof ordered == "string")
      return ordered;

    const select = selectRows(ordered, request.columns);
    if (typeof select == "string")
      return select;

    const paginate = paginateRows(select, request.pagination);

    return paginate;

  } else {
    if (cachedQuery.queryRequest.groupResults)
      return "Cached query is grouping but request is not";
    else {
      const filtered = filterRows(cachedQuery.resultTable, extraFilters);
      if (typeof filtered == "string")
        return filtered;

      const ordered = sameOrders ? filtered : orderRows(filtered, request.orders, parsedTokens);
      if (typeof ordered == "string")
        return ordered;

      const select = selectRows(ordered, request.columns);
      if (typeof select == "string")
        return select;

      const paginate = paginateRows(select, request.pagination);
      return paginate;
    }
  }
}


function groupByRows(rt: ResultTable, alreadyGrouped: boolean, tokens: string[], parsedTokens: { [token: string]: QueryToken }): ResultTable | string {

  const groups = new Map<string, ResultRow[]>();
  const keyColumns = tokens.filter(a => parsedTokens[a].queryTokenType != "Aggregate");
  const rowKey = getRowKey(rt, keyColumns, parsedTokens);


  for (var i = 0; i < rt.rows.length; i++) {

    const row = rt.rows[i];
    const key = rowKey(row);
    let array = groups.get(key);
    if (!array) {
      array = [];
      groups.set(key, array);
    }
    array.push(row);
  }

  var result: ResultRow[];


  function getGetter(token: string): string | ((gr: ResultRow[]) => any)  {
    const qt = parsedTokens[token];
    if (qt.queryTokenType != "Aggregate") {
      const index = rt.columns.indexOf(token);
      return gr => gr[0].columns[index];
    }
    else {

      if (!alreadyGrouped) {
        if (qt.key == "Count")
          return gr => gr.length;

        const index = rt.columns.indexOf(qt.parent!.fullKey);
        if (index == -1)
          return qt.parent!.fullKey;

        switch (qt.key) {
          case "Min": return rows => rows.map(a => a.columns[index]).min();
          case "Max": return rows => rows.map(a => a.columns[index]).max();
          case "Sum": return rows => rows.map(a => a.columns[index]).sum();
          case "Avg": return rows => {
            var vals = rows.map(a => a.columns[index]).notNull();
            return vals.sum() / vals.length;
          };
        }
      } else {

        if (qt.key == "Count") {
          const indexCount = rt.columns.indexOf(qt.fullKey);
          if (indexCount == -1)
            return qt.fullKey;

          return rows => rows.sum(a => a.columns[indexCount]);

        } else if (qt.key == "Avg") {
          const sumToken = qt.parent!.fullKey + ".Sum";
          const indexSum = rt.columns.indexOf(sumToken);
          if (indexSum == -1)
            return sumToken;

          const countToken = qt.parent!.fullKey + ".CountNotNull";
          const indexCount = rt.columns.indexOf(countToken);
          if (indexCount == -1)
            return countToken;

          return rows => rows.sum(a => a.columns[indexSum]) / rows.sum(a => a.columns[indexCount]);
        } else {
          const index = rt.columns.indexOf(qt.parent!.fullKey);
          if (index == -1)
            return qt.parent?.fullKey!;

          switch (qt.key) {
            case "Min": return rows => rows.map(a => a.columns[index]).min();
            case "Max": return rows => rows.map(a => a.columns[index]).max();
            case "Sum": return rows => rows.map(a => a.columns[index]).sum();
          }
        }
      }
    }

    throw new Error("Unexpected " + token);
  }



  var getters: ((gr: ResultRow[]) => any)[] = [];
  for (let i = 0; i < tokens.length; i++) {
    var g = getGetter(tokens[i]);

    if (typeof g == "string") {
      return `Column ${g} not found` + (g != tokens[i]) ? `(required for ${tokens[i]})` : "";
    }

    getters.push(g);
  }

  const newRows: ResultRow[] = []; 
  groups.forEach(rows => {

    var columns: any[] = [];
    for (var i = 0; i < getters.length; i++) {
      columns.push(getters[i](rows))
    }

    newRows.push({
      entity: undefined,
      columns: columns
    });
  })

  return ({
    columns: tokens,
    pagination: { mode: "All" },
    rows: newRows,
    uniqueValues: rt.uniqueValues,
    totalElements: newRows.length
  });
}

function getRowKey(rt: ResultTable, keyTokens: string[], parsedTokens: { [token: string]: QueryToken }): (row: ResultRow) => string {

  const rr = "rr";

  function columnKey(token: string) {
    const index = rt.columns.indexOf(token);

    const qt = parsedTokens[token];

    if (qt.filterType == "Lite")
      return `(rt.columns[${index}] && (rt.columns[${index}].EntityType + ";" + rt.columns[${index}].id))`

    return `rt.columns[${index}]`;
  }

  const parts = keyTokens.map(a => columnKey(a)).join("|");

  return new Function(rr, "return " + parts + ";") as (row: ResultRow) => string;
}

function orderRows(rt: ResultTable, orders: OrderRequest[], parseTokens: { [token: string]: QueryToken }): ResultTable | string {

  var newRows = Array.from(rt.rows);


  for (var i = orders.length - 1; i >= 0; i--) {
    var o = orders[i];

    const pt = parseTokens[o.token];

    var index = rt.columns.indexOf(o.token);

    if (index == -1)
      return "Unable to order by token " + o.token;

    if (o.orderType == "Ascending") {
      if (pt.filterType == "Lite")
        newRows.sort((ra, rb) => { const a = ra.columns[index]; const b = rb.columns[index]; return a == b ? 0 : a == null ? -1 : b == null ? 1 : a.toStr > b.toStr ? 1 : -1 });
      else
        newRows.sort((ra, rb) => { const a = ra.columns[index]; const b = rb.columns[index]; return a == b ? 0 : a == null ? -1 : b == null ? 1 : a > b ? 1 : -1 });
    } else {
      if (pt.filterType == "Lite")
        newRows.sort((ra, rb) => { const a = ra.columns[index]; const b = rb.columns[index]; return a == b ? 0 : a == null ? 1 : b == null ? -1 : a.toStr > b.toStr ? -1 : 1 });
      else
        newRows.sort((ra, rb) => { const a = ra.columns[index]; const b = rb.columns[index]; return a == b ? 0 : a == null ? 1 : b == null ? -1 : a > b ? -1 : 1 });
    }
  }


  return ({
    columns: rt.columns,
    pagination: rt.pagination,
    rows: newRows,
    uniqueValues: rt.uniqueValues,
    totalElements: rt.totalElements
  });

}

function selectRows(rt: ResultTable, columns: ColumnRequest[]): ResultTable | string {

  const indexes: number[] = [];
  for (var i = 0; i < columns.length; i++) {
    var idx = rt.columns.indexOf(columns[i].token);
    if (idx == -1)
      return "Unable to select by token " + columns[i].token;

    indexes.push(idx);
  }

  var oldRows = rt.rows;
  var newRows: ResultRow[] = [];
  for (var i = 0; i < oldRows.length; i++) {
    const or = oldRows[i];
    const nr = { entity: or.entity, columns: [] } as ResultRow;
    for (var j = 0; j < indexes.length; j++) {
      nr.columns.push(or.columns[indexes[j]]);
    }
    newRows.push(nr);
  }

  return ({
    columns: columns.map(a => a.token),
    pagination: rt.pagination,
    rows: newRows,
    uniqueValues: rt.uniqueValues,
    totalElements: rt.totalElements
  });
}

function filterRows(rt: ResultTable, filters: FilterRequest[]): ResultTable | string{

  if (filters.length == 0)
    return rt;

  if (rt.pagination.mode != "All")
    return "Unable to filter " + rt.pagination.mode;

  var filterer = createFilterer(rt, filters);

  if (typeof filterer == "string")
    return filterer;

  var newRows = filterer(rt.rows);

  return {
    columns: rt.columns,
    rows: newRows,
    uniqueValues: rt.uniqueValues,
    pagination: { mode: "All" },
    totalElements: newRows.length
  };
}

function createFilterer(result: ResultTable, filters: FilterRequest[]): ((rows: ResultRow[]) => ResultRow[]) | string{

  const cls = "cls";

  var allValues: unknown[] = [];

  function getVarName(v: unknown) {
    allValues.push(v);
    return "v" + (allValues.length - 1);
  }

  function getExpression(f: FilterRequest): string | string[] /*errors*/{
    if (isFilterGroupRequest(f)) {

      const parts = f.filters.map(ff => getExpression(ff));

      var errors = parts.filter(a => Array.isArray(a)).flatMap(a => a as string[]);
      if (errors.length > 0)
        return errors;

      if (f.groupOperation == "Or")
        return "( " + parts.join(" || ") + ")";
      return parts.join(" && ");

    } else {

      var index = result.columns.indexOf(f.token);

      if (index == -1)
        return [f.token];

      var op = "cls[" + index + "]"; 

      if (f.operation == "IsIn" || f.operation == "IsNotIn") {
        var values = f.value as unknown[];

        var exps = allValues.map(v => op + "===" + getVarName(v)).join(" || ");

        return f.operation == "IsIn" ? exps : ("!(" + exps + ")");
      }
      else {

        var vn = getVarName(f.value);
        switch (f.operation) {
          case "EqualTo": return `${op} === ${vn}`;
          case "DistinctTo": return `${op} !== ${vn}`;
          case "GreaterThan": return `${op} > ${vn}`;
          case "GreaterThanOrEqual": return `${op} >= ${vn}`;
          case "LessThan": return `${op} < ${vn}`;
          case "LessThanOrEqual": return `${op} <= ${vn}`;
          case "Contains": return `${op} != null && ${op}.includes(${vn})`;
          case "NotContains": return `!(${op} != null && ${op}.includes(${vn}))`;
          case "EndsWith": return `${op} !== null && ${op}.endsWith(${vn})`;
          case "NotEndsWith": return `!(${op} !== null && ${op}.endsWith(${vn}))`;
          case "StartsWith": return `${op} !== null && ${op}.startsWith(${vn})`;
          case "NotStartsWith": return `!(${op} !== null && ${op}.startsWith(${vn}))`;

          case "Like": return ["Like not supported"];
          case "NotLike": return ["NotLike not supported"];

          default: throw new Error("Unexpected " + f.operation);
        }
      }
    }

  }



  var expression = filters.map(f => getExpression(f)).join(" &&\n");

  var factory = new Function(...allValues.map((v, i) => "v" + i), `return rows => {
  const result = [];
  for(let i = 0; i < rows.length; i++) {
    var cls = rows[i].columns;
    if (expression) {
      result.push(rows[i]);
    }
  }
  return result;
};`);

  return factory(...allValues);
}



function splitAggregate(token: string) {
  const suffix = token.tryAfterLast(".") ?? token;

  const prefix = token.tryBeforeLast(".");

  if (
    suffix == "Count" ||
    suffix == "Average" ||
    suffix == "Sum" ||
    suffix == "Min" ||
    suffix == "Max") {
    return ({ aggregate: suffix, token: prefix });
  }


  return ({ aggregate: null, token: token });
}




function ordersEquals(cached: OrderRequest[], requested: OrderRequest[]) {
  if (cached.length != requested.length)
    return false;

  for (var i = 0; i < cached.length; i++) {
    if (cached[i].token != requested[i].token)
      return false;

    if (cached[i].orderType != requested[i].orderType)
      return false;
  }

  return true;
}

function extractRequestedFilters(cached: FilterRequest[], request: FilterRequest[]): FilterRequest[] | string {

  var cloned = JSON.parse(JSON.stringify(request)) as FilterRequest[];

  for (var i = 0; i < cached.length; i++) {

    const c = cached[i];

    const toRemove = cloned.filter(rf => equalFilter(c, rf));

    if (toRemove.length)
      return "Cached filter not found in requet";

    toRemove.forEach(r => cloned.remove(r));
  }

  return cloned;
}

function equalFilter(c: FilterRequest, r: FilterRequest): boolean {
  if (isFilterGroupRequest(c)) {
    if (!isFilterGroupRequest(r))
      return false;

    if (c.groupOperation != r.groupOperation)
      return false;

    if (c.token != r.token)
      return false;

    if (c.filters.length != r.filters.length)
      return false;

    return c.filters.every((cf, i) => equalFilter(cf, r.filters[i]));
  } else {
    if (isFilterGroupRequest(r))
      return false;

    if (c.token != r.token)
      return false;

    if (c.operation != r.operation)
      return false;

    if (!is(c.value, r.value, false, false) && c.value != r.value)
      return false;

    return true;
  }
}

function paginateRows(rt: ResultTable, req: Pagination): ResultTable | string {
  switch (rt.pagination.mode) {
    case "All":
      {
        switch (req.mode) {
          case "All": return rt;
          case "Firsts": return { ...rt, rows: rt.rows.slice(0, rt.pagination.elementsPerPage), pagination: req };
          case "Paginate":
            var startIndex = rt.pagination.elementsPerPage! * (rt.pagination.currentPage! - 1);
            return { ...rt, rows: rt.rows.slice(startIndex, startIndex + rt.pagination.elementsPerPage!), pagination: req };
        }
      }
    case "Paginate": {
      switch (req.mode) {
        case "All": throw new Error(`Requesting ${req.mode} but cached is ${rt.pagination.mode}`)
        case "Firsts":
          if (rt.pagination.currentPage == 1 && req.elementsPerPage! <= rt.pagination.elementsPerPage!)
            return { ...rt, rows: rt.rows.slice(0, rt.pagination.elementsPerPage), pagination: req };

          return `Invalid first`;

        case "Paginate":
          if (((req.elementsPerPage! == rt.pagination.elementsPerPage! && req.currentPage == rt.pagination.currentPage) ||
            (req.elementsPerPage! <= rt.pagination.elementsPerPage! && req.currentPage == 1 && rt.pagination.currentPage == 1))) {

            var startIndex = rt.pagination.elementsPerPage! * (rt.pagination.currentPage! - 1);
            return { ...rt, rows: rt.rows.slice(startIndex, startIndex + rt.pagination.elementsPerPage!), pagination: req };
          }

          return "Invalid paginate";
      }
    }
    case "Firsts": {
      switch (req.mode) {
        case "Firsts":
          if (req.elementsPerPage! <= rt.pagination.elementsPerPage!)
            return { ...rt, rows: rt.rows.slice(0, rt.pagination.elementsPerPage), pagination: req };

          throw new Error(`Invalid first`);
        case "Paginate":
        case "All": return `Requesting ${req.mode} but cached is ${rt.pagination.mode}`;
      }
    }
  }
}

function paginatinProblems(req: Pagination, cached: Pagination): symbol| string |  null {

  switch (cached.mode) {
    case "All": return null;
    case "Paginate": {

      switch (req.mode) {

        case "Firsts": {
          if (cached.currentPage == 1 && req.elementsPerPage! <= cached.elementsPerPage!)
            return Symbol("OnlyIfExactFiltersAndOrders");

          return "Invalid First";
        }

        case "Paginate": {
          if (((req.elementsPerPage! == cached.elementsPerPage! && req.currentPage == cached.currentPage) ||
              (req.elementsPerPage! <= cached.elementsPerPage! && req.currentPage == 1 && cached.currentPage == 1)))
            return Symbol("OnlyIfExactFiltersAndOrders");

          return "Invalid Paginate";
        }

        case "All": return `Requesting ${req.mode} but cached is ${cached.mode}`;
      }
    }

    case "Firsts":
      {
        switch (req.mode) {
          case "Firsts": {
            if (req.elementsPerPage! <= cached.elementsPerPage!)
              return Symbol("OnlyIfExactFiltersAndOrders");

            return "Invalid First";
          }
          case "Paginate":
          case "All": return `Requesting ${req.mode} but cached is ${cached.mode}`;
        }
      }
  }

  if (cached.mode == "All")
    return null;

  if (cached.mode == "Paginate")

  if (cached.mode == "Firsts") {
    if (req.mode == "Firsts") {
     
    }
  }


  throw new Error("Unexpected value");
}
