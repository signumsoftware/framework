import * as React from 'react'
import { ValueLine, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTabRepeater, EntityTable,
  EntityCheckboxList, EnumCheckboxList, EntityDetail, EntityStrip, RenderEntity, MultiValueLine, AutocompleteConfig, 
} from '@framework/Lines'
import { ModifiableEntity, Entity, Lite, isEntity, EntityPack } from '@framework/Signum.Entities'
import { classes, Dic } from '@framework/Globals'
import { SubTokensOptions } from '@framework/FindOptions'
import { SearchControl, SearchValueLine, FindOptionsParsed, ResultTable, SearchControlLoaded } from '@framework/Search'
import { TypeInfo, MemberInfo, getTypeInfo, tryGetTypeInfos, PropertyRoute, isTypeEntity, Binding, IsByAll, getAllTypes } from '@framework/Reflection'
import * as AppContext from '@framework/AppContext'
import * as Navigator from '@framework/Navigator'
import { TypeContext, ButtonBarElement } from '@framework/TypeContext'
import { EntityTableColumn } from '@framework/Lines/EntityTable'
import { DynamicViewValidationMessage } from '../Signum.Entities.Dynamic'
import { ExpressionOrValueComponent, FieldComponent } from './Designer'
import { ExpressionOrValue, Expression, bindExpr, toCodeEx, withClassNameEx, DesignerNode } from './NodeUtils'
import { FindOptionsLine, QueryTokenLine, ViewNameComponent, FetchQueryDescription } from './FindOptionsComponent'
import { HtmlAttributesLine } from './HtmlAttributesComponent'
import { StyleOptionsLine } from './StyleOptionsComponent'
import * as NodeUtils from './NodeUtils'
import { registeredCustomContexts, API } from '../DynamicViewClient'
import { toFindOptions, FindOptionsExpr } from './FindOptionsExpression'
import { toHtmlAttributes, HtmlAttributesExpression, withClassName } from './HtmlAttributesExpression'
import { toStyleOptions, StyleOptionsExpression } from './StyleOptionsExpression'
import { FileLine } from "../../Files/FileLine";
import { MultiFileLine } from "../../Files/MultiFileLine";
import { DownloadBehaviour } from "../../Files/FileDownloader";
import { registerSymbol } from "@framework/Reflection";
import { BsColor, BsSize } from '@framework/Components';
import { Tab, Tabs, Button } from 'react-bootstrap';
import { FileImageLine } from '../../Files/FileImageLine';
import { FileEntity, FilePathEntity, FileEmbedded, FilePathEmbedded } from '../../Files/Signum.Entities.Files';
import { ColorTypeahead } from '../../Basics/Templates/ColorTypeahead';
import { IconTypeahead, parseIcon } from '../../Basics/Templates/IconTypeahead';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { EntityOperationContext } from '@framework/Operations';
import { OperationButton } from '@framework/Operations/EntityOperations';
import { useAPI } from '@framework/Hooks';
import { ValueLineController } from '@framework/Lines/ValueLine'

export interface BaseNode {
  ref?: Expression<any>;
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
  renderCode: (node, cc) => cc.elementCodeWithChildrenSubCtx("div", node.htmlAttributes, node),
  render: (dn, parentCtx) => NodeUtils.withChildrensSubCtx(dn, parentCtx, <div {...toHtmlAttributes(dn, parentCtx, dn.node.htmlAttributes)} />),
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
  validate: (dn, parentCtx) => parentCtx && dn.node.children.filter(c => c.kind == "Column").map(col =>
    (NodeUtils.evaluate(dn, parentCtx, col, f => (f as ColumnNode).width) ?? 0) +
    (NodeUtils.evaluate(dn, parentCtx, col, f => (f as ColumnNode).offset) ?? 0)
  ).sum() > 12 ? "Sum of Column.width/offset should <= 12" : null,
  renderCode: (node, cc) => cc.elementCodeWithChildrenSubCtx("div", withClassNameEx(node.htmlAttributes, "row"), node),
  render: (dn, parentCtx) => NodeUtils.withChildrensSubCtx(dn, parentCtx, <div {...withClassName(toHtmlAttributes(dn, parentCtx, dn.node.htmlAttributes), "row")} />),
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
  renderCode: (node, cc) => {
    const className = node.offset == null ?
      bindExpr(column => "col-sm-" + column, node.width) :
      bindExpr((column, offset) => classes("col-sm-" + column, offset != undefined && "col-sm-offset-" + offset), node.width, node.offset);

    return cc.elementCodeWithChildrenSubCtx("div", withClassNameEx(node.htmlAttributes, className), node);
  },
  render: (dn, parentCtx) => {
    const column = NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.width, NodeUtils.isNumber);
    const offset = NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.offset, NodeUtils.isNumberOrNull);
    const className = classes("col-sm-" + column, offset != undefined && "col-sm-offset-" + offset)

    return NodeUtils.withChildrensSubCtx(dn, parentCtx, <div {...withClassName(toHtmlAttributes(dn, parentCtx, dn.node.htmlAttributes), className)} />);
  },
  renderDesigner: (dn) => (<div>
    <FieldComponent dn={dn} binding={Binding.create(dn.node, n => n.field)} />
    <StyleOptionsLine dn={dn} binding={Binding.create(dn.node, n => n.styleOptions)} />
    <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.htmlAttributes)} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.width)} type="number" options={[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]} defaultValue={null} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.offset)} type="number" options={[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]} defaultValue={null} />
  </div>),
});

export interface TabsNode extends ContainerNode {
  kind: "Tabs";
  field?: string;
  styleOptions?: StyleOptionsExpression;
  id: ExpressionOrValue<string>;
  defaultActiveKey?: ExpressionOrValue<string>;
  unmountOnExit?: ExpressionOrValue<boolean>;
}

NodeUtils.register<TabsNode>({
  kind: "Tabs",
  group: "Container",
  order: 2,
  isContainer: true,
  validChild: "Tab",
  initialize: dn => dn.id = "tabs",
  renderTreeNode: NodeUtils.treeNodeKind,
  renderCode: (node, cc) => cc.elementCodeWithChildrenSubCtx("Tabs", {
    id: { __code__: cc.ctxName + ".compose(" + toCodeEx(node.id) + ")" } as Expression<string>,
    defaultActiveKey: node.defaultActiveKey,
    unmountOnExit: node.unmountOnExit,
  }, node),
  render: (dn, parentCtx) => {
    return NodeUtils.withChildrensSubCtx(dn, parentCtx, <Tabs
      id={parentCtx.getUniqueId(NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.id, NodeUtils.isString)!)}
      defaultActiveKey={NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.defaultActiveKey, NodeUtils.isStringOrNull)}
      unmountOnExit={NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.unmountOnExit, NodeUtils.isBooleanOrNull)}
    />);
  },
  renderDesigner: (dn) => (<div>
    <FieldComponent dn={dn} binding={Binding.create(dn.node, n => n.field)} />
    <StyleOptionsLine dn={dn} binding={Binding.create(dn.node, n => n.styleOptions)} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.id)} type="string" defaultValue={null} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.defaultActiveKey)} type="string" defaultValue={null} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.unmountOnExit)} type="boolean" defaultValue={false} />
  </div>),
});


export interface TabNode extends ContainerNode {
  kind: "Tab";
  field?: string;
  styleOptions?: StyleOptionsExpression;
  title: ExpressionOrValue<string>;
  eventKey: string;
}

NodeUtils.register<TabNode>({
  kind: "Tab",
  group: null,
  order: null,
  isContainer: true,
  avoidHighlight: true,
  validParent: "Tabs",
  initialize: (n, parentNode) => {
    let byName = (parentNode.node.children.map(a => parseInt((a as TabNode).eventKey.tryAfter("tab") ?? "")).filter(s => isFinite(s)).max() ?? 0) + 1;
    let byPosition = parentNode.node.children.length + 1;
    let index = Math.max(byName, byPosition);
    n.title = "My Tab " + index;
    n.eventKey = "tab" + index;
  },
  renderTreeNode: dn => <span><small>{dn.node.kind}:</small> <strong>{typeof dn.node.title == "string" ? dn.node.title : dn.node.eventKey}</strong></span>,
  validate: dn => NodeUtils.mandatory(dn, n => n.eventKey),
  renderCode: (node, cc) => cc.elementCodeWithChildrenSubCtx("Tab", {
    title: node.title,
    eventKey: node.eventKey
  }, node),
  render: (dn, parentCtx) => {
    return NodeUtils.withChildrensSubCtx(dn, parentCtx, <Tab
      title={NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.title, NodeUtils.isString)}
      eventKey={dn.node.eventKey!} />);
  },
  renderDesigner: (dn) => (<div>
    <FieldComponent dn={dn} binding={Binding.create(dn.node, n => n.field)} />
    <StyleOptionsLine dn={dn} binding={Binding.create(dn.node, n => n.styleOptions)} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.eventKey)} type="string" defaultValue={null} allowsExpression={false} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.title)} type="string" defaultValue={null} />
  </div>),
});


export interface FieldsetNode extends ContainerNode {
  kind: "Fieldset";
  field?: string;
  styleOptions?: StyleOptionsExpression;
  htmlAttributes?: HtmlAttributesExpression;
  legendHtmlAttributes?: HtmlAttributesExpression;
  legend?: ExpressionOrValue<string>;
}

