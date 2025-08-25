import * as React from 'react'
import { classes } from '../Globals'
import { Navigator, ViewPromise } from '../Navigator'
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
import { getTimeMachineIcon } from './TimeMachineIcon'
import { GroupHeader, HeaderType } from './GroupHeader'

export interface EntityAccordionProps<V extends ModifiableEntity> extends EntityListBaseProps<V> {
  createAsLink?: boolean | ((er: EntityAccordionController<V>) => React.ReactElement);
  avoidFieldSet?: boolean | HeaderType;
  createMessage?: string;
  getTitle?: (ctx: TypeContext<V>) => React.ReactElement | string;
  itemExtraButtons?: (ctx: TypeContext<V>, er: EntityAccordionController<V>) => React.ReactElement;
  itemHtmlAttributes?: (ctx: TypeContext<V>, er: EntityAccordionController<V>) => React.HTMLAttributes<any>;
  headerHtmlAttributes?: (ctx: TypeContext<V>, er: EntityAccordionController<V>) => React.HTMLAttributes<any>;
  initialSelectedIndex?: number | null;
  selectedIndex?: number | null;
  onSelectTab?: (newIndex: number | null) => void;
  ref?: React.Ref<EntityAccordionController<V>>
}

function isControlled(p: EntityAccordionProps<any>) {

  if ((p.selectedIndex !== undefined) != (p.onSelectTab !== undefined))
    throw new Error("selectedIndex and onSelectTab should be set together");

  return p.selectedIndex != null;
}

export class EntityAccordionController<V extends ModifiableEntity> extends EntityListBaseController<EntityAccordionProps<V>, V> {

  selectedIndex!: number | null;
  setSelectedIndex!: (index: number | null) => void;
  initialIsControlled!: boolean;

  init(p: EntityAccordionProps<V>): void {
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

    React.useEffect(() => {
      if (!this.initialIsControlled && p.initialSelectedIndex)
        this.setSelectedIndex(p.initialSelectedIndex);
    }, [p.initialSelectedIndex]);
  }

  getDefaultProps(p: EntityAccordionProps<V>): void {
    super.getDefaultProps(p);
    p.viewOnCreate = false;
    p.createAsLink = true;
  }

  addElement(entityOrLite: V): void {

    if (isLite(entityOrLite) != (this.props.type!.isLite || false))
      throw new Error("entityOrLite should be already converted");

    const list = this.props.ctx.value!;
    list.push(newMListElement(entityOrLite));
    this.setSelectedIndex(list.length - 1);
    this.setValue(list);
  }
}


export function EntityAccordion<V extends ModifiableEntity>(props: EntityAccordionProps<V>): React.JSX.Element | null {
  var c = useController(EntityAccordionController<V>, props);
  var p = c.props;

  if (c.isHidden)
    return null;

  let ctx = p.ctx;

  return (
    <GroupHeader className={classes("sf-accordion-field sf-control-container", c.getErrorClass("border"))}
      label={p.label}
      labelIcon={p.labelIcon}
      avoidFieldSet={p.avoidFieldSet}
      buttons={renderButtons()}
      htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes, ...c.errorAttributes() }} >
      {renderAccordion()}
    </GroupHeader >
  );

  function renderButtons() {
    const buttons = (
      <span className="float-end">
        {p.extraButtonsBefore && p.extraButtonsBefore(c)}
        {p.createAsLink == false && c.renderCreateButton(false, p.createMessage)}
        {c.renderFindButton(false)}
        {p.extraButtons && p.extraButtons(c)}
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
          c.getMListItemContext(ctx).map((mlec, i): React.ReactElement => (
            <EntityAccordionElement<V> key={i}
              onSelectTab={() => handleSelectTab(mlec.index!.toString())}
              onRemove={c.canRemove(mlec.value) && !readOnly ? e => c.handleRemoveElementClick(e, mlec.index!) : undefined}
              ctx={mlec}
              move={c.canMove(mlec.value) && p.moveMode == "MoveIcons" && !readOnly ? c.getMoveConfig(false, mlec.index!, "v") : undefined}
              drag={c.canMove(mlec.value) && p.moveMode == "DragIcon" && !readOnly ? c.getDragConfig(mlec.index!, "v") : undefined}
              itemExtraButtons={p.itemExtraButtons ? (() => p.itemExtraButtons!(mlec, c)) : undefined}
              getComponent={p.getComponent as (ctx: TypeContext<V>) => React.ReactElement}
              getViewPromise={p.getViewPromise as (entity: V) => undefined | string | ViewPromise<V>}
              getTitle={p.getTitle}
              htmlAttributes={p.itemHtmlAttributes?.(mlec, c) }
              headerHtmlAttributes={p.headerHtmlAttributes?.(mlec, c)}
              title={showType ? <TypeBadge entity={mlec.value} /> : undefined} />))
        }
        {
          p.createAsLink && p.create && !readOnly &&
          (typeof p.createAsLink == "function" ? p.createAsLink(c) :
            <a href="#" title={ctx.titleLabels ? EntityControlMessage.Create.niceToString() : undefined}
              className="sf-line-button sf-create"
              onClick={c.handleCreateClick}>
              {EntityBaseController.getCreateIcon()}&nbsp;{p.createMessage ?? EntityControlMessage.Create.niceToString()}
            </a>)
        }
      </Accordion>
    );
  }
}

