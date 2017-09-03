import * as React from 'react'
import { Dic, classes } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, EntityFrame } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, ReadonlyBinding, LambdaMemberType } from '../Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks, } from '../Lines/LineBase'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString } from '../Signum.Entities'
import Typeahead from '../Lines/Typeahead'
import { EntityBase, EntityBaseProps } from './EntityBase'
import { RenderEntity } from './RenderEntity'

export interface EntityDetailProps extends EntityBaseProps {
    ctx: TypeContext<ModifiableEntity | Lite<Entity> | null | undefined>;
}

export class EntityDetail extends EntityBase<EntityDetailProps, EntityDetailProps> {

    calculateDefaultState(state: EntityDetailProps) {
        super.calculateDefaultState(state);
        state.viewOnCreate = false;
        state.view = false;
    }

    renderInternal() {

        const s = this.state;

        const hasValue = !!s.ctx.value;

        const buttons = (
            <span className="pull-right">
                {!hasValue && this.renderCreateButton(false)}
                {!hasValue && this.renderFindButton(false)}
                {hasValue && this.renderViewButton(false, s.ctx.value!)}
                {hasValue && this.renderRemoveButton(false, s.ctx.value!)}
            </span>
        );

        return (
            <fieldset className={classes("sf-entity-line-details", s.ctx.errorClass)}
                {...{ ...this.baseHtmlAttributes(), ...EntityBase.entityHtmlAttributes(s.ctx.value), ...s.formGroupHtmlAttributes }}>
                <legend>
                    <div>
                        <span>{s.labelText}</span>
                        {EntityBase.hasChildrens(buttons) ? buttons : undefined}
                    </div>
                </legend>
                <RenderEntity ctx={s.ctx} getComponent={this.props.getComponent} getViewPromise={this.props.getViewPromise} />
            </fieldset>
        );
    }
}