NodeUtils.register<FieldsetNode>({
  kind: "Fieldset",
  group: "Container",
  order: 3,
  isContainer: true,
  initialize: dn => dn.legend = "My Fieldset",
  renderTreeNode: NodeUtils.treeNodeKind,
  renderCode: (node, cc) => cc.elementCode("fieldset", node.htmlAttributes,
    node.legend && cc.elementCode("legend", node.legendHtmlAttributes, toCodeEx(node.legend)),
    cc.elementCodeWithChildrenSubCtx("div", null, node)
  ),
  render: (dn, parentCtx) => {
    return (
      <fieldset {...toHtmlAttributes(dn, parentCtx, dn.node.htmlAttributes)}>
        {dn.node.legend &&
          <legend {...toHtmlAttributes(dn, parentCtx, dn.node.legendHtmlAttributes)}>
            {NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.legend, NodeUtils.isStringOrNull)}
          </legend>}
        {NodeUtils.withChildrensSubCtx(dn, parentCtx, <div />)}
      </fieldset>
    )
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
  renderTreeNode: dn => <span><small>{dn.node.kind}:</small> <strong>{dn.node.message ? (typeof dn.node.message == "string" ? dn.node.message : (dn.node.message.__code__ ?? "")).etc(20) : ""}</strong></span>,
  renderCode: (node, cc) => cc.elementCode(bindExpr(tagName => tagName ?? "p", node.tagName), node.htmlAttributes,
    toCodeEx(node.message)
  ),
  render: (dn, ctx) => React.createElement(
    NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.tagName, NodeUtils.isStringOrNull) ?? "p",
    toHtmlAttributes(dn, ctx, dn.node.htmlAttributes),
    ...NodeUtils.addBreakLines(
      NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.breakLines, NodeUtils.isBooleanOrNull) || false,
      NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.message, NodeUtils.isString)!),
  ),
  renderDesigner: dn => (<div>
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.tagName)} type="string" defaultValue={"p"} options={["p", "span", "div", "pre", "code", "strong", "em", "del", "sub", "sup", "ins", "h1", "h2", "h3", "h4", "h5"]} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.message)} type="textArea" defaultValue={null} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.breakLines)} type="boolean" defaultValue={false} />
    <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.htmlAttributes)} />
  </div>),
});


export interface ImageNode extends BaseNode {
  kind: "Image",
  htmlAttributes?: HtmlAttributesExpression;
  src?: ExpressionOrValue<string>;
}

NodeUtils.register<ImageNode>({
  kind: "Image",
  group: "Container",
  order: 5,
  initialize: dn => { dn.src = "~/images/logo.png"; },
  renderTreeNode: dn => <span><small>{dn.node.kind}:</small> <strong>{dn.node.src ? (typeof dn.node.src == "string" ? dn.node.src : (dn.node.src.__code__ ?? "")).etc(20) : ""}</strong></span>,
  renderCode: (node, cc) => cc.elementCode("img", node.htmlAttributes && { src: node.src }),
  render: (dn, ctx) => <img {...toHtmlAttributes(dn, ctx, dn.node.htmlAttributes)} src={AppContext.toAbsoluteUrl(NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.src, NodeUtils.isString) as string)} />,
  renderDesigner: dn => (<div>
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.src)} type="string" defaultValue={null} />
    <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.htmlAttributes)} />
  </div>),
});


export interface RenderEntityNode extends ContainerNode {
  kind: "RenderEntity";
  field?: string;
  viewName?: ExpressionOrValue<string | ((mod: ModifiableEntity) => string | Navigator.ViewPromise<ModifiableEntity>)>;
  styleOptions?: StyleOptionsExpression;
  onEntityLoaded?: Expression<() => void>;
  extraProps?: Expression<{}>;
}


NodeUtils.register<RenderEntityNode>({
  kind: "RenderEntity",
  group: "Container",
  order: 5,
  isContainer: true,
  hasEntity: true,
  validate: (dn, ctx) => dn.node.field && NodeUtils.validateField(dn as DesignerNode<LineBaseNode>),
  renderTreeNode: dn => <span><small>{dn.node.kind}:</small> <strong>{dn.node.field || (typeof dn.node.viewName == "string" ? dn.node.viewName : "")}</strong></span>,
  renderCode: (node, cc) => cc.elementCode("RenderEntity", {
    ctx: cc.subCtxCode(node.field, node.styleOptions),
    getComponent: cc.getGetComponentEx(node, true),
    getViewName: NodeUtils.toFunctionCode(node.viewName),
    onEntityLoaded: node.onEntityLoaded,
  }),
  render: (dn, ctx) => {
    var styleOptions = toStyleOptions(dn, ctx, dn.node.styleOptions);
    var sctx = dn.node.field ? ctx.subCtx(dn.node.field, styleOptions) :
      styleOptions ? ctx.subCtx(styleOptions) : ctx;
    return (
      <RenderEntity
        ctx={sctx}
        getComponent={NodeUtils.getGetComponent(dn)}
        getViewPromise={NodeUtils.toFunction(NodeUtils.evaluateAndValidate(dn, sctx, dn.node, n => n.viewName, NodeUtils.isFunctionOrStringOrNull))}
        onEntityLoaded={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.onEntityLoaded, NodeUtils.isFunctionOrNull)}
        extraProps={NodeUtils.evaluateAndValidate(dn, sctx, dn.node, n => n.extraProps, NodeUtils.isObjectOrNull)}
      />
    );
  },
  renderDesigner: dn => <div>
    <FieldComponent dn={dn} binding={Binding.create(dn.node, n => n.field)} />
    <StyleOptionsLine dn={dn} binding={Binding.create(dn.node, n => n.styleOptions)} />
    <ViewNameComponent dn={dn} binding={Binding.create(dn.node, n => n.viewName)} typeName={dn.route && dn.route.typeReference().name} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.onEntityLoaded)} type={null} defaultValue={null} exampleExpression={"() => { /* do something here... */ }"} />
    <ExtraPropsComponent dn={dn} />
  </div>,
});

function ExtraPropsComponent({ dn }: { dn: DesignerNode<RenderEntityNode> }) {

  const typeName = dn.route && dn.route.typeReference().name;
  const fixedViewName = dn.route && dn.node.viewName && typeof dn.node.viewName == "string" ? dn.node.viewName : undefined;

  if (typeName && fixedViewName) {
    const es = Navigator.getSettings(typeName);
    const staticViews = ["STATIC"].concat((es?.namedViews && Dic.getKeys(es.namedViews)) ?? []);

    if (!staticViews.contains(fixedViewName)) {
      const viewProps = useAPI(signal => API.getDynamicViewProps(typeName, fixedViewName), [typeName, fixedViewName]);
      if (viewProps && viewProps.length > 0)
        return <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.extraProps)} type={null} defaultValue={null} exampleExpression={"({\r\n" + viewProps!.map(p => `  ${p.name}: null`).join(', \r\n') + "\r\n})"} />
    }
  }

  return <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.extraProps)} type={null} defaultValue={null} />;
}

export interface CustomContextNode extends ContainerNode {
  kind: "CustomContext",
  typeContext: string;
}

NodeUtils.register<CustomContextNode>({
  kind: "CustomContext",
  group: "Container",
  order: 6,
  isContainer: true,
  validate: dn => NodeUtils.mandatory(dn, n => n.typeContext) || (!registeredCustomContexts[dn.node.typeContext] ? `${dn.node.typeContext} not found` : undefined),
  renderTreeNode: dn => <span><small > {dn.node.kind}:</small > <strong>{dn.node.typeContext}</strong></span >,
  renderCode: (node, cc) => {
    const ncc = registeredCustomContexts[node.typeContext].getCodeContext(cc);
    var childrensCode = node.children.map(c => NodeUtils.renderCode(c, ncc));
    return ncc.elementCode("div", null, ...childrensCode);
  },
  render: (dn, parentCtx) => {
    const nctx = registeredCustomContexts[dn.node.typeContext].getTypeContext(parentCtx);
    if (!nctx)
      return undefined;

    return NodeUtils.withChildrensSubCtx(dn, nctx, <div />);
  },
  renderDesigner: dn => (<div>
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.typeContext)} allowsExpression={false} type="string" options={Dic.getKeys(registeredCustomContexts)} defaultValue={null} />
  </div>),
});

export interface TypeIsNode extends ContainerNode {
  kind: "TypeIs",
  typeName: string;
}

NodeUtils.register<TypeIsNode>({
  kind: "TypeIs",
  group: "Container",
  order: 7,
  isContainer: true,
  validate: dn => NodeUtils.mandatory(dn, n => n.typeName) || (!getTypeInfo(dn.node.typeName) ? `Type '${dn.node.typeName}' not found` : undefined),
  renderTreeNode: dn => <span><small> {dn.node.kind}:</small > <strong>{dn.node.typeName}</strong></span>,
  renderCode: (node, cc) => {
    const ncc = cc.createNewContext("ctx" + (cc.usedNames.length + 1));
    cc.assignments[ncc.ctxName] = `${node.typeName}Entity.isInstanceOf(${cc.ctxName}.value) ? ${cc.ctxName}.cast(${node.typeName}) : null`;
    var childrensCode = node.children.map(c => NodeUtils.renderCode(c, ncc));
    return "{" + ncc.ctxName + " && " + ncc.elementCode("div", null, ...childrensCode) + "}";
  },
  render: (dn, parentCtx) => {
    if (!isEntity(parentCtx.value) || parentCtx.value.Type != dn.node.typeName)
      return undefined;

    const nctx = TypeContext.root(parentCtx.value, undefined, parentCtx);

    return NodeUtils.withChildrensSubCtx(dn, nctx, <div />);
  },
  renderDesigner: dn => (<div>
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.typeName)} allowsExpression={false} type="string" options={getTypes(dn.route)} defaultValue={null} />
  </div>),
});

function getTypes(route: PropertyRoute | undefined): string[] | ((query: string) => string[]) {

  if (route == undefined)
    return [];

  var tr = route.typeReference();
  if (tr.name == IsByAll)
    return autoCompleteType;

  var types = tryGetTypeInfos(tr);
  if (types.length == 0 || types[0] == undefined)
    return [];

  return types.map(a => a!.name);
}

function autoCompleteType(query: string): string[] {
  return getAllTypes()
    .filter(ti => ti.kind == "Entity" && ti.name.toLowerCase().contains(query.toLowerCase()))
    .map(a => a.name)
    .orderBy(a => a.length)
    .filter((k, i) => i < 5);
}

export interface LineBaseNode extends BaseNode {
  label?: ExpressionOrValue<string>;
  field: string;
  styleOptions?: StyleOptionsExpression;
  readOnly?: ExpressionOrValue<boolean>;
  onChange?: Expression<() => void>;
  labelHtmlAttributes?: HtmlAttributesExpression;
  formGroupHtmlAttributes?: HtmlAttributesExpression;
  mandatory?: ExpressionOrValue<boolean>;
}

