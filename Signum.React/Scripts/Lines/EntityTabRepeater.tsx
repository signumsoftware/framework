import * as React from 'react'
import { Link } from 'react-router'
import { Tab, Tabs } from 'react-bootstrap'
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

export interface EntityTabRepeaterProps extends EntityListBaseProps {
    createAsLink?: boolean;
}

export class EntityTabRepeater extends EntityListBase<EntityTabRepeaterProps, EntityTabRepeaterProps> {

    calculateDefaultState(state: EntityTabRepeaterProps) {
        super.calculateDefaultState(state);
        state.viewOnCreate = false;
    }

    renderInternal() {

        var buttons = (
            <span className="pull-right">
                {this.renderCreateButton(false) }
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
                <Tabs>
                    {
                        mlistItemContext(this.state.ctx).map((mlec, i) =>
                            <Tab className="sf-repeater-element" eventKey={i} key={i}>
                                <legend>
                                    { this.state.remove && <a className={classes("sf-line-button", "sf-create") }
                                        onClick={e => this.handleRemoveElementClick(e, i) }
                                        title={EntityControlMessage.Remove.niceToString() }>
                                        <span className="glyphicon glyphicon-remove"/>
                                    </a> }
                                </legend>
                                <RenderEntity ctx={mlec} getComponent={this.props.getComponent}/>
                            </Tab>
                        )
                    }
                </Tabs>
            </fieldset>
        );
    }
}

