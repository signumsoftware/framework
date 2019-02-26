import * as React from 'react'
import { classes } from '../Globals'
import { TypeContext } from '../TypeContext'
import { ModifiableEntity, Lite, Entity } from '../Signum.Entities'
import { EntityBase, EntityBaseProps } from './EntityBase'
import { RenderEntity } from './RenderEntity'

export interface EntityDetailProps extends EntityBaseProps {
  ctx: TypeContext<ModifiableEntity | Lite<Entity> | null | undefined>;
  avoidFieldSet?: boolean;
  extraButtons?: (el: EntityDetail) => React.ReactNode;
  onEntityLoaded?: () => void;
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
      <span className="ml-1 float-right">
        {!hasValue && this.renderCreateButton(false)}
        {!hasValue && this.renderFindButton(false)}
        {hasValue && this.renderViewButton(false, s.ctx.value!)}
        {hasValue && this.renderRemoveButton(false, s.ctx.value!)}
        {this.props.extraButtons && this.props.extraButtons(this)}
      </span>
    );

    return EntityBase.hasChildrens(buttons) ? buttons : undefined;
  }

  renderElements() {
    const s = this.state;
    return (
      <RenderEntity ctx={s.ctx} getComponent={this.props.getComponent} getViewPromise={this.props.getViewPromise} onEntityLoaded={this.props.onEntityLoaded} />
    );
  }
}

