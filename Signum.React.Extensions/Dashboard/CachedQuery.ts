import * as Finder from '@framework/Finder'
import { FilterRequest, isFilterGroupRequest, OrderRequest, Pagination, QueryRequest, ResultRow, ResultTable } from '@framework/FindOptions'
import { is } from '@framework/Signum.Entities';


export interface CachedQuery {
  creationDate: string;
  queryRequest: QueryRequest;
  resultTable: ResultTable;
}

export function getCachedResultTable(cachedQuery: CachedQuery, request: QueryRequest) : ResultTable | string /*Error*/ {

  if (request.queryKey != cachedQuery.queryRequest.queryKey)
    return "Invalid queryKey";

  var pagProblems = paginatonProblems(request.pagination, cachedQuery.queryRequest.pagination);
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
    if (cachedQuery.queryRequest.groupResults) {

    } else {

    }
  } else {
    if (cachedQuery.queryRequest.groupResults)
      return "Cached query is grouping but request is not";

    let result = cachedQuery.resultTable;

    result = filterRows(result, extraFilters);
  }
}

function filterRows(result: ResultTable, filters: FilterRequest[]): ResultTable | string{
  var predicate = createPredicate(result, filters);

  return 
}

function createPredicate(result: ResultTable, filters: FilterRequest[]) : ((rr: ResultRow) => boolean) | string{

  const row = "cls";

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
        return [f.token]

      return 

    }

  }


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

function paginatonProblems(req: Pagination, cached: Pagination): symbol| string |  null {

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
          if (cached.mode == "Paginate" &&
            ((req.elementsPerPage! == cached.elementsPerPage! && req.currentPage == cached.currentPage) ||
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
