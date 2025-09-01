import { Finder } from '@framework/Finder'
import { ColumnRequest, FilterOperation, FilterOptionParsed, FilterRequest, FindOptionsParsed, OrderRequest, Pagination, QueryRequest, QueryToken, QueryValueRequest, ResultRow, ResultTable, isFilterGroup } from '@framework/FindOptions'
import { Entity, getToString, is, Lite } from '@framework/Signum.Entities';


export interface CachedQueryJS {
  creationDate: string;
  queryRequest: QueryRequest;
  resultTable: ResultTable;
}



export function executeQueryCached(request: QueryRequest, fop: FindOptionsParsed, cachedQuery: CachedQueryJS): ResultTable {

  const tokens = [
    ...fop.columnOptions.map(a => a.token).notNull(),
    ...fop.columnOptions.map(a => a.summaryToken).notNull(),
    ...fop.orderOptions.map(a => a.token),
    ...getAllFilterTokens(fop.filterOptions),
  ].notNull().toObjectDistinct(a => a.fullKey);

  const resultTable = getCachedResultTable(cachedQuery, request, tokens);

  return resultTable;
}

export function executeQueryValueCached(request: QueryValueRequest, fop: FindOptionsParsed, token: QueryToken | null, cachedQuery: CachedQueryJS): unknown {

  if (token == null)
    token = {
      fullKey: "Count",
      type: { name: "int" },
      queryTokenType: "Aggregate",
      niceName: "Count",
      key: "Count",
      toStr: "Count",
      niceTypeName: "Number",
      isGroupable: false,
      filterType: "Integer",
    };

  var queryRequest: QueryRequest = {
    queryKey: request.queryKey,
    columns: [{ token: token.fullKey, displayName: token.niceName }],
    filters: request.filters,
    groupResults: token.queryTokenType == "Aggregate",
    orders: [],
    pagination: request.multipleValues ? { mode: "All" } : { mode: "Firsts", elementsPerPage: 2 },
    systemTime: undefined,
  };

  const tokens = [
    token,
    ...getAllFilterTokens(fop.filterOptions),
  ].notNull().toObjectDistinct(a => a.fullKey);

  const resultTable = getCachedResultTable(cachedQuery, queryRequest, tokens);

  if (request.multipleValues)
    return resultTable.rows.map(r => r.columns[0]);    

  return resultTable.rows.map(r => r.columns[0]).singleOrNull();
}


export function getAllFilterTokens(fos: FilterOptionParsed[]): QueryToken[]{
  return fos.flatMap(f => isFilterGroup(f) ?
    [f.token, ...getAllFilterTokens(f.filters)] :
    [f.token])
    .notNull();
}


class CachedQueryError {
  message: string;
  constructor(error: string) {
    this.message = error;
  }

  toString() {
    return this.message;
  }
}