export interface ValueLineNode extends LineBaseNode {
  kind: "ValueLine",
  textArea?: ExpressionOrValue<string>;
  unit?: ExpressionOrValue<string>;
  format?: ExpressionOrValue<string>;
  autoTrim?: ExpressionOrValue<boolean>;
  inlineCheckbox?: ExpressionOrValue<boolean>;
  valueHtmlAttributes?: HtmlAttributesExpression;
  comboBoxItems?: Expression<string[]>;
}

NodeUtils.register<ValueLineNode>({
  kind: "ValueLine",
  group: "Property",
  order: -1,
  validate: (dn) => NodeUtils.validateFieldMandatory(dn),
  renderTreeNode: NodeUtils.treeNodeKindField,
  renderCode: (node, cc) => cc.elementCode("ValueLine", {
    ref: node.ref,
    ctx: cc.subCtxCode(node.field, node.styleOptions),
    label: node.label,
    labelHtmlAttributes: node.labelHtmlAttributes,
    formGroupHtmlAttributes: node.formGroupHtmlAttributes,
    valueHtmlAttributes: node.valueHtmlAttributes,
    unit: node.unit,
    format: node.format,
    readOnly: node.readOnly,
    mandatory: node.mandatory,
    inlineCheckbox: node.inlineCheckbox,
    valueLineType: node.textArea && bindExpr(ta => ta ? "TextArea" : undefined, node.textArea),
    comboBoxItems: node.comboBoxItems,
    autoTrim: node.autoTrim,
    onChange: node.onChange
  }),
  render: (dn, ctx) => (<ValueLine
    //ref={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.ref, NodeUtils.isObjectOrFunctionOrNull)}
    ctx={ctx.subCtx(dn.node.field, toStyleOptions(dn, ctx, dn.node.styleOptions))}
    label={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.label, NodeUtils.isStringOrNull)}
    labelHtmlAttributes={toHtmlAttributes(dn, ctx, dn.node.labelHtmlAttributes)}
    formGroupHtmlAttributes={toHtmlAttributes(dn, ctx, dn.node.formGroupHtmlAttributes)}
    valueHtmlAttributes={toHtmlAttributes(dn, ctx, dn.node.valueHtmlAttributes)}
    unit={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.unit, NodeUtils.isStringOrNull)}
    format={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.format, NodeUtils.isStringOrNull)}
    readOnly={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.readOnly, NodeUtils.isBooleanOrNull)}
    mandatory={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.mandatory, NodeUtils.isBooleanOrNull)}
    inlineCheckbox={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.inlineCheckbox, NodeUtils.isBooleanOrNull)}
    valueLineType={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.textArea, NodeUtils.isBooleanOrNull) ? "TextArea" : undefined}
    optionItems={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.comboBoxItems, NodeUtils.isArrayOrNull)}
    autoFixString={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.autoTrim, NodeUtils.isBooleanOrNull)}
    onChange={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.onChange, NodeUtils.isFunctionOrNull)}
  />),
  renderDesigner: (dn) => {
    const m = dn.route && dn.route.member;
    return (<div>
      {/*<ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.ref)} type={null} defaultValue={true} />*/}
      <FieldComponent dn={dn} binding={Binding.create(dn.node, n => n.field)} />
      <StyleOptionsLine dn={dn} binding={Binding.create(dn.node, n => n.styleOptions)} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.label)} type="string" defaultValue={m?.niceName ?? ""} />
      <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.labelHtmlAttributes)} />
      <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.formGroupHtmlAttributes)} />
      <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.valueHtmlAttributes)} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.unit)} type="string" defaultValue={m?.unit ?? ""} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.format)} type="string" defaultValue={m?.format ?? ""} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.readOnly)} type="boolean" defaultValue={null} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.mandatory)} type="boolean" defaultValue={null} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.inlineCheckbox)} type="boolean" defaultValue={false} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.textArea)} type="boolean" defaultValue={false} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.comboBoxItems)} type={null} defaultValue={null} exampleExpression={`["item1", ...]`} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.autoTrim)} type="boolean" defaultValue={true} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.onChange)} type={null} defaultValue={false} exampleExpression={"/* you must declare 'forceUpdate' in locals */ \r\n() => locals.forceUpdate()"} />
    </div>)
  },
});

export interface MultiValueLineNode extends LineBaseNode {
  kind: "MultiValueLine",
  onRenderItem?: ExpressionOrValue<(ctx: TypeContext<any>) => React.ReactElement<any>>;
  onCreate?: ExpressionOrValue<() => Promise<any[] | any | undefined>>;
  addValueText?: ExpressionOrValue<string>;
}

NodeUtils.register<MultiValueLineNode>({
  kind: "MultiValueLine",
  group: "Property",
  hasCollection: true,
  hasEntity: false,
  order: 0,
  validate: (dn) => NodeUtils.validateFieldMandatory(dn),
  renderTreeNode: NodeUtils.treeNodeKindField,
  renderCode: (node, cc) => cc.elementCode("MultiValueLine", {
    ref: node.ref,
    ctx: cc.subCtxCode(node.field, node.styleOptions),
    onRenderItem: node.onRenderItem,
    onCreate: node.onCreate,
    addValueText: node.addValueText,
    label: node.label,
    labelHtmlAttributes: node.labelHtmlAttributes,
    formGroupHtmlAttributes: node.formGroupHtmlAttributes,
    readOnly: node.readOnly,
    onChange: node.onChange,
  }),
  render: (dn, ctx) => (
    <MultiValueLine
      //ref={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.ref, NodeUtils.isObjectOrFunctionOrNull)}
      ctx={ctx.subCtx(dn.node.field, toStyleOptions(dn, ctx, dn.node.styleOptions))}
      onRenderItem={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.onRenderItem, NodeUtils.isFunctionOrNull)}
      onCreate={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.onCreate, NodeUtils.isFunctionOrNull)}
      addValueText={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.addValueText, NodeUtils.isStringOrNull)}
      label={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.label, NodeUtils.isStringOrNull)}
      labelHtmlAttributes={toHtmlAttributes(dn, ctx, dn.node.labelHtmlAttributes)}
      formGroupHtmlAttributes={toHtmlAttributes(dn, ctx, dn.node.formGroupHtmlAttributes)}
      readOnly={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.readOnly, NodeUtils.isBooleanOrNull)}
      onChange={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.onChange, NodeUtils.isFunctionOrNull)}
    />
  ),
  renderDesigner: (dn) => {
    const m = dn.route && dn.route.member;
    return (<div>
      {/*<ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.ref)} type={null} defaultValue={true} />*/}
      <FieldComponent dn={dn} binding={Binding.create(dn.node, n => n.field)} />
      <StyleOptionsLine dn={dn} binding={Binding.create(dn.node, n => n.styleOptions)} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.onRenderItem)} type={null} defaultValue={null} exampleExpression={"mctx => modules.React.createElement(ValueLine, {ctx: mctx})"} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.onCreate)} type={null} defaultValue={null} exampleExpression={"() => Promise.resolve(null)"} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.addValueText)} type="string" defaultValue={null} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.label)} type="string" defaultValue={m?.niceName ?? ""} />
      <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.labelHtmlAttributes)} />
      <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.formGroupHtmlAttributes)} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.readOnly)} type="boolean" defaultValue={null} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.onChange)} type={null} defaultValue={false} exampleExpression={"/* you must declare 'forceUpdate' in locals */ \r\n() => locals.forceUpdate()"} />
    </div>)
  },
});

export interface EntityBaseNode extends LineBaseNode, ContainerNode {
  createOnFind?: ExpressionOrValue<boolean>;
  create?: ExpressionOrValue<boolean>;
  onCreate?: Expression<() => Promise<ModifiableEntity | Lite<Entity> | undefined> | undefined>;
  find?: ExpressionOrValue<boolean>;
  onFind?: Expression<() => Promise<ModifiableEntity | Lite<Entity> | undefined> | undefined>;
  remove?: ExpressionOrValue<boolean | ((item: ModifiableEntity | Lite<Entity>) => boolean)>;
  onRemove?: Expression<(remove: ModifiableEntity | Lite<Entity>) => Promise<boolean>>;
  view?: ExpressionOrValue<boolean | ((item: ModifiableEntity | Lite<Entity>) => boolean)>;
  onView?: Expression<(entity: ModifiableEntity | Lite<Entity>, pr: PropertyRoute) => Promise<ModifiableEntity | undefined> | undefined>;
  viewOnCreate?: ExpressionOrValue<boolean>;
  findOptions?: FindOptionsExpr;
  viewName?: ExpressionOrValue<string | ((mod: ModifiableEntity) => string | Navigator.ViewPromise<ModifiableEntity>)>;
}

export interface EntityLineNode extends EntityBaseNode {
  kind: "EntityLine",
  autoComplete?: ExpressionOrValue<AutocompleteConfig<unknown> | null>;
  itemHtmlAttributes?: HtmlAttributesExpression;
}

NodeUtils.register<EntityLineNode>({
  kind: "EntityLine",
  group: "Property",
  order: 1,
  isContainer: true,
  hasEntity: true,
  validate: (dn, ctx) => NodeUtils.validateEntityBase(dn, ctx),
  renderTreeNode: NodeUtils.treeNodeKindField,
  renderCode: (node, cc) => cc.elementCode("EntityLine", cc.getEntityBasePropsEx(node, { showAutoComplete: true })),
  render: (dn, ctx) => (<EntityLine {...NodeUtils.getEntityBaseProps(dn, ctx, { showAutoComplete: true, isEntityLine: true })} />),
  renderDesigner: dn => NodeUtils.designEntityBase(dn, { showAutoComplete: true, isEntityLine: true }),
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
  validate: (dn, ctx) => NodeUtils.validateEntityBase(dn, ctx),
  renderTreeNode: NodeUtils.treeNodeKindField,
  renderCode: (node, cc) => cc.elementCode("EntityCombo", cc.getEntityBasePropsEx(node, {})),
  render: (dn, ctx) => (<EntityCombo {...NodeUtils.getEntityBaseProps(dn, ctx, {})} />),
  renderDesigner: dn => NodeUtils.designEntityBase(dn, {}),
});

