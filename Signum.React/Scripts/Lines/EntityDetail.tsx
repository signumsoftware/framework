import * as React from 'react'
import { classes } from '../Globals'
import { TypeContext } from '../TypeContext'
import { ModifiableEntity, Lite, Entity } from '../Signum.Entities'
import { EntityBaseController, EntityBaseProps } from './EntityBase'
import { RenderEntity } from './RenderEntity'
import { useController } from './LineBase'

export interface EntityDetailProps extends EntityBaseProps {
  ctx: TypeContext<ModifiableEntity | Lite<Entity> | null | undefined>;
  avoidFieldSet?: boolean;
  onEntityLoaded?: () => void;
}


export class EntityDetailController extends EntityBaseController<EntityDetailProps> {
  getDefaultProps(p: EntityDetailProps) {
    super.getDefaultProps(p);
    p.viewOnCreate = false;
    p.view = false;
  }
}

export const EntityDetail = React.forwardRef(function EntityDetail(props: EntityDetailProps, ref: React.Ref<EntityDetailController>) {

  const c = useController(EntityDetailController, props, ref);
  const p = c.props;

  if (c.isHidden)
    return null;

  if (p.avoidFieldSet == true)
    return (
      <div className={classes("sf-entity-line-details", p.ctx.errorClass, c.mandatoryClass)}
        {...{ ...c.baseHtmlAttributes(), ...EntityBaseController.entityHtmlAttributes(p.ctx.value), ...p.formGroupHtmlAttributes, ...p.ctx.errorAttributes() }}>
        {renderButtons()}
        <RenderEntity ctx={p.ctx} getComponent={p.getComponent} getViewPromise={p.getViewPromise} onEntityLoaded={p.onEntityLoaded} />
      </div>
    );

  return (
    <fieldset className={classes("sf-entity-line-details", p.ctx.errorClass, c.mandatoryClass)}
      {...{ ...c.baseHtmlAttributes(), ...EntityBaseController.entityHtmlAttributes(p.ctx.value), ...p.formGroupHtmlAttributes, ...p.ctx.errorAttributes() }}>
      <legend>
        <div>
          <span>{p.labelText}</span>
          {renderButtons()}
        </div>
      </legend>
      <RenderEntity ctx={p.ctx} getComponent={p.getComponent} getViewPromise={p.getViewPromise} onEntityLoaded={p.onEntityLoaded} />
    </fieldset>
  );

  function renderButtons() {
    const hasValue = !!p.ctx.value;
    const buttons = (
      <span className="ml-1 float-right">
        {p.extraButtonsBefore && p.extraButtonsBefore(c)}
        {!hasValue && c.renderCreateButton(false)}
        {!hasValue && c.renderFindButton(false)}
        {hasValue && c.renderViewButton(false, p.ctx.value!)}
        {hasValue && c.renderRemoveButton(false, p.ctx.value!)}
        {p.extraButtonsAfter && p.extraButtonsAfter(c)}
      </span>
    );
    return EntityBaseController.hasChildrens(buttons) ? buttons : undefined;
  }
});
