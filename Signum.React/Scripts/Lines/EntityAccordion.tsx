import * as React from 'react'
import { classes } from '../Globals'
import * as Navigator from '../Navigator'
import { TypeContext } from '../TypeContext'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, getToString, isLite } from '../Signum.Entities'
import { EntityBaseController } from './EntityBase'
import { EntityListBaseController, EntityListBaseProps, DragConfig, MoveConfig } from './EntityListBase'
import { RenderEntity } from './RenderEntity'
import { newMListElement } from '../Signum.Entities';
import { tryGetTypeInfos, getTypeInfo } from '../Reflection';
import { useController } from './LineBase'
import { TypeBadge } from './AutoCompleteConfig'
import { Accordion } from 'react-bootstrap'
import { useForceUpdate } from '../Hooks'
import { AccordionEventKey } from 'react-bootstrap/esm/AccordionContext'

export interface EntityAccordionProps extends EntityListBaseProps {
  createAsLink?: boolean | ((er: EntityAccordionController) => React.ReactElement<any>);
  avoidFieldSet?: boolean;
  createMessage?: string;
  getTitle?: (ctx: TypeContext<any /*T*/>) => React.ReactChild;
  itemExtraButtons?: (er: EntityListBaseController<EntityListBaseProps>, index: number) => React.ReactElement<any>;
  initialSelectedIndex?: number | null;
  selectedIndex?: number | null;
  onSelectTab?: (newIndex: number | null) => void;
}

function isControlled(p: EntityAccordionProps) {

  if ((p.selectedIndex !== undefined) != (p.onSelectTab !== undefined))
    throw new Error("selectedIndex and onSelectTab should be set together");

  return p.selectedIndex != null;
}

export class EntityAccordionController extends EntityListBaseController<EntityAccordionProps> {

  selectedIndex!: number | null;
  setSelectedIndex!: (index: number | null) => void;
  initialIsControlled!: boolean;

  init(p: EntityAccordionProps) {
    super.init(p);

    this.initialIsControlled = React.useMemo(() => isControlled(p), []);
    const currentIsControlled = isControlled(p);
    if (currentIsControlled != this.initialIsControlled)
      throw new Error(`selectedIndex was isControlled=${this.initialIsControlled} but now is ${currentIsControlled}`);

    if (!this.initialIsControlled) {
      [this.selectedIndex, this.setSelectedIndex] = React.useState<number | null>(p.initialSelectedIndex ?? null);
    } else {
      this.selectedIndex = p.selectedIndex!;
      this.setSelectedIndex = p.onSelectTab!;
    }
  }

  getDefaultProps(p: EntityAccordionProps) {
    super.getDefaultProps(p);
    p.viewOnCreate = false;
    p.createAsLink = true;
  }

  addElement(entityOrLite: Lite<Entity> | ModifiableEntity) {

    if (isLite(entityOrLite) != (this.props.type!.isLite || false))
      throw new Error("entityOrLite should be already converted");

    const list = this.props.ctx.value!;
    list.push(newMListElement(entityOrLite));
    this.setSelectedIndex(list.length - 1);
    this.setValue(list);
  }
}