export interface EntityDetailNode extends EntityBaseNode, ContainerNode {
  kind: "EntityDetail",
  avoidFieldSet?: ExpressionOrValue<boolean>;
  onEntityLoaded?: Expression<() => void>;
}

NodeUtils.register<EntityDetailNode>({
  kind: "EntityDetail",
  group: "Property",
  order: 3,
  isContainer: true,
  hasEntity: true,
  validate: (dn, ctx) => NodeUtils.validateEntityBase(dn, ctx),
  renderTreeNode: NodeUtils.treeNodeKindField,
  renderCode: (node, cc) => cc.elementCode("EntityDetail", {
    ctx: cc.getEntityBasePropsEx(node, {}),
    avoidFieldSet: node.avoidFieldSet,
    onEntityLoaded: node.onEntityLoaded,
  }),
  render: (dn, ctx) => (<EntityDetail
    avoidFieldSet={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.avoidFieldSet, NodeUtils.isBooleanOrNull)}
    onEntityLoaded={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.onEntityLoaded, NodeUtils.isFunctionOrNull)}
    {...NodeUtils.getEntityBaseProps(dn, ctx, {})} />),
  renderDesigner: dn =>
    <div>
      {NodeUtils.designEntityBase(dn, {})}
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.avoidFieldSet)} type="boolean" defaultValue={false} allowsExpression={false} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.onEntityLoaded)} type={null} defaultValue={null} exampleExpression={"() => { /* do something here... */ }"} />
    </div>
});

export interface FileLineNode extends EntityBaseNode {
  kind: "FileLine",
  download?: ExpressionOrValue<DownloadBehaviour>;
  dragAndDrop?: ExpressionOrValue<boolean>;
  dragAndDropMessage?: ExpressionOrValue<string>;
  fileType?: ExpressionOrValue<string>;
  accept?: ExpressionOrValue<string>;
  maxSizeInBytes?: ExpressionOrValue<number>;
}

const DownloadBehaviours: DownloadBehaviour[] = ["SaveAs", "View", "None"];

NodeUtils.register<FileLineNode>({
  kind: "FileLine",
  group: "Property",
  order: 4,
  isContainer: false,
  hasEntity: true,
  validate: (dn, ctx) => NodeUtils.validateFieldMandatory(dn),
  renderTreeNode: NodeUtils.treeNodeKindField,
  renderCode: (node, cc) => cc.elementCode("FileLine", {
    ref: node.ref,
    ctx: cc.subCtxCode(node.field, node.styleOptions),
    label: node.label,
    labelHtmlAttributes: node.labelHtmlAttributes,
    formGroupHtmlAttributes: node.formGroupHtmlAttributes,
    visible: node.visible,
    readOnly: node.readOnly,
    remove: node.remove,
    download: node.download,
    dragAndDrop: node.dragAndDrop,
    dragAndDropMessage: node.dragAndDropMessage,
    fileType: bindExpr(key => registerSymbol("FileType", key), node.fileType),
    accept: node.accept,
    maxSizeInBytes: node.maxSizeInBytes,
    onChange: node.onChange
  }),
  render: (dn, parentCtx) => (<FileLine
    //ref={NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.ref, NodeUtils.isObjectOrFunctionOrNull)}
    ctx={parentCtx.subCtx(dn.node.field, toStyleOptions(dn, parentCtx, dn.node.styleOptions))}
    label={NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.label, NodeUtils.isStringOrNull)}
    labelHtmlAttributes={toHtmlAttributes(dn, parentCtx, dn.node.labelHtmlAttributes)}
    formGroupHtmlAttributes={toHtmlAttributes(dn, parentCtx, dn.node.formGroupHtmlAttributes)}
    visible={NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.visible, NodeUtils.isBooleanOrNull)}
    readOnly={NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.readOnly, NodeUtils.isBooleanOrNull)}
    remove={NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.remove, NodeUtils.isBooleanOrNull)}
    download={NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.download, a => NodeUtils.isInListOrNull(a, DownloadBehaviours))}
    dragAndDrop={NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.dragAndDrop, NodeUtils.isBooleanOrNull)}
    dragAndDropMessage={NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.dragAndDropMessage, NodeUtils.isStringOrNull)}
    fileType={toFileTypeSymbol(NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.fileType, NodeUtils.isStringOrNull))}
    accept={NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.accept, NodeUtils.isStringOrNull)}
    maxSizeInBytes={NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.maxSizeInBytes, NodeUtils.isNumberOrNull)}
    onChange={NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.onChange, NodeUtils.isFunctionOrNull)}
  />),
  renderDesigner: dn => {
    const m = dn.route && dn.route.member;
    return (
      <div>
        {/*<ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.ref)} type={null} defaultValue={true} />*/}
        <FieldComponent dn={dn} binding={Binding.create(dn.node, n => n.field)} />
        <StyleOptionsLine dn={dn} binding={Binding.create(dn.node, n => n.styleOptions)} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.label)} type="string" defaultValue={m?.niceName ?? ""} />
        <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.labelHtmlAttributes)} />
        <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.formGroupHtmlAttributes)} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.readOnly)} type="boolean" defaultValue={null} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.remove)} type="boolean" defaultValue={null} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.download)} type="string" defaultValue={null} options={DownloadBehaviours} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.dragAndDrop)} type="boolean" defaultValue={null} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.dragAndDropMessage)} type="string" defaultValue={null} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.fileType)} type="string" defaultValue={null} options={getFileTypes()} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.accept)} type="string" defaultValue={null} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.maxSizeInBytes)} type="number" defaultValue={null} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.onChange)} type={null} defaultValue={null} exampleExpression={"/* you must declare 'forceUpdate' in locals */ \r\n() => locals.forceUpdate()"} />
      </div>
    );
  }
});

export interface FileImageLineNode extends EntityBaseNode {
  kind: "FileImageLine",
  dragAndDrop?: ExpressionOrValue<boolean>;
  dragAndDropMessage?: ExpressionOrValue<string>;
  fileType?: ExpressionOrValue<string>;
  accept?: ExpressionOrValue<string>;
  maxSizeInBytes?: ExpressionOrValue<number>;
  imageHtmlAttributes?: HtmlAttributesExpression;
}

NodeUtils.register<FileImageLineNode>({
  kind: "FileImageLine",
  group: "Property",
  order: 5,
  isContainer: false,
  hasEntity: true,
  validate: (dn, ctx) => NodeUtils.validateFieldMandatory(dn),
  renderTreeNode: NodeUtils.treeNodeKindField,
  renderCode: (node, cc) => cc.elementCode("FileImageLine", {
    ref: node.ref,
    ctx: cc.subCtxCode(node.field, node.styleOptions),
    label: node.label,
    labelHtmlAttributes: node.labelHtmlAttributes,
    formGroupHtmlAttributes: node.formGroupHtmlAttributes,
    visible: node.visible,
    readOnly: node.readOnly,
    remove: node.remove,
    dragAndDrop: node.dragAndDrop,
    dragAndDropMessage: node.dragAndDropMessage,
    fileType: bindExpr(key => registerSymbol("FileType", key), node.fileType),
    accept: node.accept,
    maxSizeInBytes: node.maxSizeInBytes,
    onChange: node.onChange
  }),
  render: (dn, parentCtx) => (<FileImageLine
    //ref={NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.ref, NodeUtils.isObjectOrFunctionOrNull)}
    ctx={parentCtx.subCtx(dn.node.field, toStyleOptions(dn, parentCtx, dn.node.styleOptions))}
    label={NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.label, NodeUtils.isStringOrNull)}
    labelHtmlAttributes={toHtmlAttributes(dn, parentCtx, dn.node.labelHtmlAttributes)}
    formGroupHtmlAttributes={toHtmlAttributes(dn, parentCtx, dn.node.formGroupHtmlAttributes)}
    imageHtmlAttributes={toHtmlAttributes(dn, parentCtx, dn.node.imageHtmlAttributes)}
    visible={NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.visible, NodeUtils.isBooleanOrNull)}
    readOnly={NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.readOnly, NodeUtils.isBooleanOrNull)}
    remove={NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.remove, NodeUtils.isBooleanOrNull)}
    dragAndDrop={NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.dragAndDrop, NodeUtils.isBooleanOrNull)}
    dragAndDropMessage={NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.dragAndDropMessage, NodeUtils.isStringOrNull)}
    fileType={toFileTypeSymbol(NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.fileType, NodeUtils.isStringOrNull))}
    accept={NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.accept, NodeUtils.isStringOrNull)}
    maxSizeInBytes={NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.maxSizeInBytes, NodeUtils.isNumberOrNull)}
    onChange={NodeUtils.evaluateAndValidate(dn, parentCtx, dn.node, n => n.onChange, NodeUtils.isFunctionOrNull)}
  />),
  renderDesigner: dn => {
    const m = dn.route && dn.route.member;
    return (
      <div>
        {/*<ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.ref)} type={null} defaultValue={true} />*/}
        <FieldComponent dn={dn} binding={Binding.create(dn.node, n => n.field)} />
        <StyleOptionsLine dn={dn} binding={Binding.create(dn.node, n => n.styleOptions)} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.label)} type="string" defaultValue={m?.niceName ?? ""} />
        <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.labelHtmlAttributes)} />
        <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.formGroupHtmlAttributes)} />
        <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.imageHtmlAttributes)} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.readOnly)} type="boolean" defaultValue={null} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.remove)} type="boolean" defaultValue={null} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.dragAndDrop)} type="boolean" defaultValue={null} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.dragAndDropMessage)} type="string" defaultValue={null} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.fileType)} type="string" defaultValue={null} options={getFileTypes()} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.accept)} type="string" defaultValue={null} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.maxSizeInBytes)} type="number" defaultValue={null} />
        <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.onChange)} type={null} defaultValue={null} exampleExpression={"/* you must declare 'forceUpdate' in locals */ \r\n() => locals.forceUpdate()"} />
      </div>
    );
  }
});

