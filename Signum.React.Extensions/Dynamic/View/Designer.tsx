import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater } from '@framework/Lines'
import { ModifiableEntity, External, JavascriptMessage, EntityControlMessage } from '@framework/Signum.Entities'
import { classes, Dic } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import { FindOptions } from '@framework/FindOptions'
import { getQueryNiceName, MemberInfo, PropertyRoute, Binding } from '@framework/Reflection'
import * as Navigator from '@framework/Navigator'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import { Expression, ExpressionOrValue, DesignerContext, DesignerNode } from './NodeUtils'
import { BaseNode, LineBaseNode } from './Nodes'
import * as NodeUtils from './NodeUtils'
import JavascriptCodeMirror from '../../Codemirror/JavascriptCodeMirror'
import { DynamicViewEntity, DynamicViewMessage } from '../Signum.Entities.Dynamic'
import { openModal, IModalProps } from '@framework/Modals';
import TypeHelpComponent from '../../TypeHelp/TypeHelpComponent'
import ValueLineModal from '@framework/ValueLineModal'
import { Button, Modal, Typeahead } from '@framework/Components';
import { ModalHeaderButtons } from '@framework/Components/Modal';

export interface ExpressionOrValueProps {
    binding: Binding<any>;
    dn: DesignerNode<BaseNode>;
    refreshView?: () => void;
    type: "number" | "string" | "boolean" | "textArea" |  null;
    options?: (string | number)[] | ((query: string) => string[]);
    defaultValue: number | string | boolean | null;
    allowsExpression?: boolean;
    avoidDelete?: boolean;
    hideLabel?: boolean;
    exampleExpression?: string;
}

export class ExpressionOrValueComponent extends React.Component<ExpressionOrValueProps> {

    updateValue(value: string | boolean | undefined) {
        var p = this.props;

        var parsedValue = p.type != "number" ? value : (parseFloat(value as string) || null);

        if (parsedValue === "")
            parsedValue = null;

        if (parsedValue == p.defaultValue && !p.avoidDelete)
            p.binding.deleteValue();
        else
            p.binding.setValue(parsedValue);

        (p.refreshView || p.dn.context.refreshView)();
    }

    handleChangeCheckbox = (e: React.ChangeEvent<any>) => {
        var sender = (e.currentTarget as HTMLInputElement);
        this.updateValue(sender.checked);
    }

    handleChangeSelectOrInput = (e: React.ChangeEvent<any>) => {
        var sender = (e.currentTarget as HTMLSelectElement | HTMLInputElement);
        this.updateValue(sender.value);
    }

    handleTypeaheadSelect = (item: unknown) => {
        this.updateValue(item as string);
        return item as string;
    }

    handleToggleExpression = (e: React.MouseEvent<any>) => {
        e.preventDefault();
        e.stopPropagation();
        var p = this.props;
        var value = p.binding.getValue();

        if (value instanceof Object && (value as Object).hasOwnProperty("__code__"))
        {
            if (p.avoidDelete)
                p.binding.setValue(undefined);
            else
                p.binding.deleteValue();
        }
        else
            p.binding.setValue({
                __code__: this.props.exampleExpression || JSON.stringify(value == undefined ? this.props.defaultValue : value)
            } as Expression<any>);

        (p.refreshView || p.dn.context.refreshView)();
    }

    render() {
        const p = this.props;
        const value = p.binding.getValue();
        
        const expr = value instanceof Object && (value as Object).hasOwnProperty("__code__") ? value as Expression<any> : null;

        const expressionIcon = this.props.allowsExpression != false && <span className={classes("formula", expr && "active")} onClick={this.handleToggleExpression}><FontAwesomeIcon icon="calculator" /></span>;


        if (!expr && p.type == "boolean") {


            if (p.defaultValue == null) {

                return (<div>
                    <label className="label-xs">
                        {expressionIcon}
                        <NullableCheckBox value={value}
                            onChange={newValue => this.updateValue(newValue)}
                            label={!this.props.hideLabel && this.renderMember(value)}
                            />
                    </label>
                </div>
                );
            } else {
                return (
                    <div>
                        <label className="label-xs">
                            {expressionIcon}
                            <input className="design-check-box"
                                type="checkbox"
                                checked={value == undefined ? this.props.defaultValue as boolean : value}
                                onChange={this.handleChangeCheckbox} />
                            {!this.props.hideLabel && this.renderMember(value)}
                        </label>
                    </div>
                );
            }
        }

        if (this.props.hideLabel) {
            return (
                <div className="form-inline">
                    {expressionIcon}
                    {expr ? this.renderExpression(expr, p.dn!) : this.renderValue(value)}
                </div>
            );
        }

        return (
            <div className="form-group form-group-xs">
                <label className="control-label label-xs">
                    { expressionIcon }
                    { this.renderMember(value) }
                </label>
                <div>
                    {expr ? this.renderExpression(expr, p.dn!) : this.renderValue(value)}
                </div>
            </div>
        );
    }

