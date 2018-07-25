import * as React from 'react'
import { Dic, classes } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, EntityFrame } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, ReadonlyBinding, MemberType } from '../Reflection'
import { LineBase, LineBaseProps, runTasks, } from './LineBase'
import { FormGroup } from './FormGroup'
import { FormControlReadonly } from './FormControlReadonly'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString } from '../Signum.Entities'
import { EntityBase, EntityBaseProps } from './EntityBase'
import { RenderEntity } from './RenderEntity'

export interface EntityDetailProps extends EntityBaseProps {
    ctx: TypeContext<ModifiableEntity | Lite<Entity> | null | undefined>;
    avoidFieldSet?: boolean;
}

export class EntityDetail extends EntityBase<EntityDetailProps, EntityDetailProps> {

    calculateDefaultState(state: EntityDetailProps) {
        super.calculateDefaultState(state);
        state.viewOnCreate = false;
        state.view = false;
    }

    renderInternal() {

        const s = this.state;

        if (this.props.avoidFieldSet == true)
            return (
                <div className={classes("sf-entity-line-details", s.ctx.errorClass)}
                    {...{ ...this.baseHtmlAttributes(), ...EntityBase.entityHtmlAttributes(s.ctx.value), ...s.formGroupHtmlAttributes }}>
                    {this.renderButtons()}
                    {this.renderElements()}
                </div>
            );

        return (
            <fieldset className={classes("sf-entity-line-details", s.ctx.errorClass)}
                {...{ ...this.baseHtmlAttributes(), ...EntityBase.entityHtmlAttributes(s.ctx.value), ...s.formGroupHtmlAttributes }}>
                <legend>
                    <div>
                        <span>{s.labelText}</span>
                        {this.renderButtons()}
                    </div>
                </legend>
                {this.renderElements()}
            </fieldset>
        );
    }

    renderButtons() {
        const s = this.state;
        const hasValue = !!s.ctx.value;

        const buttons = (
            <span className="float-right">
                {!hasValue && this.renderCreateButton(false)}
                {!hasValue && this.renderFindButton(false)}
                {hasValue && this.renderViewButton(false, s.ctx.value!)}
                {hasValue && this.renderRemoveButton(false, s.ctx.value!)}
            </span>
        );

        return EntityBase.hasChildrens(buttons) ? buttons : undefined;
    }

    renderElements() {
        const s = this.state;
        return (
            <RenderEntity ctx={s.ctx} getComponent={this.props.getComponent} getViewPromise={this.props.getViewPromise} />
        );
    }
}

