import * as React from 'react'
import { Tabs, Tab } from 'react-bootstrap'
import {
    FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTabRepeater, EntityTable,
    EntityCheckboxList, EnumCheckboxList, EntityDetail, EntityStrip
} from '../../../../Framework/Signum.React/Scripts/Lines'
import { ModifiableEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { classes, Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { FindOptions, SearchControl, CountSearchControl, CountSearchControlLayout } from '../../../../Framework/Signum.React/Scripts/Search'
import {
    getQueryNiceName, TypeInfo, MemberInfo, getTypeInfo, EntityData, EntityKind, getTypeInfos, KindOfType,
    PropertyRoute, PropertyRouteType, LambdaMemberType, isTypeEntity, Binding
} from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { EntityBase, EntityBaseProps } from '../../../../Framework/Signum.React/Scripts/Lines/EntityBase'
import { EntityTableColumn } from '../../../../Framework/Signum.React/Scripts/Lines/EntityTable'
import { DynamicViewValidationMessage } from '../Signum.Entities.Dynamic'
import { ExpressionOrValueComponent, FieldComponent } from './Designer'
import { ExpressionOrValue } from './NodeUtils'
import { FindOptionsLine } from './FindOptionsComponent'
import { HtmlAttributesLine } from './HtmlAttributesComponent'
import { StyleOptionsLine } from './StyleOptionsComponent'
import * as NodeUtils from './NodeUtils'
import { toFindOptions, FindOptionsExpr } from './FindOptionsExpression'
import { toHtmlAttributes, HtmlAttributesExpression, withClassName } from './HtmlAttributesExpression'
import { toStyleOptions, subCtx, StyleOptionsExpression } from './StyleOptionsExpression'

export interface BaseNode {
    kind: string;
    visible?: ExpressionOrValue<boolean>;
}

export interface ContainerNode extends BaseNode {
    children: BaseNode[]
}

export interface DivNode extends ContainerNode {
    kind: "Div",
    field?: string;
    styleOptions?: StyleOptionsExpression;
    htmlAttributes?: HtmlAttributesExpression;
}

NodeUtils.register<DivNode>({
    kind: "Div",
    group: "Container",
    order: 0,
    isContainer: true,
    renderTreeNode: NodeUtils.treeNodeKind,
    render: (dn, parentCtx) => NodeUtils.withChildrens(dn, parentCtx, <div {...toHtmlAttributes(parentCtx, dn.node.htmlAttributes) } />),
    renderDesigner: dn => (<div>
        <FieldComponent dn={dn} binding={Binding.create(dn.node, n => n.field)} />
        <StyleOptionsLine dn={dn} binding={Binding.create(dn.node, n => n.styleOptions)} />
        <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.htmlAttributes)} />
    </div>),
});

export interface RowNode extends ContainerNode {
    kind: "Row", 
    field?: string;
    styleOptions?: StyleOptionsExpression;
    htmlAttributes?: HtmlAttributesExpression;
}

NodeUtils.register<RowNode>({
    kind: "Row",
    group: "Container",
    order: 1,
    isContainer: true,
    validChild: "Column",
    renderTreeNode: NodeUtils.treeNodeKind, 
    render: (dn, parentCtx) => NodeUtils.withChildrens(dn, parentCtx, <div {...withClassName(toHtmlAttributes(parentCtx, dn.node.htmlAttributes), "row") } />),
    renderDesigner: dn => (<div>
        <FieldComponent dn={dn} binding={Binding.create(dn.node, n => n.field)} />
        <StyleOptionsLine dn={dn} binding={Binding.create(dn.node, n => n.styleOptions)} />
        <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.htmlAttributes)} />
    </div>),
});


export interface ColumnNode extends ContainerNode {
    kind: "Column";
    field?: string;
    styleOptions?: StyleOptionsExpression;
    htmlAttributes?: HtmlAttributesExpression;
    width: ExpressionOrValue<number>;
    offset: ExpressionOrValue<number>;
}

