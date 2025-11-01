import * as React from 'react'
import { classes } from '../Globals'
import { TypeContext, mlistItemContext } from '../TypeContext'
import { ModifiableEntity, Lite, Entity, MListElement, EntityControlMessage, getToString, MList } from '../Signum.Entities'
import { EntityListBaseController, EntityListBaseProps } from './EntityListBase'
import { RenderEntity } from './RenderEntity'
import { newMListElement } from '../Signum.Entities';
import { isLite } from '../Signum.Entities';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { EntityBaseController } from '../Lines';
import { EntityTableProps } from './EntityTable'
import { Tabs, Tab } from 'react-bootstrap'
import { useController } from './LineBase'
import { getTimeMachineIcon } from './TimeMachineIcon'
import { GroupHeader, HeaderType } from './GroupHeader'

export interface EntityTabRepeaterProps<V extends ModifiableEntity> extends EntityListBaseProps<V> {
  createAsLink?: boolean | ((er: EntityTabRepeaterController<V>) => React.ReactElement);
  createMessage?: string;
  avoidFieldSet?: boolean | HeaderType;
  getTitle?: (ctx: TypeContext<V>) => React.ReactElement | string;
  extraTabs?: (c: EntityTabRepeaterController<V>) => React.ReactNode;
  selectedIndex?: number;
  onSelectTab?: (newIndex: number) => void;
  ref?: React.Ref<EntityTabRepeaterController<V>>
}


function isControlled(p: EntityTabRepeaterProps<any>) {

  if ((p.selectedIndex != null) != (p.onSelectTab != null))
    throw new Error("selectedIndex and onSelectTab should be set together");

  return p.selectedIndex != null;
}

export class EntityTabRepeaterController<V extends ModifiableEntity> extends EntityListBaseController<EntityTabRepeaterProps<V>, V> {

  selectedIndex!: number;
  setSelectedIndex!: (index: number) => void;
  initialIsControlled!: boolean;

  init(p: EntityTabRepeaterProps<V>): void {
    super.init(p);

    this.initialIsControlled = React.useMemo(() => isControlled(p), []);
    const currentIsControlled = isControlled(p);
    if (currentIsControlled != this.initialIsControlled)
      throw new Error(`selectedIndex was isControlled=${this.initialIsControlled} but now is ${currentIsControlled}`);

    if (!this.initialIsControlled) {
      [this.selectedIndex, this.setSelectedIndex] = React.useState(0);
    } else {
      this.selectedIndex = p.selectedIndex!;
      this.setSelectedIndex = p.onSelectTab!;
    }
  }

  getDefaultProps(p: EntityTabRepeaterProps<V>): void {
    super.getDefaultProps(p);
    p.createAsLink = true;
    p.viewOnCreate = false;
  }

  removeElement(mle: MListElement<V>): void {
    const list = this.props.ctx.value!;
    let deleteIndex = list.indexOf(mle);

    list.remove(mle);
    this.setSelectedIndex(coerce(deleteIndex < this.selectedIndex ? this.selectedIndex - 1 : this.selectedIndex, list.length));
    this.setValue(list);
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

export function EntityTabRepeater<V extends ModifiableEntity>(props: EntityTabRepeaterProps<V>): React.JSX.Element | null {
  const c = useController<EntityTabRepeaterController<V>, EntityTabRepeaterProps<V>, MList<V>>(EntityTabRepeaterController, props);
  const p = c.props;

  const ctx = p.ctx!;

  if (c.isHidden)
    return null;

  return (
    <GroupHeader className={classes("sf-repeater-field sf-control-container", c.getErrorClass("border"))}
      label={p.label}
      labelIcon={p.labelIcon}
      avoidFieldSet={p.avoidFieldSet}
      buttons={renderButtons()}
      htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes, ...c.errorAttributes() }} >
      {renderTabs()}
    </GroupHeader >
  );

