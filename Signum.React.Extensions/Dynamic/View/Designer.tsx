import * as React from 'react'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ModifiableEntity, External } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { classes, Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { FindOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, MemberInfo, PropertyRoute } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { Expression, ExpressionOrValue, DesignerContext, DesignerNode } from './NodeUtils'
import { BaseNode, LineBaseNode } from './Nodes'
import * as NodeUtils from './NodeUtils'
import ExpressionComponent from './ExpressionComponent'
import { DynamicViewEntity, DynamicViewMessage } from '../Signum.Entities.Dynamic'

export interface DesignerProps {
    dn: DesignerNode<BaseNode>,
    member: string,
}

export interface ExpressionOrValueProps extends DesignerProps {
    type: "number" | "string" | "boolean";
    options?: (string | number | null)[];
    defaultValue: number | string | boolean | null;
}

export class ExpressionOrValueComponent extends React.Component<ExpressionOrValueProps, void> {

    updateValue(value: string | boolean) {
        var p = this.props;

        var parsedValue = p.type != "number" ? value : (parseFloat(value as string) || null);

        if (parsedValue === "")
            parsedValue = null;

        if (parsedValue == p.defaultValue)
            delete (p.dn.node as any)[p.member];
        else
            (p.dn.node as any)[p.member] = parsedValue;

        p.dn.context.refreshView();
    }

    handleChangeCheckbox = (e: React.MouseEvent) => {
        var sender = (e.currentTarget as HTMLInputElement);
        this.updateValue(sender.checked);
    }

    handleChangeSelectOrInput = (e: React.MouseEvent) => {
        var sender = (e.currentTarget as HTMLSelectElement | HTMLInputElement);
        this.updateValue(sender.value);
    }

    handleToggleExpression = () => {
        var p = this.props;
        var value = ((p.dn.node as any)[p.member]) as any | Expression<any>;

        if (value instanceof Object && (value as Object).hasOwnProperty("code"))
            delete (p.dn.node as any)[p.member];
        else
            (p.dn.node as any)[p.member] = { code: "" } as Expression<any>;

        p.dn.context.refreshView();
    }

    render() {
        var p = this.props;
        var value = ((p.dn.node as any)[p.member]) as any | Expression<any>;
        
        var expr = value instanceof Object && (value as Object).hasOwnProperty("code") ? value as Expression<any> : null;

        if (!expr && p.type == "boolean") {
            return (<div>
                <label>
                    <i className={classes("fa fa-calculator fa-1 formula", expr && "active")} onClick={this.handleToggleExpression}></i>
                    {this.renderCheckbox(value)}
                    {this.renderMember(value)}
                </label>
            </div>);
        }

        return (
            <div className="form-group">
                <label className="control-label">
                    <i className={classes("fa fa-calculator fa-1 formula", expr && "active")} onClick={this.handleToggleExpression}></i>
                    { this.renderMember(value) }
                </label>
                <div>
                    {expr ? this.renderExpression(expr, p.dn.parent!) : this.renderValue(value)}
                </div>
            </div>
        );
    }

    renderMember(value: number | string | null | undefined): React.ReactNode {
        return (<span
            className={value === undefined ? "design-default" : "design-changed"}>
            {this.props.member}
        </span>);
    }

    renderValue(value: number | string | null | undefined) {

        const val = value === undefined ? this.props.defaultValue : value;

        if (this.props.options) {
            return (
                <select className="form-control" value={val == null ? "" : val.toString()} onChange={this.handleChangeSelectOrInput} >
                    {this.props.options.map((o, i) =>
                        <option key={i} value={o == null ? "" : o.toString()}>{o == null ? " - " : o.toString()}</option>)
                    }
                </select>);
        }
        else {
            return (<input className="form-control"
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

    renderExpression(expression: Expression<any>, parentDN: DesignerNode<BaseNode>) {

        const typeName = parentDN.route!.typeReference().name.split(",").map(tn => tn + "Entity").join(" | ");

        return <ExpressionComponent expression={expression} typeName={typeName} onChange={() => this.props.dn.context.refreshView()} />
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
            <option  value=""> - </option>
            {Dic.getKeys(subMembers).filter(k => subMembers[k].name != "Id").map((name, i) =>
                <option key={i} value={name}>{name}</option>)
            })
        </select>);
    }
}

export interface DesignFindOptionsProps extends DesignerProps {
}

export class DesignFindOptions extends React.Component<DesignFindOptionsProps, void> {
    render() {
        return null;
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