export function getCachedResultTable(cachedQuery: CachedQueryJS, request: QueryRequest, parsedTokens: { [token: string]: QueryToken }): ResultTable {

  if (request.queryKey != cachedQuery.queryRequest.queryKey)
    throw new CachedQueryError("Invalid queryKey");

  var pagProblems = pagionationRestriction(request.pagination, cachedQuery.queryRequest.pagination);

  const exactFiltersAndOrders = pagProblems == "ExactFiltersAndOrders";

  const sameOrders = ordersEquals(cachedQuery.queryRequest.orders, request.orders)
  if (!sameOrders && exactFiltersAndOrders)
    throw new CachedQueryError("Incompatible pagination if the orders are not identical");

  const extraFilters = extractRequestedFilters(cachedQuery.queryRequest.filters, request.filters);
  if (extraFilters.length && exactFiltersAndOrders)
    throw new CachedQueryError("Incompatible pagination if the filters are not identical");

  if (request.groupResults) {

    if (exactFiltersAndOrders) {

      if (!cachedQuery.queryRequest.groupResults)
        throw new CachedQueryError("Incompatible pagination if the request is grouping but the cached query is not");
      else {

        const requestKeyColumns = request.columns.map(a => parsedTokens[a.token].queryTokenType != "Aggregate");

        const cachedKeyColumns = cachedQuery.queryRequest.columns.map(a => parsedTokens[a.token].queryTokenType != "Aggregate");

        var extraColumns = cachedKeyColumns.filter(c => !requestKeyColumns.contains(c));
        if (extraColumns.length && exactFiltersAndOrders)
          throw new CachedQueryError("Incompatible pagination if the key columns are not identical");
      }
    }

    const aggregateFilters = extraFilters.extract(f => !isFilterGroup(f) && parsedTokens[f.token].queryTokenType == "Aggregate");

    const filtered = filterRows(cachedQuery.resultTable, extraFilters);
    
    const allColumns = [...request.columns.map(a => a.token), ...aggregateFilters.map(a => a.token!), ...sameOrders ? [] : request.orders.map(a => a.token)].distinctBy(a => a);

    const grouped = groupByRows(filtered, true, allColumns, parsedTokens);
  
    const reFiltered = filterRows(grouped, aggregateFilters);
    
    const ordered = sameOrders ? reFiltered : orderRows(reFiltered, request.orders, parsedTokens);
    
    const select = selectRows(ordered, request.columns);
    
    const paginate = paginateRows(select, request.pagination);

    return paginate;

  } else {
    if (cachedQuery.queryRequest.groupResults)
      throw new CachedQueryError("Cached query is grouping but request is not");
    else {
      const filtered = filterRows(cachedQuery.resultTable, extraFilters);
      
      const ordered = sameOrders ? filtered : orderRows(filtered, request.orders, parsedTokens);
      
      const select = selectRows(ordered, request.columns);
      
      const paginate = paginateRows(select, request.pagination);
      return paginate;
    }
  }
}


