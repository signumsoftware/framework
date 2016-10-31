import * as React from 'react'
import { Tabs, Tab } from 'react-bootstrap'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityDetail, EntityStrip } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ModifiableEntity, isLite, isEntity, External } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { classes, Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { FindOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import {
    getQueryNiceName, TypeInfo, MemberInfo, getTypeInfo, EntityData, EntityKind, getTypeInfos, Binding, EnumType,
    KindOfType, PropertyRoute, PropertyRouteType, LambdaMemberType, isTypeEntity, isTypeModel, isModifiableEntity
} from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { EntityBase, EntityBaseProps } from '../../../../Framework/Signum.React/Scripts/Lines/EntityBase'
import { EntityListBase, EntityListBaseProps } from '../../../../Framework/Signum.React/Scripts/Lines/EntityListBase'
import { DynamicViewValidationMessage } from '../Signum.Entities.Dynamic'
import { ExpressionOrValueComponent, FieldComponent } from './Designer'
import { FindOptionsLine } from './FindOptionsComponent'
import { FindOptionsExpr, toFindOptions } from './FindOptionsExpression'
import { AuthInfo } from './AuthInfo'
import { BaseNode, LineBaseNode, EntityBaseNode, EntityListBaseNode, EntityLineNode, ContainerNode, EntityTableColumnNode } from './Nodes'
import { toHtmlAttributes, HtmlAttributesExpression, withClassName } from './HtmlAttributesExpression'
import { toStyleOptions, StyleOptionsExpression, subCtx } from './StyleOptionsExpression'
import { HtmlAttributesLine } from './HtmlAttributesComponent'
import { StyleOptionsLine } from './StyleOptionsComponent'

export type ExpressionOrValue<T> = T | Expression<T>;

//ctx -> value
export type Expression<T> = { code: string };

export interface NodeOptions<N extends BaseNode> {
    kind: string;
    group: "Container" | "Property" | "Collection" | "Search" | null;
    order: number | null;
    isContainer?: boolean;
    hasEntity?: boolean;
    hasCollection?: boolean;
    render: (node: DesignerNode<N>, parentCtx: TypeContext<ModifiableEntity>) => React.ReactElement<any>;
    renderTreeNode: (node: DesignerNode<N>) => React.ReactElement<any>;
    renderDesigner: (node: DesignerNode<N>) => React.ReactElement<any>;
    validate?: (node: DesignerNode<N>, parentCtx: TypeContext<ModifiableEntity> | undefined) => string | null | undefined;
    validParent?: string;
    validChild?: string;
    avoidHighlight?: boolean;
    initialize?: (node: N, parentNode: DesignerNode<ContainerNode>) => void
}

export interface DesignerContext {
    refreshView: () => void;
    onClose: () => void;
    getSelectedNode: () => DesignerNode<BaseNode> | undefined;
    setSelectedNode: (newSelectedNode: DesignerNode<BaseNode>) => void;
}

export class DesignerNode<N extends BaseNode> {
    parent?: DesignerNode<BaseNode>;
    context: DesignerContext;
    node: N;
    route?: PropertyRoute;

    static zero<N extends BaseNode>(context: DesignerContext, typeName: string) {
        var res = new DesignerNode();
        res.context = context;
        res.route = PropertyRoute.root(typeName);
        return res;
    }

    createChild(node: BaseNode) {
        var res = new DesignerNode();
        res.parent = this;
        res.context = this.context;
        res.node = node;
        res.route = this.fixRoute();
        const lbn = node as any as { field: string };
        if (lbn.field && res.route)
            res.route = res.route.tryAddMember({ name: lbn.field, type: "Member" });

        return res;
    }

    reCreateNode() {
        if (this.parent == undefined)
            return this;

        return this.parent.createChild(this.node);
    }

    fixRoute(): PropertyRoute | undefined {
        let res = this.route;

        if (!res)
            return undefined;

        if (this.node == undefined)
            return res;

        const options = registeredNodes[this.node.kind];
        if (options.hasCollection)
            res = res.tryAddMember({ name: "", type: "Indexer" });

        if (!res)
            return undefined;

        if (options.hasEntity)
        {
            const tr = res.typeReference();
            if (tr.isLite)
                res = res.tryAddMember({ name: "entity", type: "Member" });
        }
        return res;
    }
}

export const registeredNodes: { [nodeType: string]: NodeOptions<BaseNode> } = {};

export function register<T extends BaseNode>(options: NodeOptions<T>) {
    registeredNodes[options.kind] = options;
}

export function treeNodeKind(dn: DesignerNode<BaseNode>) {
    return <small>{dn.node.kind}</small>;
}

export function treeNodeKindField(dn: DesignerNode<LineBaseNode>) {
    return <span><small>{dn.node.kind}:</small> <strong>{dn.node.field}</strong></span>;
}

export function treeNodeTableColumnProperty(dn: DesignerNode<EntityTableColumnNode>) {
    return <span><small>ETColumn:</small> <strong>{dn.node.property}</strong></span>;
}

export function render(dn: DesignerNode<BaseNode>, parentCtx: TypeContext<ModifiableEntity>) {

    const error = validate(dn, parentCtx);
    if (error)
        return (<div className="alert alert-danger">{getErrorTitle(dn)} {error}</div>);

    try {
        if (evaluateAndValidate(parentCtx, dn.node, n => n.visible, isBooleanOrNull) == false)
            return null;

        const sn = dn.context.getSelectedNode();

        if (sn && sn.node == dn.node && registeredNodes[sn.node.kind].avoidHighlight != true)
            return (
                <div style={{ border: "1px solid #337ab7", borderRadius: "2px" }}>
                    {registeredNodes[dn.node.kind].render(dn, parentCtx)}
                </div>);
    
        return registeredNodes[dn.node.kind].render(dn, parentCtx);

    } catch (e) {
        return (<div className="alert alert-danger">{getErrorTitle(dn)}&nbsp;{(e as Error).message}</div>);
    }
}

export function getErrorTitle(dn: DesignerNode<BaseNode>) {
    const lbn = dn.node as LineBaseNode;
    if (lbn.field)
        return <strong>{dn.node.kind} ({lbn.field})</strong>;
    else
        return <strong>{dn.node.kind}</strong>;
}

export function renderDesigner(dn: DesignerNode<BaseNode>) {
    return (
        <div>
            <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, a => a.visible)} type="boolean" defaultValue={true} />
            {registeredNodes[dn.node.kind].renderDesigner(dn)}
        </div>
    );
}