export interface MultiFileLineNode extends LineBaseNode {
  kind: "MultiFileLine",
  download?: ExpressionOrValue<DownloadBehaviour>;
  dragAndDrop?: ExpressionOrValue<boolean>;
  dragAndDropMessage?: ExpressionOrValue<string>;
  fileType?: ExpressionOrValue<string>;
  accept?: ExpressionOrValue<string>;
  maxSizeInBytes?: ExpressionOrValue<number>;
}

NodeUtils.register<MultiFileLineNode>({
  kind: "MultiFileLine",
  group: "Property",
  hasCollection: true,
  hasEntity: true,
  order: 6,
  validate: (dn) => NodeUtils.validateFieldMandatory(dn),
  renderTreeNode: NodeUtils.treeNodeKindField,
  renderCode: (node, cc) => cc.elementCode("MultiFileLine", {
    ref: node.ref,
    ctx: cc.subCtxCode(node.field, node.styleOptions),
    label: node.label,
    labelHtmlAttributes: node.labelHtmlAttributes,
    formGroupHtmlAttributes: node.formGroupHtmlAttributes,
    readOnly: node.readOnly,
    download: node.download,
    dragAndDrop: node.dragAndDrop,
    dragAndDropMessage: node.dragAndDropMessage,
    fileType: bindExpr(key => registerSymbol("FileType", key), node.fileType),
    accept: node.accept,
    maxSizeInBytes: node.maxSizeInBytes,
    onChange: node.onChange,
  }),
  render: (dn, ctx) => (
    <MultiFileLine
      //ref={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.ref, NodeUtils.isObjectOrFunctionOrNull)}
      ctx={ctx.subCtx(dn.node.field, toStyleOptions(dn, ctx, dn.node.styleOptions))}
      label={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.label, NodeUtils.isStringOrNull)}
      labelHtmlAttributes={toHtmlAttributes(dn, ctx, dn.node.labelHtmlAttributes)}
      formGroupHtmlAttributes={toHtmlAttributes(dn, ctx, dn.node.formGroupHtmlAttributes)}
      readOnly={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.readOnly, NodeUtils.isBooleanOrNull)}
      download={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.download, a => NodeUtils.isInListOrNull(a, DownloadBehaviours))}
      dragAndDrop={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.dragAndDrop, NodeUtils.isBooleanOrNull)}
      dragAndDropMessage={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.dragAndDropMessage, NodeUtils.isStringOrNull)}
      fileType={toFileTypeSymbol(NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.fileType, NodeUtils.isStringOrNull))}
      accept={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.accept, NodeUtils.isStringOrNull)}
      maxSizeInBytes={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.maxSizeInBytes, NodeUtils.isNumberOrNull)}
      onChange={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.onChange, NodeUtils.isFunctionOrNull)}
    />
  ),
  renderDesigner: (dn) => {
    const m = dn.route && dn.route.member;
    return (<div>
      {/*<ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.ref)} type={null} defaultValue={true} />*/}
      <FieldComponent dn={dn} binding={Binding.create(dn.node, n => n.field)} />
      <StyleOptionsLine dn={dn} binding={Binding.create(dn.node, n => n.styleOptions)} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.label)} type="string" defaultValue={m?.niceName ?? ""} />
      <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.labelHtmlAttributes)} />
      <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.formGroupHtmlAttributes)} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.readOnly)} type="boolean" defaultValue={null} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.download)} type="string" defaultValue={null} options={DownloadBehaviours} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.dragAndDrop)} type="boolean" defaultValue={null} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.dragAndDropMessage)} type="string" defaultValue={null} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.fileType)} type="string" defaultValue={null} options={getFileTypes()} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.accept)} type="string" defaultValue={null} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.maxSizeInBytes)} type="number" defaultValue={null} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.onChange)} type={null} defaultValue={false} exampleExpression={"/* you must declare 'forceUpdate' in locals */ \r\n() => locals.forceUpdate()"} />
    </div>)
  },
});

function getFileTypes() {
  return getAllTypes()
    .filter(a => a.kind == "SymbolContainer" && a.name.endsWith("FileType"))
    .flatMap(t => Dic.getValues(t.members).map(m => t.name + "." + m.name));
}

function toFileTypeSymbol(fileTypeKey?: string) {
  if (fileTypeKey == undefined)
    return undefined;

  return registerSymbol("FileType", fileTypeKey);
}

export interface EnumCheckboxListNode extends LineBaseNode {
  kind: "EnumCheckboxList",
  columnCount?: ExpressionOrValue<number>;
  columnWidth?: ExpressionOrValue<number>;
  avoidFieldSet?: ExpressionOrValue<boolean>;
}

NodeUtils.register<EnumCheckboxListNode>({
  kind: "EnumCheckboxList",
  group: "Collection",
  order: 0,
  hasCollection: true,
  validate: (dn) => NodeUtils.validateFieldMandatory(dn),
  renderTreeNode: NodeUtils.treeNodeKindField,
  renderCode: (node, cc) => cc.elementCode("EnumCheckboxList", {
    ref: node.ref,
    ctx: cc.subCtxCode(node.field, node.styleOptions),
    label: node.label,
    avoidFieldSet: node.avoidFieldSet,
    readOnly: node.readOnly,
    columnCount: node.columnCount,
    columnWidth: node.columnWidth,
    onChange: node.onChange,
  }),
  render: (dn, ctx) => (<EnumCheckboxList
    //ref={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.ref, NodeUtils.isObjectOrFunctionOrNull)}
    ctx={ctx.subCtx(dn.node.field, toStyleOptions(dn, ctx, dn.node.styleOptions))}
    label={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.label, NodeUtils.isStringOrNull)}
    avoidFieldSet={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.avoidFieldSet, NodeUtils.isBooleanOrNull)}
    readOnly={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.readOnly, NodeUtils.isBooleanOrNull)}
    columnCount={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.columnCount, NodeUtils.isNumberOrNull)}
    columnWidth={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.columnWidth, NodeUtils.isNumberOrNull)}
    onChange={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.onChange, NodeUtils.isFunctionOrNull)}
  />),
  renderDesigner: (dn) => {
    const m = dn.route && dn.route.member;
    return (<div>
      {/*<ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.ref)} type={null} defaultValue={true} />*/}
      <FieldComponent dn={dn} binding={Binding.create(dn.node, n => n.field)} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.label)} type="string" defaultValue={m?.niceName ?? ""} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.avoidFieldSet)} type="boolean" defaultValue={false} allowsExpression={false} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.readOnly)} type="boolean" defaultValue={null} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.columnCount)} type="number" defaultValue={null} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.columnWidth)} type="number" defaultValue={200} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.onChange)} type={null} defaultValue={null} exampleExpression={"/* you must declare 'forceUpdate' in locals */ \r\n() => locals.forceUpdate()"} />
    </div>)
  },
});

export interface EntityListBaseNode extends EntityBaseNode {
  move?: ExpressionOrValue<boolean | ((item: ModifiableEntity | Lite<Entity>) => boolean)>;
  onFindMany?: Expression<() => Promise<(ModifiableEntity | Lite<Entity>)[] | undefined> | undefined>;
  filterRows?: Expression<(ctxs: TypeContext<any /*T*/>[]) => TypeContext<any /*T*/>[]>;
}

export interface EntityCheckboxListNode extends EntityListBaseNode {
  kind: "EntityCheckboxList",
  columnCount?: ExpressionOrValue<number>;
  columnWidth?: ExpressionOrValue<number>;
  avoidFieldSet?: ExpressionOrValue<boolean>;
}

NodeUtils.register<EntityCheckboxListNode>({
  kind: "EntityCheckboxList",
  group: "Collection",
  order: 1,
  hasEntity: true,
  hasCollection: true,
  validate: (dn, ctx) => NodeUtils.validateEntityBase(dn, ctx),
  renderTreeNode: NodeUtils.treeNodeKindField,
  renderCode: (node, cc) => cc.elementCode("EntityCheckboxList", {
    ...cc.getEntityBasePropsEx(node, { showMove: false, filterRows: true }),
    columnCount: node.columnCount,
    columnWidth: node.columnWidth,
    avoidFieldSet: node.avoidFieldSet,
  }),
  render: (dn, ctx) => (<EntityCheckboxList {...NodeUtils.getEntityBaseProps(dn, ctx, { showMove: false, filterRows: true })}
    columnCount={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.columnCount, NodeUtils.isNumberOrNull)}
    columnWidth={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.columnWidth, NodeUtils.isNumberOrNull)}
    avoidFieldSet={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.avoidFieldSet, NodeUtils.isBooleanOrNull)}
  />),
  renderDesigner: dn => <div>
    {NodeUtils.designEntityBase(dn, { filterRows: true })}
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.columnCount)} type="number" defaultValue={null} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.columnWidth)} type="number" defaultValue={200} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.avoidFieldSet)} type="boolean" defaultValue={false} allowsExpression={false} />
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
  validate: (dn, ctx) => NodeUtils.validateEntityBase(dn, ctx),
  renderTreeNode: NodeUtils.treeNodeKindField,
  renderCode: (node, cc) => cc.elementCode("EntityList", cc.getEntityBasePropsEx(node, { findMany: true, showMove: true, filterRows: true })),
  render: (dn, ctx) => (<EntityList {...NodeUtils.getEntityBaseProps(dn, ctx, { findMany: true, showMove: true, filterRows: true })} />),
  renderDesigner: dn => NodeUtils.designEntityBase(dn, { findMany: true, showMove: true, filterRows: true })
});


export interface EntityStripNode extends EntityListBaseNode {
  kind: "EntityStrip",
  autoComplete?: ExpressionOrValue<AutocompleteConfig<unknown> | null>;
  iconStart?: boolean;
  vertical?: boolean;
}

