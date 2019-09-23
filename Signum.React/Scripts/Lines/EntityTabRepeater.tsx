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
import { SelectCallback } from 'react-bootstrap/helpers'

export interface EntityTabRepeaterProps extends EntityListBaseProps {
  createAsLink?: boolean | ((er: EntityTabRepeaterController) => React.ReactElement<any>);
  createMessage?: string;
  avoidFieldSet?: boolean;
  selectedIndex?: number;
  getTitle?: (ctx: TypeContext<any /*T*/>) => React.ReactChild;
  extraTabs?: (c: EntityTabRepeaterController) => React.ReactNode;
  onSelectTab?: (newIndex: number) => void;
}

export interface EntityTabRepeaterState extends EntityTabRepeaterProps {
  selectedIndex?: number;
}

export class EntityTabRepeaterController extends EntityListBaseController<EntityTabRepeaterProps> {

  selectedIndex?: number;
  setSelectedIndex: (index: number | undefined) => void;

  constructor(p: EntityTabRepeaterProps) {
    super(p);
    [this.selectedIndex, this.setSelectedIndex] = React.useState();
  }


  getDefaultProps(p: EntityTabRepeaterProps) {
    super.getDefaultProps(p);

    this.setSelectedIndex(this.selectedIndex == null ? 0 : coerce(this.selectedIndex, p.ctx.value.length));

    p.createAsLink = true;
    p.viewOnCreate = false;
  }

  removeElement(mle: MListElement<ModifiableEntity | Lite<Entity>>) {
    const list = this.props.ctx.value!;
    let currentIndex = list.indexOf(mle);
    if (this.selectedIndex != null && this.selectedIndex < currentIndex)
      this.setSelectedIndex(this.selectedIndex - 1);

    list.remove(mle);

    this.setSelectedIndex(coerce(this.selectedIndex, list.length));

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

export function EntityTabRepeater(props: EntityTabRepeaterProps) {
  const c = new EntityTabRepeaterController(props);
  const p = c.props;

  const ctx = p.ctx!;

  if (c.isHidden)
    return null;

  if (p.avoidFieldSet == true)
    return (
      <div className={classes("SF-repeater-field SF-control-container", ctx.errorClassBorder)}
        {...c.baseHtmlAttributes()} {...p.formGroupHtmlAttributes} {...ctx.errorAttributes()}>
        {renderButtons()}
        {renderTabs()}
      </div>
    );

  return (
    <fieldset className={classes("SF-repeater-field SF-control-container", ctx.errorClass)}
      {...c.baseHtmlAttributes()} {...p.formGroupHtmlAttributes} {...ctx.errorAttributes()}>
      <legend>
        <div>
          <span>{p.labelText}</span>
          {renderButtons()}
        </div>
      </legend>
      {renderTabs()}
    </fieldset>
  );

  function renderButtons() {
    const buttons = (
      <span className="ml-2">
        {!p.createAsLink && c.renderCreateButton(false, p.createMessage)}
        {c.renderFindButton(false)}
        {p.extraButtons && p.extraButtons(c)}
      </span>
    );

    return React.Children.count(buttons) ? buttons : undefined;
  }

  function handleSelectTab(eventKey: any) {
    if (p.onSelectTab)
      p.onSelectTab(eventKey as number);
    else
      c.setSelectedIndex(eventKey as number);
  }

  function renderTabs() {
    const ctx = p.ctx!;
    const readOnly = ctx.readOnly;

    return (
      <Tabs activeKey={c.selectedIndex || 0} onSelect={handleSelectTab} id={ctx.prefix + "_tab"}>
        {
          c.getMListItemContext(ctx).map(mlec => {
            const drag = c.canMove(mlec.value) && !readOnly ? c.getDragConfig(mlec.index!, "h") : undefined;

            return (
              <Tab eventKey={mlec.index!} key={c.keyGenerator.getKey(mlec.value)}
                {...EntityListBaseController.entityHtmlAttributes(mlec.value)}
                className="sf-repeater-element"
                title={
                  <div
                    className={classes("item-group", "sf-tab-dropable", drag && drag.dropClass)}
                    onDragEnter={drag && drag.onDragOver}
                    onDragOver={drag && drag.onDragOver}
                    onDrop={drag && drag.onDrop}>
                    {p.getTitle ? p.getTitle(mlec) : getToString(mlec.value)}
                    {c.canRemove(mlec.value) && !readOnly &&
                      <span className={classes("sf-line-button", "sf-remove", "ml-2")}
                        onClick={e => { e.stopPropagation(); c.handleRemoveElementClick(e, mlec.index!) }}
                        title={ctx.titleLabels ? EntityControlMessage.Remove.niceToString() : undefined}>
                        {EntityBaseController.removeIcon}
                      </span>
                    }
                    {drag && <span className={classes("sf-line-button", "sf-move", "ml-2")}
                      draggable={true}
                      onDragStart={drag.onDragStart}
                      onDragEnd={drag.onDragEnd}
                      title={ctx.titleLabels ? EntityControlMessage.Move.niceToString() : undefined}>
                      {EntityBaseController.moveIcon}
                    </span>}
                  </div> as any
                }>
                <RenderEntity ctx={mlec} getComponent={p.getComponent} getViewPromise={p.getViewPromise} />
              </Tab>
            );
          })
        }
        {
          p.createAsLink && p.create && !readOnly &&
          (typeof p.createAsLink == "function" ? p.createAsLink(c) :
            <a href="#" title={ctx.titleLabels ? EntityControlMessage.Create.niceToString() : undefined}
              className="sf-line-button sf-create nav-link"
              onClick={c.handleCreateClick}>
              {EntityBaseController.createIcon}&nbsp;{p.createMessage || EntityControlMessage.Create.niceToString()}
            </a>)
        }
        {p.extraTabs && p.extraTabs(c)}
      </Tabs>
    );
  }
}

function coerce(index: number | undefined, length: number): number | undefined {
  if (index == undefined)
    return undefined;

  if (length <= index)
    index = length - 1;

  if (index < 0)
    return undefined;

  return index;
}

