import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes, Dic } from '@framework/Globals'
import { Finder } from '@framework/Finder'
import { PropertyRoute, Binding } from '@framework/Reflection'
import { Expression, DesignerNode } from './NodeUtils'
import { BaseNode } from './Nodes'
import * as NodeUtils from './NodeUtils'
import JavascriptCodeMirror from '../../Signum.CodeMirror/JavascriptCodeMirror'
import { openModal, IModalProps } from '@framework/Modals';
import TypeHelpComponent from '../../Signum.Eval/TypeHelp/TypeHelpComponent'
import AutoLineModal from '@framework/AutoLineModal'
import { Typeahead } from '@framework/Components';
import { Modal, Tabs, Tab, DropdownButton, Dropdown } from 'react-bootstrap';
import { ModalFooterButtons, ModalHeaderButtons } from '@framework/Components/ModalHeaderButtons';
import { ModulesHelp } from './ModulesHelp';
import { DynamicViewMessage } from '../Signum.Dynamic.Views'
import { TextAreaLine } from '@framework/Lines'
import { LinkButton } from '@framework/Basics/LinkButton'

export interface ExpressionOrValueProps {
  binding: Binding<any>;
  dn: DesignerNode<BaseNode>;
  refreshView?: () => void;
  type: "number" | "string" | "boolean" | "textArea" | null;
  options?: (string | number)[] | ((query: string) => string[]);
  defaultValue: number | string | boolean | null;
  allowsExpression?: boolean;
  avoidDelete?: boolean;
  hideLabel?: boolean;
  exampleExpression?: string;
  onRenderValue?: (value: number | string | null | undefined, e: ExpressionOrValueComponentHandle) => React.ReactElement<any>;
}

interface ExpressionOrValueComponentHandle {
  updateValue(value: string | boolean | null | undefined): void;
}