function groupByRows(rt: ResultTable, alreadyGrouped: boolean, tokens: string[], parsedTokens: { [token: string]: QueryToken }): ResultTable {

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


  function getGetter(token: string): ((gr: ResultRow[]) => any) {

    function getColumnIndex(t: string) {

      var idx = rt.columns.indexOf(t);
      if (idx == -1)
        throw new CachedQueryError(`Column ${t} not found` + (t != token ? ` (required for ${token})` : ""));

      return idx;
    }

    function tryColumnIndex(t: string) {

      var idx = rt.columns.indexOf(t);
      if (idx == -1)
        return null; 

      return idx;
    }

    const qt = parsedTokens[token];
    if (qt.queryTokenType != "Aggregate") {
      const index = rt.columns.indexOf(token);
      return gr => gr[0].columns[index];
    }
    else {

      if (!alreadyGrouped) {
        if (qt.key == "Count")
          return gr => gr.length;

        const index = getColumnIndex(qt.parent!.fullKey);

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
          const indexCount = getColumnIndex(qt.fullKey);

          return rows => rows.sum(a => a.columns[indexCount]);

        } else if (qt.key == "Average") {
          var avg = tryColumnIndex(qt.fullKey);
          if (avg != null) { //No interaction group
            return rows => rows.single().columns[avg!];
          }

          const sumToken = qt.parent!.fullKey + ".Sum";
          const indexSum = getColumnIndex(sumToken);

          const countToken = qt.parent!.fullKey + ".CountNotNull";
          const indexCount2 = getColumnIndex(countToken);
          return rows => rows.sum(a => a.columns[indexSum]) / rows.sum(a => a.columns[indexCount2]);
        } else {
          const index = tryColumnIndex(qt.fullKey) ?? getColumnIndex(qt.parent!.fullKey);

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

  var getters = tokens.map(t => {
    var g = getGetter(t);

    
    return g;
  });

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

    if (index == -1)
      throw new CachedQueryError("Token " + token + " not found for grouping");

    const qt = parsedTokens[token];

    if (qt.filterType == "Lite")
      return `(rr.columns[${index}] && (rr.columns[${index}].EntityType + ";" + rr.columns[${index}].id))`

    return `rr.columns[${index}]`;
  }


  const parts = keyTokens.map(token => columnKey(token)).join("+ \"|\" + ");

  return new Function(rr, "return " + parts + ";") as (row: ResultRow) => string;
}

function orderRows(rt: ResultTable, orders: OrderRequest[], parseTokens: { [token: string]: QueryToken }): ResultTable {

  var newRows = Array.from(rt.rows);


  for (var i = orders.length - 1; i >= 0; i--) {
    var o = orders[i];

    const pt = parseTokens[o.token];

    var index = rt.columns.indexOf(o.token);

    if (index == -1)
      throw new CachedQueryError("Unable to order by token " + o.token);

    if (o.orderType == "Ascending") {
      if (pt.filterType == "Lite")
        newRows.sort((ra, rb) => { const a = ra.columns[index]; const b = rb.columns[index]; return a == b ? 0 : a == null ? -1 : b == null ? 1 : getToString(a) > getToString(b) ? 1 : -1 });
      else
        newRows.sort((ra, rb) => { const a = ra.columns[index]; const b = rb.columns[index]; return a == b ? 0 : a == null ? -1 : b == null ? 1 : a > b ? 1 : -1 });
    } else {
      if (pt.filterType == "Lite")
        newRows.sort((ra, rb) => { const a = ra.columns[index]; const b = rb.columns[index]; return a == b ? 0 : a == null ? 1 : b == null ? -1 : getToString(a) > getToString(b) ? -1 : 1 });
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

function selectRows(rt: ResultTable, columns: ColumnRequest[]): ResultTable {

  const indexes: number[] = [];
  for (var i = 0; i < columns.length; i++) {
    var idx = rt.columns.indexOf(columns[i].token);
    if (idx == -1)
      throw new CachedQueryError("Unable to select by token " + columns[i].token);

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

function filterRows(rt: ResultTable, filters: FilterRequest[]): ResultTable{

  if (filters.length == 0)
    return rt;

  if (rt.pagination.mode != "All")
    throw new CachedQueryError("Unable to filter " + rt.pagination.mode);

  var filterer = createFilterer(rt, filters);

  var newRows = filterer(rt.rows);

  return {
    columns: rt.columns,
    rows: newRows,
    uniqueValues: rt.uniqueValues,
    pagination: { mode: "All" },
    totalElements: newRows.length
  };
}

function createFilterer(result: ResultTable, filters: FilterRequest[]): ((rows: ResultRow[]) => ResultRow[]){

  const cls = "cls";

  var allValues: unknown[] = [];

  function getUniqueValue(v: unknown, token: string) {
    var uvs = result.uniqueValues[token];

    if (uvs) {
      for (var i = 0; i < uvs.length; i++) {
        if (uvs[i] == v || is(uvs[i], v as Lite<Entity>, false, false))
          return uvs[i];
      }
    }

    return v;
  }

  function getVarName(v: unknown) {
    allValues.push(v);
    return "v" + (allValues.length - 1);
  }

  function getExpression(f: FilterRequest): string {
    if (isFilterGroup(f)) {

      const parts = f.filters.map(ff => getExpression(ff));

      if (f.groupOperation == "Or")
        return "( " + parts.join(" || ") + ")";
      return parts.join(" && ");

    } else {

      var index = result.columns.indexOf(f.token);

      if (index == -1)
        throw new CachedQueryError("Unable to filter " + f.token + ", column not found");

      var op = "cls[" + index + "]"; 

      if (f.operation == "IsIn" || f.operation == "IsNotIn") {
        var values = f.value as unknown[];

        var exps = allValues.map(v => op + "===" + getVarName(getUniqueValue(v, f.token))).join(" || ");

        return f.operation == "IsIn" ? exps : ("!(" + exps + ")");
      }
      else {

        var vn = getVarName(getUniqueValue(f.value, f.token));
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

          case "Like": throw new CachedQueryError("Like not supported");
          case "NotLike": throw new CachedQueryError("NotLike not supported");

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
    if (${expression}) {
      result.push(rows[i]);
    }
  }
  return result;
};`);

  return factory(...allValues);
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

function extractRequestedFilters(cached: FilterRequest[], request: FilterRequest[]): FilterRequest[] {

  var cloned = JSON.parse(JSON.stringify(request)) as FilterRequest[];

  for (var i = 0; i < cached.length; i++) {

    const c = cached[i];

    const removed = cloned.extract(rf => equalFilter(c, rf));

    if (removed.length == 0)
      throw new CachedQueryError("Cached filter not found in request");
  }

  return cloned;
}

function equalFilter(c: FilterRequest, r: FilterRequest): boolean {
  if (isFilterGroup(c)) {
    if (!isFilterGroup(r))
      return false;

    if (c.groupOperation != r.groupOperation)
      return false;

    if (c.token != r.token)
      return false;

    if (c.filters.length != r.filters.length)
      return false;

    return c.filters.every((cf, i) => equalFilter(cf, r.filters[i]));
  } else {
    if (isFilterGroup(r))
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

function paginateRows(rt: ResultTable, reqPag: Pagination): ResultTable{
  switch (rt.pagination.mode) {
    case "All":
      {
        switch (reqPag.mode) {
          case "All": return rt;
          case "Firsts": return { ...rt, rows: rt.rows.slice(0, reqPag.elementsPerPage), pagination: reqPag };
          case "Paginate":
            var startIndex = reqPag.elementsPerPage! * (reqPag.currentPage! - 1);
            return { ...rt, rows: rt.rows.slice(startIndex, startIndex + reqPag.elementsPerPage!), pagination: reqPag };
        }
      }
    case "Paginate": {
      switch (reqPag.mode) {
        case "All": throw new Error(`Requesting ${reqPag.mode} but cached is ${rt.pagination.mode}`)
        case "Firsts":
          if (reqPag.currentPage == 1 && reqPag.elementsPerPage! <= rt.pagination.elementsPerPage!)
            return { ...rt, rows: rt.rows.slice(0, reqPag.elementsPerPage), pagination: reqPag };

          throw new CachedQueryError(`Invalid first`);

        case "Paginate":
          if (((reqPag.elementsPerPage! == rt.pagination.elementsPerPage! && reqPag.currentPage == rt.pagination.currentPage) ||
            (reqPag.elementsPerPage! <= rt.pagination.elementsPerPage! && reqPag.currentPage == 1 && rt.pagination.currentPage == 1))) {

            var startIndex = reqPag.elementsPerPage! * (reqPag.currentPage! - 1);
            return { ...rt, rows: rt.rows.slice(startIndex, startIndex + reqPag.elementsPerPage!), pagination: reqPag };
          }

          throw new CachedQueryError("Invalid paginate");
      }
    }
    case "Firsts": {
      switch (reqPag.mode) {
        case "Firsts":
          if (reqPag.elementsPerPage! <= rt.pagination.elementsPerPage!)
            return { ...rt, rows: rt.rows.slice(0, reqPag.elementsPerPage), pagination: reqPag };

          throw new Error(`Invalid first`);
        case "Paginate":
        case "All": throw new CachedQueryError(`Requesting ${reqPag.mode} but cached is ${rt.pagination.mode}`);
      }
    }
  }
}

function pagionationRestriction(req: Pagination, cached: Pagination): null | "ExactFiltersAndOrders" {

  switch (cached.mode) {
    case "All": return null;
    case "Paginate": {

      switch (req.mode) {

        case "Firsts": {
          if (cached.currentPage == 1 && req.elementsPerPage! <= cached.elementsPerPage!)
            return "ExactFiltersAndOrders";

          throw new CachedQueryError("Invalid First");
        }

        case "Paginate": {
          if (((req.elementsPerPage! == cached.elementsPerPage! && req.currentPage == cached.currentPage) ||
              (req.elementsPerPage! <= cached.elementsPerPage! && req.currentPage == 1 && cached.currentPage == 1)))
            return "ExactFiltersAndOrders";

          throw new CachedQueryError("Invalid Paginate");
        }

        case "All": throw new CachedQueryError(`Requesting ${req.mode} but cached is ${cached.mode}`);
      }
    }

    case "Firsts":
      {
        switch (req.mode) {
          case "Firsts": {
            if (req.elementsPerPage! <= cached.elementsPerPage!)
              return "ExactFiltersAndOrders";

            throw new CachedQueryError("Invalid First");
          }
          case "Paginate":
          case "All": throw new CachedQueryError(`Requesting ${req.mode} but cached is ${cached.mode}`);
        }
      }
  }
}