export function asFunction(expression: Expression<any>, getFieldName: () => string): (e: TypeContext<ModifiableEntity>, auth: AuthInfo) => any {
    let code = expression.code;

    if (!code.contains(";") && !code.contains("return"))
        code = "return " + expression.code + ";";

    code = "(function(ctx, auth){ " + code + "})";

    try {
        return eval(code);
    } catch (e) {
        throw new Error("Syntax in '" + getFieldName() + "':\r\n" + code + "\r\n" + (e as Error).message);
    }
}

export function asFieldFunction(field: string): (e: ModifiableEntity) => any {
    const fixedRoute = field.split(".").map(m => m.firstLower()).join(".");

    const code = "(function(e){ return e." + fixedRoute + ";})";

    try {
        return eval(code);
    } catch (e) {
        throw new Error("Syntax in '" + fixedRoute + "':\r\n" + code + "\r\n" + (e as Error).message);
    }
}

export function evaluate<F, T>(parentCtx: TypeContext<ModifiableEntity>, object: F, fieldAccessor: (from: F) => ExpressionOrValue<T> | undefined): T | undefined {

    return evaluateUntyped(parentCtx, fieldAccessor(object), () => Binding.getSingleMember(fieldAccessor));
}

export function evaluateUntyped(parentCtx: TypeContext<ModifiableEntity>, expressionOrValue: ExpressionOrValue<any> | undefined, getFieldName: () => string): any {
    if (expressionOrValue == null)
        return undefined;

    var ex = expressionOrValue as Expression<any>;
    if (!(ex as Object).hasOwnProperty("code"))
        return expressionOrValue as any;

    if (!ex.code)
        return undefined;

    var f = asFunction(ex, getFieldName);

    try {
        return f(parentCtx, new AuthInfo());
    } catch (e) {
        throw new Error("Eval '" + getFieldName() + "':\r\n" + (e as Error).message);
    }
}