export function ExpressionOrValueComponent(p: ExpressionOrValueProps): React.JSX.Element {
  function updateValue(value: string | boolean | null | undefined) {

    var parsedValue = p.type != "number" ? value : (parseFloat(value as string) ?? null);

    if (parsedValue === "")
      parsedValue = null;

    if (parsedValue == p.defaultValue && !p.avoidDelete)
      p.binding.deleteValue();
    else
      p.binding.setValue(parsedValue);

    (p.refreshView ?? p.dn.context.refreshView)();
  }

  function handleChangeCheckbox(e: React.ChangeEvent<any>) {
    var sender = (e.currentTarget as HTMLInputElement);
    updateValue(sender.checked);
  }

  function handleChangeSelectOrInput(e: React.ChangeEvent<any>) {
    var sender = (e.currentTarget as HTMLSelectElement | HTMLInputElement);
    updateValue(sender.value);
  }

  function handleTypeaheadSelect(item: unknown) {
    updateValue(item as string);
    return item as string;
  }

  function handleToggleExpression(e: React.MouseEvent<any>) {
    e.preventDefault();
    e.stopPropagation();
    var value = p.binding.getValue();

    if (value instanceof Object && (value as Object).hasOwnProperty("__code__")) {
      if (p.avoidDelete)
        p.binding.setValue(undefined);
      else
        p.binding.deleteValue();
    }
    else
      p.binding.setValue({
        __code__: p.exampleExpression ?? JSON.stringify(value == undefined ? p.defaultValue : value)
      } as Expression<any>);

    (p.refreshView ?? p.dn.context.refreshView)();
  }


  function renderMember(value: number | string | null | undefined): React.ReactNode | undefined {

    return (
      <span
        className={value === undefined ? "design-default" : "design-changed"}>
        {p.binding.member}
      </span>
    );
  }

  function renderValue(value: number | string | null | undefined) {
    if (p.onRenderValue)
      return p.onRenderValue(value, { updateValue });

    if (p.type == null)
      return <p className="form-control-static form-control-xs">{DynamicViewMessage.UseExpression.niceToString()}</p>;

    const val = value === undefined ? p.defaultValue : value;

    const style = p.hideLabel ? { display: "inline-block" } as React.CSSProperties : undefined;

    if (p.options) {
      if (typeof p.options == "function")
        return (
          <Typeahead
            inputAttrs={{ className: "form-control form-control-xs sf-entity-autocomplete" }}
            getItems={handleGetItems}
            onSelect={handleTypeaheadSelect} />
        );
      else
        return (
          <select className="form-select form-select-xs" style={style}
            value={val == null ? "" : val.toString()} onChange={handleChangeSelectOrInput} >
            {p.defaultValue == null && <option value="">{" - "}</option>}
            {p.options.map((o, i) =>
              <option key={i} value={o.toString()}>{o.toString()}</option>)
            }
          </select>);
    }
    else {

      if (p.type == "textArea") {
        return (<textarea className="form-control form-select-xs" style={style}
          value={val == null ? "" : val.toString()}
          onChange={handleChangeSelectOrInput} />);
      }

      return (<input className="form-control form-control-xs" style={style}
        type="text"
        value={val == null ? "" : val.toString()}
        onChange={handleChangeSelectOrInput} />);
    }
  }

  function handleGetItems(query: string) {
    if (typeof p.options != "function")
      throw new Error("Unexpected options");

    const result = p.options(query);

    return Promise.resolve(result);
  }



  function renderExpression(expression: Expression<any>, dn: DesignerNode<BaseNode>) {
    if (p.allowsExpression == false)
      throw new Error("Unexpected expression");

    const typeName = dn.parent!.fixRoute()!.typeReference().name.split(",").map(tn => tn.endsWith("Entity") ? tn : tn + "Entity").join(" | ");

    return (
      <div className="code-container">
        <pre style={{ border: "0px", margin: "0px", overflow: "visible" }}>
          {"(ctx: TypeContext<" + typeName + ">, "}
          <div style={{ display: "inline-flex" }}>
            <ModulesHelp cleanName={typeName.replace("Entity", "")} />{", "}<PropsHelp node={dn} />{", locals) =>"}
          </div>
        </pre>
        <JavascriptCodeMirror code={expression.__code__} onChange={newCode => { expression.__code__ = newCode; p.dn.context.refreshView() }} />
      </div>
    );

  }
  const value = p.binding.getValue();

  const expr = value instanceof Object && (value as Object).hasOwnProperty("__code__") ? value as Expression<any> : null;

  const expressionIcon = p.allowsExpression != false && <span className={classes("formula", expr && "active")} onClick={handleToggleExpression}  ><FontAwesomeIcon icon="calculator" title={DynamicViewMessage.UseExpression.niceToString()} /></span>;


  if (!expr && p.type == "boolean") {


    if (p.defaultValue == null) {

      return (<div>
        <label className="label-xs">
          {expressionIcon}
          <NullableCheckBox value={value}
            onChange={newValue => updateValue(newValue)}
            label={!p.hideLabel && renderMember(value)}
          />
        </label>
      </div>
      );
    } else {
      return (
        <div>
          <label className="label-xs">
            {expressionIcon}
            <input className="design-check-box form-check-input"
              type="checkbox"
              checked={value == undefined ? p.defaultValue as boolean : value}
              onChange={handleChangeCheckbox} />
            {!p.hideLabel && renderMember(value)}
          </label>
        </div>
      );
    }
  }

  if (p.hideLabel) {
    return (
      <div className="row gx-1">
        <div className="col-auto">
          {expressionIcon}
        </div>
        <div className="col-auto">
          {expr ? renderExpression(expr, p.dn!) : renderValue(value)}
        </div>
      </div>
    );
  }

  return (
    <div className="form-group form-group-xs">
      <label className="control-label label-xs">
        {expressionIcon}
        {renderMember(value)}
      </label>
      <div>
        {expr ? renderExpression(expr, p.dn!) : renderValue(value)}
      </div>
    </div>
  );
}


interface NullableCheckBoxProps {
  label: React.ReactNode | undefined;
  value: boolean | undefined;
  onChange: (newValue: boolean | undefined) => void;
}

export function NullableCheckBox(p: NullableCheckBoxProps): React.JSX.Element {
  function getIcon() {
    switch (p.value) {
      case true: return "check";
      case false: return "times";
      case undefined: return "minus"
    }
  }

  function getClass() {
    switch (p.value) {
      case true: return "design-changed";
      case false: return "design-changed";
      case undefined: return "design-default"
    }
  }

  function handleClick(e: React.MouseEvent<any>) {
    switch (p.value) {
      case true: p.onChange(false); break;
      case false: p.onChange(undefined); break;
      case undefined: p.onChange(true); break;
    }
  }

  return (
    <LinkButton title={undefined} onClick={handleClick}>
      <FontAwesomeIcon icon={getIcon()} className={getClass()} />
      {" "}
      {p.label}
    </LinkButton>
  );
}

export interface FieldComponentProps {
  dn: DesignerNode<BaseNode>,
  binding: Binding<string | undefined>,
}

