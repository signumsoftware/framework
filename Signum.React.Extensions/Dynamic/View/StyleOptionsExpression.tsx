import { ModifiableEntity } from '@framework/Signum.Entities'
import { TypeContext, FormGroupStyle, FormSize, StyleOptions,  } from '@framework/TypeContext'
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
