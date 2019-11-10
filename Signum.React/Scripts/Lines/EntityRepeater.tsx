import * as React from 'react'
import { classes } from '../Globals'
import * as Navigator from '../Navigator'
import { TypeContext } from '../TypeContext'
import { ModifiableEntity, Lite, Entity, EntityControlMessage } from '../Signum.Entities'
import { EntityBaseController } from './EntityBase'
import { EntityListBaseController, EntityListBaseProps, DragConfig } from './EntityListBase'
import { RenderEntity } from './RenderEntity'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { getTypeInfos, getTypeInfo } from '../Reflection';
import { useController } from './LineBase'

export interface EntityRepeaterProps extends EntityListBaseProps {
  createAsLink?: boolean | ((er: EntityRepeaterController) => React.ReactElement<any>);
  avoidFieldSet?: boolean;
  createMessage?: string;
}

export class EntityRepeaterController extends EntityListBaseController<EntityRepeaterProps> {

  getDefaultProps(p: EntityRepeaterProps) {
    super.getDefaultProps(p);
    p.viewOnCreate = false;
    p.createAsLink = true;
  }
}


export const EntityRepeater = React.forwardRef(function EntityRepeater(props: EntityRepeaterProps, ref: React.Ref<EntityRepeaterController>) {
  var c = useController(EntityRepeaterController, props, ref);
  var p = c.props;

  if (c.isHidden)
    return null;

  let ctx = p.ctx;

  if (p.avoidFieldSet == true)
    return (
      <div className={classes("SF-repeater-field SF-control-container", ctx.errorClassBorder)}
        {...{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes, ...ctx.errorAttributes() }}>
        {renderButtons()}
        {renderElements()}
      </div>
    );

  return (
    <fieldset className={classes("SF-repeater-field SF-control-container", ctx.errorClass)}
      {...{ ...c.baseHtmlAttributes(), ...c.props.formGroupHtmlAttributes, ...ctx.errorAttributes() }}>
      <legend>
        <div>
          <span>{p.labelText}</span>
          {renderButtons()}
        </div>
      </legend>
      {renderElements()}
    </fieldset>
  );


  function renderButtons() {
    const buttons = (
      <span className="float-right">
        {p.createAsLink == false && c.renderCreateButton(false, p.createMessage)}
        {c.renderFindButton(false)}
        {p.extraButtons && p.extraButtons(c)}
      </span>
    );

    return EntityBaseController.hasChildrens(buttons) ? buttons : undefined;
  }

  function renderElements() {
    const readOnly = ctx.readOnly;
    const showType = getTypeInfos(ctx.propertyRoute.typeReference().name).length > 1;
    return (
      <div className="sf-repater-elements">
        {
          c.getMListItemContext(ctx).map(mlec =>
            (<EntityRepeaterElement key={c.keyGenerator.getKey(mlec.value)}
              onRemove={c.canRemove(mlec.value) && !readOnly ? e => c.handleRemoveElementClick(e, mlec.index!) : undefined}
              ctx={mlec}
              drag={c.canMove(mlec.value) && !readOnly ? c.getDragConfig(mlec.index!, "v") : undefined}
              getComponent={p.getComponent}
              getViewPromise={p.getViewPromise}
              title={showType ? <span className="sf-type-badge">{getTypeInfo(mlec.value.Type ?? mlec.value.EntityType).niceName}</span> : undefined} />))
        }
        {
          p.createAsLink && p.create && !readOnly &&
          (typeof p.createAsLink == "function" ? p.createAsLink(c) :
            <a href="#" title={ctx.titleLabels ? EntityControlMessage.Create.niceToString() : undefined}
              className="sf-line-button sf-create"
              onClick={c.handleCreateClick}>
              {EntityBaseController.createIcon}&nbsp;{p.createMessage ?? EntityControlMessage.Create.niceToString()}
            </a>)
        }
      </div>
    );
  }
});


export interface EntityRepeaterElementProps {
  ctx: TypeContext<Lite<Entity> | ModifiableEntity>;
  getComponent?: (ctx: TypeContext<ModifiableEntity>) => React.ReactElement<any>;
  getViewPromise?: (entity: ModifiableEntity) => undefined | string | Navigator.ViewPromise<ModifiableEntity>;
  onRemove?: (event: React.MouseEvent<any>) => void;
  drag?: DragConfig;
  title?: React.ReactElement<any>;
}

export function EntityRepeaterElement({ ctx, getComponent, getViewPromise, onRemove, drag, title }: EntityRepeaterElementProps)
{

  return (
    <div className={drag?.dropClass}
      onDragEnter={drag?.onDragOver}
      onDragOver={drag?.onDragOver}
      onDrop={drag?.onDrop}>
      <fieldset className="sf-repeater-element"
        {...EntityListBaseController.entityHtmlAttributes(ctx.value)}>
        <legend>
          <div className="item-group">
            {onRemove && <a href="#" className={classes("sf-line-button", "sf-remove")}
              onClick={onRemove}
              title={ctx.titleLabels ? EntityControlMessage.Remove.niceToString() : undefined}>
              {EntityListBaseController.removeIcon}
            </a>}
            &nbsp;
            {drag && <a href="#" className={classes("sf-line-button", "sf-move")}
              draggable={true}
              onDragStart={drag.onDragStart}
              onDragEnd={drag.onDragEnd}
              title={ctx.titleLabels ? EntityControlMessage.Move.niceToString() : undefined}>
              {EntityListBaseController.moveIcon}
            </a>}
            {title && '\xa0'}
            {title}
          </div>
        </legend>
        <div className="sf-line-entity">
          <RenderEntity ctx={ctx} getComponent={getComponent} getViewPromise={getViewPromise} />
        </div>
      </fieldset>
    </div>
  );
}

