import * as React from 'react'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityDetail } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ModifiableEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { classes, Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { FindOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, TypeInfo, MemberInfo, getTypeInfo, EntityData, EntityKind, getTypeInfos, KindOfType } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { DesignCombo, DesignValue, DesignFindOptions } from './Designer'

export interface BaseNode {
    kind: string;
}

export interface ContainerNode extends BaseNode{
    children: BaseNode[],
}

export interface DivNode extends ContainerNode {
    kind: "Div",
}

export interface RowNode extends ContainerNode {
    kind: "Row", 
}

type ExpressionOrValue<T> = T | Expression<T>;

type Expression<T> = { code: string };

export interface ColumnNode extends ContainerNode {
    kind: "Column";
    width: ExpressionOrValue<number>;
    offset?: ExpressionOrValue<number>;
}

export interface LineBaseNode extends BaseNode {
    labelText?: ExpressionOrValue<string>;
    route: string;
    visible?: ExpressionOrValue<boolean>;
    readOnly?: ExpressionOrValue<boolean>;
    redrawOnChange?: boolean;
}

export interface ValueLineNode extends LineBaseNode {
    kind: "ValueLine",
    textArea?: ExpressionOrValue<string>;
    unitText?: ExpressionOrValue<string>;
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
    render: (node: N, ctx: TypeContext<ModifiableEntity>) => React.ReactElement<any>;
    renderDesigner: (node: N, dc: DesignerContext) => React.ReactElement<any>;
    validate?: (node: N) => string | null | undefined;
}

export interface DesignerContext {
    refreshView: () => void;
    onClose: () => void;
}

export const registeredNodes: { [nodeType: string]: NodeOptions<BaseNode> } = {};

export function register<T extends BaseNode>(options: NodeOptions<T>) {
    registeredNodes[options.kind] = options;
}

export function render(node: BaseNode, ctx: TypeContext<ModifiableEntity>) {
    return registeredNodes[node.kind].render(node, ctx);
}

export function renderDesigner(node: BaseNode, dc: DesignerContext) {
    return registeredNodes[node.kind].renderDesigner(node, dc);
}

export function asFunction(expression: Expression<any>): (a: any) => any {
    let code = expression.code;

    if (!code.contains(";") && !code.contains("return"))
        code = "return " + expression.code + ";";

    code = "(function(e){ " + code + "})";

    try {
        return eval(code);
    } catch (e) {
        throw new Error("Impossible to evaluate:\r\n" + code + "\r\n" + (e as Error).message);
    }
}

export function asRouteFunction(route: string): (a: any) => any {
    const fixedRoute = route.split(".").map(m => m.firstLower()).join(".");
    return asFunction({ code: "e." + fixedRoute });
}

export function evaluate<T>(ctx: TypeContext<ModifiableEntity>, expression?: ExpressionOrValue<T>): T {
    return undefined as any;
}

export function evaluateOnChange<T>(ctx: TypeContext<ModifiableEntity>, redrawOnChange?: ExpressionOrValue<boolean>): (() => void) | undefined {
    if (evaluate(ctx, redrawOnChange) == true)
        return () => ctx.frame!.entityComponent.forceUpdate();

    return undefined;
}
register<DivNode>({
    kind: "Div",
    isContainer: true,
    render: (node, ctx) => (<div>
        {node.children && node.children.map(child => render(child, ctx))}
    </div>),
    renderDesigner: node => (<div></div>),
});

register<RowNode>({
    kind: "Row",
    isContainer: true,
    validate: node => node.children && node.children.some(c => c.kind != "Column") ? "Only child nodes of type Columns allowed" : null,
    render: (node, ctx) => (<div className="row">
        {node.children && node.children.map(child => render(child, ctx))}
    </div>),
    renderDesigner: node => (<div></div>),
});

register<ColumnNode>({
    kind: "Column",
    isContainer: true,
    render: (node, ctx) => {
        const offset = evaluate(ctx, node.offset);

        return (<div className={classes("col-sm-" + evaluate(ctx, node.width), offset ? "col-sm-offset-" + offset : null)} >
            {node.children && node.children.map(child => render(child, ctx))}
        </div>)
    },
    renderDesigner: (node, dc) => (<div>
        <DesignCombo dc={dc} node= { node } member="width" options={[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]} />
        <DesignCombo dc={dc} node={node} member="offset" options={[null, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]} />
    </div>),
});


register<ValueLineNode>({
    kind: "ValueLine",
    
    render: (node, ctx) => (<ValueLine
        ctx={ctx.subCtx(asRouteFunction(node.route))}
        labelText={evaluate(ctx, node.labelText)}
        unitText={evaluate(ctx, node.unitText)}
        visible={evaluate(ctx, node.visible)}
        readOnly={evaluate(ctx, node.readOnly)}
        inlineCheckbox={evaluate(ctx, node.inlineCheckbox)}
        valueLineType={evaluate(ctx, node.textArea) ? ValueLineType.TextArea : undefined}
        autoTrim={evaluate(ctx, node.autoTrim)}
        onChange={evaluateOnChange(ctx, node.redrawOnChange)}
        />),
    renderDesigner: (node, dc) => (<div>
        <DesignValue dc={dc} node={node} member="route" type="string" />
        <DesignValue dc={dc} node={node} member="labelText" type="string" />
        <DesignValue dc={dc} node={node} member="unitText" type="string" />
        <DesignValue dc={dc} node={node} member="visible" type="boolean" />
        <DesignValue dc={dc} node={node} member="readOnly" type="boolean" />
        <DesignValue dc={dc} node={node} member="inlineCheckbox" type="boolean" />
        <DesignValue dc={dc} node={node} member="textArea" type="boolean" />
        <DesignValue dc={dc} node={node} member="autoTrim" type="boolean" />
        <DesignValue dc={dc} node={node} member="redrawOnChange" type="boolean" />
    </div>),
});

register<EntityLineNode>({
    kind: "EntityLine",

    render: (node, ctx) => (<EntityLine
        ctx={ctx.subCtx(asRouteFunction(node.route))}
        labelText={evaluate(ctx, node.labelText)}
        visible={evaluate(ctx, node.visible)}
        readOnly={evaluate(ctx, node.readOnly)}
        create={evaluate(ctx, node.create)}
        remove={evaluate(ctx, node.remove)}
        find={evaluate(ctx, node.find)}
        view={evaluate(ctx, node.view)}
        autoComplete={evaluate(ctx, node.autoComplete) == true ? undefined : null}
        findOptions={evaluate(ctx, node.findOptions)}
        onChange={evaluateOnChange(ctx, node.redrawOnChange)}
        />),
    renderDesigner: (node, dc) => (<div>
        <DesignValue dc={dc} node={node} member="route" type="string" />
        <DesignValue dc={dc} node={node} member="labelText" type="string" />
        <DesignValue dc={dc} node={node} member="visible" type="boolean" />
        <DesignValue dc={dc} node={node} member="readOnly" type="boolean" />
        <DesignValue dc={dc} node={node} member="create" type="boolean" />
        <DesignValue dc={dc} node={node} member="remove" type="boolean" />
        <DesignValue dc={dc} node={node} member="find" type="boolean" />
        <DesignValue dc={dc} node={node} member="view" type="boolean" />
        <DesignValue dc={dc} node={node} member="autoComplete" type="boolean" />
        <DesignFindOptions dc={dc} node={node} member="findOptions" />
        <DesignValue dc={dc} node={node} member="redrawOnChange" type="boolean" />
    </div>),
});

register<EntityComboNode>({
    kind: "EntityCombo",

    render: (node, ctx) => (<EntityDetail
        ctx={ctx.subCtx(asRouteFunction(node.route))}
        labelText={evaluate(ctx, node.labelText)}
        visible={evaluate(ctx, node.visible)}
        readOnly={evaluate(ctx, node.readOnly)}
        create={evaluate(ctx, node.create)}
        remove={evaluate(ctx, node.remove)}
        find={evaluate(ctx, node.find)}
        view={evaluate(ctx, node.view)}
        findOptions={evaluate(ctx, node.findOptions)}
        onChange={evaluateOnChange(ctx, node.redrawOnChange)}
        />),
    renderDesigner: (node, dc) => (<div>
        <DesignValue dc={dc} node={node} member="route" type="string" />
        <DesignValue dc={dc} node={node} member="labelText" type="string" />
        <DesignValue dc={dc} node={node} member="visible" type="boolean" />
        <DesignValue dc={dc} node={node} member="readOnly" type="boolean" />
        <DesignValue dc={dc} node={node} member="create" type="boolean" />
        <DesignValue dc={dc} node={node} member="remove" type="boolean" />
        <DesignValue dc={dc} node={node} member="find" type="boolean" />
        <DesignValue dc={dc} node={node} member="view" type="boolean" />
        <DesignFindOptions dc={dc} node={node} member="findOptions" />
        <DesignValue dc={dc} node={node} member="redrawOnChange" type="boolean" />
    </div>),
});

register<EntityDetailNode>({
    kind: "EntityDetail",
    isContainer: true,

    render: (node, ctx) => (<EntityDetail
        ctx={ctx.subCtx(asRouteFunction(node.route))}
        labelText={evaluate(ctx, node.labelText)}
        visible={evaluate(ctx, node.visible)}
        readOnly={evaluate(ctx, node.readOnly)}
        create={evaluate(ctx, node.create)}
        remove={evaluate(ctx, node.remove)}
        find={evaluate(ctx, node.find)}
        view={evaluate(ctx, node.view)}
        findOptions={evaluate(ctx, node.findOptions)}
        onChange={evaluateOnChange(ctx, node.redrawOnChange)}
        />),
    renderDesigner: (node, dc) => (<div>
        <DesignValue dc={dc} node={node} member="route" type="string" />
        <DesignValue dc={dc} node={node} member="labelText" type="string" />
        <DesignValue dc={dc} node={node} member="visible" type="boolean" />
        <DesignValue dc={dc} node={node} member="readOnly" type="boolean" />
        <DesignValue dc={dc} node={node} member="create" type="boolean" />
        <DesignValue dc={dc} node={node} member="remove" type="boolean" />
        <DesignValue dc={dc} node={node} member="find" type="boolean" />
        <DesignValue dc={dc} node={node} member="view" type="boolean" />
        <DesignFindOptions dc={dc} node={node} member="findOptions" />
        <DesignValue dc={dc} node={node} member="redrawOnChange" type="boolean" />
    </div>),
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

        var route = mi.name;

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
            return { kind: "EntityLine", route } as EntityLineNode;

        if (ti) {
            if (ti.kind == KindOfType.Enum)
                return { kind: "ValueLine", route } as ValueLineNode;

            if (ti.entityKind == EntityKind.Part || ti.entityKind == EntityKind.SharedPart)
                return { kind: "EntityDetail", route } as EntityDetailNode;

            if (ti.isLowPopulation)
                return { kind: "EntityCombo", route } as EntityComboNode;

            return { kind: "EntityLine", route } as EntityLineNode;
        }

        if (tr.isEmbedded)
            return { kind: "EntityDetail", route } as EntityDetailNode;

        return { kind: "ValueLine", route } as ValueLineNode;
    }
}

