import * as React from 'react'
import { Link } from 'react-router'
import { classes, Dic } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, mlistItemContext, EntityFrame } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, ReadonlyBinding, LambdaMemberType } from '../Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks, } from '../Lines/LineBase'
import { ModifiableEntity, Lite, Entity, MList, MListElement, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString } from '../Signum.Entities'
import Typeahead from '../Lines/Typeahead'
import { EntityBase } from './EntityBase'
import { EntityListBase, EntityListBaseProps } from './EntityListBase'
import { RenderEntity } from './RenderEntity'

export interface EntityRepeaterProps extends EntityListBaseProps {
    createAsLink?: boolean | ((er: EntityRepeater) => React.ReactElement<any>);
}

export class EntityRepeater extends EntityListBase<EntityRepeaterProps, EntityRepeaterProps> {

    calculateDefaultState(state: EntityRepeaterProps) {
        super.calculateDefaultState(state);
        state.viewOnCreate = false;
        state.createAsLink = true;
    }

    renderInternal() {

        const buttons = (
            <span className="pull-right">
                {this.state.createAsLink == false && this.renderCreateButton(false) }
                {this.renderFindButton(false) }
            </span>
        );

        let ctx = this.state.ctx;

        const readOnly = this.state.ctx.readOnly;

        return (
            <fieldset className={classes("SF-repeater-field SF-control-container", ctx.errorClass) } {...Dic.extend(this.baseHtmlProps(), this.state.formGroupHtmlProps) }>
                <legend>
                    <div>
                        <span>{this.state.labelText}</span>
                        {EntityBase.hasChildrens(buttons) ? buttons : undefined}
                    </div>
                </legend>
                <div className="sf-repater-elements">
                    {
                        mlistItemContext(ctx).map((mlec, i) =>
                            (<EntityRepeaterElement key={i}
                                onRemove={this.state.remove && !readOnly? e => this.handleRemoveElementClick(e, i) : undefined}
                                onMoveDown ={this.state.move && !readOnly? e => this.moveDown(i) : undefined}
                                onMoveUp ={this.state.move && !readOnly? e => this.moveUp(i) : undefined}
                                ctx={mlec}
                                getComponent={this.props.getComponent} />))
                    }
                    {
                        this.state.createAsLink && this.state.create && !readOnly &&
                            (typeof this.state.createAsLink == "function" ? this.state.createAsLink(this) :
                            <a title={EntityControlMessage.Create.niceToString()}
                                className="sf-line-button sf-create"
                                onClick={this.handleCreateClick}>
                                <span className="glyphicon glyphicon-plus sf-create sf-create-label" />{EntityControlMessage.Create.niceToString()}
                            </a> )
                    }
                </div>
            </fieldset>
        );
    }
}


export interface EntityRepeaterElementProps {
    ctx: TypeContext<Lite<Entity> | ModifiableEntity>;
    getComponent?: (ctx: TypeContext<ModifiableEntity>) => React.ReactElement<any>;
    onRemove?: (event: React.MouseEvent) => void;
    onMoveUp?: (event: React.MouseEvent) => void;
    onMoveDown?: (event: React.MouseEvent) => void;
}

export class EntityRepeaterElement extends React.Component<EntityRepeaterElementProps, void>
{
    render() {
        return (
            <fieldset className="sf-repeater-element" {...EntityListBase.entityHtmlProps(this.props.ctx.value) }>
                <legend>
                    <div className="item-group">
                        { this.props.onRemove && <a className={classes("sf-line-button", "sf-remove") }
                            onClick={this.props.onRemove}
                            title={EntityControlMessage.Remove.niceToString() }>
                            <span className="glyphicon glyphicon-remove"/> 
                        </a> }

                        { this.props.onMoveUp && <a className={classes("sf-line-button", "move-up") }
                            onClick={this.props.onMoveUp}
                            title={EntityControlMessage.MoveUp.niceToString() }>
                            <span className="glyphicon glyphicon-chevron-up"/>
                        </a> }

                        { this.props.onMoveDown && <a className={classes("sf-line-button", "move-down") }
                            onClick={this.props.onMoveDown}
                            title={EntityControlMessage.MoveDown.niceToString() }>
                            <span className="glyphicon glyphicon-chevron-down"/>
                        </a> }
                    </div>
                </legend>
                <div className="sf-line-entity">
                    <RenderEntity ctx={this.props.ctx} getComponent={this.props.getComponent}/>
                </div>
            </fieldset>
        );
    }
}