NodeUtils.register<ColumnNode>({
    kind: "Column",
    group: null,
    order: null,
    isContainer: true,
    avoidHighlight: true,
    validParent: "Row",
    validate: dn => NodeUtils.mandatory(dn, n => n.width),
    initialize: dn => dn.width = 6,
    renderTreeNode: NodeUtils.treeNodeKind, 
    render: (dn, parentCtx) => {
        const column = NodeUtils.evaluateAndValidate(parentCtx, dn.node, n => n.width, NodeUtils.isNumber);
        const offset = NodeUtils.evaluateAndValidate(parentCtx, dn.node, n => n.offset, NodeUtils.isNumberOrNull);
        const className = classes("col-sm-" + column, offset != undefined && "col-sm-offset-" + offset)

        return NodeUtils.withChildrens(dn, parentCtx, <div {...withClassName(toHtmlAttributes(parentCtx, dn.node.htmlAttributes), className)} />);
    },
    renderDesigner: (dn) => (<div>
        <FieldComponent dn={dn} binding={Binding.create(dn.node, n => n.field)} />
        <StyleOptionsLine dn={dn} binding={Binding.create(dn.node, n => n.styleOptions)} />
        <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.htmlAttributes)} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.width)} type="string" options={[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]} defaultValue={null} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.offset)} type="string" options={[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]} defaultValue={null} />
    </div>),
});

export interface TabsNode extends ContainerNode {
    kind: "Tabs";
    field?: string;
    styleOptions?: StyleOptionsExpression;
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
    render: (dn, parentCtx) => {
        return NodeUtils.withChildrens(dn, parentCtx, <Tabs id={parentCtx.compose(NodeUtils.evaluateAndValidate(parentCtx, dn.node, n => n.id, NodeUtils.isString) !)} />);
    },
    renderDesigner: (dn) => (<div>
        <FieldComponent dn={dn} binding={Binding.create(dn.node, n => n.field)} />
        <StyleOptionsLine dn={dn} binding={Binding.create(dn.node, n => n.styleOptions)} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.id)} type="string" defaultValue={null} />
    </div>),
});


export interface TabNode extends ContainerNode {
    kind: "Tab";
    field?: string;
    styleOptions?: StyleOptionsExpression;
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
    render: (dn, parentCtx) => {
        return NodeUtils.withChildrens(dn, parentCtx, <Tab title={NodeUtils.evaluateAndValidate(parentCtx, dn.node, n => n.title, NodeUtils.isString)} />);
    },
    renderDesigner: (dn) => (<div>
        <FieldComponent dn={dn} binding={Binding.create(dn.node, n => n.field)} />
        <StyleOptionsLine dn={dn} binding={Binding.create(dn.node, n => n.styleOptions)} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.title)} type="string" defaultValue={null} />
    </div>),
});


export interface FieldsetNode extends ContainerNode {
    kind: "Fieldset";
    field?: string;
    styleOptions?: StyleOptionsExpression;
    htmlAttributes?: HtmlAttributesExpression;
    legendHtmlAttributes?: HtmlAttributesExpression;
    legend: ExpressionOrValue<string>;
}

NodeUtils.register<FieldsetNode>({
    kind: "Fieldset",
    group: "Container",
    order: 3,
    isContainer: true,
    initialize: dn => dn.legend = "My Fieldset",
    renderTreeNode: NodeUtils.treeNodeKind,
    render: (dn, parentCtx) => {
        return (
            <fieldset {...toHtmlAttributes(parentCtx, dn.node.htmlAttributes) }>
            <legend {...toHtmlAttributes(parentCtx, dn.node.legendHtmlAttributes) }>
                    {NodeUtils.evaluateAndValidate(parentCtx, dn.node, n => n.legend, NodeUtils.isString)}
            </legend>
            {NodeUtils.withChildrens(dn,  parentCtx, <div />)}
        </fieldset>)
    },
    renderDesigner: (dn) => (<div>
        <FieldComponent dn={dn} binding={Binding.create(dn.node, n => n.field)} />
        <StyleOptionsLine dn={dn} binding={Binding.create(dn.node, n => n.styleOptions)} />
        <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.htmlAttributes)} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.legend)} type="string" defaultValue={null} />
        <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.legendHtmlAttributes)} />
    </div>),
});



export interface TextNode extends BaseNode {
    kind: "Text",
    htmlAttributes?: HtmlAttributesExpression;
    breakLines?: ExpressionOrValue<boolean>
    tagName?: ExpressionOrValue<string>;
    message: ExpressionOrValue<string>;
}

