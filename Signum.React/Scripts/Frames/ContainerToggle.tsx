import * as React from 'react'
import { classes } from '../Globals'
import * as Navigator from '../Navigator'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../FindOptions'
import { Entity, Lite, is, toLite, LiteMessage, getToString, EntityPack, ModelState, ModifiableEntity } from '../Signum.Entities'
import { TypeContext, StyleOptions, EntityFrame } from '../TypeContext'
import { getTypeInfo, TypeInfo, PropertyRoute, ReadonlyBinding, getTypeInfos } from '../Reflection'

export default class ContainerToggleComponent extends React.Component<React.Props<ContainerToggleComponent>, { fluid: boolean }>{

    state = { fluid: false };
    
    constructor(props: React.Props<ContainerToggleComponent>) {
        super(props);
        Navigator.Expander.onGetExpanded = () => this.state.fluid;
        Navigator.Expander.onSetExpanded = (isExpanded: boolean) => this.setState({ fluid: isExpanded });
    }

    handleExpandToggle = (e: React.MouseEvent<any>) => {
        e.preventDefault();
        this.setState({ fluid: !this.state.fluid });
    }

    render() {
        return (
            <div className={this.state.fluid ? "container-fluid" : "container"}>
                <a className="expand-window" onClick={this.handleExpandToggle} href="#">
                    <span className={classes("fa", this.state.fluid ? "fa-compress" : "fa-expand")} />
                </a> 
                { this.props.children }
            </div>
        );
    }
}