NodeUtils.register<EntityStripNode>({
  kind: "EntityStrip",
  group: "Collection",
  order: 3,
  isContainer: true,
  hasEntity: true,
  hasCollection: true,
  validate: (dn, ctx) => NodeUtils.validateEntityBase(dn, ctx),
  renderTreeNode: NodeUtils.treeNodeKindField,
  renderCode: (node, cc) => cc.elementCode("EntityStrip", {
    ...cc.getEntityBasePropsEx(node, { showAutoComplete: true, findMany: true, showMove: true, filterRows: true }),
    iconStart: node.iconStart,
    vertical: node.vertical,
  }),
  render: (dn, ctx) => (<EntityStrip
    {...NodeUtils.getEntityBaseProps(dn, ctx, { showAutoComplete: true, findMany: true, showMove: true, filterRows: true })}
    iconStart={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.iconStart, NodeUtils.isBooleanOrNull)}
    vertical={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.vertical, NodeUtils.isBooleanOrNull)}
  />),
  renderDesigner: dn =>
    <div>
      {NodeUtils.designEntityBase(dn, { showAutoComplete: true, findMany: true, showMove: true, filterRows: true })}
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.iconStart)} type="boolean" defaultValue={false} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.vertical)} type="boolean" defaultValue={false} />
    </div>
});


export interface EntityRepeaterNode extends EntityListBaseNode {
  kind: "EntityRepeater",
  avoidFieldSet?: ExpressionOrValue<boolean>;
}

NodeUtils.register<EntityRepeaterNode>({
  kind: "EntityRepeater",
  group: "Collection",
  order: 4,
  isContainer: true,
  hasEntity: true,
  hasCollection: true,
  validate: (dn, ctx) => NodeUtils.validateEntityBase(dn, ctx),
  renderTreeNode: NodeUtils.treeNodeKindField,
  renderCode: (node, cc) => cc.elementCode("EntityRepeater", { ...cc.getEntityBasePropsEx(node, { findMany: true, showMove: true, filterRows: true }), avoidFieldSet: node.avoidFieldSet }),
  render: (dn, ctx) => (<EntityRepeater
    {...NodeUtils.getEntityBaseProps(dn, ctx, { findMany: true, showMove: true, filterRows: true })}
    avoidFieldSet={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.avoidFieldSet, NodeUtils.isBooleanOrNull)}
  />),
  renderDesigner: dn =>
    <div>
      {NodeUtils.designEntityBase(dn, { findMany: true, showMove: true, filterRows: true })}
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.avoidFieldSet)} type="boolean" defaultValue={false} allowsExpression={false} />
    </div>
});

export interface EntityTabRepeaterNode extends EntityListBaseNode {
  kind: "EntityTabRepeater",
  avoidFieldSet?: ExpressionOrValue<boolean>;
}

NodeUtils.register<EntityTabRepeaterNode>({
  kind: "EntityTabRepeater",
  group: "Collection",
  order: 5,
  isContainer: true,
  hasEntity: true,
  hasCollection: true,
  validate: (dn, ctx) => NodeUtils.validateEntityBase(dn, ctx),
  renderTreeNode: NodeUtils.treeNodeKindField,
  renderCode: (node, cc) => cc.elementCode("EntityTabRepeater", { ...cc.getEntityBasePropsEx(node, { findMany: true, showMove: true, filterRows: true }), avoidFieldSet: node.avoidFieldSet }),
  render: (dn, ctx) => (<EntityTabRepeater
    {...NodeUtils.getEntityBaseProps(dn, ctx, { findMany: true, showMove: true, filterRows: true })}
    avoidFieldSet={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.avoidFieldSet, NodeUtils.isBooleanOrNull)} />),
  renderDesigner: dn =>
    <div>
      {NodeUtils.designEntityBase(dn, { findMany: true, showMove: true, filterRows: true })}
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.avoidFieldSet)} type="boolean" defaultValue={false} allowsExpression={false} />
    </div>
});

export interface EntityTableNode extends EntityListBaseNode {
  kind: "EntityTable",
  avoidFieldSet?: ExpressionOrValue<boolean>;
  scrollable?: ExpressionOrValue<boolean>;
  maxResultsHeight?: Expression<number | string>;
}

NodeUtils.register<EntityTableNode>({
  kind: "EntityTable",
  group: "Collection",
  order: 6,
  isContainer: true,
  hasEntity: true,
  hasCollection: true,
  validChild: "EntityTableColumn",
  validate: (dn, ctx) => NodeUtils.validateEntityBase(dn, ctx),
  renderTreeNode: NodeUtils.treeNodeKindField,
  renderCode: (node, cc) => cc.elementCode("EntityTable", {
    ...cc.getEntityBasePropsEx(node, { findMany: true, showMove: true, avoidGetComponent: true, filterRows: true }),
    avoidFieldSet: node.avoidFieldSet,
    scrollable: node.scrollable,
    maxResultsHeight: node.maxResultsHeight,
    columns: ({ __code__: "EntityTable.typedColumns<YourEntityHere>(" + cc.stringifyObject(node.children.map(col => ({ __code__: NodeUtils.renderCode(col as EntityTableColumnNode, cc) }))) + ")" })
  }),
  render: (dn, ctx) => (<EntityTable
    columns={dn.node.children.length == 0 ? undefined : dn.node.children.filter(c => (c.visible == undefined || NodeUtils.evaluateAndValidate(dn, ctx, c, n => n.visible, NodeUtils.isBooleanOrNull)) &&
      NodeUtils.validate(dn.createChild(c), ctx) == null).map(col => NodeUtils.render(dn.createChild(col as EntityTableColumnNode), ctx) as any)}
    {...NodeUtils.getEntityBaseProps(dn, ctx, { findMany: true, showMove: true, avoidGetComponent: true, filterRows: true })}
    avoidFieldSet={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.avoidFieldSet, NodeUtils.isBooleanOrNull)}
    scrollable={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.scrollable, NodeUtils.isBooleanOrNull)}
    maxResultsHeight={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.maxResultsHeight, NodeUtils.isNumberOrStringOrNull)}
  />),

  renderDesigner: dn =>
    <div>
      {NodeUtils.designEntityBase(dn, { findMany: true, showMove: true, filterRows: true })}
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.avoidFieldSet)} type="boolean" defaultValue={false} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.scrollable)} type="boolean" defaultValue={false} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.maxResultsHeight)} type={null} defaultValue={null} />
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
  validate: (dn) => dn.node.property ? undefined : NodeUtils.mandatory(dn, n => n.header),
  renderTreeNode: NodeUtils.treeNodeTableColumnProperty,
  renderCode: (node, cc) => cc.stringifyObject({
    property: node.property && { __code__: "a => a." + node.property },
    header: node.header,
    headerHtmlAttributes: node.headerHtmlAttributes,
    cellHtmlAttributes: node.cellHtmlAttributes,
    template: cc.getGetComponentEx(node, false)
  }),
  render: (dn, ctx) => ({
    property: dn.node.property && NodeUtils.asFieldFunction(dn.node.property),
    header: NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.header, NodeUtils.isStringOrNull),
    headerHtmlAttributes: toHtmlAttributes(dn, ctx, dn.node.headerHtmlAttributes),
    cellHtmlAttributes: toHtmlAttributes(dn, ctx, dn.node.cellHtmlAttributes),
    template: NodeUtils.getGetComponent(dn)
  }) as EntityTableColumn<ModifiableEntity, any> as any, //HACK
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
  searchOnLoad?: ExpressionOrValue<boolean>;
  showContextMenu?: Expression<(fop: FindOptionsParsed) => boolean | "Basic">;
  extraButtons?: Expression<(searchControl: SearchControlLoaded) => (ButtonBarElement | null | undefined | false)[]>;
  viewName?: ExpressionOrValue<string | ((mod: ModifiableEntity) => string | Navigator.ViewPromise<ModifiableEntity>)>;
  showHeader?: ExpressionOrValue<boolean>;
  showFilters?: ExpressionOrValue<boolean>;
  showFilterButton?: ExpressionOrValue<boolean>;
  showFooter?: ExpressionOrValue<boolean>;
  showGroupButton?: ExpressionOrValue<boolean>;
  showBarExtension?: ExpressionOrValue<boolean>;
  showChartButton?: ExpressionOrValue<boolean>;
  showExcelMenu?: ExpressionOrValue<boolean>;
  showUserQuery?: ExpressionOrValue<boolean>;
  showWordReport?: ExpressionOrValue<boolean>;
  hideFullScreenButton?: ExpressionOrValue<boolean>;
  allowSelection?: ExpressionOrValue<boolean>;
  allowChangeColumns?: ExpressionOrValue<boolean>;
  create?: ExpressionOrValue<boolean>;
  onCreate?: Expression<() => Promise<undefined | EntityPack<any> | ModifiableEntity | "no_change">>;
  navigate?: ExpressionOrValue<boolean>;
  deps?: Expression<React.DependencyList | undefined>;
  maxResultsHeight?: Expression<number | string>;
  onSearch?: Expression<(fo: FindOptionsParsed, dataChange: boolean) => void>;
  onResult?: Expression<(table: ResultTable, dataChange: boolean) => void>;
}

