import * as React from 'react'
import { Tabs, Tab } from 'react-bootstrap'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityDetail } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ModifiableEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { classes, Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { FindOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, TypeInfo, MemberInfo, getTypeInfo, EntityData, EntityKind, getTypeInfos, KindOfType, PropertyRoute, LambdaMemberType } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { EntityBase, EntityBaseProps } from '../../../../Framework/Signum.React/Scripts/Lines/EntityBase'
import { DynamicViewValidationMessage } from '../Signum.Entities.Dynamic'
import { ExpressionOrValueComponent, DesignFindOptions, FieldComponent } from './Designer'

export interface BaseNode {
    kind: string;
}

export interface ContainerNode extends BaseNode {
    children: BaseNode[],    
}

export interface DivNode extends ContainerNode {
    kind: "Div",
}

export interface RowNode extends ContainerNode {
    kind: "Row", 
}

export type ExpressionOrValue<T> = T | Expression<T>;

//ctx -> value
export type Expression<T> = { code: string };

export interface ColumnNode extends ContainerNode {
    kind: "Column";
    width: ExpressionOrValue<number>;
}

export interface TabsNode extends ContainerNode {
    kind: "Tabs";
    id: ExpressionOrValue<string>;
}

export interface TabNode extends ContainerNode {
    kind: "Tab";
    title: ExpressionOrValue<string>;
}

export interface FieldsetNode extends ContainerNode {
    kind: "Fieldset";
    legend: ExpressionOrValue<string>;
}

export interface LineBaseNode extends BaseNode {
    labelText?: ExpressionOrValue<string>;
    field: string;
    visible?: ExpressionOrValue<boolean>;
    readOnly?: ExpressionOrValue<boolean>;
    redrawOnChange?: boolean;
}

export interface ValueLineNode extends LineBaseNode {
    kind: "ValueLine",
    textArea?: ExpressionOrValue<string>;
    unitText?: ExpressionOrValue<string>;
    formatText?: ExpressionOrValue<string>;
    autoTrim?: ExpressionOrValue<boolean>;
    inlineCheckbox?: ExpressionOrValue<boolean>;
}

export interface EntityBaseNode extends LineBaseNode {
    create?: ExpressionOrValue<boolean>;
    find?: ExpressionOrValue<boolean>;
    remove?: ExpressionOrValue<boolean>;
    view?: ExpressionOrValue<boolean>;
    findOptions?: FindOptions;
}

export interface EntityLineNode extends EntityBaseNode {
    kind: "EntityLine",
    autoComplete?: ExpressionOrValue<boolean>;
}

export interface EntityDetailNode extends EntityBaseNode, ContainerNode {
    kind: "EntityDetail",
}

export interface EntityComboNode extends EntityBaseNode {
    kind: "EntityCombo",
}

export interface NodeOptions<N extends BaseNode> {
    kind: string;
    isContainer?: boolean;
    render: (node: DesignerNode<N>, ctx: TypeContext<ModifiableEntity>) => React.ReactElement<any>;
    renderDesigner: (node: DesignerNode<N>) => React.ReactElement<any>;
    validate?: (node: DesignerNode<N>) => string | null | undefined;
    validParent?: string;
    validChild?: string;
}

export interface DesignerContext {
    refreshView: () => void;
    onClose: () => void;
}

export class DesignerNode<N extends BaseNode> {
    parent?: DesignerNode<BaseNode>;
    context: DesignerContext;
    node: N;
    route: PropertyRoute;

    constructor(node: N, parent: DesignerNode<BaseNode> | undefined) {
        this.node = node;
        if (parent) {
            this.parent = parent;
            this.context = parent.context;
            this.route = parent.route;
            const lbn = node as BaseNode as LineBaseNode;
            if (lbn.field) {
                lbn.field.split(".").forEach(p =>
                    this.route = this.route.addMember({ name: p, type: LambdaMemberType.Member })
                );
            }
        }
    }

    get isCreable() { return EntityBase.defaultIsCreable(this.route.typeReference(), false); }
    get isViewable() { return EntityBase.defaultIsViewable(this.route.typeReference(), false); }
    get isFindable() { return EntityBase.defaultIsFindable(this.route.typeReference()); }
}

export const registeredNodes: { [nodeType: string]: NodeOptions<BaseNode> } = {};

export function register<T extends BaseNode>(options: NodeOptions<T>) {
    registeredNodes[options.kind] = options;
}

export function render(dn: DesignerNode<BaseNode>, ctx: TypeContext<ModifiableEntity>) {

    const error = validate(dn);
    if (error)
        return (<div className="alert alert-danger"><strong>{dn.node.kind}</strong> {error}</div>);

    try {
        return registeredNodes[dn.node.kind].render(dn, ctx);
    } catch (e) {
        return (<div className="alert alert-danger"><strong>{dn.node.kind}</strong> {(e as Error).message}</div>);
    }
}

export function renderDesigner(dn: DesignerNode<BaseNode>) {
    return registeredNodes[dn.node.kind].renderDesigner(dn);
}

export function asFunction(expression: Expression<any>, memberName: string): (e: ModifiableEntity) => any {
    let code = expression.code;

    if (!code.contains(";") && !code.contains("return"))
        code = "return " + expression.code + ";";

    code = "(function(e){ " + code + "})";

    try {
        return eval(code);
    } catch (e) {
        throw new Error("Syntax in '" + memberName + "':\r\n" + code + "\r\n" + (e as Error).message);
    }
}

export function asFieldFunction(field: string): (a: any) => any {
    const fixedRoute = field.split(".").map(m => m.firstLower()).join(".");
    return asFunction({ code: "e." + fixedRoute }, "field");
}

export function evaluate<T>(ctx: TypeContext<ModifiableEntity>, expressionOrValue: ExpressionOrValue<T> | undefined, memberName: string): T | undefined {
    if (expressionOrValue == null)
        return undefined;

    var ex = expressionOrValue as Expression<T>;
    if (!(ex as Object).hasOwnProperty("code"))
        return expressionOrValue as T;

    if (!ex.code)
        return undefined;

    var f = asFunction(ex, memberName);

    try {
        return f(ctx.value);
    } catch (e) {
        throw new Error("Eval '" + memberName + "':\r\n" + (e as Error).message);
    }
}

export function evaluateAndValidate(ctx: TypeContext<ModifiableEntity>, dn: DesignerNode<BaseNode>, memberName: string, validate: (val: any) => string | null) {

    const expressionOrValue = (dn.node as any)[memberName];
    var result = evaluate(ctx, expressionOrValue, memberName);

    var error = validate(result);
    if (error)
        throw new Error("Result '" + memberName + "':\r\n" + error);

    if (result == null)
        return undefined;

    return result;
}

export function evaluateOnChange<T>(ctx: TypeContext<ModifiableEntity>, redrawOnChange?: ExpressionOrValue<boolean>): (() => void) | undefined {
    if (evaluate(ctx, redrawOnChange, "redrawOnChange") == true)
        return () => ctx.frame!.entityComponent.forceUpdate();

    return undefined;
}


export function validate(dn: DesignerNode<BaseNode>) {
    const options = registeredNodes[dn.node.kind];
    if (options.isContainer && options.validChild && (dn.node as ContainerNode).children && (dn.node as ContainerNode).children.some(c => c.kind != options.validChild))
        return DynamicViewValidationMessage.OnlyChildNodesOfType0Allowed.niceToString(options.validChild);

    if (options.validate)
        return options.validate(dn);

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

export function isNumberOrNull(val: any) {
    return val == null || typeof val == "number" ? null : `The returned value (${JSON.stringify(val)}) should be a number or null`;
}

export function isBooleanOrNull(val: any) {
    return val == null || typeof val == "boolean" ? null : `The returned value (${JSON.stringify(val)}) should be a boolean or null`;
}

export function isFindOptionsOrNull(val: any) {
    return val == null || isFindOptions(val) == null ? null : `The returned value (${JSON.stringify(val)}) should be a findOptions or null`;
}

register<DivNode>({
    kind: "Div",
    isContainer: true,
    render: (dn, ctx) => withChildrens(dn, ctx, <div />),
    renderDesigner: node => (<div></div>),
});

register<RowNode>({
    kind: "Row",
    isContainer: true,
    validChild: "Column",
    render: (dn, ctx) => withChildrens(dn, ctx, <div className="row" />),
    renderDesigner: node => (<div></div>),
});

register<ColumnNode>({
    kind: "Column",
    isContainer: true,
    validParent: "Row",
    render: (dn, ctx) => {
        const column = evaluateAndValidate(ctx, dn, "width", isNumber);
        const offset = evaluateAndValidate(ctx, dn, "offset", isNumberOrNull);
        const className = classes("col-sm-" + column, offset && "col-sm-offset-" + offset)

        return withChildrens(dn, ctx, <div className={className} />);
    },
    renderDesigner: (dn) => (<div>
        <ExpressionOrValueComponent dn={dn} member="width" type="string" options={[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]} defaultValue={6}/>
    </div>),
});

function withChildrens(dn: DesignerNode<ContainerNode>, ctx: TypeContext<ModifiableEntity>, element: React.ReactElement<any>) {
    var nodes = dn.node.children && dn.node.children.map(child => render(new DesignerNode(child, dn), ctx));
    return React.cloneElement(element, undefined, ...nodes);
}

register<TabsNode>({
    kind: "Tabs",
    isContainer: true,
    validChild: "Tab",
    render: (dn, ctx) => {
        return withChildrens(dn, ctx, <Tabs id={ctx.compose(evaluateAndValidate(ctx, dn, "id", isString) !)} />);
    },
    renderDesigner: (dn) => (<div>
        <ExpressionOrValueComponent dn={dn} member="id" type="string" defaultValue="tab"/>
    </div>),
});


register<TabNode>({
    kind: "Tab",
    isContainer: true,
    validParent: "Tabs",
    render: (dn, ctx) => {
        return withChildrens(dn, ctx, <Tab title={evaluateAndValidate(ctx, dn, "title", isString)} />);
    },
    renderDesigner: (dn) => (<div>
        <ExpressionOrValueComponent dn={dn} member="title" type="string" defaultValue="Tab"/>
    </div>),
});

register<FieldsetNode>({
    kind: "Fieldset",
    isContainer: true,
    render: (dn, ctx) => {
        return (<fieldset>
            <legend>{evaluateAndValidate(ctx, dn, "legend", isString)}</legend>
            {withChildrens(dn, ctx, <div />)}
        </fieldset>)
    },
    renderDesigner: (dn) => (<div>
        <ExpressionOrValueComponent dn={dn} member="legend" type="string" defaultValue="Legend"/>
    </div>),
});

register<ValueLineNode>({
    kind: "ValueLine",
    validate: (dn) => validateFieldMandatory(dn),
    render: (dn, ctx) => (<ValueLine
        ctx={ctx.subCtx(asFieldFunction(dn.node.field))}
        labelText={evaluateAndValidate(ctx, dn, "labelText", isStringOrNull)}
        unitText={evaluateAndValidate(ctx, dn, "unitText", isStringOrNull)}
        formatText={evaluateAndValidate(ctx, dn, "formatText", isStringOrNull)}
        visible={evaluateAndValidate(ctx, dn, "visible", isBooleanOrNull)}
        readOnly={evaluateAndValidate(ctx, dn, "readOnly", isBooleanOrNull)}
        inlineCheckbox={evaluateAndValidate(ctx, dn, "inlineCheckbox", isBooleanOrNull)}
        valueLineType={evaluateAndValidate(ctx, dn, "textArea", isBooleanOrNull) ? ValueLineType.TextArea : undefined}
        autoTrim={evaluateAndValidate(ctx, dn, "autoTrim", isBooleanOrNull)}
        onChange={evaluateOnChange(ctx, dn.node.redrawOnChange)}
        />),
    renderDesigner: (dn) => {
        const m = dn.route.member;
        return (<div>
            <FieldComponent dn={dn} />
            <ExpressionOrValueComponent dn={dn} member="labelText" type="string" defaultValue={m && m.niceName || ""} />
            <ExpressionOrValueComponent dn={dn} member="unitText" type="string" defaultValue={m && m.unit || ""} />
            <ExpressionOrValueComponent dn={dn} member="format" type="string" defaultValue={m && m.format || ""} />
            <ExpressionOrValueComponent dn={dn} member="visible" type="boolean" defaultValue={true} />
            <ExpressionOrValueComponent dn={dn} member="readOnly" type="boolean" defaultValue={false} />
            <ExpressionOrValueComponent dn={dn} member="inlineCheckbox" type="boolean" defaultValue={false} />
            <ExpressionOrValueComponent dn={dn} member="textArea" type="boolean" defaultValue={false} />
            <ExpressionOrValueComponent dn={dn} member="autoTrim" type="boolean" defaultValue={true} />
            <ExpressionOrValueComponent dn={dn} member="redrawOnChange" type="boolean" defaultValue={false} />
        </div>)
    },
});

function mandatory(dn: DesignerNode<BaseNode>, member: string) {
    if (!(dn.node as any)[member])
        return DynamicViewValidationMessage.Member0IsMandatoryFor1.niceToString(dn.node.kind);

    return undefined
}

function validateFieldMandatory(dn: DesignerNode<LineBaseNode>) {
    return mandatory(dn, "field") || validateField(dn);
}

function validateField(dn: DesignerNode<LineBaseNode>) {

    if (!dn.parent!.route.subMembers()[dn.node.field])
        return DynamicViewValidationMessage.Type0DoesNotContainsField1.niceToString(dn.route.typeReference().name, dn.node.field);

    return undefined;
}

function getEntityBaseProps(dn: DesignerNode<EntityBaseNode>, ctx: TypeContext<ModifiableEntity>): EntityBaseProps {

    return {
        ctx: ctx.subCtx(asFieldFunction(dn.node.field)),
        labelText: evaluateAndValidate(ctx, dn, "labelText", isStringOrNull),
        visible: evaluateAndValidate(ctx, dn, "visible", isBooleanOrNull),
        readOnly: evaluateAndValidate(ctx, dn, "readOnly", isBooleanOrNull),
        create: evaluateAndValidate(ctx, dn, "create", isBooleanOrNull),
        remove: evaluateAndValidate(ctx, dn, "remove", isBooleanOrNull),
        find: evaluateAndValidate(ctx, dn, "find", isBooleanOrNull),
        view: evaluateAndValidate(ctx, dn, "view", isBooleanOrNull),
        onChange: evaluateOnChange(ctx, dn.node.redrawOnChange),
        findOptions: evaluateAndValidate(ctx, dn, "findOptions", isFindOptionsOrNull),
    };
}

function designEntityBase(dn: DesignerNode<EntityBaseNode>) {

    const m = dn.route.member;
    return (<div>
        <FieldComponent dn={dn} />
        <ExpressionOrValueComponent dn={dn} member="labelText" type="string" defaultValue={m && m.niceName || ""} />
        <ExpressionOrValueComponent dn={dn} member="visible" type="boolean" defaultValue={true} />
        <ExpressionOrValueComponent dn={dn} member="readOnly" type="boolean" defaultValue={false} />
        <ExpressionOrValueComponent dn={dn} member="create" type="boolean" defaultValue={dn.isCreable} />
        <ExpressionOrValueComponent dn={dn} member="remove" type="boolean" defaultValue={true} />
        <ExpressionOrValueComponent dn={dn} member="find" type="boolean" defaultValue={dn.isFindable} />
        <ExpressionOrValueComponent dn={dn} member="view" type="boolean" defaultValue={dn.isViewable} />
        {(dn.node.kind == "EntityLine" || dn.node.kind == "EntityStrip") &&
            <ExpressionOrValueComponent dn={dn} member="autoComplete" type="boolean" defaultValue={true} />}
        <DesignFindOptions dn={dn} member="findOptions" />
        <ExpressionOrValueComponent dn={dn} member="redrawOnChange" type="boolean" defaultValue={false} />
    </div>)
}

register<EntityLineNode>({
    kind: "EntityLine",
    validate: (dn) => validateFieldMandatory(dn),
    render: (dn, ctx) => (<EntityLine {...getEntityBaseProps(dn, ctx)}
        autoComplete={evaluateAndValidate(ctx, dn, "autoComplete", isBooleanOrNull) == true ? undefined : null} />),
    renderDesigner: designEntityBase,
});


register<EntityComboNode>({
    kind: "EntityCombo",
    validate: (dn) => validateFieldMandatory(dn),
    render: (dn, ctx) => (<EntityCombo {...getEntityBaseProps(dn, ctx) }/>),
    renderDesigner: designEntityBase,
});

register<EntityDetailNode>({
    kind: "EntityDetail",
    isContainer: true,
    validate: (dn) => validateFieldMandatory(dn),
    render: (dn, ctx) => (<EntityDetail {...getEntityBaseProps(dn, ctx) } />),
    renderDesigner: designEntityBase,
});

export namespace NodeConstructor {

    export function createDefaultNode(ti: TypeInfo) {
        return {
            kind: "Div",
            children: Dic.getValues(ti.members).filter(mi => mi.name != "Id" && !mi.name.contains(".") && !mi.name.contains("/")).map(mi => appropiateComponent(mi))
        } as DivNode;
    }
    
    export const specificComponents: {
        [typeName: string]: (ctx: MemberInfo) => BaseNode | undefined;
    } = {};

    function notImplemented(what :string): never{
        throw new Error(what + " not implemented");
    }

    export var appropiateComponent = (mi: MemberInfo): BaseNode => {
        const tr = mi.type;
        const sc = specificComponents[tr.name];
        if (sc) {
            const result = sc(mi);
            if (result)
                return result;
        }

        var field = mi.name;

        const tis = getTypeInfos(tr);
        const ti = tis.firstOrNull();

        if (tr.isCollection) {
            if (tr.isEmbedded || ti!.entityKind == EntityKind.Part || ti!.entityKind == EntityKind.SharedPart)
                return notImplemented("EntityRepeater");
            else if (ti!.isLowPopulation)
                return notImplemented("EntityCheckboxList");
            else
                return notImplemented("EntityStrip");
        }

        if (tr.name == "[ALL]")
            return { kind: "EntityLine", field } as EntityLineNode;

        if (ti) {
            if (ti.kind == KindOfType.Enum)
                return { kind: "ValueLine", field } as ValueLineNode;

            if (ti.entityKind == EntityKind.Part || ti.entityKind == EntityKind.SharedPart)
                return { kind: "EntityDetail", field } as EntityDetailNode;

            if (ti.isLowPopulation)
                return { kind: "EntityCombo", field } as EntityComboNode;

            return { kind: "EntityLine", field } as EntityLineNode;
        }

        if (tr.isEmbedded)
            return { kind: "EntityDetail", field } as EntityDetailNode;

        return { kind: "ValueLine", field } as ValueLineNode;
    }
}