  function renderButtons() {
    const buttons = (
      <span className="ms-2">
        {p.extraButtonsBefore && p.extraButtonsBefore(c)}
        {!p.createAsLink && c.renderCreateButton(false, p.createMessage)}
        {c.renderFindButton(false)}
        {p.extraButtons && p.extraButtons(c)}
      </span>
    );

    return React.Children.count(buttons) ? buttons : undefined;
  }

  function handleSelectTab(eventKey: string | null) {
    var num = parseInt(eventKey ?? "");
    if (!isNaN(num)) { //Create tab
      c.setSelectedIndex(num);
    }
  }

  function renderTabs() {
    const ctx = p.ctx!;
    const readOnly = ctx.readOnly;
    return (
      <Tabs activeKey={c.selectedIndex || 0} onSelect={handleSelectTab} id={ctx.prefix + "_tab"} transition={false} mountOnEnter unmountOnExit>
        {
          c.getMListItemContext(ctx).map((mlec, i): React.ReactElement => {

            if (mlec.binding == null && mlec.previousVersion) {
              return (
                <Tab eventKey={i} key={i} style={{ minWidth:150 }}
                  className="sf-repeater-element"
                  title={<div className="item-group" > {getTimeMachineIcon({ ctx: mlec, translateX: "-115%", translateY: "-65%" })} </div>}>
                </Tab>
              );
            }

            const drag = c.canMove(mlec.value) && p.moveMode == "DragIcon" && !readOnly ? c.getDragConfig(mlec.index!, "h") : undefined;
            const move = c.canMove(mlec.value) && p.moveMode == "MoveIcons" && !readOnly ? c.getMoveConfig(false, mlec.index!, "h") : undefined;

            return (
              <Tab eventKey={i} key={c.keyGenerator.getKey(mlec.value)}
                {...EntityBaseController.entityHtmlAttributes(mlec.value)}
                className="sf-repeater-element"
                title={
                  <div
                    className={classes("item-group", "sf-tab-dropable", drag?.dropClass)}
                    onDragEnter={drag?.onDragOver}
                    onDragOver={drag?.onDragOver}
                    onDrop={drag?.onDrop}>
                    {getTimeMachineIcon({ ctx: mlec, translateX: "-115%", translateY:"-65%"  })}
                    {p.getTitle ? p.getTitle(mlec) : getToString(mlec.value)}
                    {c.canRemove(mlec.value) && !readOnly &&
                      <span className={classes("sf-line-button", "sf-remove", "ms-2")}
                        onClick={e => { e.stopPropagation(); c.handleRemoveElementClick(e, mlec.index!) }}
                        title={ctx.titleLabels ? EntityControlMessage.Remove.niceToString() : undefined}>
                        {EntityBaseController.getTrashIcon()}
                      </span>
                    }
                    {drag && <span className={classes("sf-line-button", "sf-move", "ms-2")} onClick={e => { e.preventDefault(); e.stopPropagation(); }}
                      draggable={true}
                      onDragStart={drag.onDragStart}
                      onKeyDown={drag.onKeyDown}
                      onDragEnd={drag.onDragEnd}
                      title={drag.title}>
                      {EntityBaseController.getMoveIcon()}
                    </span>}
                    {move?.renderMoveUp()}
                    {move?.renderMoveDown()}
                  </div> as any
                }>
                <RenderEntity ctx={mlec} getComponent={p.getComponent} getViewPromise={p.getViewPromise} onRefresh={c.forceUpdate} />
              </Tab>
            );
          })
        }
        {
          p.createAsLink && p.create && !readOnly &&
          (typeof p.createAsLink == "function" ? p.createAsLink(c) :
            <Tab eventKey="create-new" title={
              <span className="sf-line-button sf-create" onClick={c.handleCreateClick} title={ctx.titleLabels ? EntityControlMessage.Create.niceToString() : undefined}>
                {EntityBaseController.getCreateIcon()}&nbsp;{p.createMessage || EntityControlMessage.Create.niceToString()}
              </span>} />)
        }
        {p.extraTabs && p.extraTabs(c)}
      </Tabs>
    );
  }
}

function coerce(index: number, length: number): number {
  if (length <= index)
    index = length - 1;

  if (index < 0)
    return 0;

  return index;
}