export function evaluateAndValidate<F, T>(parentCtx: TypeContext<ModifiableEntity>, object: F, fieldAccessor: (from: F) => ExpressionOrValue<T>, validate: (val: any) => string | null)   {

    var result = evaluate(parentCtx, object, fieldAccessor);

    var error = validate(result);
    if (error)
        throw new Error("Result '" + Binding.getSingleMember(fieldAccessor) + "':\r\n" + error);

    if (result == null)
        return undefined;

    return result;
}

export function evaluateOnChange<T>(ctx: TypeContext<ModifiableEntity>, dn: DesignerNode<LineBaseNode>): (() => void) | undefined {

    return evaluateAndValidate(ctx, dn.node, n => n.redrawOnChange, isBooleanOrNull) == true ?
        () => ctx.frame!.entityComponent.forceUpdate() :
        undefined;
}


export function validate(dn: DesignerNode<BaseNode>, parentCtx: TypeContext<ModifiableEntity> | undefined) {
    const options = registeredNodes[dn.node.kind];
    if (options.isContainer && options.validChild && (dn.node as ContainerNode).children && (dn.node as ContainerNode).children.some(c => c.kind != options.validChild))
        return DynamicViewValidationMessage.OnlyChildNodesOfType0Allowed.niceToString(options.validChild);

    if (options.validate)
        return options.validate(dn, parentCtx);

    return undefined;
}

export function isString(val: any){
    return typeof val == "string" ? null : `The returned value (${JSON.stringify(val)}) should be a string`;
}

export function isNumber(val: any) {
    return typeof val == "number" ? null : `The returned value (${JSON.stringify(val)}) should be a number`;
}

export function isBoolean(val: any) {
    return typeof val == "boolean" ? null : `The returned value (${JSON.stringify(val)}) should be a boolean`;
}

export function isFindOptions(val: any) {
    return typeof val == "Object" ? null : `The returned value (${JSON.stringify(val)}) should be a valid findOptions`;
}

export function isStringOrNull(val: any) {
    return val == null || typeof val == "string" ? null : `The returned value (${JSON.stringify(val)}) should be a string or null`;
}

export function isEnum(val: any, enumType: EnumType<any>) {
    return val != null && typeof val == "string" && enumType.values().contains(val) ? null : `The returned value (${JSON.stringify(val)}) should be a valid ${enumType.type} (like ${enumType.values().joinComma(" or ")})`;
}

export function isEnumOrNull(val: any, enumType: EnumType<any>) {
    return val == null || typeof val == "string" && enumType.values().contains(val) ? null : `The returned value (${JSON.stringify(val)}) should be a valid ${enumType.type} (like ${enumType.values().joinComma(" or ")}) or null`;
}

export function isInList(val: any, values: string[]) {
    return val != null && typeof val == "string" && values.contains(val) ? null : `The returned value (${JSON.stringify(val)}) should be a value like ${values.joinComma(" or ")}`;
}

export function isInListOrNull(val: any, values: string[]) {
    return val == null || typeof val == "string" && values.contains(val) ? null : `The returned value (${JSON.stringify(val)}) should be a value like ${values.joinComma(" or ")} or null`;
}

export function isNumberOrNull(val: any) {
    return val == null || typeof val == "number" ? null : `The returned value (${JSON.stringify(val)}) should be a number or null`;
}

export function isBooleanOrNull(val: any) {
    return val == null || typeof val == "boolean" ? null : `The returned value (${JSON.stringify(val)}) should be a boolean or null`;
}

export function isFindOptionsOrNull(val: any) {
    return val == null || isFindOptions(val) == null ? null : `The returned value (${JSON.stringify(val)}) should be a findOptions or null`;
}