NodeUtils.register<TextNode>({
    kind: "Text",
    group: "Container",
    order: 4,
    initialize: dn => { dn.message = "My message"; },
    renderTreeNode: dn => <span><small>{dn.node.kind}:</small> <strong>{(typeof dn.node.message == "string" ? dn.node.message : (dn.node.message.code || "")).etc(20)}</strong></span>,
    render: (dn, ctx) => React.createElement(
        NodeUtils.evaluateAndValidate(ctx, dn.node, n => n.tagName, NodeUtils.isStringOrNull) || "p",
        toHtmlAttributes(ctx, dn.node.htmlAttributes),
        ...NodeUtils.addBreakLines(
            NodeUtils.evaluateAndValidate(ctx, dn.node, n => n.breakLines, NodeUtils.isBooleanOrNull) || false,
            NodeUtils.evaluateAndValidate(ctx, dn.node, n => n.message, NodeUtils.isString) !),
    ),
    renderDesigner: dn => (<div>
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.tagName)} type="string" defaultValue={"p"} options={["p", "span", "div", "pre", "code", "strong", "em", "del", "sub", "sup", "ins", "h1", "h2", "h3", "h4", "h5"]} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.message)} type="textArea" defaultValue={null} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.breakLines)} type="boolean" defaultValue={false} />
        <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.htmlAttributes)} />
    </div>),
});

export interface LineBaseNode extends BaseNode {
    labelText?: ExpressionOrValue<string>;
    field: string;
    styleOptions?: StyleOptionsExpression;
    readOnly?: ExpressionOrValue<boolean>;
    redrawOnChange?: boolean;
    labelHtmlAttributes?: HtmlAttributesExpression;
    formGroupHtmlAttributes?: HtmlAttributesExpression;
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
        labelText={NodeUtils.evaluateAndValidate(ctx, dn.node, n => n.labelText, NodeUtils.isStringOrNull)}
        labelHtmlProps={toHtmlAttributes(ctx, dn.node.labelHtmlAttributes)}
        formGroupHtmlProps={toHtmlAttributes(ctx, dn.node.formGroupHtmlAttributes)}
        unitText={NodeUtils.evaluateAndValidate(ctx, dn.node, n => n.unitText, NodeUtils.isStringOrNull)}
        formatText={NodeUtils.evaluateAndValidate(ctx, dn.node, n => n.formatText, NodeUtils.isStringOrNull)}
        readOnly={NodeUtils.evaluateAndValidate(ctx, dn.node, n => n.readOnly, NodeUtils.isBooleanOrNull)}
        inlineCheckbox={NodeUtils.evaluateAndValidate(ctx, dn.node, n => n.inlineCheckbox, NodeUtils.isBooleanOrNull)}
        valueLineType={NodeUtils.evaluateAndValidate(ctx, dn.node, n => n.textArea, NodeUtils.isBooleanOrNull) ? ValueLineType.TextArea : undefined}
        autoTrim={NodeUtils.evaluateAndValidate(ctx, dn.node, n => n.autoTrim, NodeUtils.isBooleanOrNull)}
        onChange={NodeUtils.evaluateOnChange(ctx, dn)}
        />),
    renderDesigner: (dn) => {
        const m = dn.route && dn.route.member;
        return (<div>
            <FieldComponent dn={dn} binding={Binding.create(dn.node, n => n.field)} />
            <StyleOptionsLine dn={dn} binding={Binding.create(dn.node, n => n.styleOptions)} />
            <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.labelText)} type="string" defaultValue={m && m.niceName || ""} />
            <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.labelHtmlAttributes)} />
            <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.formGroupHtmlAttributes)} />
            <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.unitText)} type="string" defaultValue={m && m.unit || ""} />
            <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.formatText)} type="string" defaultValue={m && m.format || ""} />
            <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.readOnly)} type="boolean" defaultValue={null} />
            <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.inlineCheckbox)} type="boolean" defaultValue={false} />
            <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.textArea)} type="boolean" defaultValue={false} />
            <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.autoTrim)} type="boolean" defaultValue={true}  />
            <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.redrawOnChange)} type="boolean" defaultValue={false} />
        </div>)
    },
});


