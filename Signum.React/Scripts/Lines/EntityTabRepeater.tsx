import * as React from 'react'
import { classes } from '../Globals'
import { TypeContext, mlistItemContext } from '../TypeContext'
import { ModifiableEntity, Lite, Entity, MListElement, EntityControlMessage, getToString } from '../Signum.Entities'
import { EntityListBase, EntityListBaseProps } from './EntityListBase'
import { RenderEntity } from './RenderEntity'
import { Tab, Tabs } from '../Components/Tabs';
import { newMListElement } from '../Signum.Entities';
import { isLite } from '../Signum.Entities';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { TitleManager } from './EntityBase';

export interface EntityTabRepeaterProps extends EntityListBaseProps {
  createAsLink?: boolean;
  avoidFieldSet?: boolean;
  selectedIndex?: number;
  getTitle?: (mlec: TypeContext<any /*T*/>) => React.ReactChild;
  extraTabs?: (c: EntityTabRepeater) => React.ReactNode;
  onSelectTab?: (newIndex: number) => void;
}

export interface EntityTabRepeaterState extends EntityTabRepeaterProps {
  selectedIndex?: number;
}

export class EntityTabRepeater extends EntityListBase<EntityTabRepeaterProps, EntityTabRepeaterState> {
  calculateDefaultState(state: EntityTabRepeaterProps) {
    super.calculateDefaultState(state);

    state.selectedIndex = this.state == null ? 0 :
      coerce(this.state.selectedIndex, this.state.ctx.value.length);

    state.viewOnCreate = false;
  }

  renderInternal() {

    var ctx = this.state.ctx!;

    if (this.props.avoidFieldSet == true)
      return (
        <div className={classes("SF-repeater-field SF-control-container", ctx.errorClassBorder)}
          {...this.baseHtmlAttributes()} {...this.state.formGroupHtmlAttributes}>
          {this.renderButtons()}
          {this.renderTabs()}
        </div>
      );

    return (
      <fieldset className={classes("SF-repeater-field SF-control-container", ctx.errorClass)}
        {...this.baseHtmlAttributes()} {...this.state.formGroupHtmlAttributes}>
        <legend>
          <div>
            <span>{this.state.labelText}</span>
            {this.renderButtons()}
          </div>
        </legend>
        {this.renderTabs()}
      </fieldset>
    );
  }

  renderButtons() {
    const buttons = (
      <span className="ml-2">
        {this.renderCreateButton(false)}
        {this.renderFindButton(false)}
        {this.props.extraButtons && this.props.extraButtons(this)}
      </span>
    );

    return React.Children.count(buttons) ? buttons : undefined;
  }

  handleSelectTab = (activeKey: string | number) => {
    if (this.props.onSelectTab)
      this.props.onSelectTab(activeKey as number);
    else
      this.setState({ selectedIndex: activeKey as number })
  }

  renderTabs() {
    const ctx = this.state.ctx!;
    const readOnly = ctx.readOnly;

    return (
      <Tabs activeEventKey={this.state.selectedIndex || 0} toggle={this.handleSelectTab}>
        {
          mlistItemContext(ctx).map((mlec, i) => {
            const drag = this.canMove(mlec.value) && !readOnly ? this.getDragConfig(i, "h") : undefined;

            return <Tab eventKey={i} key={i}
              {...EntityListBase.entityHtmlAttributes(mlec.value)}
              className="sf-repeater-element"
              title={
                <div
                  className={classes("item-group", "sf-tab-dropable", drag && drag.dropClass)}
                  onDragEnter={drag && drag.onDragOver}
                  onDragOver={drag && drag.onDragOver}
                  onDrop={drag && drag.onDrop}>
                  {this.props.getTitle ? this.props.getTitle(mlec) : getToString(mlec.value)}
                  &nbsp;
                {this.canRemove(mlec.value) && !readOnly &&
                    <span className={classes("sf-line-button", "sf-remove")}
                      onClick={e => { e.stopPropagation(); this.handleRemoveElementClick(e, i) }}
                      title={TitleManager.useTitle ? EntityControlMessage.Remove.niceToString() : undefined}>
                      <FontAwesomeIcon icon="times" />
                    </span>
                  }
                  &nbsp;
                {drag && <span className={classes("sf-line-button", "sf-move")}
                    draggable={true}
                    onDragStart={drag.onDragStart}
                    onDragEnd={drag.onDragEnd}
                    title={TitleManager.useTitle ? EntityControlMessage.Move.niceToString() : undefined}>
                    <FontAwesomeIcon icon="bars" />
                  </span>}
                </div> as any
              }>
              <RenderEntity ctx={mlec} getComponent={this.props.getComponent} getViewPromise={this.props.getViewPromise} />
            </Tab>
          })
        }
        {this.props.extraTabs && this.props.extraTabs(this)}
      </Tabs>
    );
  }

  removeElement(mle: MListElement<ModifiableEntity | Lite<Entity>>) {
    const list = this.props.ctx.value!;
    let currentIndex = list.indexOf(mle);
    if (this.state.selectedIndex != null && this.state.selectedIndex < currentIndex)
      this.state.selectedIndex--

    list.remove(mle);

    this.state.selectedIndex = coerce(this.state.selectedIndex, list.length);

    this.setValue(list);
  }

  addElement(entityOrLite: Lite<Entity> | ModifiableEntity) {

    if (isLite(entityOrLite) != (this.state.type!.isLite || false))
      throw new Error("entityOrLite should be already converted");

    const list = this.props.ctx.value!;
    list.push(newMListElement(entityOrLite));
    this.state.selectedIndex = list.length - 1;
    this.setValue(list);
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

