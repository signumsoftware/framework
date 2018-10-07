import * as React from 'react'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTabRepeater, EntityTable,  EntityCheckboxList, EnumCheckboxList, EntityDetail, EntityStrip } from '@framework/Lines'
import { ModifiableEntity } from '@framework/Signum.Entities'
import { classes, Dic } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import { ColumnOptionsMode, FilterOperation, OrderType, PaginationMode, FindOptions, FilterOption, OrderOption, ColumnOption, Pagination, QueryToken } from '@framework/FindOptions'
import { SearchControl, ValueSearchControl } from '@framework/Search'
import { getQueryNiceName, TypeInfo, MemberInfo, getTypeInfo, EntityData, EntityKind, getTypeInfos, KindOfType, PropertyRoute, PropertyRouteType, MemberType, isTypeEntity } from '@framework/Reflection'
import * as Navigator from '@framework/Navigator'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import { EntityBase, EntityBaseProps } from '@framework/Lines/EntityBase'
import { EntityTableColumn } from '@framework/Lines/EntityTable'
import { DynamicViewValidationMessage } from '../Signum.Entities.Dynamic'
import { ExpressionOrValueComponent, FieldComponent } from './Designer'
import { ExpressionOrValue } from './NodeUtils'
import * as NodeUtils from './NodeUtils'


export interface FindOptionsExpr {
    queryName?: string;
    parentToken?: string;
    parsedParentToken?: QueryToken;
    parentValue?: ExpressionOrValue<any>;

    filterOptions?: FilterOptionExpr[];
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

export function toFindOptions(ctx: TypeContext<ModifiableEntity>, foe: FindOptionsExpr): FindOptions {
    return {
        queryName: foe.queryName!,
        parentToken: foe.parentToken,
        parentValue: NodeUtils.evaluate(ctx, foe, f => f.parentValue),

        filterOptions: foe.filterOptions ?
            foe.filterOptions
                .filter(fo => NodeUtils.evaluateAndValidate(ctx, fo, f => f.applicable, NodeUtils.isBooleanOrNull) != false)
                .map(fo => ({
                    token: fo.token,
                    frozen: NodeUtils.evaluateAndValidate(ctx, fo, f => f.frozen, NodeUtils.isBooleanOrNull),
                    operation: NodeUtils.evaluateAndValidate(ctx, fo, f => f.operation, v => NodeUtils.isEnumOrNull(v, FilterOperation)),
                    value: NodeUtils.evaluate(ctx, fo, f => f.value)
                } as FilterOption)) : undefined,

        orderOptions: foe.orderOptions ?
            foe.orderOptions
                .filter(oo => NodeUtils.evaluateAndValidate(ctx, oo, o => o.applicable, NodeUtils.isBooleanOrNull) != false)
                .map(oo => ({
                    token: oo.token,
                    orderType: NodeUtils.evaluateAndValidate(ctx, oo, o => o.orderType, v => NodeUtils.isEnumOrNull(v, OrderType))
                } as OrderOption)) : undefined,

        columnOptionsMode: NodeUtils.evaluateAndValidate(ctx, foe, f => f.columnOptionsMode, v => NodeUtils.isEnumOrNull(v, ColumnOptionsMode)),

        columnOptions: foe.columnOptions ?
            foe.columnOptions
                .filter(co => NodeUtils.evaluateAndValidate(ctx, co, c => c.applicable, NodeUtils.isBooleanOrNull) != false)
                .map(co => ({
                    token: co.token,
                    displayName: NodeUtils.evaluateAndValidate(ctx, co, c => c.displayName, NodeUtils.isStringOrNull)
                } as ColumnOption)) : undefined,

        pagination: NodeUtils.evaluateAndValidate(ctx, foe, f => f.paginationMode, v => NodeUtils.isEnumOrNull(v, PaginationMode)) ? ({
            mode: NodeUtils.evaluateAndValidate(ctx, foe, f => f.paginationMode, v => NodeUtils.isEnumOrNull(v, PaginationMode)),
            currentPage: NodeUtils.evaluateAndValidate(ctx, foe, f => f.currentPage, NodeUtils.isNumberOrNull),
            elementsPerPage: NodeUtils.evaluateAndValidate(ctx, foe, f => f.elementsPerPage, NodeUtils.isNumberOrNull)
        } as Pagination) : undefined,     
    }
}