export interface EntityBaseNode extends LineBaseNode, ContainerNode {
    create?: ExpressionOrValue<boolean>;
    find?: ExpressionOrValue<boolean>;
    remove?: ExpressionOrValue<boolean>;
    view?: ExpressionOrValue<boolean>;
    viewOnCreate?: ExpressionOrValue<boolean>;
    findOptions?: FindOptionsExpr;
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
    validate: (dn) => NodeUtils.validateEntityBase(dn),
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
    validate: (dn) => NodeUtils.validateEntityBase(dn),
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
    validate: (dn) => NodeUtils.validateEntityBase(dn),
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
        labelText={NodeUtils.evaluateAndValidate(ctx, dn.node, n => n.labelText, NodeUtils.isStringOrNull)}
        readOnly={NodeUtils.evaluateAndValidate(ctx, dn.node, n => n.readOnly, NodeUtils.isBooleanOrNull)}
        columnCount={NodeUtils.evaluateAndValidate(ctx, dn.node, n => n.columnCount, NodeUtils.isNumberOrNull)}
        columnWidth={NodeUtils.evaluateAndValidate(ctx, dn.node, n => n.columnWidth, NodeUtils.isNumberOrNull)}
        onChange={NodeUtils.evaluateOnChange(ctx, dn)}
        />),
    renderDesigner: (dn) => {
        const m = dn.route && dn.route.member;
        return (<div>
            <FieldComponent dn={dn} binding={Binding.create(dn.node, n => n.field)} />
            <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.labelText)} type="string" defaultValue={m && m.niceName || ""} />
            <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.readOnly)} type="boolean" defaultValue={null} />
            <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.columnCount)} type="number" defaultValue={null} />
            <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.columnWidth)} type="number" defaultValue={200} />
            <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.redrawOnChange)} type="boolean" defaultValue={null} />
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
    validate: (dn) => NodeUtils.validateEntityBase(dn),
    renderTreeNode: NodeUtils.treeNodeKindField,
    render: (dn, ctx) => (<EntityCheckboxList {...NodeUtils.getEntityBaseProps(dn, ctx, { showMove: false }) }
        columnCount={NodeUtils.evaluateAndValidate(ctx, dn.node, n => n.columnCount, NodeUtils.isNumberOrNull)}
        columnWidth={NodeUtils.evaluateAndValidate(ctx, dn.node, n => n.columnWidth, NodeUtils.isNumberOrNull)}
        />),
    renderDesigner: dn => <div>
        {NodeUtils.designEntityBase(dn, { isCreable: false, isFindable: false, isViewable: false, showAutoComplete: false, showMove: false })}
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.columnCount)} type="number" defaultValue={null} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.columnWidth)} type="number" defaultValue={200} />
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
    validate: (dn) => NodeUtils.validateEntityBase(dn),
    renderTreeNode: NodeUtils.treeNodeKindField,
    render: (dn, ctx) => (<EntityList {...NodeUtils.getEntityBaseProps(dn, ctx, { showMove: true }) } />),
    renderDesigner: dn => NodeUtils.designEntityBase(dn, { isCreable: true, isFindable: true, isViewable: true, showAutoComplete: false, showMove: true })
});


export interface EntityStripNode extends EntityListBaseNode {
    kind: "EntityStrip",
    autoComplete?: ExpressionOrValue<boolean>;
    vertical?: boolean;
}

NodeUtils.register<EntityStripNode>({
    kind: "EntityStrip",
    group: "Collection",
    order: 3,
    isContainer: true,
    hasEntity: true,
    hasCollection: true,
    validate: (dn) => NodeUtils.validateEntityBase(dn),
    renderTreeNode: NodeUtils.treeNodeKindField,
    render: (dn, ctx) => (<EntityStrip
        {...NodeUtils.getEntityBaseProps(dn, ctx, { showAutoComplete: true, showMove: false }) }
        vertical={NodeUtils.evaluateAndValidate(ctx, dn.node, n => n.vertical, NodeUtils.isBooleanOrNull)}
        />),
    renderDesigner: dn =>
        <div>
            {NodeUtils.designEntityBase(dn, { isCreable: false, isFindable: false, isViewable: true, showAutoComplete: true, showMove: false })}
            <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.vertical)} type="boolean" defaultValue={false} />
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
    validate: (dn) => NodeUtils.validateEntityBase(dn),
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
    validate: (dn) => NodeUtils.validateEntityBase(dn),
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
    validate: (dn) => NodeUtils.validateEntityBase(dn),
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
    headerHtmlAttributes?: HtmlAttributesExpression,
    cellHtmlAttributes?: HtmlAttributesExpression,
}