NodeUtils.register<SearchControlNode>({
  kind: "SearchControl",
  group: "Search",
  order: 1,
  validate: (dn, ctx) => NodeUtils.mandatory(dn, n => n.findOptions) || dn.node.findOptions && NodeUtils.validateFindOptions(dn.node.findOptions, ctx),
  renderTreeNode: dn => <span><small>SearchControl:</small> <strong>{dn.node.findOptions?.queryName ?? " - "}</strong></span>,
  renderCode: (node, cc) => cc.elementCode("SearchControl", {
    ref: node.ref,
    findOptions: node.findOptions,
    searchOnLoad: node.searchOnLoad,
    showContextMenu: node.showContextMenu,
    extraButtons: node.extraButtons,
    getViewPromise: NodeUtils.toFunctionCode(node.viewName),
    showHeader: node.showHeader,
    showFilters: node.showFilters,
    showFilterButton: node.showFilterButton,
    showFooter: node.showFooter,
    showGroupButton: node.showGroupButton,
    showBarExtension: node.showBarExtension,
    showChartButton: node.showChartButton,
    showExcelMenu: node.showExcelMenu,
    showUserQuery: node.showUserQuery,
    showWordReport: node.showWordReport,
    hideFullScreenButton: node.hideFullScreenButton,
    allowSelection: node.allowSelection,
    allowChangeColumns: node.allowChangeColumns,
    create: node.create,
    onCreate: node.onCreate,
    navigate: node.navigate,
    deps: node.deps,
    maxResultsHeight: node.maxResultsHeight,
    onSearch: node.onSearch,
    onResult: node.onResult,
  }),
  render: (dn, ctx) => <SearchControl
    ref={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.ref, NodeUtils.isObjectOrFunctionOrNull)}
    findOptions={toFindOptions(dn, ctx, dn.node.findOptions!)}
    getViewPromise={NodeUtils.toFunction(NodeUtils.evaluateAndValidate(dn, ctx, dn.node, f => f.viewName, NodeUtils.isFunctionOrStringOrNull))}
    searchOnLoad={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, f => f.searchOnLoad, NodeUtils.isBooleanOrNull)}
    showContextMenu={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, f => f.showContextMenu, NodeUtils.isFunctionOrNull)}
    extraButtons={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, f => f.extraButtons, NodeUtils.isFunctionOrNull)}
    showHeader={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, f => f.showHeader, NodeUtils.isBooleanOrNull)}
    showFilters={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, f => f.showFilters, NodeUtils.isBooleanOrNull)}
    showFilterButton={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, f => f.showFilterButton, NodeUtils.isBooleanOrNull)}
    showFooter={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, f => f.showFooter, NodeUtils.isBooleanOrNull)}
    showGroupButton={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, f => f.showGroupButton, NodeUtils.isBooleanOrNull)}
    showBarExtension={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, f => f.showBarExtension, NodeUtils.isBooleanOrNull)}
    showBarExtensionOption={{
      showChartButton: NodeUtils.evaluateAndValidate(dn, ctx, dn.node, f => f.showChartButton, NodeUtils.isBooleanOrNull),
      showExcelMenu: NodeUtils.evaluateAndValidate(dn, ctx, dn.node, f => f.showExcelMenu, NodeUtils.isBooleanOrNull),
      showUserQuery: NodeUtils.evaluateAndValidate(dn, ctx, dn.node, f => f.showUserQuery, NodeUtils.isBooleanOrNull),
      showWordReport: NodeUtils.evaluateAndValidate(dn, ctx, dn.node, f => f.showWordReport, NodeUtils.isBooleanOrNull),
    }}
    hideFullScreenButton={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, f => f.hideFullScreenButton, NodeUtils.isBooleanOrNull)}
    allowSelection={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, f => f.allowSelection, NodeUtils.isBooleanOrNull)}
    allowChangeColumns={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, f => f.allowChangeColumns, NodeUtils.isBooleanOrNull)}
    create={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, f => f.create, NodeUtils.isBooleanOrNull)}
    onCreate={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, f => f.onCreate, NodeUtils.isFunctionOrNull)}
    view={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, f => f.navigate, NodeUtils.isBooleanOrNull)}
    deps={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, f => f.deps, NodeUtils.isNumberOrStringOrNull)}
    maxResultsHeight={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, f => f.maxResultsHeight, NodeUtils.isNumberOrStringOrNull)}
    onSearch={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, f => f.onSearch, NodeUtils.isFunctionOrNull)}
    onResult={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, f => f.onResult, NodeUtils.isFunctionOrNull)}
  />,
  renderDesigner: dn => <div>
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.ref)} type={null} defaultValue={true} />
    <FindOptionsLine dn={dn} binding={Binding.create(dn.node, a => a.findOptions)} />
    <FetchQueryDescription queryName={dn.node.findOptions && dn.node.findOptions.queryName} >
      {qd => <ViewNameComponent dn={dn} binding={Binding.create(dn.node, n => n.viewName)} typeName={qd?.columns["Entity"].type.name} />}
    </FetchQueryDescription>
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, f => f.searchOnLoad)} type="boolean" defaultValue={null} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, f => f.showContextMenu)} type={null} defaultValue={null} exampleExpression={"fop => \"Basic\""} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, f => f.extraButtons)} type={null} defaultValue={null} exampleExpression={`sc => [
  { 
    order: -1.1,
    button: modules.React.createElement("button", { className: "btn btn-light", title: "Setting", onClick: e => alert(e) },
                                          modules.React.createElement(modules.FontAwesomeIcon, { icon: "gear", color: "green" }), " ", "Setting")
  },
]`} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, f => f.showHeader)} type="boolean" defaultValue={null} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, f => f.showFilters)} type="boolean" defaultValue={null} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, f => f.showFilterButton)} type="boolean" defaultValue={null} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, f => f.showFooter)} type="boolean" defaultValue={null} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, f => f.showGroupButton)} type="boolean" defaultValue={null} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, f => f.showBarExtension)} type="boolean" defaultValue={null} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, f => f.showChartButton)} type="boolean" defaultValue={null} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, f => f.showExcelMenu)} type="boolean" defaultValue={null} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, f => f.showUserQuery)} type="boolean" defaultValue={null} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, f => f.showWordReport)} type="boolean" defaultValue={null} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, f => f.hideFullScreenButton)} type="boolean" defaultValue={null} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, f => f.allowSelection)} type="boolean" defaultValue={null} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, f => f.allowChangeColumns)} type="boolean" defaultValue={null} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, f => f.create)} type="boolean" defaultValue={null} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, f => f.onCreate)} type={null} defaultValue={null} exampleExpression={`() =>
{
    modules.Constructor.construct("YourTypeHere").then(pack => {
        if (pack == undefined)
            return;

        /* Set entity properties here... */
        /* pack.entity.[propertyName] = ... */
        modules.Navigator.view(pack);
    });
}`} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, f => f.navigate)} type="boolean" defaultValue={null} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, f => f.deps)} type={null} defaultValue={null} exampleExpression={"ctx.frame.refreshCount"} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, f => f.maxResultsHeight)} type={null} defaultValue={null} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, f => f.onSearch)} type={null} defaultValue={null} exampleExpression={"(fop, dataChange) => {}"} />
    <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, f => f.onResult)} type={null} defaultValue={null} exampleExpression={"(table, dataChange) => dataChange && ctx.frame.onReload()"} />
  </div>
});

export interface SearchValueLineNode extends BaseNode {
  kind: "SearchValueLine",
  findOptions?: FindOptionsExpr;
  valueToken?: string;
  label?: ExpressionOrValue<string>;
  labelHtmlAttributes?: HtmlAttributesExpression;
  isBadge?: ExpressionOrValue<boolean>;
  isLink?: ExpressionOrValue<boolean>;
  isFormControl?: ExpressionOrValue<boolean>;
  findButton?: ExpressionOrValue<boolean>;
  viewEntityButton?: ExpressionOrValue<boolean>;
  deps?: Expression<React.DependencyList | undefined>;
  formGroupHtmlAttributes?: HtmlAttributesExpression;
}

NodeUtils.register<SearchValueLineNode>({
  kind: "SearchValueLine",
  group: "Search",
  order: 1,
  validate: (dn, ctx) => {
    if (!dn.node.findOptions && !dn.node.valueToken)
      return DynamicViewValidationMessage.Member0IsMandatoryFor1.niceToString("findOptions (or valueToken)", dn.node.kind);

    if (dn.node.findOptions) {
      const error = NodeUtils.validateFindOptions(dn.node.findOptions, ctx);
      if (error)
        return error;
    }

    if (dn.node.valueToken && !dn.node.findOptions) {
      if (ctx) {
        var name = ctx.propertyRoute!.typeReference().name;
        if (!isTypeEntity(name))
          return DynamicViewValidationMessage.ValueTokenCanNotBeUseFor0BecauseIsNotAnEntity.niceToString(name);
      }
    }

    return null;
  },
  renderTreeNode: dn => <span><small>SearchValueLine:</small> <strong>{
    dn.node.valueToken ? dn.node.valueToken :
      dn.node.findOptions ? dn.node.findOptions.queryName : " - "
  }</strong></span>,
  renderCode: (node, cc) => cc.elementCode("SearchValueLine", {
    ref: node.ref,
    ctx: cc.subCtxCode(),
    findOptions: node.findOptions,
    valueToken: node.valueToken,
    label: node.label,
    isBadge: node.isBadge,
    isLink: node.isLink,
    isFormControl: node.isFormControl,
    findButton: node.findButton,
    viewEntityButton: node.viewEntityButton,
    labelHtmlAttributes: node.labelHtmlAttributes,
    formGroupHtmlAttributes: node.formGroupHtmlAttributes,
    deps: node.deps,
  }),
  render: (dn, ctx) => <SearchValueLine ctx={ctx}
    ref={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.ref, NodeUtils.isObjectOrFunctionOrNull)}
    findOptions={dn.node.findOptions && toFindOptions(dn, ctx, dn.node.findOptions!)}
    valueToken={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.valueToken, NodeUtils.isStringOrNull)}
    label={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.label, NodeUtils.isStringOrNull)}
    isBadge={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.isBadge, NodeUtils.isBooleanOrNull)}
    isLink={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.isLink, NodeUtils.isBooleanOrNull)}
    isFormControl={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.isFormControl, NodeUtils.isBooleanOrNull)}
    findButton={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.findButton, NodeUtils.isBooleanOrNull)}
    viewEntityButton={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.viewEntityButton, NodeUtils.isBooleanOrNull)}
    labelHtmlAttributes={toHtmlAttributes(dn, ctx, dn.node.labelHtmlAttributes)}
    formGroupHtmlAttributes={toHtmlAttributes(dn, ctx, dn.node.formGroupHtmlAttributes)}
    deps={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, f => f.deps, NodeUtils.isNumberOrStringOrNull)}
  />,
  renderDesigner: dn => {
    return (<div>
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.ref)} type={null} defaultValue={true} />
      <QueryTokenLine dn={dn} binding={Binding.create(dn.node, a => a.valueToken)} queryKey={dn.node.findOptions && dn.node.findOptions.queryName ||
        (isTypeEntity(dn.route!.typeReference().name) ? dn.route!.typeReference().name : dn.route!.findRootType().name)}
        subTokenOptions={SubTokensOptions.CanAggregate | SubTokensOptions.CanElement} />
      <FindOptionsLine dn={dn} binding={Binding.create(dn.node, a => a.findOptions)} onQueryChanged={() => dn.node.valueToken = undefined} />
      <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.labelHtmlAttributes)} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.label)} type="string" defaultValue={null} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.isBadge)} type="boolean" defaultValue={null} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.isLink)} type="boolean" defaultValue={null} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.isFormControl)} type="boolean" defaultValue={null} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.findButton)} type="boolean" defaultValue={null} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.viewEntityButton)} type="boolean" defaultValue={null} />
      <HtmlAttributesLine dn={dn} binding={Binding.create(dn.node, n => n.formGroupHtmlAttributes)} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, f => f.deps)} type={null} defaultValue={null} exampleExpression={"ctx.frame.refreshCount"} />
    </div>);
  }
});


