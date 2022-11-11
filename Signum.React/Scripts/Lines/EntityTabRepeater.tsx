import * as React from 'react'
import { classes } from '../Globals'
import { TypeContext, mlistItemContext } from '../TypeContext'
import { ModifiableEntity, Lite, Entity, MListElement, EntityControlMessage, getToString } from '../Signum.Entities'
import { EntityListBaseController, EntityListBaseProps } from './EntityListBase'
import { RenderEntity } from './RenderEntity'
import { newMListElement } from '../Signum.Entities';
import { isLite } from '../Signum.Entities';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { EntityBaseController } from '../Lines';
import { EntityTableProps } from './EntityTable'
import { Tabs, Tab } from 'react-bootstrap'
import { useController } from './LineBase'

export interface EntityTabRepeaterProps extends EntityListBaseProps {
  createAsLink?: boolean | ((er: EntityTabRepeaterController) => React.ReactElement<any>);
  createMessage?: string;
  avoidFieldSet?: boolean;
  getTitle?: (ctx: TypeContext<any /*T*/>) => React.ReactChild;
  extraTabs?: (c: EntityTabRepeaterController) => React.ReactNode;
  selectedIndex?: number;
  onSelectTab?: (newIndex: number) => void;
}


function isControlled(p: EntityTabRepeaterProps) {

  if ((p.selectedIndex != null) != (p.onSelectTab != null))
    throw new Error("selectedIndex and onSelectTab should be set together");

  return p.selectedIndex != null;
}

export class EntityTabRepeaterController extends EntityListBaseController<EntityTabRepeaterProps> {

  selectedIndex!: number;
  setSelectedIndex!: (index: number) => void;
  initialIsControlled!: boolean;

  init(p: EntityTabRepeaterProps) {
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

  getDefaultProps(p: EntityTabRepeaterProps) {
    super.getDefaultProps(p);
    p.createAsLink = true;
    p.viewOnCreate = false;
  }

  removeElement(mle: MListElement<ModifiableEntity | Lite<Entity>>) {
    const list = this.props.ctx.value!;
    let deleteIndex = list.indexOf(mle);

    list.remove(mle);
    this.setSelectedIndex(coerce(deleteIndex < this.selectedIndex ? this.selectedIndex - 1 : this.selectedIndex, list.length));
    this.setValue(list);
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

export const EntityTabRepeater = React.forwardRef(function EntityTabRepeater(props: EntityTabRepeaterProps, ref: React.Ref<EntityTabRepeaterController>) {
  const c = useController(EntityTabRepeaterController, props, ref);
  const p = c.props;

  const ctx = p.ctx!;

  if (c.isHidden)
    return null;

  if (p.avoidFieldSet == true)
    return (
      <div className={classes("sf-repeater-field sf-control-container", ctx.errorClassBorder)}
        {...c.baseHtmlAttributes()} {...p.formGroupHtmlAttributes} {...ctx.errorAttributes()}>
        {renderButtons()}
        {renderTabs()}
      </div>
    );

  return (
    <fieldset className={classes("sf-repeater-field sf-control-container", ctx.errorClass)}
      {...c.baseHtmlAttributes()} {...p.formGroupHtmlAttributes} {...ctx.errorAttributes()}>
      <legend>
        <div>
          <span>{p.label}</span>
          {renderButtons()}
        </div>
      </legend>
      {renderTabs()}
    </fieldset>
  );

  function renderButtons() {
    const buttons = (
      <span className="ms-2">
        {p.extraButtonsBefore && p.extraButtonsBefore(c)}
        {!p.createAsLink && c.renderCreateButton(false, p.createMessage)}
        {c.renderFindButton(false)}
        {p.extraButtonsAfter && p.extraButtonsAfter(c)}
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
          c.getMListItemContext(ctx).map(mlec => {
            const drag = c.canMove(mlec.value) && p.moveMode == "DragIcon" && !readOnly ? c.getDragConfig(mlec.index!, "h") : undefined;
            const move = c.canMove(mlec.value) && p.moveMode == "MoveIcons" && !readOnly ? c.getMoveConfig(false, mlec.index!, "h") : undefined;

            return (
              <Tab eventKey={mlec.index!.toString()} key={c.keyGenerator.getKey(mlec.value)}
                {...EntityListBaseController.entityHtmlAttributes(mlec.value)}
                className="sf-repeater-element"
                title={
                  <div
                    className={classes("item-group", "sf-tab-dropable", drag?.dropClass)}
                    onDragEnter={drag?.onDragOver}
                    onDragOver={drag?.onDragOver}
                    onDrop={drag?.onDrop}>
                    {p.getTitle ? p.getTitle(mlec) : getToString(mlec.value)}
                    {c.canRemove(mlec.value) && !readOnly &&
                      <span className={classes("sf-line-button", "sf-remove", "ms-2")}
                        onClick={e => { e.stopPropagation(); c.handleRemoveElementClick(e, mlec.index!) }}
                        title={ctx.titleLabels ? EntityControlMessage.Remove.niceToString() : undefined}>
                        {EntityBaseController.removeIcon}
                      </span>
                    }
                    {drag && <span className={classes("sf-line-button", "sf-move", "ms-2")} onClick={e => { e.preventDefault(); e.stopPropagation(); }}
                      draggable={true}
                      onDragStart={drag.onDragStart}
                      onKeyDown={drag.onKeyDown}
                      onDragEnd={drag.onDragEnd}
                      title={drag.title}>
                      {EntityBaseController.moveIcon}
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
                {EntityBaseController.createIcon}&nbsp;{p.createMessage || EntityControlMessage.Create.niceToString()}
              </span>} />)
        }
        {p.extraTabs && p.extraTabs(c)}
      </Tabs>
    );
  }
});

function coerce(index: number, length: number): number {
  if (length <= index)
    index = length - 1;

  if (index < 0)
    return 0;

  return index;
}