NodeUtils.register<EntityTableColumnNode>({
    kind: "EntityTableColumn",
    group: null,
    order: null,
    isContainer: true,
    avoidHighlight: true,
    validParent: "EntityTable",
    validate: (dn) => dn.node.property ? NodeUtils.validateTableColumnProperty(dn) : NodeUtils.mandatory(dn, n => n.header),
    renderTreeNode: NodeUtils.treeNodeTableColumnProperty,
    render: (dn, ctx) => ({
        property: dn.node.property && NodeUtils.asFieldFunction(dn.node.property),
        header: NodeUtils.evaluateAndValidate(ctx, dn.node, n => n.header, NodeUtils.isStringOrNull),
        headerProps: toHtmlAttributes(ctx, dn.node.headerHtmlAttributes),
        cellProps: toHtmlAttributes(ctx, dn.node.cellHtmlAttributes),
        template: dn.node.children && dn.node.children.length > 0 ? NodeUtils.getGetComponent(dn, ctx) : undefined
    }) as EntityTableColumn<ModifiableEntity> as any, //HACK
    renderDesigner: dn => <div>
        <FieldComponent dn={dn} binding={Binding.create(dn.node, n => n.property)} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.header)} type="string" defaultValue={null} />
        <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.headerHtmlAttributes)} />
        <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.cellHtmlAttributes)} />
    </div>
});

export interface SearchControlNode extends BaseNode {
    kind: "SearchControl",
    findOptions?: FindOptionsExpr;
}

NodeUtils.register<SearchControlNode>({
    kind: "SearchControl",
    group: "Search",
    order: 1,
    validate: (dn) => NodeUtils.mandatory(dn, n => n.findOptions) || dn.node.findOptions && NodeUtils.validateFindOptions(dn.node.findOptions),
    renderTreeNode: dn => <span><small>SearchControl:</small><strong>{dn.node.findOptions && dn.node.findOptions.queryKey || " - " }</strong></span>,
    render: (dn, ctx) => <div><SearchControl findOptions={toFindOptions(ctx, dn.node.findOptions!)} /> </div>,
    renderDesigner: dn => <div>
        <FindOptionsLine dn={dn} binding={Binding.create(dn.node, a => a.findOptions)} />
    </div>
});

export interface CountSearchControlNode extends BaseNode {
    kind: "CountSearchControl",
    findOptions?: FindOptionsExpr;
    labelText?: ExpressionOrValue<string>;
    labelHtmlAttributes?: HtmlAttributesExpression,
    layout?: ExpressionOrValue<CountSearchControlLayout>;
    formGroupHtmlAttributes?: HtmlAttributesExpression,
}

NodeUtils.register<CountSearchControlNode>({
    kind: "CountSearchControl",
    group: "Search",
    order: 1,
    validate: (dn) => NodeUtils.mandatory(dn, n => n.findOptions) || dn.node.findOptions && NodeUtils.validateFindOptions(dn.node.findOptions),
    renderTreeNode: dn => <span><small>CountSearchControl:</small><strong>{dn.node.findOptions && dn.node.findOptions.queryKey || " - "}</strong></span>,
    render: (dn, ctx) => <div><CountSearchControl ctx={ctx}
        findOptions={toFindOptions(ctx, dn.node.findOptions!)}
        labelText={NodeUtils.evaluateAndValidate(ctx, dn.node, n => n.labelText, NodeUtils.isStringOrNull)}
        layout={NodeUtils.evaluateAndValidate(ctx, dn.node, n => n.layout, NodeUtils.isStringOrNull)}
        labelProps={toHtmlAttributes(ctx, dn.node.labelHtmlAttributes)}
        formGroupHtmlProps={toHtmlAttributes(ctx, dn.node.formGroupHtmlAttributes)}
        /> </div>,
    renderDesigner: dn => <div>
        <FindOptionsLine dn={dn} binding={Binding.create(dn.node, a => a.findOptions)} />
        <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.labelHtmlAttributes)} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.labelText)} type="string" defaultValue={null} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.layout)} type="string" defaultValue={null} options={["View", "Link", "Badge", "Span"]} />
        <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.formGroupHtmlAttributes)} />
    </div>
});

export namespace NodeConstructor {

    export function createDefaultNode(ti: TypeInfo) {
        return {
            kind: "Div",
            children: Dic.getValues(ti.members).filter(mi => mi.name != "Id" && !mi.name.contains(".") && !mi.name.contains("/")).map(mi => appropiateComponent(mi)).filter(a => !!a).map(a => a!)
        } as DivNode;
    }

    export const specificComponents: {
        [typeName: string]: (ctx: MemberInfo) => BaseNode | undefined;
    } = {};

    export var appropiateComponent = (mi: MemberInfo): BaseNode | undefined => {
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

        if (ValueLine.getValueLineType(tr) != undefined)
            return { kind: "ValueLine", field } as ValueLineNode;

        return undefined;
    }
}

