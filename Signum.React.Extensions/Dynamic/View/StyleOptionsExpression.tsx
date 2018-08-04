import * as React from 'react'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTabRepeater, EntityTable,  EntityCheckboxList, EnumCheckboxList, EntityDetail, EntityStrip } from '@framework/Lines'
import { ModifiableEntity } from '@framework/Signum.Entities'
import { classes, Dic } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import { ColumnOptionsMode, FilterOperation, OrderType, PaginationMode, FindOptions, FilterOption, OrderOption, ColumnOption, Pagination, QueryToken } from '@framework/FindOptions'
import { SearchControl, ValueSearchControl } from '@framework/Search'
import { getQueryNiceName, TypeInfo, MemberInfo, getTypeInfo, EntityData, EntityKind, getTypeInfos, KindOfType, PropertyRoute, PropertyRouteType, MemberType, isTypeEntity } from '@framework/Reflection'
import * as Navigator from '@framework/Navigator'
import { TypeContext, FormGroupStyle, FormSize, StyleOptions, BsColumns } from '@framework/TypeContext'
import { EntityBase, EntityBaseProps } from '@framework/Lines/EntityBase'
import { EntityTableColumn } from '@framework/Lines/EntityTable'
import { DynamicViewValidationMessage } from '../Signum.Entities.Dynamic'
import { ExpressionOrValueComponent, FieldComponent } from './Designer'
import { ExpressionOrValue } from './NodeUtils'
import * as NodeUtils from './NodeUtils'

export interface StyleOptionsExpression {
    formGroupStyle?: ExpressionOrValue<FormGroupStyle>;
    formSize?: ExpressionOrValue<FormSize>;
    placeholderLabels?: ExpressionOrValue<boolean>;
    readonlyAsPlainText?: ExpressionOrValue<boolean>;
    labelColumns?: ExpressionOrValue<number>;
    valueColumns?: ExpressionOrValue<number>;
    readOnly?: ExpressionOrValue<boolean>;
}

export const formSize: FormSize[] = ["ExtraSmall", "Small", "Normal", "Large"];
export const formGroupStyle: FormGroupStyle[] = ["None", "Basic", "BasicDown", "SrOnly", "LabelColumns"];

export function subCtx(ctx: TypeContext<ModifiableEntity>, field: string | undefined, soe: StyleOptionsExpression | undefined) {

    if (field == undefined && soe == undefined)
            return ctx;

    if (field == undefined)
        return ctx.subCtx(toStyleOptions(ctx, soe)!);

    return ctx.subCtx(NodeUtils.asFieldFunction(field), toStyleOptions(ctx, soe));
}

export function toStyleOptions(ctx: TypeContext<ModifiableEntity>, soe: StyleOptionsExpression | undefined): StyleOptions | undefined {

    if (soe == undefined)
        return undefined;
    
    return {
        formGroupStyle: NodeUtils.evaluateAndValidate(ctx, soe, s => s.formGroupStyle, val => NodeUtils.isInListOrNull(val, formGroupStyle)),
        formSize: NodeUtils.evaluateAndValidate(ctx, soe, s => s.formSize, val => NodeUtils.isInListOrNull(val, formSize)),
        placeholderLabels: NodeUtils.evaluateAndValidate(ctx, soe, s => s.placeholderLabels, NodeUtils.isBooleanOrNull),
        readonlyAsPlainText: NodeUtils.evaluateAndValidate(ctx, soe, s => s.readonlyAsPlainText, NodeUtils.isBooleanOrNull),
        labelColumns: NodeUtils.evaluateAndValidate(ctx, soe, s => s.labelColumns, NodeUtils.isNumberOrNull),
        valueColumns: NodeUtils.evaluateAndValidate(ctx, soe, s => s.valueColumns, NodeUtils.isNumberOrNull),
        readOnly: NodeUtils.evaluateAndValidate(ctx, soe, s => s.readOnly, NodeUtils.isBooleanOrNull),
    };
}