export function FieldComponent(p: FieldComponentProps): React.JSX.Element {
  function handleChange(e: React.ChangeEvent<any>) {
    var sender = (e.currentTarget as HTMLSelectElement);

    const node = p.dn.node;
    if (!sender.value)
      p.binding.deleteValue()
    else
      p.binding.setValue(sender.value);


    p.dn.context.refreshView();
  }


  function renderValue(value: string | null | undefined) {
    const strValue = value == null ? "" : value.toString();

    const route = p.dn.parent!.fixRoute();

    const subMembers = route ? route.subMembers() : {};

    return (<select className="form-select form-select-xs" value={strValue} onChange={handleChange} >
      <option value=""> - </option>
      {Dic.getKeys(subMembers).filter(k => subMembers[k].name != "Id").map((name, i) =>
        <option key={i} value={name}>{name}</option>)
      })
    </select>);
  }
  var p = p;
  var value = p.binding.getValue();

  return (
    <div className="form-group form-group-xs">
      <label className="control-label label-xs">
        {p.binding.member}
      </label>
      <div>
        {renderValue(value)}
      </div>
    </div>
  );
}

export function DynamicViewInspector(p: { selectedNode?: DesignerNode<BaseNode> }): React.JSX.Element {
  const sn = p.selectedNode;

  if (!sn)
    return <h4>{DynamicViewMessage.SelectANodeFirst.niceToString()}</h4>;

  const error = NodeUtils.validate(sn, undefined);

  return (<div className="form-sm ">
    <h4>
      {sn.node.kind}
      {sn.route && <small> ({Finder.getTypeNiceName(sn.route.typeReference())})</small>}
    </h4>
    {error && <div className="alert alert-danger">{error}</div>}
    {NodeUtils.renderDesigner(sn)}
  </div>);
}


export function CollapsableTypeHelp(p: { initialTypeName?: string }): React.JSX.Element {

  const [open, setOpen] = React.useState<boolean>(false);

  function handleHelpClick(e: React.FormEvent<any>) {
    e.preventDefault();
    setOpen(!open);
  }

  function handleTypeHelpClick(pr: PropertyRoute | undefined) {
    if (!pr)
      return;

    AutoLineModal.show({
      type: { name: "string" },
      initialValue: TypeHelpComponent.getExpression("e", pr, "TypeScript"),
      customComponent: p => <TextAreaLine {...p} />,
      title: "Property Template",
      message: "Copy to clipboard: Ctrl+C, ESC",
    });
  }

  return (
    <div>
      <LinkButton title={undefined} onClick={handleHelpClick} className="design-help-button">
        {open ?
          DynamicViewMessage.HideHelp.niceToString() :
          DynamicViewMessage.ShowHelp.niceToString()}
      </LinkButton>
      {open &&
        <TypeHelpComponent
          initialType={p.initialTypeName}
          mode="TypeScript"
          onMemberClick={handleTypeHelpClick} />}
    </div>
  );
}

interface DesignerModalProps extends IModalProps<boolean | undefined> {
  title: React.ReactNode;
  mainComponent: () => React.ReactElement<any>;
}

export function DesignerModal(p: DesignerModalProps): React.JSX.Element {

  const [show, setShow] = React.useState<boolean>(true);
  const okClicked = React.useRef<boolean | undefined>(undefined);

  function handleOkClicked() {
    okClicked.current = true;
    setShow(false);
  }

  function handleCancelClicked() {
    setShow(false);
  }

  function handleOnExited() {
    p.onExited!(okClicked.current);
  }

  return (
    <Modal size="lg" onHide={handleCancelClicked} show={show} onExited={handleOnExited} className="sf-selector-modal">
      <ModalHeaderButtons>
        {p.title}
      </ModalHeaderButtons>
      <div className="modal-body">
        {p.mainComponent()}
      </div>
      <ModalFooterButtons
        onOk={handleOkClicked}
        onCancel={handleCancelClicked}>
      </ModalFooterButtons>
    </Modal>
  );
}

export namespace DesignerModal {
  export function show(title: React.ReactNode, mainComponent: () => React.ReactElement<any>): Promise<boolean | undefined> {
    return openModal<boolean>(<DesignerModal title={title} mainComponent={mainComponent} />);
  }
}

export function PropsHelp(p: { node: DesignerNode<BaseNode> }): React.JSX.Element {

  return (
    <DropdownButton id="props_help_dropdown" variant="success" size={"xs" as any} title={DynamicViewMessage.PropsHelp.niceToString()}>
      {Dic.map(p.node.context.propTypes, (name, typeName, i) =>
        <Dropdown.Item style={{ paddingTop: "0", paddingBottom: "0" }} key={i} onClick={() => handlePropsClick(name)}>{name}: {typeName}</Dropdown.Item>)}
    </DropdownButton>
  );

  function handlePropsClick(val: string) {

    AutoLineModal.show({
      type: { name: "string" },
      initialValue: `props.${val}`,
      customComponent: a => <TextAreaLine {...a} />,
      title: `${DynamicViewMessage.PropsHelp.niceToString()}.${val}`,
      message: "Copy to clipboard: Ctrl+C, ESC",
      valueHtmlAttributes: { style: { height: "200px" } },
    });
  }
}
