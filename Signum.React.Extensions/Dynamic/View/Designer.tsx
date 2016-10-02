import * as React from 'react'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ModifiableEntity, External, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { classes, Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { FindOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, MemberInfo, PropertyRoute, Binding } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { Expression, ExpressionOrValue, DesignerContext, DesignerNode } from './NodeUtils'
import { BaseNode, LineBaseNode } from './Nodes'
import * as NodeUtils from './NodeUtils'
import JavascriptCodeMirror from './JavascriptCodeMirror'
import { DynamicViewEntity, DynamicViewMessage } from '../Signum.Entities.Dynamic'
import { Modal, ModalProps, ModalClass, ButtonToolbar, Button } from 'react-bootstrap'
import { openModal, IModalProps } from '../../../../Framework/Signum.React/Scripts/Modals';

export interface ExpressionOrValueProps {
    binding: Binding<any>;
    dn: DesignerNode<BaseNode>;
    refreshView?: () => void;
    type: "number" | "string" | "boolean" | null;
    options?: (string | number | null)[];
    defaultValue: number | string | boolean | null;
    allowsExpression?: boolean;
    avoidDelete?: boolean;
    hideLabel?: boolean;
}

export class ExpressionOrValueComponent extends React.Component<ExpressionOrValueProps, void> {

    updateValue(value: string | boolean) {
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

    handleChangeCheckbox = (e: React.MouseEvent) => {
        var sender = (e.currentTarget as HTMLInputElement);
        this.updateValue(sender.checked);
    }

    handleChangeSelectOrInput = (e: React.MouseEvent) => {
        var sender = (e.currentTarget as HTMLSelectElement | HTMLInputElement);
        this.updateValue(sender.value);
    }

    handleToggleExpression = (e: React.MouseEvent) => {
        e.preventDefault();
        e.stopPropagation();
        var p = this.props;
        var value = p.binding.getValue();

        if (value instanceof Object && (value as Object).hasOwnProperty("code"))
        {
            if (p.avoidDelete)
                p.binding.setValue(undefined);
            else
                p.binding.deleteValue();
        }
        else
            p.binding.setValue({ code: "" } as Expression<any>);

        (p.refreshView || p.dn.context.refreshView)();
    }

    render() {
        const p = this.props;
        const value = p.binding.getValue();
        
        const expr = value instanceof Object && (value as Object).hasOwnProperty("code") ? value as Expression<any> : null;

        const expressionIcon = this.props.allowsExpression != false && < i className={classes("fa fa-calculator fa-1 formula", expr && "active")} onClick={this.handleToggleExpression}></i>;


        if (!expr && p.type == "boolean") {
            return (
                <div>
                    <label>
                        {expressionIcon}
                        {this.renderCheckbox(value)}
                        {!this.props.hideLabel && this.renderMember(value)}
                    </label>
                </div>
            );
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
            <div className="form-group">
                <label className="control-label">
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
            return <p className="form-control-static">{DynamicViewMessage.UseExpression.niceToString()}</p>;

        const val = value === undefined ? this.props.defaultValue : value;

        const style = this.props.hideLabel ? { display: "inline-block" } as React.CSSProperties : undefined;
        
        if (this.props.options) {
            return (
                <select className="form-control" style={style}
                    value={val == null ? "" : val.toString()} onChange={this.handleChangeSelectOrInput} >
                    {this.props.options.map((o, i) =>
                        <option key={i} value={o == null ? "" : o.toString()}>{o == null ? " - " : o.toString()}</option>)
                    }
                </select>);
        }
        else {
            return (<input className="form-control" style={style}
                type="text"
                value={val == null ? "" : val.toString()}
                onChange={this.handleChangeSelectOrInput} />);
        }
    }


    renderCheckbox(value: boolean | null | undefined) {
        return (<input className="design-check-box"
            type="checkbox"
            checked={value == undefined ? this.props.defaultValue as boolean : value}
            onChange={this.handleChangeCheckbox} />);
    }

    renderExpression(expression: Expression<any>, dn: DesignerNode<BaseNode>) {

        if (this.props.allowsExpression == false)
            throw new Error("Unexpected expression");

        const typeName = dn.route!.typeReference().name.split(",").map(tn => tn + "Entity").join(" | ");
        return (
            <div>
                <pre style={{ border: "0px", margin: "0px" }}>{"(ctx: TypeContext<" + typeName + ">, auth) =>"}</pre>
                <JavascriptCodeMirror code={expression.code} onChange={newCode => { expression.code = newCode; this.props.dn.context.refreshView() } } />
            </div>
        );
        
    }
}


export interface FieldComponentProps  {
    dn: DesignerNode<BaseNode>,
    member: string,
}

export class FieldComponent extends React.Component<FieldComponentProps, void> {
    
    handleChange = (e: React.MouseEvent) => {
        var sender = (e.currentTarget as HTMLSelectElement);

        const node = this.props.dn.node;
        if (!sender.value)
            delete (node as any)[this.props.member];
        else
            (node as any)[this.props.member] = sender.value;


        this.props.dn.context.refreshView();
    }
    
    render() {
        var p = this.props;
        var value = (p.dn.node as any)[this.props.member];
        
        return (
            <div className="form-group">
                <label className="control-label">
                    field
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

        return (<select className="form-control" value={strValue} onChange={this.handleChange} >
            <option value=""> - </option>
            {Dic.getKeys(subMembers).filter(k => subMembers[k].name != "Id").map((name, i) =>
                <option key={i} value={name}>{name}</option>)
            })
        </select>);
    }
}

export class DynamicViewInspector extends React.Component<{ selectedNode?: DesignerNode<BaseNode> }, void>{
    render() {

        const sn = this.props.selectedNode;

        if (!sn)
            return <h4>{DynamicViewMessage.SelectANodeFirst.niceToString()}</h4>;

        const error = NodeUtils.validate(sn);

        return (<div className="form-sm form-horizontal">
            <h4>
                {sn.node.kind}
                {sn.route && <small> ({Finder.getTypeNiceName(sn.route.typeReference())})</small>}
            </h4>
            {error && <div className="alert alert-danger">{error}</div>}
            {NodeUtils.renderDesigner(sn)}
        </div>);
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

    okClicked: boolean
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
        return <Modal bsSize="lg" onHide={this.handleCancelClicked} show={this.state.show} onExited={this.handleOnExited} className="sf-selector-modal">
            <Modal.Header closeButton={true}>
                <h4 className="modal-title">
                    {this.props.title}
                </h4>
                <ButtonToolbar>
                    <Button className="sf-entity-button sf-close-button sf-ok-button" bsStyle="primary" onClick={this.handleOkClicked}>{JavascriptMessage.ok.niceToString()}</Button>
                    <Button className="sf-entity-button sf-close-button sf-cancel-button" bsStyle="default" onClick={this.handleCancelClicked}>{JavascriptMessage.cancel.niceToString()}</Button>
                </ButtonToolbar>
            </Modal.Header>

            <Modal.Body>
                {this.props.mainComponent()}
            </Modal.Body>
        </Modal>;
    }

    static show(title: React.ReactNode, mainComponent: () => React.ReactElement<any>): Promise<boolean> {
        return openModal<boolean>(<DesignerModal title={title} mainComponent={mainComponent} />);
    }
}