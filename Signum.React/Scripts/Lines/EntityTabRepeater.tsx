import * as React from 'react'
import { TabPane, TabContent } from 'reactstrap'
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
import { EntityListBase, EntityListBaseProps } from './EntityListBase'
import { RenderEntity } from './RenderEntity'

export interface EntityTabRepeaterProps extends EntityListBaseProps {
    createAsLink?: boolean;
}

export class EntityTabRepeater extends EntityListBase<EntityTabRepeaterProps, EntityTabRepeaterProps> {

    calculateDefaultState(state: EntityTabRepeaterProps) {
        super.calculateDefaultState(state);
        state.viewOnCreate = false;
    }

    renderInternal() {

        const buttons = (
            <span className="pull-right">
                {this.renderCreateButton(false)}
                {this.renderFindButton(false)}
            </span>
        );

        var ctx = this.state.ctx!;

        const readOnly = this.state.ctx.readOnly;

        return (
            <fieldset className={classes("SF-repeater-field SF-control-container", ctx.errorClass)}
                {...this.baseHtmlAttributes() } {...this.state.formGroupHtmlAttributes}>
                <legend>
                    <div>
                        <span>{this.state.labelText}</span>
                        {React.Children.count(buttons) ? buttons : undefined}
                    </div>
                </legend>
                <TabContent id={ctx.compose("tabs")}>
                    {
                        mlistItemContext(ctx).map((mlec, i) => {
                            const drag = this.canMove(mlec.value) && !readOnly ? this.getDragConfig(i, "h") : undefined;

                            return <TabPane tabId={i} key={i}
                                {...EntityListBase.entityHtmlAttributes(mlec.value) }
                                className="sf-repeater-element"
                                title={
                                    <div 
                                        className={classes("item-group", "sf-tab-dropable", drag && drag.dropClass)}
                                        onDragEnter={drag && drag.onDragOver}
                                        onDragOver={drag && drag.onDragOver}
                                        onDrop={drag && drag.onDrop}>
                                        {getToString(mlec.value)}
                                        &nbsp;
										{this.canRemove(mlec.value) && !readOnly &&
                                            <span className={classes("sf-line-button", "sf-create")}
                                                onClick={e => this.handleRemoveElementClick(e, i)}
                                                title={EntityControlMessage.Remove.niceToString()}>
                                                <span className="glyphicon glyphicon-remove" />
                                            </span>
                                        }
                                        &nbsp;
                                        {drag && <span className={classes("sf-line-button", "sf-move")}
                                            draggable={true}
                                            onDragStart={drag.onDragStart}
                                            onDragEnd={drag.onDragEnd}
                                            title={EntityControlMessage.Move.niceToString()}>
                                            <span className="glyphicon glyphicon-menu-hamburger" />
                                        </span>}
                                    </div> as any
                                }>
                                <RenderEntity ctx={mlec} getComponent={this.props.getComponent} viewPromise={this.props.viewPromise} />
                            </TabPane>
                        })

                    }
                </TabContent>
            </fieldset>
        );
    }
}

