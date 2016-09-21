import * as React from 'react'
import { Tabs, Tab } from 'react-bootstrap'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTabRepeater, EntityTable,  EntityCheckboxList, EnumCheckboxList, EntityDetail, EntityStrip } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ModifiableEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { classes, Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { FindOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, TypeInfo, MemberInfo, getTypeInfo, EntityData, EntityKind, getTypeInfos, KindOfType, PropertyRoute, PropertyRouteType, LambdaMemberType, isTypeEntity } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { EntityBase, EntityBaseProps } from '../../../../Framework/Signum.React/Scripts/Lines/EntityBase'
import { EntityTableColumn } from '../../../../Framework/Signum.React/Scripts/Lines/EntityTable'
import { DynamicViewValidationMessage } from '../Signum.Entities.Dynamic'
import { ExpressionOrValueComponent, DesignFindOptions, FieldComponent } from './Designer'
import { ExpressionOrValue } from './NodeUtils'
import * as NodeUtils from './NodeUtils'

export interface BaseNode {
    kind: string;
    visible?: ExpressionOrValue<boolean>;
}

export interface ContainerNode extends BaseNode {
    children: BaseNode[],    
}

export interface DivNode extends ContainerNode {
    kind: "Div",
}

NodeUtils.register<DivNode>({
    kind: "Div",
    group: "Container",
    order: 0,
    isContainer: true,
    renderTreeNode: NodeUtils.treeNodeKind, 
    render: (dn, ctx) => NodeUtils.withChildrens(dn, ctx, <div />),
    renderDesigner: dn => (<div>
    </div>),
});

export interface RowNode extends ContainerNode {
    kind: "Row", 
}

NodeUtils.register<RowNode>({
    kind: "Row",
    group: "Container",
    order: 1,
    isContainer: true,
    validChild: "Column",
    renderTreeNode: NodeUtils.treeNodeKind, 
    render: (dn, ctx) => NodeUtils.withChildrens(dn, ctx, <div className="row" />),
    renderDesigner: dn => (<div>
    </div>),
});


export interface ColumnNode extends ContainerNode {
    kind: "Column";
    width: ExpressionOrValue<number>;
}

NodeUtils.register<ColumnNode>({
    kind: "Column",
    group: null,
    order: null,
    isContainer: true,
    avoidHighlight: true,
    validParent: "Row",
    validate: dn => NodeUtils.mandatory(dn, "width"),
    initialize: dn => dn.width = 6,
    renderTreeNode: NodeUtils.treeNodeKind, 
    render: (dn, ctx) => {
        const column = NodeUtils.evaluateAndValidate(ctx, dn, "width", NodeUtils.isNumber);
        const offset = NodeUtils.evaluateAndValidate(ctx, dn, "offset", NodeUtils.isNumberOrNull);
        const className = classes("col-sm-" + column, offset && "col-sm-offset-" + offset)

        return NodeUtils.withChildrens(dn, ctx, <div className={className} />);
    },
    renderDesigner: (dn) => (<div>
        <ExpressionOrValueComponent dn={dn} member="width" type="string" options={[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]} defaultValue={null} />
    </div>),
});

export interface TabsNode extends ContainerNode {
    kind: "Tabs";
    id: ExpressionOrValue<string>;
}

NodeUtils.register<TabsNode>({
    kind: "Tabs",
    group: "Container",
    order: 2,
    isContainer: true,
    validChild: "Tab",
    initialize: dn => dn.id = "tabs", 
    renderTreeNode: NodeUtils.treeNodeKind, 
    render: (dn, ctx) => {
        return NodeUtils.withChildrens(dn, ctx, <Tabs id={ctx.compose(NodeUtils.evaluateAndValidate(ctx, dn, "id", NodeUtils.isString) !)} />);
    },
    renderDesigner: (dn) => (<div>
        <ExpressionOrValueComponent dn={dn} member="id" type="string" defaultValue={null} />
    </div>),
});


export interface TabNode extends ContainerNode {
    kind: "Tab";
    title: ExpressionOrValue<string>;
}

NodeUtils.register<TabNode>({
    kind: "Tab",
    group: null,
    order: null,
    isContainer: true,
    avoidHighlight: true,
    validParent: "Tabs",
    initialize: dn => dn.title = "My Tab",
    renderTreeNode: NodeUtils.treeNodeKind, 
    render: (dn, ctx) => {
        return NodeUtils.withChildrens(dn, ctx, <Tab title={NodeUtils.evaluateAndValidate(ctx, dn, "title", NodeUtils.isString)} />);
    },
    renderDesigner: (dn) => (<div>
        <ExpressionOrValueComponent dn={dn} member="title" type="string" defaultValue={null} />
    </div>),
});


export interface FieldsetNode extends ContainerNode {
    kind: "Fieldset";
    legend: ExpressionOrValue<string>;
}

NodeUtils.register<FieldsetNode>({
    kind: "Fieldset",
    group: "Container",
    order: 3,
    isContainer: true,
    initialize: dn => dn.legend = "My Fieldset",
    renderTreeNode: NodeUtils.treeNodeKind, 
    render: (dn, ctx) => {
        return (<fieldset>
            <legend>{NodeUtils.evaluateAndValidate(ctx, dn, "legend", NodeUtils.isString)}</legend>
            {NodeUtils.withChildrens(dn, ctx, <div />)}
        </fieldset>)
    },
    renderDesigner: (dn) => (<div>
        <ExpressionOrValueComponent dn={dn} member="legend" type="string" defaultValue={null} />
    </div>),
});

export interface LineBaseNode extends BaseNode {
    labelText?: ExpressionOrValue<string>;
    field: string;
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

NodeUtils.register<ValueLineNode>({
    kind: "ValueLine",
    group: "Property",
    order: 0,
    validate: (dn) => NodeUtils.validateFieldMandatory(dn),
    renderTreeNode: NodeUtils.treeNodeKindField, 
    render: (dn, ctx) => (<ValueLine
        ctx={ctx.subCtx(NodeUtils.asFieldFunction(dn.node.field))}
        labelText={NodeUtils.evaluateAndValidate(ctx, dn, "labelText", NodeUtils.isStringOrNull)}
        unitText={NodeUtils.evaluateAndValidate(ctx, dn, "unitText", NodeUtils.isStringOrNull)}
        formatText={NodeUtils.evaluateAndValidate(ctx, dn, "formatText", NodeUtils.isStringOrNull)}
        readOnly={NodeUtils.evaluateAndValidate(ctx, dn, "readOnly", NodeUtils.isBooleanOrNull)}
        inlineCheckbox={NodeUtils.evaluateAndValidate(ctx, dn, "inlineCheckbox", NodeUtils.isBooleanOrNull)}
        valueLineType={NodeUtils.evaluateAndValidate(ctx, dn, "textArea", NodeUtils.isBooleanOrNull) ? ValueLineType.TextArea : undefined}
        autoTrim={NodeUtils.evaluateAndValidate(ctx, dn, "autoTrim", NodeUtils.isBooleanOrNull)}
        onChange={NodeUtils.evaluateOnChange(ctx, dn.node.redrawOnChange)}
        />),
    renderDesigner: (dn) => {
        const m = dn.route && dn.route.member;
        return (<div>
            <FieldComponent dn={dn} member="field"/>
            <ExpressionOrValueComponent dn={dn} member="labelText" type="string" defaultValue={m && m.niceName || ""} />
            <ExpressionOrValueComponent dn={dn} member="unitText" type="string" defaultValue={m && m.unit || ""} />
            <ExpressionOrValueComponent dn={dn} member="format" type="string" defaultValue={m && m.format || ""} />
            <ExpressionOrValueComponent dn={dn} member="readOnly" type="boolean" defaultValue={false} />
            <ExpressionOrValueComponent dn={dn} member="inlineCheckbox" type="boolean" defaultValue={false} />
            <ExpressionOrValueComponent dn={dn} member="textArea" type="boolean" defaultValue={false} />
            <ExpressionOrValueComponent dn={dn} member="autoTrim" type="boolean" defaultValue={true} />
            <ExpressionOrValueComponent dn={dn} member="redrawOnChange" type="boolean" defaultValue={false} />
        </div>)
    },
});


export interface EntityBaseNode extends LineBaseNode, ContainerNode {
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

NodeUtils.register<EntityLineNode>({
    kind: "EntityLine",
    group: "Property",
    order: 1,
    isContainer: true,
    hasEntity: true,
    validate: (dn) => NodeUtils.validateFieldMandatory(dn),
    renderTreeNode: NodeUtils.treeNodeKindField,
    render: (dn, ctx) => (<EntityLine {...NodeUtils.getEntityBaseProps(dn, ctx, { showAutoComplete : true }) } />),
    renderDesigner: dn => NodeUtils.designEntityBase(dn, { isCreable: true, isFindable: true, isViewable: true, showAutoComplete: true }),
});


export interface EntityComboNode extends EntityBaseNode {
    kind: "EntityCombo",
}

NodeUtils.register<EntityComboNode>({
    kind: "EntityCombo",
    group: "Property",
    order: 2,
    isContainer: true,
    hasEntity: true,
    validate: (dn) => NodeUtils.validateFieldMandatory(dn),
    renderTreeNode: NodeUtils.treeNodeKindField,
    render: (dn, ctx) => (<EntityCombo {...NodeUtils.getEntityBaseProps(dn, ctx, {}) } />),
    renderDesigner: dn => NodeUtils.designEntityBase(dn, { isCreable: false, isFindable: false, isViewable: false, showAutoComplete: false }),
});

export interface EntityDetailNode extends EntityBaseNode, ContainerNode {
    kind: "EntityDetail",
}

NodeUtils.register<EntityDetailNode>({
    kind: "EntityDetail",
    group: "Property",
    order: 3,
    isContainer: true,
    hasEntity: true,
    validate: (dn) => NodeUtils.validateFieldMandatory(dn),
    renderTreeNode: NodeUtils.treeNodeKindField,
    render: (dn, ctx) => (<EntityDetail {...NodeUtils.getEntityBaseProps(dn, ctx, {}) } />),
    renderDesigner: dn => NodeUtils.designEntityBase(dn, { isCreable: true, isFindable: true, isViewable: false, showAutoComplete: false }),
});


export interface EnumCheckboxListNode extends LineBaseNode {
    kind: "EnumCheckboxList",
    columnCount?: ExpressionOrValue<number>;
    columnWidth?: ExpressionOrValue<number>;
}

NodeUtils.register<EnumCheckboxListNode>({
    kind: "EnumCheckboxList",
    group: "Collection",
    order: 0,
    hasCollection: true,
    validate: (dn) => NodeUtils.validateFieldMandatory(dn),
    renderTreeNode: NodeUtils.treeNodeKindField,
    render: (dn, ctx) => (<EnumCheckboxList
        ctx={ctx.subCtx(NodeUtils.asFieldFunction(dn.node.field))}
        labelText={NodeUtils.evaluateAndValidate(ctx, dn, "labelText", NodeUtils.isStringOrNull)}
        readOnly={NodeUtils.evaluateAndValidate(ctx, dn, "readOnly", NodeUtils.isBooleanOrNull)}
        columnCount={NodeUtils.evaluateAndValidate(ctx, dn, "columnCount", NodeUtils.isNumberOrNull)}
        columnWidth={NodeUtils.evaluateAndValidate(ctx, dn, "columnWidth", NodeUtils.isNumberOrNull)}
        onChange={NodeUtils.evaluateOnChange(ctx, dn.node.redrawOnChange)}
        />),
    renderDesigner: (dn) => {
        const m = dn.route && dn.route.member;
        return (<div>
            <FieldComponent dn={dn} member="field" />
            <ExpressionOrValueComponent dn={dn} member="labelText" type="string" defaultValue={m && m.niceName || ""} />
            <ExpressionOrValueComponent dn={dn} member="readOnly" type="boolean" defaultValue={false} />
            <ExpressionOrValueComponent dn={dn} member="columnCount" type="number" defaultValue={null} />
            <ExpressionOrValueComponent dn={dn} member="columnWidth" type="number" defaultValue={200} />
            <ExpressionOrValueComponent dn={dn} member="redrawOnChange" type="boolean" defaultValue={false} />
        </div>)
    },
});

export interface EntityListBaseNode extends EntityBaseNode {
    move?: ExpressionOrValue<boolean>;
}

export interface EntityCheckboxListNode extends EntityListBaseNode {
    kind: "EntityCheckboxList",
    columnCount?: ExpressionOrValue<number>;
    columnWidth?: ExpressionOrValue<number>;
}

NodeUtils.register<EntityCheckboxListNode>({
    kind: "EntityCheckboxList",
    group: "Collection",
    order: 1,
    hasEntity: true,
    hasCollection: true,
    validate: (dn) => NodeUtils.validateFieldMandatory(dn),
    renderTreeNode: NodeUtils.treeNodeKindField,
    render: (dn, ctx) => (<EntityCheckboxList {...NodeUtils.getEntityBaseProps(dn, ctx, { showMove: false }) }
        columnCount={NodeUtils.evaluateAndValidate(ctx, dn, "columnCount", NodeUtils.isNumberOrNull)}
        columnWidth={NodeUtils.evaluateAndValidate(ctx, dn, "columnWidth", NodeUtils.isNumberOrNull)}
        />),
    renderDesigner: dn => <div>
        {NodeUtils.designEntityBase(dn, { isCreable: false, isFindable: false, isViewable: false, showAutoComplete: false, showMove: false })}
        <ExpressionOrValueComponent dn={dn} member="columnCount" type="number" defaultValue={null} />
        <ExpressionOrValueComponent dn={dn} member="columnWidth" type="number" defaultValue={200} />
    </div>
});

export interface EntityListNode extends EntityListBaseNode {
    kind: "EntityList",
}

NodeUtils.register<EntityListNode>({
    kind: "EntityList",
    group: "Collection",
    order: 2,
    isContainer: true,
    hasEntity: true,
    hasCollection: true,
    validate: (dn) => NodeUtils.validateFieldMandatory(dn),
    renderTreeNode: NodeUtils.treeNodeKindField,
    render: (dn, ctx) => (<EntityList {...NodeUtils.getEntityBaseProps(dn, ctx, { showMove: true }) } />),
    renderDesigner: dn => NodeUtils.designEntityBase(dn, { isCreable: true, isFindable: true, isViewable: true, showAutoComplete: false, showMove: true })
});


export interface EntityStripNode extends EntityListBaseNode {
    kind: "EntityStrip",
    autoComplete?: ExpressionOrValue<boolean>;
}

NodeUtils.register<EntityStripNode>({
    kind: "EntityStrip",
    group: "Collection",
    order: 3,
    isContainer: true,
    hasEntity: true,
    hasCollection: true,
    validate: (dn) => NodeUtils.validateFieldMandatory(dn),
    renderTreeNode: NodeUtils.treeNodeKindField,
    render: (dn, ctx) => (<EntityStrip
        {...NodeUtils.getEntityBaseProps(dn, ctx, { showAutoComplete: true, showMove: false }) }
        vertical={NodeUtils.evaluateAndValidate(ctx, dn, "vertical", NodeUtils.isBooleanOrNull)}
        />),
    renderDesigner: dn =>
        <div>
            {NodeUtils.designEntityBase(dn, { isCreable: false, isFindable: false, isViewable: true, showAutoComplete: true, showMove: false })}
            <ExpressionOrValueComponent dn={dn} member="vertical" type="boolean" defaultValue={false} />
        </div>
});


export interface EntityRepeaterNode extends EntityListBaseNode {
    kind: "EntityRepeater",
}

NodeUtils.register<EntityRepeaterNode>({
    kind: "EntityRepeater",
    group: "Collection",
    order: 4,
    isContainer: true,
    hasEntity: true,
    hasCollection: true,
    validate: (dn) => NodeUtils.validateFieldMandatory(dn),
    renderTreeNode: NodeUtils.treeNodeKindField,
    render: (dn, ctx) => (<EntityRepeater {...NodeUtils.getEntityBaseProps(dn, ctx, { showMove: true }) } />),
    renderDesigner: dn => NodeUtils.designEntityBase(dn, { isCreable: true, isFindable: true, isViewable: false, showAutoComplete: false, showMove: true })
});

export interface EntityTabRepeaterNode extends EntityListBaseNode {
    kind: "EntityTabRepeater",
}

NodeUtils.register<EntityTabRepeaterNode>({
    kind: "EntityTabRepeater",
    group: "Collection",
    order: 5,
    isContainer: true,
    hasEntity: true,
    hasCollection: true,
    validate: (dn) => NodeUtils.validateFieldMandatory(dn),
    renderTreeNode: NodeUtils.treeNodeKindField,
    render: (dn, ctx) => (<EntityTabRepeater {...NodeUtils.getEntityBaseProps(dn, ctx, { showMove: true }) } />),
    renderDesigner: dn => NodeUtils.designEntityBase(dn, { isCreable: true, isFindable: true, isViewable: false, showAutoComplete: false, showMove: true })
});



export interface EntityTableNode extends EntityListBaseNode {
    kind: "EntityTable",
}

NodeUtils.register<EntityTableNode>({
    kind: "EntityTable",
    group: "Collection",
    order: 6,
    isContainer: true,
    hasEntity: true,
    hasCollection: true,
    validChild: "EntityTableColumn",
    validate: (dn) => NodeUtils.validateFieldMandatory(dn),
    renderTreeNode: NodeUtils.treeNodeKindField,

    render: (dn, ctx) => (<EntityTable
        columns={dn.node.children.filter(c => NodeUtils.validate(dn.createChild(c)) == null).map((col: EntityTableColumnNode) => NodeUtils.render(dn.createChild(col), ctx) as any)}
        {...NodeUtils.getEntityBaseProps(dn, ctx, { showMove: true }) } />),

    renderDesigner: dn => <div>
        {NodeUtils.designEntityBase(dn, { isCreable: true, isFindable: true, isViewable: false, showAutoComplete: false, showMove: true })}
    </div>
});

export interface EntityTableColumnNode extends ContainerNode {
    kind: "EntityTableColumn",
    property?: string;
    header?: string;
    width?: string;
}

NodeUtils.register<EntityTableColumnNode>({
    kind: "EntityTableColumn",
    group: null,
    order: null,
    isContainer: true,
    avoidHighlight: true,
    validParent: "EntityTable",
    validate: (dn) => dn.node.property ? NodeUtils.validateTableColumnProperty(dn) : NodeUtils.mandatory(dn, "header"),
    renderTreeNode: NodeUtils.treeNodeTableColumnProperty,
    render: (dn, ctx) => ({
        property: dn.node.property && NodeUtils.asFieldFunction(dn.node.property),
        header: NodeUtils.evaluateAndValidate(ctx, dn, "header", NodeUtils.isStringOrNull),
        headerProps: dn.node.width ? { style: { width: dn.node.width } } : undefined,
        template: dn.node.children && dn.node.children.length > 0 ? NodeUtils.getGetComponent(dn, ctx) : undefined
    }) as EntityTableColumn<ModifiableEntity> as any, //HACK
    renderDesigner: dn => <div>
        <FieldComponent dn={dn} member="property" />
        <ExpressionOrValueComponent dn={dn} member="header" type="string" defaultValue={null} />
        <ExpressionOrValueComponent dn={dn} member="width" type="string" defaultValue={null} />
    </div>
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

    function notImplemented(what: string): never {
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
            if (tr.isEmbedded || ti!.entityKind == "Part" || ti!.entityKind == "SharedPart")
                return { kind: "EntityRepeater", field, children: [] } as EntityRepeaterNode;
            else if (ti!.isLowPopulation)
                return { kind: "EntityCheckboxList", field, children: [] } as EntityCheckboxListNode;
            else
                return { kind: "EntityStrip", field, children: [] } as EntityStripNode;
        }

        if (tr.name == "[ALL]")
            return { kind: "EntityLine", field, children: [] } as EntityLineNode;

        if (ti) {
            if (ti.kind == "Enum")
                return { kind: "ValueLine", field } as ValueLineNode;

            if (ti.entityKind == "Part" || ti.entityKind == "SharedPart")
                return { kind: "EntityDetail", field, children: [] } as EntityDetailNode;

            if (ti.isLowPopulation)
                return { kind: "EntityCombo", field, children: [] } as EntityComboNode;

            return { kind: "EntityLine", field, children: [] } as EntityLineNode;
        }

        if (tr.isEmbedded)
            return { kind: "EntityDetail", field, children: [] } as EntityDetailNode;

        return { kind: "ValueLine", field } as ValueLineNode;
    }
}

