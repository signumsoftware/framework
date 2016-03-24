import * as React from 'react'
import { Link } from 'react-router'
import { classes } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, mlistItemContext } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, ReadonlyBinding, LambdaMemberType } from '../Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks, } from '../Lines/LineBase'
import { EntityComponentProps, EntityFrame } from '../Lines'
import { ModifiableEntity, Lite, Entity, MList, MListElement, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString } from '../Signum.Entities'
import Typeahead from '../Lines/Typeahead'
import { EntityListBase, EntityListBaseProps } from './EntityListBase'
import { RenderEntity } from './RenderEntity'

export interface EntityRepeaterProps extends EntityListBaseProps {
    createAsLink?: boolean;
}

export class EntityRepeater extends EntityListBase<EntityRepeaterProps, EntityRepeaterProps> {

    calculateDefaultState(state: EntityRepeaterProps) {
        super.calculateDefaultState(state);
        state.viewOnCreate = false;
        state.createAsLink = true;
    }

    renderInternal() {

        var buttons = (
            <span className="pull-right">
                {!this.state.createAsLink && this.renderCreateButton(false) }
                {this.renderFindButton(false) }
            </span>
        );


        if (!buttons.props.children.some(a => a))
            buttons = null;

        return (
            <fieldset className={classes("SF-repeater-field SF-control-container", this.state.ctx.binding.errorClass) }>
                <legend>
                    <div>
                        <span>{this.state.labelText}</span>
                        {buttons}
                    </div>
                </legend>
                <div>
                    {
                        mlistItemContext(this.state.ctx).map((mlec, i) =>
                            (<EntityRepeaterElement key={i}
                            onRemove={this.state.remove ? e => this.handleRemoveElementClick(e, i) : null}
                            ctx={mlec}
                            getComponent={this.props.getComponent} />))
                    }
                    {
                        this.state.createAsLink && this.state.create &&
                        <a title={EntityControlMessage.Create.niceToString() }
                            className="sf-line-button sf-create"
                            onClick={this.handleCreateClick}>
                            <span className="glyphicon glyphicon-plus" style={{ marginRight: "5px" }}/>{EntityControlMessage.Create.niceToString() }
                        </a>
                    }
                </div>
            </fieldset>
        );
    }
}


export interface EntityRepeaterElementProps {
    ctx: TypeContext<Lite<Entity> | ModifiableEntity>;
    getComponent: (ctx: TypeContext<ModifiableEntity>, frame: EntityFrame<ModifiableEntity>) => React.ReactElement<any>;
    onRemove: (event: React.MouseEvent) => void;
}

export class EntityRepeaterElement extends React.Component<EntityRepeaterElementProps, void>
{
    render() {
        return (
            <fieldset className="sf-repeater-element">
                <legend>
                    { this.props.onRemove && <a className={classes("sf-line-button", "sf-remove") }
                    onClick={this.props.onRemove}
                    title={EntityControlMessage.Remove.niceToString() }>
                    <span className="glyphicon glyphicon-remove"/>
                    </a> }
                </legend>
                <RenderEntity ctx={this.props.ctx} getComponent={this.props.getComponent}/>
            </fieldset>
        );
    }
}