export function withChildrensSubCtx(dn: DesignerNode<ContainerNode>, parentCtx: TypeContext<ModifiableEntity>, element: React.ReactElement<any>) {
    var ctx = subCtx(parentCtx, (dn.node as any).field, (dn.node as any).styleOptions);
    return withChildrens(dn, ctx, element);
}

export function withChildrens(dn: DesignerNode<ContainerNode>, ctx: TypeContext<ModifiableEntity>, element: React.ReactElement<any>) {   
    var nodes = dn.node.children && dn.node.children.map(n => render(dn.createChild(n), ctx)).filter(a => a != null).map(a => a!);
    return React.cloneElement(element, undefined, ...nodes);
}

export function mandatory<T extends BaseNode>(dn: DesignerNode<T>, fieldAccessor: (from: T) => any) {
    if (!fieldAccessor(dn.node))
        return DynamicViewValidationMessage.Member0IsMandatoryFor1.niceToString(Binding.getSingleMember(fieldAccessor), dn.node.kind);

    return undefined;
}

export function validateFieldMandatory(dn: DesignerNode<LineBaseNode>) {
    return mandatory(dn, n => n.field) || validateField(dn);
}

export function validateEntityBase(dn: DesignerNode<EntityBaseNode>, parentCtx: TypeContext<ModifiableEntity> | undefined) {
    return validateFieldMandatory(dn) || (dn.node.findOptions && validateFindOptions(dn.node.findOptions, parentCtx));
}


export function validateField(dn: DesignerNode<LineBaseNode>) {

    const parentRoute = dn.parent!.route;

    if (parentRoute == undefined)
        return undefined;
    
    const m = parentRoute.subMembers()[dn.node.field!]

    if (!m)
        return DynamicViewValidationMessage.Type0DoesNotContainsField1.niceToString(parentRoute.typeReference().name, dn.node.field);

    const options = registeredNodes[dn.node.kind]

    const entity = isModifiableEntity(m.type);

    const DVVM = DynamicViewValidationMessage;

    if ((entity || false) != (options.hasEntity || false) ||
        (m.type.isCollection || false) != (options.hasCollection || false))
        return DVVM._0RequiresA1.niceToString(dn.node.kind,
            (options.hasEntity ?
                (options.hasCollection ? DVVM.CollectionOfEntities : DVVM.Entity) :
                (options.hasCollection ? DVVM.CollectionOfEnums : DVVM.Value)).niceToString());


    return undefined;
}

export function validateTableColumnProperty(dn: DesignerNode<EntityTableColumnNode>) {

    const parentRoute = dn.parent!.route;

    if (parentRoute == undefined)
        return undefined;

    const m = parentRoute.subMembers()[dn.node.property!]
    const DVVM = DynamicViewValidationMessage;

    if ( m.type.isCollection)
        return DVVM._0RequiresA1.niceToString(dn.node.kind, DVVM.EntityOrValue.niceToString());

    return undefined;
}


export function validateFindOptions(foe: FindOptionsExpr, parentCtx: TypeContext<ModifiableEntity> | undefined) : string | undefined {
    if (!foe.queryKey)
        return DynamicViewValidationMessage._0RequiresA1.niceToString("findOptions", "queryKey");

    if (parentCtx) {
        let list = [
            getTypeIfNew(parentCtx, foe.parentValue),
            ...(foe.filterOptions || []).map(a => getTypeIfNew(parentCtx, a.value))
        ].filter(a => !!a);

        if (list.length)
            return DynamicViewValidationMessage.FilteringWithNew0ConsiderChangingVisibility.niceToString(list.joinComma(External.CollectionMessage.And.niceToString()));
    }

    return undefined;
}


export function getTypeIfNew(parentCtx: TypeContext<ModifiableEntity>, value: ExpressionOrValue<any> | undefined) {
    
    var v = evaluateUntyped(parentCtx, value, ()=>"value");

    if (v == undefined)
        return undefined;

    if (isEntity(v) && v.isNew)
        return v.Type;

    if (isLite(v) && v.id == null)
        return v.EntityType;

    return undefined;
}