export const EntityAccordion = React.forwardRef(function EntityAccordion(props: EntityAccordionProps, ref: React.Ref<EntityAccordionController>) {
  var c = useController(EntityAccordionController, props, ref);
  var p = c.props;

  if (c.isHidden)
    return null;

  let ctx = p.ctx;

  if (p.avoidFieldSet == true)
    return (
      <div className={classes("sf-accordion-field sf-control-container", ctx.errorClassBorder)}
        {...{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes, ...ctx.errorAttributes() }}>
        {renderButtons()}
        {renderAccordion()}
      </div>
    );

  return (
    <fieldset className={classes("sf-accordion-field sf-control-container", ctx.errorClass)}
      {...{ ...c.baseHtmlAttributes(), ...c.props.formGroupHtmlAttributes, ...ctx.errorAttributes() }}>
      <legend>
        <div>
          <span>{p.label}</span>
          {renderButtons()}
        </div>
      </legend>
      {renderAccordion()}
    </fieldset>
  );


  function renderButtons() {
    const buttons = (
      <span className="float-end">
        {p.extraButtonsBefore && p.extraButtonsBefore(c)}
        {p.createAsLink == false && c.renderCreateButton(false, p.createMessage)}
        {c.renderFindButton(false)}
        {p.extraButtonsAfter && p.extraButtonsAfter(c)}
      </span>
    );

    return EntityBaseController.hasChildrens(buttons) ? buttons : undefined;
  }

  function handleSelectTab(eventKey: AccordionEventKey | null) {
    var num = eventKey == null ? null:  parseInt(eventKey as string);
    c.setSelectedIndex(num);
  }

  function renderAccordion() {
    const readOnly = ctx.readOnly;
    const showType = tryGetTypeInfos(ctx.propertyRoute!.typeReference().name).length > 1;
    return (
      <Accordion className="sf-accordion-elements" activeKey={c.selectedIndex?.toString()} onSelect={handleSelectTab}>
        {
          c.getMListItemContext(ctx).map((mlec, i) => (
            <EntityAccordionElement key={c.keyGenerator.getKey(mlec.value)}
              onRemove={c.canRemove(mlec.value) && !readOnly ? e => c.handleRemoveElementClick(e, mlec.index!) : undefined}
              ctx={mlec}
              move={c.canMove(mlec.value) && p.moveMode == "MoveIcons" && !readOnly ? c.getMoveConfig(false, mlec.index!, "v") : undefined}
              drag={c.canMove(mlec.value) && p.moveMode == "DragIcon" && !readOnly ? c.getDragConfig(mlec.index!, "v") : undefined}
              itemExtraButtons={p.itemExtraButtons ? (() => p.itemExtraButtons!(c, mlec.index!)) : undefined}
              getComponent={p.getComponent}
              getViewPromise={p.getViewPromise}
              getTitle={p.getTitle}
              title={showType ? <TypeBadge entity={mlec.value} /> : undefined} />))
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
      </Accordion>
    );
  }
});


export interface EntityAccordionElementProps {
  ctx: TypeContext<Lite<Entity> | ModifiableEntity>;
  getComponent?: (ctx: TypeContext<ModifiableEntity>) => React.ReactElement<any>;
  getViewPromise?: (entity: ModifiableEntity) => undefined | string | Navigator.ViewPromise<ModifiableEntity>;
  getTitle?: (ctx: TypeContext<any /*T*/>) => React.ReactChild;
  onRemove?: (event: React.MouseEvent<any>) => void;
  move?: MoveConfig;
  drag?: DragConfig;
  title?: React.ReactElement<any>;
  itemExtraButtons?: () => React.ReactElement<any>;
}

export function EntityAccordionElement({ ctx, getComponent, getViewPromise, onRemove, move, drag, itemExtraButtons, title, getTitle }: EntityAccordionElementProps)
{

  const forceUpdate = useForceUpdate();

  return (
    <Accordion.Item className={classes(drag?.dropClass, "sf-accordion-element")} eventKey={ctx.index!.toString()}
      onDragEnter={drag?.onDragOver}
      onDragOver={drag?.onDragOver}
      onDrop={drag?.onDrop}>

      <Accordion.Header {...EntityListBaseController.entityHtmlAttributes(ctx.value)}>
        <div className="d-flex align-items-center flex-grow-1">
          {onRemove && <a href="#" className={classes("sf-line-button", "sf-remove")}
            onClick={onRemove}
            title={ctx.titleLabels ? EntityControlMessage.Remove.niceToString() : undefined}>
            {EntityListBaseController.removeIcon}
          </a>}
          &nbsp;
          {move?.renderMoveUp()}
          {move?.renderMoveDown()}
          {drag && <a href="#" className={classes("sf-line-button", "sf-move")} onClick={e => { e.preventDefault(); e.stopPropagation(); } }
            draggable={true}
            onDragStart={drag.onDragStart}
            onDragEnd={drag.onDragEnd}
            onKeyDown={drag.onKeyDown}
            title={drag.title}>
            {EntityListBaseController.moveIcon}
          </a>}
          {itemExtraButtons && itemExtraButtons()}
          {'\xa0'}
          {getTitle ? getTitle(ctx) : getToString(ctx.value)}
        </div>
      </Accordion.Header>
      <Accordion.Body>
        <RenderEntity ctx={ctx} getComponent={getComponent} getViewPromise={getViewPromise} onRefresh={forceUpdate} />
      </Accordion.Body>
    </Accordion.Item>
  );
}