export interface EntityAccordionElementProps<V extends ModifiableEntity> {
  ctx: TypeContext<V>;
  onSelectTab: () => void;
  getComponent?: (ctx: TypeContext<V>) => React.ReactElement;
  getViewPromise?: (entity: V) => undefined | string | ViewPromise<V>;
  getTitle?: (ctx: TypeContext<V>) => React.ReactElement | string;
  onRemove?: (event: React.MouseEvent<any>) => void;
  move?: MoveConfig;
  drag?: DragConfig;
  title?: React.ReactElement;
  itemExtraButtons?: () => React.ReactElement;
  htmlAttributes?: React.HTMLAttributes<any>;
  headerHtmlAttributes?: React.HTMLAttributes<any>;
}

export function EntityAccordionElement<V extends ModifiableEntity>({ ctx, getComponent, getViewPromise, onRemove, move, drag, itemExtraButtons, title, getTitle, htmlAttributes, headerHtmlAttributes, onSelectTab }: EntityAccordionElementProps<V>): React.ReactElement
{

  const forceUpdate = useForceUpdate();
  const refHtml = React.useRef<HTMLDivElement>(null);

  React.useEffect(() => {
    var div = refHtml.current;
    if (div) {

      function listener(e: Event) {
        onSelectTab();
      }

      div.addEventListener("openError", listener);
      return () => {
        div!.removeEventListener("openError", listener);
      }
    }
  }, [refHtml.current]);

  if (ctx.binding == null && ctx.previousVersion) {
    return (
      <Accordion.Item {...htmlAttributes} className={classes(drag?.dropClass, "sf-accordion-element")} eventKey="removed" title={EntityControlMessage.Removed0.niceToString()}>
        <h2 className="accordion-header bg-danger">
          <Accordion.Button>
            <div className="d-flex align-items-center flex-grow-1" style={{ backgroundColor: "#ff000021" }}>
              {getTimeMachineIcon({ ctx: ctx, isContainer: true })}
            </div>
          </Accordion.Button>
        </h2>
        <Accordion.Body>
        </Accordion.Body>
      </Accordion.Item>
    );
  }

  return (
    <Accordion.Item {...htmlAttributes} className={classes(drag?.dropClass, "sf-accordion-element")} eventKey={ctx.index!.toString()}
      ref={refHtml }
      onDragEnter={drag?.onDragOver}
      onDragOver={drag?.onDragOver}
      onDrop={drag?.onDrop}
      data-error-container={ctx.prefix}
    >

      <Accordion.Header {...EntityBaseController.entityHtmlAttributes(ctx.value)} {...headerHtmlAttributes}>
        <div className="d-flex align-items-center flex-grow-1">
          {getTimeMachineIcon({ ctx: ctx, isContainer: true })}
          {onRemove && <a href="#" className={classes("sf-line-button", "sf-remove")}
            onClick={onRemove}
            title={ctx.titleLabels ? EntityControlMessage.Remove.niceToString() : undefined}>
            {EntityBaseController.getRemoveIcon()}
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
            {EntityBaseController.getMoveIcon()}
          </a>}
          {itemExtraButtons && itemExtraButtons()}
          {'\xa0'}
          {getTitle ? getTitle(ctx) : getToString(ctx.value)}
        </div>
      </Accordion.Header>
      <Accordion.Body>
        <RenderEntity ctx={ctx} getComponent={getComponent as any} getViewPromise={getViewPromise as any} onRefresh={forceUpdate} />
      </Accordion.Body>
    </Accordion.Item>
  );
}
