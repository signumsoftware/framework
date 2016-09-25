import * as React from 'react'
import { Tabs, Tab } from 'react-bootstrap'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTabRepeater, EntityTable,  EntityCheckboxList, EnumCheckboxList, EntityDetail, EntityStrip } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ModifiableEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { classes, Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { ColumnOptionsMode, FilterOperation, OrderType, PaginationMode, FindOptions, FilterOption, OrderOption, ColumnOption, Pagination, QueryToken } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { SearchControl, CountSearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
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
    queryKey?: string;
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

    searchOnLoad?: ExpressionOrValue<boolean>;
    showHeader?: ExpressionOrValue<boolean>;
    showFilters?: ExpressionOrValue<boolean>;
    showFilterButton?: ExpressionOrValue<boolean>;
    showFooter?: ExpressionOrValue<boolean>;
    allowChangeColumns?: ExpressionOrValue<boolean>;
    create?: ExpressionOrValue<boolean>;
    navigate?: ExpressionOrValue<boolean>;
    contextMenu?: ExpressionOrValue<boolean>;
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
        queryName: foe.queryKey!,
        parentColumn: foe.parentColumn,
        parentValue: NodeUtils.evaluate(ctx, foe.parentValue, "parentValue"),

        filterOptions: foe.filterOptions ?
            foe.filterOptions
                .filter(fo => NodeUtils.evaluate(ctx, fo.applicable, "applicable") != false)
                .map(fo => ({
                    columnName: fo.columnName,
                    frozen: NodeUtils.evaluate(ctx, fo.frozen, "frozen"),
                    operation: NodeUtils.evaluate(ctx, fo.operation, "operation"),
                    value: NodeUtils.evaluate(ctx, fo.value, "value")
                } as FilterOption)) : undefined,

        orderOptions: foe.orderOptions ?
            foe.orderOptions
                .filter(oo => NodeUtils.evaluate(ctx, oo.applicable, "applicable") != false)
                .map(oo => ({
                    columnName: oo.columnName,
                    orderType: NodeUtils.evaluate(ctx, oo.orderType, "orderType")
                } as OrderOption)) : undefined,

        columnOptionsMode: NodeUtils.evaluate(ctx, foe.columnOptionsMode, "columnOptionsMode"),

        columnOptions: foe.columnOptions ?
            foe.columnOptions
                .filter(co => NodeUtils.evaluate(ctx, co.applicable, "applicable") != false)
                .map(co => ({
                    columnName: co.columnName,
                    displayName: NodeUtils.evaluate(ctx, co.displayName, "displayName")
                } as ColumnOption)) : undefined,

        pagination: NodeUtils.evaluate(ctx, foe.paginationMode, "paginationMode") ? ({
            mode : NodeUtils.evaluate(ctx, foe.paginationMode, "paginationMode"),
            currentPage: NodeUtils.evaluate(ctx, foe.currentPage, "currentPage"),
            elementsPerPage: NodeUtils.evaluate(ctx, foe.elementsPerPage, "elementsPerPage")
        } as Pagination) : undefined,

        searchOnLoad: NodeUtils.evaluate(ctx, foe.searchOnLoad, "searchOnLoad"),
        showHeader: NodeUtils.evaluate(ctx, foe.showHeader, "showHeader"),
        showFilters: NodeUtils.evaluate(ctx, foe.showFilters, "showFilters"),
        showFilterButton: NodeUtils.evaluate(ctx, foe.showFilterButton, "showFilterButton"),
        showFooter: NodeUtils.evaluate(ctx, foe.showFooter, "showFooter"),
        allowChangeColumns: NodeUtils.evaluate(ctx, foe.allowChangeColumns, "allowChangeColumns"),
        create: NodeUtils.evaluate(ctx, foe.create, "create"),
        navigate: NodeUtils.evaluate(ctx, foe.navigate, "navigate"),
        contextMenu: NodeUtils.evaluate(ctx, foe.contextMenu, "contextMenu"),
    }
}