export function addBreakLines(breakLines: boolean, message: string): React.ReactNode[] {
    if (!breakLines)
        return [message];

    return message.split("\n").flatMap((e, i) => i == 0 ? [e] : [<br />, e]);
}

export function getEntityBaseProps(dn: DesignerNode<EntityBaseNode>, parentCtx: TypeContext<ModifiableEntity>, options: { showAutoComplete?: boolean, showMove?: boolean }): EntityBaseProps {

    var result: EntityBaseProps = {
        ctx: parentCtx.subCtx(asFieldFunction(dn.node.field)),
        labelText: evaluateAndValidate(parentCtx, dn.node, n => n.labelText, isStringOrNull),
        labelHtmlProps: toHtmlAttributes(parentCtx, dn.node.labelHtmlAttributes),
        formGroupHtmlProps: toHtmlAttributes(parentCtx, dn.node.formGroupHtmlAttributes),
        visible: evaluateAndValidate(parentCtx, dn.node, n => n.visible, isBooleanOrNull),
        readOnly: evaluateAndValidate(parentCtx, dn.node, n => n.readOnly, isBooleanOrNull),
        create: evaluateAndValidate(parentCtx, dn.node, n => n.create, isBooleanOrNull),
        remove: evaluateAndValidate(parentCtx, dn.node, n => n.remove, isBooleanOrNull),
        find: evaluateAndValidate(parentCtx, dn.node, n => n.find, isBooleanOrNull),
        view: evaluateAndValidate(parentCtx, dn.node, n => n.view, isBooleanOrNull),
        viewOnCreate: evaluateAndValidate(parentCtx, dn.node, n => n.viewOnCreate, isBooleanOrNull),
        onChange: evaluateOnChange(parentCtx, dn),
        findOptions: dn.node.findOptions && toFindOptions(parentCtx, dn.node.findOptions),
        getComponent: getGetComponent(dn),
    };


    if (options.showAutoComplete)
        result = Dic.extend(result, { autoComplete: evaluateAndValidate(parentCtx, dn.node, (n: EntityLineNode) => n.autoComplete, isBooleanOrNull) == false ? null : undefined});

    if (options.showMove)
        result = Dic.extend(result, { move: evaluateAndValidate(parentCtx, dn.node, (n: EntityListBaseNode) => n.move, isBooleanOrNull) });

    return result;
}


export function getGetComponent(dn: DesignerNode<ContainerNode>) {
    if (!dn.node.children || !dn.node.children.length)
        return undefined;

    return (ctxe: TypeContext<ModifiableEntity>) => withChildrens(dn, ctxe, <div />);
}

export function designEntityBase(dn: DesignerNode<EntityBaseNode>, options: { isCreable: boolean; isFindable: boolean; isViewable: boolean; showAutoComplete: boolean, showMove?: boolean }) {
  
    const m = dn.route && dn.route.member;
    return (
        <div>
            <FieldComponent dn={dn} binding={Binding.create(dn.node, n => n.field)} />
            <StyleOptionsLine dn={dn} binding={Binding.create(dn.node, n => n.styleOptions)} />
            <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.labelText)} type="string" defaultValue={m && m.niceName || ""} />
            <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.labelHtmlAttributes)} />
            <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.formGroupHtmlAttributes)} />
            <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.readOnly)} type="boolean" defaultValue={null} />
            <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.create)} type="boolean" defaultValue={null} />
            <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.remove)} type="boolean" defaultValue={null} />
            <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.find)} type="boolean" defaultValue={null} />
            <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.view)} type="boolean" defaultValue={null} />
            <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.viewOnCreate)} type="boolean" defaultValue={null} />
            {options.showMove && <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, (n: EntityListBaseNode) => n.move)} type="boolean" defaultValue={null} />}
            {options.showAutoComplete && <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, (n: EntityLineNode) => n.autoComplete)} type="boolean" defaultValue={null} />}
            <FindOptionsLine dn={dn} binding={Binding.create(dn.node, n => n.findOptions)} avoidSuggestion={true} />
            <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.redrawOnChange)} type="boolean" defaultValue={null} />
        </div>
    );
}
