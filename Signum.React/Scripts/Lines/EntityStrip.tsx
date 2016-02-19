import * as React from 'react'
import { Link } from 'react-router'
import { classes } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, mlistItemContext } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, ReadonlyBinding, subModelState, LambdaMemberType } from '../Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks, } from '../Lines/LineBase'
import { EntityComponentProps, EntityFrame } from '../Lines'
import { ModifiableEntity, Lite, IEntity, Entity, MList, MListElement, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString } from '../Signum.Entities'
import Typeahead from '../Lines/Typeahead'
import { EntityListBase, EntityListBaseProps } from './EntityListBase'

export interface EntityStripProps extends EntityListBaseProps {
}

export class EntityStrip extends EntityListBase<EntityStripProps, EntityStripProps> {

    renderInternal() {
        
        return (
            <fieldset className={classes("SF-repeater-field SF-control-container", this.state.ctx.binding.errorClass) }>
                <legend>
                    <div>
                        <span>{this.state.labelText}</span>
                        <span className="pull-right">
                            {this.renderCreateButton(true) }
                            {this.renderFindButton(true) }
                        </span>
                    </div>
                </legend>
                <div>
                    {
                        mlistItemContext(this.state.ctx).map((mlec, i) =>
                            (<EntityStripElement key={i}
                            onRemove={this.state.remove ? e => this.handleRemoveElementClick(e, i) : null} />))
                    }
                </div>
            </fieldset>
        );
    }


}


export interface EntityStripElementProps {
    onRemove: (event: React.MouseEvent) => void;

}

export class EntityStripElement extends React.Component<EntityStripElementProps, void>
{
    render() {
        return (
            <fieldset className="sf-repeater-element">
                <legend>
                    { this.props.onRemove && <a className={classes("sf-line-button", "sf-create") }
                    onClick={this.props.onRemove}
                    title={EntityControlMessage.Remove.niceToString() }>
                    <span className="glyphicon glyphicon-remove"/>
                    </a> }
                </legend>
            </fieldset>
        );
    }
}

