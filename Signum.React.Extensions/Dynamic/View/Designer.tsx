import * as React from 'react'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ModifiableEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { FindOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { DesignerContext, BaseNode } from './Nodes'
import * as Nodes from './Nodes'
import { DynamicViewEntity } from '../Signum.Entities.Dynamic'

export interface DesignerProps {
    node: any,
    member: string,
    dc: DesignerContext;
}

export interface DesignValueProps extends DesignerProps {
    type: "number" | "string" | "boolean";
}

export class DesignValue extends React.Component<DesignValueProps, void> {
    render() {
        return null;
    }
}

export interface DesignComboProps extends DesignerProps {
    options: (string | number | null)[];
}

export class DesignCombo extends React.Component<DesignComboProps, void> {
    render() {
        return null;
    }
}

export interface DesignFindOptionsProps extends DesignerProps {
}

export class DesignFindOptions extends React.Component<DesignFindOptionsProps, void> {
    render() {
        return null;
    }
}

export class DynamicViewInspector extends React.Component<{ selectedNode?: BaseNode, dc: DesignerContext }, void>{
    render() {

        if (!this.props.selectedNode)
            return <h4>Select a node first</h4>;

        return (<div>
            <h3>{this.props.selectedNode.kind}</h3>
            {Nodes.renderDesigner(this.props.selectedNode, this.props.dc)}
        </div>);
    }
}