    renderMember(value: number | string | null | undefined): React.ReactNode | undefined {
      
        return (
            <span
                className={value === undefined ? "design-default" : "design-changed"}>
                {this.props.binding.member}
            </span>
        );
    }

    renderValue(value: number | string | null | undefined) {

        if (this.props.type == null)
            return <p className="form-control-static form-control-xs">{DynamicViewMessage.UseExpression.niceToString()}</p>;

        const val = value === undefined ? this.props.defaultValue : value;

        const style = this.props.hideLabel ? { display: "inline-block" } as React.CSSProperties : undefined;
        
        if (this.props.options) {
            if (typeof this.props.options == "function")
                return (
                    <div style={{ position: "relative" }}>
                        <Typeahead
                            inputAttrs={{ className: "form-control form-control-xs sf-entity-autocomplete" }}
                            getItems={this.handleGetItems}
                            onSelect={this.handleTypeaheadSelect} />
                    </div>
                );
                else
            return (
                <select className="form-control form-control-xs" style={style}
                    value={val == null ? "" : val.toString()} onChange={this.handleChangeSelectOrInput} >
                    {this.props.defaultValue == null && <option value="">{" - "}</option>}
                    {this.props.options.map((o, i) =>
                        <option key={i} value={o.toString()}>{o.toString()}</option>)
                    }
                </select>);
        }
        else {

            if (this.props.type == "textArea") {
                return (<textarea className="form-control form-control-xs" style={style}
                    value={val == null ? "" : val.toString()}
                    onChange={this.handleChangeSelectOrInput} />);
            }

            return (<input className="form-control form-control-xs" style={style}
                type="text"
                value={val == null ? "" : val.toString()}
                onChange={this.handleChangeSelectOrInput} />);
        }
    }

    handleGetItems = (query: string) => {

        if (typeof this.props.options != "function")
            throw new Error("Unexpected options");

        const result = this.props.options(query);

        return Promise.resolve(result);
    }



    renderExpression(expression: Expression<any>, dn: DesignerNode<BaseNode>) {

        if (this.props.allowsExpression == false)
            throw new Error("Unexpected expression");

        const typeName = dn.parent!.fixRoute() !.typeReference().name.split(",").map(tn => tn.endsWith("Entity") ? tn : tn + "Entity").join(" | ");
        return (
            <div className="code-container">
                <pre style={{ border: "0px", margin: "0px" }}>{"(ctx: TypeContext<" + typeName + ">, modules) =>"}</pre>
                <JavascriptCodeMirror code={expression.__code__} onChange={newCode => { expression.__code__ = newCode; this.props.dn.context.refreshView() } } />
            </div>
        );
        
    }
}


interface NullableCheckBoxProps {
    label: React.ReactNode | undefined;
    value: boolean | undefined;
    onChange: (newValue: boolean | undefined) => void;
}

export class NullableCheckBox extends React.Component<NullableCheckBoxProps>{
    getIcon() {
        switch (this.props.value) {
            case true: return "check";
            case false: return "times";
            case undefined: return "minus"
        }
    }

    getClass() {
        switch (this.props.value) {
            case true: return "design-changed";
            case false: return "design-changed";
            case undefined: return "design-default"
        }
    }

    handleClick = (e: React.MouseEvent<any>) => {
        e.preventDefault();
        switch (this.props.value) {
            case true: this.props.onChange(false); break;
            case false: this.props.onChange(undefined); break;
            case undefined: this.props.onChange(true); break;
        }
    }

    render() {
        return (
            <a href="#" onClick={this.handleClick}>
                <FontAwesomeIcon icon={this.getIcon()} className={this.getClass()} />
                {" "}
                {this.props.label}
            </a>
        );
    }
}

