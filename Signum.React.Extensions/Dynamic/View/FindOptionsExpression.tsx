import { ModifiableEntity } from '@framework/Signum.Entities'
import { ColumnOptionsMode, FilterOperation, OrderType, PaginationMode, FindOptions, FilterOption, OrderOption, ColumnOption, Pagination, QueryToken } from '@framework/FindOptions'
import { TypeContext } from '@framework/TypeContext'
import * as Finder from '@framework/Finder'
import { ExpressionOrValue } from './NodeUtils'
import * as NodeUtils from './NodeUtils'
//import { BaseNode } from './Nodes';

export interface FindOptionsExpr {
  queryName?: string;
  parentToken?: string;
  parsedParentToken?: QueryToken;
  parentValue?: ExpressionOrValue<any>;

  filterOptions?: FilterOptionExpr[];
  includeDefaultFilters?: boolean;
  orderOptions?: OrderOptionExpr[];
  columnOptionsMode?: ExpressionOrValue<ColumnOptionsMode>;
  columnOptions?: ColumnOptionExpr[];
  paginationMode?: PaginationMode;
  elementsPerPage?: ExpressionOrValue<number>;
  currentPage?: ExpressionOrValue<number>;
}

export interface FilterOptionExpr {
  token?: string;
  parsedToken?: QueryToken;
  operation?: ExpressionOrValue<FilterOperation>;
  value: ExpressionOrValue<any>;
  frozen?: ExpressionOrValue<boolean>;
  applicable: ExpressionOrValue<boolean>;
}

export interface OrderOptionExpr {
  token?: string;
  parsedToken?: QueryToken;
  orderType: ExpressionOrValue<OrderType>;
  applicable: ExpressionOrValue<boolean>;
}

export interface ColumnOptionExpr {
  token?: string;
  parsedToken?: QueryToken;
  displayName?: ExpressionOrValue<string>;
  applicable: ExpressionOrValue<boolean>;
}

export function toFindOptions(dn: any/*NodeUtils.DesignerNode<BaseNode>*/, ctx: TypeContext<ModifiableEntity>, foe: FindOptionsExpr): FindOptions {
  return {
    queryName: foe.queryName!,
    parentToken: foe.parentToken,
    parentValue: NodeUtils.evaluate(dn, ctx, foe, f => f.parentValue),

    filterOptions: [
      ...(foe.filterOptions ? foe.filterOptions
        .filter(fo => NodeUtils.evaluateAndValidate(dn, ctx, fo, f => f.applicable, NodeUtils.isBooleanOrNull) != false)
        .map(fo => ({
          token: fo.token,
          frozen: NodeUtils.evaluateAndValidate(dn, ctx, fo, f => f.frozen, NodeUtils.isBooleanOrNull),
          operation: NodeUtils.evaluateAndValidate(dn, ctx, fo, f => f.operation, v => NodeUtils.isEnumOrNull(v, FilterOperation)),
          value: NodeUtils.evaluate(dn, ctx, fo, f => f.value)
        } as FilterOption)) : []),
    ],

    includeDefaultFilters: foe.includeDefaultFilters,

    orderOptions: foe.orderOptions ?
      foe.orderOptions
        .filter(oo => NodeUtils.evaluateAndValidate(dn, ctx, oo, o => o.applicable, NodeUtils.isBooleanOrNull) != false)
        .map(oo => ({
          token: oo.token,
          orderType: NodeUtils.evaluateAndValidate(dn, ctx, oo, o => o.orderType, v => NodeUtils.isEnumOrNull(v, OrderType))
        } as OrderOption)) : undefined,

    columnOptionsMode: NodeUtils.evaluateAndValidate(dn, ctx, foe, f => f.columnOptionsMode, v => NodeUtils.isEnumOrNull(v, ColumnOptionsMode)),

    columnOptions: foe.columnOptions ?
      foe.columnOptions
        .filter(co => NodeUtils.evaluateAndValidate(dn, ctx, co, c => c.applicable, NodeUtils.isBooleanOrNull) != false)
        .map(co => ({
          token: co.token,
          displayName: NodeUtils.evaluateAndValidate(dn, ctx, co, c => c.displayName, NodeUtils.isStringOrNull)
        } as ColumnOption)) : undefined,

    pagination: NodeUtils.evaluateAndValidate(dn, ctx, foe, f => f.paginationMode, v => NodeUtils.isEnumOrNull(v, PaginationMode)) ? ({
      mode: NodeUtils.evaluateAndValidate(dn, ctx, foe, f => f.paginationMode, v => NodeUtils.isEnumOrNull(v, PaginationMode)),
      currentPage: NodeUtils.evaluateAndValidate(dn, ctx, foe, f => f.currentPage, NodeUtils.isNumberOrNull),
      elementsPerPage: NodeUtils.evaluateAndValidate(dn, ctx, foe, f => f.elementsPerPage, NodeUtils.isNumberOrNull)
    } as Pagination) : undefined,
  }
}
