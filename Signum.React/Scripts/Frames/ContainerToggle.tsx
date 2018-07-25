import * as React from 'react'
import { classes } from '../Globals'
import * as Navigator from '../Navigator'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../FindOptions'
import { Entity, Lite, is, toLite, LiteMessage, getToString, EntityPack, ModelState, ModifiableEntity } from '../Signum.Entities'
import { TypeContext, StyleOptions, EntityFrame } from '../TypeContext'
import { getTypeInfo, TypeInfo, PropertyRoute, ReadonlyBinding, getTypeInfos } from '../Reflection'
import { ErrorBoundary } from "../Components/ErrorBoundary";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

export default class ContainerToggleComponent extends React.Component<{}, { fluid: boolean }>{

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
            <div className={classes(this.state.fluid ? "container-fluid" : "container", "mt-3")}>
                <a className="expand-window d-none d-md-block" onClick={this.handleExpandToggle} href="#" >
                    <FontAwesomeIcon icon={this.state.fluid ? "compress" : "expand"}/>
                </a> 
                <ErrorBoundary>
                    {this.props.children}
                </ErrorBoundary>
            </div>
        );
    }
}

