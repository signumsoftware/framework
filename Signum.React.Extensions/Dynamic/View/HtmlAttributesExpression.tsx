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
import { ExpressionOrValue, Expression, isExpression } from './NodeUtils'
import * as NodeUtils from './NodeUtils'

export interface HtmlAttributesExpression {
    style?: CssPropertiesExpression;
    [key: string]: ExpressionOrValue<any>;
}

export interface CssPropertiesExpression {
    [key: string]: ExpressionOrValue<any>;
}

export function toHtmlAttributes(parentCtx: TypeContext<ModifiableEntity>, hae: HtmlAttributesExpression | undefined): React.HTMLAttributes<any> | undefined {

    if (hae == undefined)
        return undefined;

    var result: React.HTMLAttributes<any> = {};
    Dic.getKeys(hae as any).filter(k => k != "style").forEach(key => (result as any)[toPascal(key)] = NodeUtils.evaluateUntyped(parentCtx, hae[key], () => key));
    if (hae.style)
        result.style = toCssProperties(parentCtx, hae.style);
    
    return result;
}

export function withClassName(attrs: React.HTMLAttributes<any> | undefined, className: string): React.HTMLAttributes<any> {
    if (attrs == undefined)
        return { className: className };

    attrs.className = classes(className, attrs.className);

    return attrs;
}

export function toCssProperties(parentCtx: TypeContext<ModifiableEntity>, cpe: CssPropertiesExpression): React.CSSProperties {

    var result: React.CSSProperties = {};
    Dic.getKeys(cpe as any).forEach(key => (result as any)[toPascal(key)] = NodeUtils.evaluateUntyped(parentCtx, cpe[key], ()=>key));
    return result;

}

export function toPascal(dashedName: string) {
    if (dashedName == "class")
        return "className";

    if (dashedName == "for")
        return "htmlFor";

    return dashedName.split("-").map((p, i) => i == 0 ? p : p.firstUpper()).join("");
}