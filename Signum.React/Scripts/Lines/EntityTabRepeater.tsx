import * as React from 'react'
import { classes, Dic } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, mlistItemContext, EntityFrame } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, ReadonlyBinding, LambdaMemberType } from '../Reflection'
import { LineBase, LineBaseProps } from '../Lines/LineBase'
import { ModifiableEntity, Lite, Entity, MList, MListElement, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString } from '../Signum.Entities'
import { EntityListBase, EntityListBaseProps } from './EntityListBase'
import { RenderEntity } from './RenderEntity'
import { Tab, UncontrolledTabs, Tabs } from '../Components/Tabs';
import { newMListElement } from '../Signum.Entities';
import { isLite } from '../Signum.Entities';

export interface EntityTabRepeaterProps extends EntityListBaseProps {
    createAsLink?: boolean;
    avoidFieldSet?: boolean;
    selectedIndex?: number;
    getTitle?: (mlec: TypeContext<any /*T*/>) => React.ReactChild;
    
}

export interface EntityTabRepeaterState extends EntityTabRepeaterProps {
    selectedIndex?: number;
}

export class EntityTabRepeater extends EntityListBase<EntityTabRepeaterProps, EntityTabRepeaterState> {


    calculateDefaultState(state: EntityTabRepeaterProps) {
        super.calculateDefaultState(state);

        state.selectedIndex = this.state == null ? 0 :
            coerce(this.state.selectedIndex, this.state.ctx.value.length);

        state.viewOnCreate = false;
    }

    renderInternal() {

        var ctx = this.state.ctx!;

        if (this.props.avoidFieldSet == true)
            return (
                <div className={classes("SF-repeater-field SF-control-container", ctx.errorClass)}
                    {...this.baseHtmlAttributes() } {...this.state.formGroupHtmlAttributes}>
                    {this.renderButtons()}
                    {this.renderTabs()}
                </div>
            );

        return (
            <fieldset className={classes("SF-repeater-field SF-control-container", ctx.errorClass)}
                {...this.baseHtmlAttributes() } {...this.state.formGroupHtmlAttributes}>
                <legend>
                    <div>
                        <span>{this.state.labelText}</span>
                        {this.renderButtons()}
                    </div>
                </legend>
                {this.renderTabs()}
            </fieldset>
        );
    }

    renderButtons() {
        const buttons = (
            <span className="pull-right">
                {this.renderCreateButton(false)}
                {this.renderFindButton(false)}
            </span>
        );

        return React.Children.count(buttons) ? buttons : undefined;
    }

    renderTabs() {
        const ctx = this.state.ctx!;
        const readOnly = ctx.readOnly;

        return (
            <Tabs activeEventKey={this.state.selectedIndex || 0} toggle={(activeKey: any) => this.setState({ selectedIndex: activeKey })}>
                {
                    mlistItemContext(ctx).map((mlec, i) => {
                        const drag = this.canMove(mlec.value) && !readOnly ? this.getDragConfig(i, "h") : undefined;

                        return <Tab eventKey={i} key={i}
                            {...EntityListBase.entityHtmlAttributes(mlec.value) }
                            className="sf-repeater-element"
                            title={
                                <div
                                    className={classes("item-group", "sf-tab-dropable", drag && drag.dropClass)}
                                    onDragEnter={drag && drag.onDragOver}
                                    onDragOver={drag && drag.onDragOver}
                                    onDrop={drag && drag.onDrop}>
                                    {this.props.getTitle ? this.props.getTitle(mlec) : getToString(mlec.value)}
                                    &nbsp;
										{this.canRemove(mlec.value) && !readOnly &&
                                        <span className={classes("sf-line-button", "sf-create")}
                                            onClick={e => { e.stopPropagation(); this.handleRemoveElementClick(e, i) } }
                                            title={EntityControlMessage.Remove.niceToString()}>
                                            <span className="fa fa-remove" />
                                        </span>
                                    }
                                    &nbsp;
                                        {drag && <span className={classes("sf-line-button", "sf-move")}
                                        draggable={true}
                                        onDragStart={drag.onDragStart}
                                        onDragEnd={drag.onDragEnd}
                                        title={EntityControlMessage.Move.niceToString()}>
                                        <span className="fa fa-bars" />
                                    </span>}
                                </div> as any
                            }>
                            <RenderEntity ctx={mlec} getComponent={this.props.getComponent} getViewPromise={this.props.getViewPromise} />
                        </Tab>
                    })
                }
            </Tabs>
        );
    }

    removeElement(mle: MListElement<ModifiableEntity | Lite<Entity>>) {
        const list = this.props.ctx.value!;
        let currentIndex = list.indexOf(mle);
        if (this.state.selectedIndex != null && this.state.selectedIndex < currentIndex)
            this.state.selectedIndex-- 

        list.remove(mle);

        this.state.selectedIndex = coerce(this.state.selectedIndex, list.length);
        
        this.setValue(list);
    }

    addElement(entityOrLite: Lite<Entity> | ModifiableEntity) {

        if (isLite(entityOrLite) != (this.state.type!.isLite || false))
            throw new Error("entityOrLite should be already converted");

        const list = this.props.ctx.value!;
        list.push(newMListElement(entityOrLite));
        this.state.selectedIndex = list.length - 1;
        this.setValue(list);
    }
}

function coerce(index: number | undefined, length: number): number | undefined {
    if (index == undefined)
        return undefined;

    if (length <= index)
        index = length - 1;

    if (index < 0)
        return undefined;

    return index;
}

