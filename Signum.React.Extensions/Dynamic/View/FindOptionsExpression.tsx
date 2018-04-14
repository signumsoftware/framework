import * as React from 'react'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTabRepeater, EntityTable,  EntityCheckboxList, EnumCheckboxList, EntityDetail, EntityStrip } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ModifiableEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { classes, Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { ColumnOptionsMode, FilterOperation, OrderType, PaginationMode, FindOptions, FilterOption, OrderOption, ColumnOption, Pagination, QueryToken } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { SearchControl, ValueSearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { getQueryNiceName, TypeInfo, MemberInfo, getTypeInfo, EntityData, EntityKind, getTypeInfos, KindOfType, PropertyRoute, PropertyRouteType, LambdaMemberType, isTypeEntity } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { EntityBase, EntityBaseProps } from '../../../../Framework/Signum.React/Scripts/Lines/EntityBase'
import { EntityTableColumn } from '../../../../Framework/Signum.React/Scripts/Lines/EntityTable'
import { DynamicViewValidationMessage } from '../Signum.Entities.Dynamic'
import { ExpressionOrValueComponent, FieldComponent } from './Designer'
import { ExpressionOrValue } from './NodeUtils'
import * as NodeUtils from './NodeUtils'


export interface FindOptionsExpr {
    queryName?: string;
    parentColumn?: string;
    parentToken?: QueryToken;
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
    columnName?: string;
    token?: QueryToken;
    operation?: ExpressionOrValue<FilterOperation>;
    value: ExpressionOrValue<any>;
    frozen?: ExpressionOrValue<boolean>;
    applicable: ExpressionOrValue<boolean>;
}

export interface OrderOptionExpr {
    columnName?: string;
    token?: QueryToken;
    orderType: ExpressionOrValue<OrderType>;
    applicable: ExpressionOrValue<boolean>;
}

export interface ColumnOptionExpr {
    columnName?: string;
    token?: QueryToken;
    displayName?: ExpressionOrValue<string>;
    applicable: ExpressionOrValue<boolean>;
}

export function toFindOptions(ctx: TypeContext<ModifiableEntity>, foe: FindOptionsExpr): FindOptions{
    return {
        queryName: foe.queryName!,
        parentColumn: foe.parentColumn,
        parentValue: NodeUtils.evaluate(ctx, foe, f => f.parentValue),

        filterOptions: foe.filterOptions ?
            foe.filterOptions
                .filter(fo => NodeUtils.evaluateAndValidate(ctx, fo, f => f.applicable, NodeUtils.isBooleanOrNull) != false)
                .map(fo => ({
                    columnName: fo.columnName,
                    frozen: NodeUtils.evaluateAndValidate(ctx, fo, f => f.frozen, NodeUtils.isBooleanOrNull),
                    operation: NodeUtils.evaluateAndValidate(ctx, fo, f => f.operation, v => NodeUtils.isEnumOrNull(v, FilterOperation)),
                    value: NodeUtils.evaluate(ctx, fo, f => f.value)
                } as FilterOption)) : undefined,

        orderOptions: foe.orderOptions ?
            foe.orderOptions
                .filter(oo => NodeUtils.evaluateAndValidate(ctx, oo, o => o.applicable, NodeUtils.isBooleanOrNull) != false)
                .map(oo => ({
                    columnName: oo.columnName,
                    orderType: NodeUtils.evaluateAndValidate(ctx, oo, o => o.orderType, v => NodeUtils.isEnumOrNull(v, OrderType))
                } as OrderOption)) : undefined,

        columnOptionsMode: NodeUtils.evaluateAndValidate(ctx, foe, f => f.columnOptionsMode, v => NodeUtils.isEnumOrNull(v, ColumnOptionsMode)),

        columnOptions: foe.columnOptions ?
            foe.columnOptions
                .filter(co => NodeUtils.evaluateAndValidate(ctx, co, c => c.applicable, NodeUtils.isBooleanOrNull) != false)
                .map(co => ({
                    columnName: co.columnName,
                    displayName: NodeUtils.evaluateAndValidate(ctx, co, c => c.displayName, NodeUtils.isStringOrNull)
                } as ColumnOption)) : undefined,

        pagination: NodeUtils.evaluateAndValidate(ctx, foe, f => f.paginationMode, v => NodeUtils.isEnumOrNull(v, PaginationMode)) ? ({
            mode: NodeUtils.evaluateAndValidate(ctx, foe, f => f.paginationMode, v => NodeUtils.isEnumOrNull(v, PaginationMode)),
            currentPage: NodeUtils.evaluateAndValidate(ctx, foe, f => f.currentPage, NodeUtils.isNumberOrNull),
            elementsPerPage: NodeUtils.evaluateAndValidate(ctx, foe, f => f.elementsPerPage, NodeUtils.isNumberOrNull)
        } as Pagination) : undefined,     
    }
}