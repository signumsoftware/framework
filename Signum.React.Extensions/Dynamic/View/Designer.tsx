import * as React from 'react'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ModifiableEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { classes, Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { FindOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, MemberInfo } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { Expression, ExpressionOrValue, DesignerContext, BaseNode, DesignerNode, LineBaseNode } from './Nodes'
import * as Nodes from './Nodes'
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
                    {p.member}
                </label>
            </div>);
        }

        return (
            <div className="form-group">
                <label className="control-label">
                    <i className={classes("fa fa-calculator fa-1 formula", expr && "active")} onClick={this.handleToggleExpression}></i>
                    {p.member}
                </label>
                <div>
                    {expr ? this.renderExpression(expr, p.dn.parent!) : this.renderValue(value)}
                </div>
            </div>
        );
    }


    renderValue(value: number | string | null | undefined) {
        if (this.props.options) {
            const val = value === undefined ? this.props.defaultValue : value;
            
            return (
                <select className="form-control" value={val == null ? "" : val.toString()} onChange={this.handleChangeSelectOrInput} >
                    {this.props.options.map((o, i) =>
                        <option key={i} value={o == null ? "" : o.toString()}>{o == null ? " - " : o.toString()}</option>)
                    }
                </select>);
        }
        else
            return (<input className="form-control"
                type="text"
                value={value == null ? this.props.defaultValue!.toString() : value.toString()}
                onChange={this.handleChangeSelectOrInput} />);
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
    dn: DesignerNode<LineBaseNode>
}

export class FieldComponent extends React.Component<FieldComponentProps, void> {
    
    handleChange = (e: React.MouseEvent) => {
        var sender = (e.currentTarget as HTMLSelectElement);

        const node = this.props.dn.node;
        if (!sender.value)
            delete node.field;
        else
            node.field = sender.value;

        this.props.dn.context.refreshView();
    }
    
    render() {
        var p = this.props;
        var value = p.dn.node.field;
        
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

        var strValue = value == null ? "" : value.toString();

        const route = this.props.dn.parent!.route;

        var subMembers = Dic.getValues(route.subMembers()).filter(m => m.name != "Id") as Array<MemberInfo | null>;
        subMembers.insertAt(0, null);

        return (<select className="form-control" value={strValue} onChange={this.handleChange} >
            {subMembers.map((o, i) =>
                <option key={i} value={o == null ? "" : o.name}>{o == null ? " - " : o.niceName}</option>)
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

        if (!this.props.selectedNode)
            return <h4>{DynamicViewMessage.SelectANodeFirst.niceToString()}</h4>;

        const error = Nodes.validate(this.props.selectedNode);

        return (<div className="form-sm form-horizontal">
            <h3>{this.props.selectedNode.node.kind}</h3>
            {error && <div className="alert alert-danger">{error}</div>}
            {Nodes.renderDesigner(this.props.selectedNode)}
        </div>);
    }
}