export interface ButtonNode extends BaseNode {
  kind: "Button",
  name: string;
  operationName?: string;
  onOperationClick?: ExpressionOrValue<(e: EntityOperationContext<any>) => void>;
  canExecute?: ExpressionOrValue<string>;
  text?: ExpressionOrValue<string>;
  active?: ExpressionOrValue<boolean>;
  color?: ExpressionOrValue<string>;
  icon?: ExpressionOrValue<string>;
  iconColor?: ExpressionOrValue<string>;
  disabled?: ExpressionOrValue<boolean>;
  outline?: ExpressionOrValue<boolean>;
  onClick?: ExpressionOrValue<(e: React.MouseEvent<any>) => void>;
  size?: ExpressionOrValue<string>;
  className?: ExpressionOrValue<string>;
}

NodeUtils.register<ButtonNode>({
  kind: "Button",
  group: "Simple",
  hasCollection: false,
  hasEntity: false,
  order: 0,
  renderTreeNode: dn => <span><small>Button:</small> <strong>{dn.node.name}</strong></span>,
  renderCode: (node, cc) => cc.elementCode(node.operationName ? "OperationButton" : "Button", {
    ref: node.ref,
    eoc: node.operationName ? { __code__: `EntityOperationContext.fromTypeContext(${cc.subCtxCode().__code__}, ${node.operationName}))` } : undefined,
    onOperationClick: node.onOperationClick,
    canExecute: node.canExecute,
    active: node.active,
    color: node.color,
    icon: node.icon,
    iconColor: node.iconColor,
    disabled: node.disabled,
    outline: node.outline,
    onClick: node.onClick,
    size: node.size,
    className: node.className,
  }),
  render: (dn, ctx) => {

    var icon = NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.icon, NodeUtils.isStringOrNull);
    var pIcon = parseIcon(icon);
    var iconColor = NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.iconColor, NodeUtils.isStringOrNull);

    var children = pIcon || iconColor ? <>
      {pIcon && <FontAwesomeIcon icon={pIcon} color={iconColor} className="me-2" />}
      {NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.text, NodeUtils.isStringOrNull)}
    </> : undefined;

    if (dn.node.operationName) {
      const eoc = EntityOperationContext.fromTypeContext(ctx as TypeContext<Entity>, dn.node.operationName);
      return (
        <OperationButton
          eoc={eoc}
          canExecute={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.canExecute, NodeUtils.isStringOrNull)}
          onOperationClick={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.onOperationClick, NodeUtils.isFunctionOrNull)}
          disabled={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.disabled, NodeUtils.isBooleanOrNull)}
          className={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.className, NodeUtils.isStringOrNull)}
          variant={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.color, NodeUtils.isStringOrNull) as BsColor}
          size={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.size, NodeUtils.isStringOrNull) as BsSize as any}
          children={children}
        />
      );
    }

    return (
      <Button
        active={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.active, NodeUtils.isBooleanOrNull)}
        disabled={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.disabled, NodeUtils.isBooleanOrNull)}
        onClick={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.onClick, NodeUtils.isFunctionOrNull)}
        className={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.className, NodeUtils.isStringOrNull)}
        variant={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.color, NodeUtils.isStringOrNull) as BsColor}
        size={NodeUtils.evaluateAndValidate(dn, ctx, dn.node, n => n.size, NodeUtils.isStringOrNull) as BsSize as any}
        children={children}
      />
    );
  },
  renderDesigner: (dn) => {

    var ti = dn.route && getTypeInfo(dn.route.typeReference().name);

    var operations = (ti?.operations && Dic.getValues(ti.operations).map(o => o.key)) ?? [];

    return (<div>
      {/*<ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.ref)} type={null} defaultValue={true} />*/}
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.name)} type="string" defaultValue={null} allowsExpression={false} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.operationName)} type="string" defaultValue={null} allowsExpression={false} options={operations} refreshView={() => {
        if (dn.node.operationName == null) {
          delete dn.node.canExecute;
          delete dn.node.onOperationClick;
        }
        dn.context.refreshView();
      }} />
      {dn.node.operationName && <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.canExecute)} type="string" defaultValue={null} />}
      {dn.node.operationName && <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.onOperationClick)} type={null} defaultValue={false} exampleExpression={"(eoc) => eoc.defaultClick()"} />}
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.text)} type="string" defaultValue={null} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.color)} type="string" defaultValue={null} options={["primary", "secondary", "success", "danger", "warning", "info", "light", "dark"] as BsColor[]} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.size)} type="string" defaultValue={null} options={["lg", "md", "sm", "xs"] as BsSize[]} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.icon)} type="string" defaultValue={null} onRenderValue={(val, e) => <IconTypeahead icon={val as string | null | undefined} formControlClass="form-control form-control-xs" onChange={newIcon => e.updateValue(newIcon)} />} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.iconColor)} type="string" defaultValue={null} onRenderValue={(val, e) => <ColorTypeahead color={val as string | null | undefined} formControlClass="form-control form-control-xs" onChange={newColor => e.updateValue(newColor)} />} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.active)} type="boolean" defaultValue={null} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.disabled)} type="boolean" defaultValue={null} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.onClick)} type={null} defaultValue={false} exampleExpression={"/* you must declare 'forceUpdate' in locals */ \r\n(e) => locals.forceUpdate()"} />
      <ExpressionOrValueComponent dn={dn} binding={Binding.create(dn.node, n => n.className)} type="string" defaultValue={null} />
    </div>)
  },
});

export namespace NodeConstructor {

  export function createDefaultNode(ti: TypeInfo) {
    return {
      kind: "Div",
      children: createSubChildren(PropertyRoute.root(ti))
    } as DivNode;
  }

  export function createEntityTableSubChildren(pr: PropertyRoute): BaseNode[] {

    const subMembers = pr.subMembers();

    return Dic.map(subMembers, (field, mi) => ({ kind: "EntityTableColumn", property: field, children: [] }) as BaseNode).filter(a => (a as any).property != "Id");
  }

  export function createSubChildren(pr: PropertyRoute): BaseNode[] {

    const subMembers = pr.subMembers();

    return Dic.map(subMembers, (field, mi) => appropiateComponent(mi, field)).filter(a => !!a).map(a => a!);
  }

  export const specificComponents: {
    [typeName: string]: (mi: MemberInfo, field: string) => BaseNode | undefined;
  } = {};

  export var appropiateComponent = (mi: MemberInfo, field: string): BaseNode | undefined => {
    if (mi.name == "Id" || mi.notVisible == true)
      return undefined;

    const tr = mi.type;
    const sc = specificComponents[tr.name];
    if (sc) {
      const result = sc(mi, field);
      if (result)
        return result;
    }

    const tis = tryGetTypeInfos(tr);
    const ti = tis.firstOrNull();

    if (tr.isCollection) {
      if (tr.name == IsByAll)
        return { kind: "EntityStrip", field, children: [] } as EntityStripNode;
      else if (!ti && !tr.isEmbedded)
        return { kind: "MultiValueLine", field, children: [] } as MultiValueLineNode;
      else if (tr.isEmbedded || ti!.entityKind == "Part" || ti!.entityKind == "SharedPart")
        return { kind: "EntityTable", field, children: [] } as EntityTableNode;
      else if (ti!.isLowPopulation)
        return { kind: "EntityCheckboxList", field, children: [] } as EntityCheckboxListNode;
      else
        return { kind: "EntityStrip", field, children: [] } as EntityStripNode;
    }

    if (tr.name == IsByAll)
      return { kind: "EntityLine", field, children: [] } as EntityLineNode;

    if (ti) {
      if (ti.kind == "Enum")
        return { kind: "ValueLine", field } as ValueLineNode;

      if (tr.name == FilePathEntity.typeName && mi.defaultFileTypeInfo && mi.defaultFileTypeInfo.onlyImages)
        return { kind: "FileImageLine", field } as FileImageLineNode;

      if (ti.name == FileEntity.typeName || ti.name == FilePathEntity.typeName)
        return { kind: "FileLine", field } as FileLineNode;

      if (ti.entityKind == "Part" || ti.entityKind == "SharedPart")
        return { kind: "EntityDetail", field, children: [] } as EntityDetailNode;

      if (ti.isLowPopulation)
        return { kind: "EntityCombo", field, children: [] } as EntityComboNode;

      return { kind: "EntityLine", field, children: [] } as EntityLineNode;
    }

    if (tr.isEmbedded) {

      if (tr.name == FilePathEmbedded.typeName && mi.defaultFileTypeInfo && mi.defaultFileTypeInfo.onlyImages)
        return { kind: "FileImageLine", field } as FileImageLineNode;

      if (tr.name == FileEmbedded.typeName || tr.name == FilePathEmbedded.typeName)
        return { kind: "FileLine", field } as FileLineNode;

      return { kind: "EntityDetail", field, children: [] } as EntityDetailNode;
    }

    if (ValueLineController.getValueLineType(tr) != undefined)
      return { kind: "ValueLine", field } as ValueLineNode;

    return undefined;
  }
}

