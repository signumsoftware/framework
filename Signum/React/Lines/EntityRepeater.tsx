import * as React from 'react'
import { classes } from '../Globals'
import { Navigator, ViewPromise } from '../Navigator'
import { TypeContext } from '../TypeContext'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, MList } from '../Signum.Entities'
import { AsEntity, EntityBaseController } from './EntityBase'
import { EntityListBaseController, EntityListBaseProps, DragConfig, MoveConfig } from './EntityListBase'
import { RenderEntity } from './RenderEntity'
import { tryGetTypeInfos, getTypeInfo } from '../Reflection';
import { useController } from './LineBase'
import { TypeBadge } from './AutoCompleteConfig'
import { getTimeMachineIcon } from './TimeMachineIcon'
import { GroupHeader, HeaderType } from './GroupHeader'
import { LinkButton } from '../Basics/LinkButton'

export interface EntityRepeaterProps<V extends ModifiableEntity | Lite<Entity>> extends EntityListBaseProps<V> {
  createAsLink?: boolean | ((er: EntityRepeaterController<V>) => React.ReactElement);
  avoidFieldSet?: boolean | HeaderType;
  createMessage?: string;
  getTitle?: (ctx: TypeContext<V>) => React.ReactElement | string;
  itemExtraButtons?: (er: EntityRepeaterController<V>, index: number) => React.ReactElement;
  elementHtmlAttributes?: (ctx: TypeContext<NoInfer<V>>) => React.HTMLAttributes<any> | null | undefined;
  ref?: React.Ref<EntityRepeaterController<V>>
}

export class EntityRepeaterController<V extends ModifiableEntity | Lite<Entity>> extends EntityListBaseController<EntityRepeaterProps<V>, V> {

  override getDefaultProps(p: EntityRepeaterProps<V>): void {
    super.getDefaultProps(p);
    p.viewOnCreate = false;
    p.createAsLink = true;
  }
}


export function EntityRepeater<V extends ModifiableEntity | Lite<Entity>>(props: EntityRepeaterProps<V>): React.JSX.Element | null {
  var c = useController<EntityRepeaterController<V>, EntityRepeaterProps<V>, MList<V>>(EntityRepeaterController, props);
  var p = c.props;

  if (c.isHidden)
    return null;

  let ctx = p.ctx;

  return (
    <GroupHeader className={classes("sf-repeater-field sf-control-container", c.getErrorClass("border"))}
      label={p.label}
      labelIcon={p.labelIcon}
      avoidFieldSet={p.avoidFieldSet}
      buttons={renderButtons()}
      htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes, ...c.errorAttributes() }} >
      {renderElements()}
    </GroupHeader >
  );

  function renderButtons() {
    const buttons = (
      <span>
        {p.extraButtonsBefore && p.extraButtonsBefore(c)}
        {p.createAsLink == false && c.renderCreateButton(false, p.createMessage)}
        {c.renderFindButton(false)}
        {p.extraButtons && p.extraButtons(c)}
      </span>
    );

    return EntityBaseController.hasChildrens(buttons) ? buttons : undefined;
  }

  function renderElements() {
    const readOnly = ctx.readOnly;
    const showType = tryGetTypeInfos(ctx.propertyRoute!.typeReference().name).length > 1;
    return (
      <div className="sf-repater-elements">
        {
          c.getMListItemContext(ctx).map((mlec, i) =>
          <EntityRepeaterElement<V> key={c.keyGenerator.getKey(mlec.value)}
            onRemove={c.canRemove(mlec.value) && !readOnly ? e => c.handleRemoveElementClick(e, mlec.index!) : undefined}
            ctx={mlec}
            move={c.canMove(mlec.value) && p.moveMode == "MoveIcons" && !readOnly ? c.getMoveConfig(false, mlec.index!, "v") : undefined}
            drag={c.canMove(mlec.value) && p.moveMode == "DragIcon" && !readOnly ? c.getDragConfig(mlec.index!, "v") : undefined}
            itemExtraButtons={p.itemExtraButtons ? (() => p.itemExtraButtons!(c, mlec.index!)) : undefined}
            htmlAttributes={p.elementHtmlAttributes ? (() => p.elementHtmlAttributes!(mlec)) : undefined}
            getComponent={p.getComponent}
            getViewPromise={p.getViewPromise}
            title={<>{p.getTitle?.(mlec)}{showType && p.getTitle && '\xa0'}{showType ? <TypeBadge entity={mlec.value} /> : undefined}</>}
            />
        )}
        {
          p.createAsLink && p.create && !readOnly &&
          (typeof p.createAsLink == "function" ? p.createAsLink(c) :
            <LinkButton title={ctx.titleLabels ? EntityControlMessage.Create.niceToString() : undefined}
              className="sf-line-button sf-create"
              onClick={c.handleCreateClick}>
              {EntityBaseController.getCreateIcon()}&nbsp;{p.createMessage ?? EntityControlMessage.Create.niceToString()}
            </LinkButton>)
        }
      </div>
    );
  }
}


export interface EntityRepeaterElementProps<V extends ModifiableEntity | Lite<Entity>> {
  ctx: TypeContext<V>;
  getComponent?: (ctx: TypeContext<AsEntity<V>>) => React.ReactElement;
  getViewPromise?: (entity: AsEntity<V>) => undefined | string | ViewPromise<AsEntity<V>>;
  onRemove?: (event: React.MouseEvent<any>) => void;
  move?: MoveConfig;
  drag?: DragConfig;
  title?: React.ReactElement;
  itemExtraButtons?: () => React.ReactElement;
  htmlAttributes?: () => React.HTMLAttributes<any> | null | undefined;
}

export function EntityRepeaterElement<V extends ModifiableEntity | Lite<Entity>>({ ctx, getComponent, getViewPromise, onRemove, move, drag, itemExtraButtons, title, htmlAttributes }: EntityRepeaterElementProps<V>): React.ReactElement {

  var attrs = htmlAttributes?.();

  return (
    <div
      {...attrs}
      className={classes(drag?.dropClass, attrs?.className)}
      onDragEnter={drag?.onDragOver}
      onDragOver={drag?.onDragOver}
      onDrop={drag?.onDrop}>
      {getTimeMachineIcon({ ctx: ctx, isContainer: true, translateY: "250%" })}
      <fieldset className="sf-repeater-element"
        {...EntityBaseController.entityHtmlAttributes(ctx.value)}>
        {(onRemove || move || drag || itemExtraButtons || title) &&
          <legend>
            <div className="d-flex">
              {onRemove && <LinkButton className={classes("sf-line-button", "sf-remove")}
                onClick={onRemove}
                title={ctx.titleLabels ? EntityControlMessage.Remove.niceToString() : undefined}>
                {EntityBaseController.getTrashIcon()}
              </LinkButton>}
              &nbsp;
              {move?.renderMoveUp()}
              {move?.renderMoveDown()}
              {drag && <LinkButton className={classes("sf-line-button", "sf-move")} onClick={e => { e.stopPropagation(); }}
                draggable={true}
                onDragStart={drag.onDragStart}
                onDragEnd={drag.onDragEnd}
                onKeyDown={drag.onKeyDown}
                title={drag.title}>
                {EntityBaseController.getMoveIcon()}
              </LinkButton>}
              {itemExtraButtons && itemExtraButtons()}
              {title && '\xa0'}
              {title}
            </div>
          </legend>}
        <div className="sf-line-entity">
          <RenderEntity ctx={ctx} getComponent={getComponent} getViewPromise={getViewPromise} />
        </div>
      </fieldset>
    </div>
  );
}