export interface FieldComponentProps  {
    dn: DesignerNode<BaseNode>,
    binding: Binding<string | undefined>,
}

export class FieldComponent extends React.Component<FieldComponentProps> {
    
    handleChange = (e: React.ChangeEvent<any>) => {
        var sender = (e.currentTarget as HTMLSelectElement);

        const node = this.props.dn.node;
        if (!sender.value)
            this.props.binding.deleteValue()
        else
            this.props.binding.setValue(sender.value);


        this.props.dn.context.refreshView();
    }
    
    render() {
        var p = this.props;
        var value = p.binding.getValue();
        
        return (
            <div className="form-group form-group-xs">
                <label className="control-label label-xs">
                    {p.binding.member}
                </label>
                <div>
                    {this.renderValue(value)}
                </div>
            </div>
        );
    }

    renderValue(value: string | null | undefined) {

        const strValue = value == null ? "" : value.toString();

        const route = this.props.dn.parent!.fixRoute();

        const subMembers = route ? route.subMembers() : {};

        return (<select className="form-control form-control-xs" value={strValue} onChange={this.handleChange} >
            <option value=""> - </option>
            {Dic.getKeys(subMembers).filter(k => subMembers[k].name != "Id").map((name, i) =>
                <option key={i} value={name}>{name}</option>)
            })
        </select>);
    }
}

export class DynamicViewInspector extends React.Component<{ selectedNode?: DesignerNode<BaseNode> }>{
    render() {

        const sn = this.props.selectedNode;

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
}


export interface CollapsableTypeHelpState{
    open: boolean;
}

export class CollapsableTypeHelp extends React.Component<{ initialTypeName?: string }, CollapsableTypeHelpState>{

    constructor(props: any) {
        super(props);
        this.state = { open: false };
    }

    handleHelpClick = (e: React.FormEvent<any>) => {
        e.preventDefault();
        this.setState({
            open: !this.state.open
        });
    }

    handleTypeHelpClick = (pr: PropertyRoute | undefined) => {
        if (!pr)
            return;

        ValueLineModal.show({
            type: { name: "string" },
            initialValue: TypeHelpComponent.getExpression("e", pr, "TypeScript"),
            valueLineType: "TextArea",
            title: "Property Template",
            message: "Copy to clipboard: Ctrl+C, ESC",
            initiallyFocused: true,
        }).done();
    }

    render() {
        return (
            <div>
                <a href="#" onClick={this.handleHelpClick} className="design-help-button">
                    {this.state.open ?
                        DynamicViewMessage.HideHelp.niceToString() :
                        DynamicViewMessage.ShowHelp.niceToString()}
                </a>
                {this.state.open &&
                    <TypeHelpComponent
                        initialType={this.props.initialTypeName}
                        mode="TypeScript"
                        onMemberClick={this.handleTypeHelpClick} />}
            </div>
        );
    }
}

interface DesignerModalProps extends React.Props<DesignerModal>, IModalProps {
    title: React.ReactNode;
    mainComponent: () => React.ReactElement<any>;
}

export class DesignerModal extends React.Component<DesignerModalProps, { show: boolean }>  {

    constructor(props: DesignerModalProps) {
        super(props);

        this.state = { show: true };
    }

    okClicked?: boolean
    handleOkClicked = () => {
        this.okClicked = true;
        this.setState({ show: false });

    }

    handleCancelClicked = () => {
        this.setState({ show: false });
    }

    handleOnExited = () => {
        this.props.onExited!(this.okClicked);
    }

    render() {
        return (
            <Modal size="lg" onHide={this.handleCancelClicked} show={this.state.show} onExited={this.handleOnExited} className="sf-selector-modal">
                <ModalHeaderButtons
                    onOk={this.handleOkClicked}
                    onCancel={this.handleCancelClicked}>
                    {this.props.title}
                </ModalHeaderButtons>
                <div className="modal-body">
                    {this.props.mainComponent()}
                </div>
            </Modal>
        );
    }

    static show(title: React.ReactNode, mainComponent: () => React.ReactElement<any>): Promise<boolean | undefined> {
        return openModal<boolean>(<DesignerModal title={title} mainComponent={mainComponent} />);
    }
}