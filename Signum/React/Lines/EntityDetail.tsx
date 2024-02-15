import * as React from 'react'
import { classes } from '../Globals'
import { TypeContext } from '../TypeContext'
import { ModifiableEntity, Lite, Entity, isLite, isEntity } from '../Signum.Entities'
import { EntityBaseController, EntityBaseProps } from './EntityBase'
import { RenderEntity } from './RenderEntity'
import { genericForwardRef, useController } from './LineBase'
import { getTypeInfos, tryGetTypeInfos } from '../Reflection'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { TypeBadge } from './AutoCompleteConfig'
import { getTimeMachineIcon } from './TimeMachineIcon'
import { GroupHeader, HeaderType } from './GroupHeader'


export interface EntityDetailProps<V extends Entity | Lite<Entity>> extends EntityBaseProps<V> {
  avoidFieldSet?: boolean | HeaderType;
  showAsCheckBox?: boolean;
  onEntityLoaded?: () => void;
  showType?: boolean;
}


export class EntityDetailController<V extends Entity | Lite<Entity>> extends EntityBaseController<EntityDetailProps<V>, V> {
  getDefaultProps(p: EntityDetailProps<V>) {
    super.getDefaultProps(p);
    p.viewOnCreate = false;
    p.view = false;
  }
}

export const EntityDetail = genericForwardRef(function EntityDetail<V extends Entity | Lite<Entity>>(props: EntityDetailProps<V>, ref: React.Ref<EntityDetailController<V>>) {

  const c = useController(EntityDetailController, props, ref);
  const p = c.props;

  if (c.isHidden)
    return null;

  var ti = tryGetTypeInfos(p.type!).onlyOrNull();

  var showAsCheckBox = p.showAsCheckBox ??
    ((p.type!.isEmbedded || ti != null && ti.entityKind == "Part") && p.extraButtons == undefined && p.extraButtonsBefore == undefined);


  function renderType() {
    var entity = p.ctx.value;
    if (entity == null)
      return null;

    if (isLite(entity) || isEntity(entity)) {
      if (p.showType ?? tryGetTypeInfos(p.type!).length > 1)
        return <TypeBadge entity={entity!} />;
    }

  }

  if (p.avoidFieldSet == true)
    return (
      <div className={classes("sf-entity-line-details", p.ctx.errorClass, c.mandatoryClass, p.ctx.value && "mb-4")}
        {...{ ...c.baseHtmlAttributes(), ...EntityBaseController.entityHtmlAttributes(p.ctx.value), ...p.formGroupHtmlAttributes, ...p.ctx.errorAttributes() }}>
        {getTimeMachineIcon({ ctx: p.ctx})}
        {showAsCheckBox ?
          <label className="lead">
            {renderCheckBox()}
            {p.label} {renderType()}
            {p.extraButtons && p.extraButtons(c)}
          </label>
          :
          <div className="lead">
            <span>{p.label} {renderType()}</span>
            {renderButtons()}
          </div>
        }
        <div className="ms-4 mt-2">
          <RenderEntity ctx={p.ctx} getComponent={p.getComponent} getViewPromise={p.getViewPromise} onEntityLoaded={p.onEntityLoaded} />
        </div>
      </div>
    );

  return (
    <>
      {getTimeMachineIcon({ ctx: p.ctx, translateY:"150%" })}
      <fieldset className={classes("sf-entity-line-details", p.ctx.errorClass, c.mandatoryClass)}
        {...{ ...c.baseHtmlAttributes(), ...EntityBaseController.entityHtmlAttributes(p.ctx.value), ...p.formGroupHtmlAttributes, ...p.ctx.errorAttributes() }}>
        <legend className="lead">
          {showAsCheckBox ?
            <label>
              {renderCheckBox()}
              {p.label} {renderType()}
              {p.extraButtons && p.extraButtons(c)}
            </label>
            :
            <div>
              <span>{p.label} {renderType()}</span>
              {renderButtons()}
            </div>
          }
        </legend>
        <RenderEntity ctx={p.ctx} getComponent={p.getComponent} getViewPromise={p.getViewPromise} onEntityLoaded={p.onEntityLoaded} />
      </fieldset>
    </>
  );

  function renderCheckBox() {
    const hasValue = !!p.ctx.value;
    var disabled = p.ctx.readOnly || (hasValue ? !p.remove : !p.create);

    return <input type="checkbox" className="form-check-input me-1" checked={hasValue} disabled={disabled}
      onChange={e => {
        e.preventDefault();
        e.stopPropagation();
        window.setTimeout(() => {
          if (!p.readOnly) {
            if (hasValue)
              c.handleRemoveClick(e)
            else
              c.handleCreateClick(e);
          }
        });
      
      }} />;
  }

  function renderButtons() {
    const hasValue = !!p.ctx.value;
    const buttons = (
      <span className="ms-1 float-end">
        {p.extraButtonsBefore && p.extraButtonsBefore(c)}
        {!hasValue && c.renderCreateButton(false)}
        {!hasValue && c.renderFindButton(false)}
        {hasValue && c.renderViewButton(false)}
        {hasValue && c.renderRemoveButton(false)}
        {p.extraButtons && p.extraButtons(c)}
      </span>
    );
    return EntityBaseController.hasChildrens(buttons) ? buttons : undefined;
  }
});
