import * as React from 'react'
import { ModifiableEntity, Lite, Entity, is, getToString, EntityControlMessage, MList } from '../Signum.Entities'
import { FormGroup } from './FormGroup'
import { EntityListBaseController, EntityListBaseProps } from './EntityListBase'
import { genericMemo, useController } from './LineBase';
import { classes } from '../Globals';
import { EntityBaseController } from './EntityBase';
import { LinkButton } from '../Basics/LinkButton';

export interface EntityListProps<V extends Lite<Entity> | ModifiableEntity> extends EntityListBaseProps<V> {
  size?: number;
  ref?: React.Ref<EntityListController<V>>;
}

export class EntityListController<V extends Lite<Entity> | ModifiableEntity> extends EntityListBaseController<EntityListProps<V>, V>
{
  override moveUp(index: number): void {
    super.moveUp(index);
    this.forceUpdate();
  }

  override moveDown(index: number): void {
    super.moveDown(index);
    this.forceUpdate();
  }

  handleOnSelect = (e: React.FormEvent<HTMLSelectElement>): void => {
    this.forceUpdate();
  }


  selectElement?: HTMLSelectElement | null;
  handleSelectLoad = (sel: HTMLSelectElement | null): void => {
    let refresh = this.selectElement == undefined && sel;

    this.selectElement = sel;

    if (refresh)
      this.forceUpdate();
  }

  getSelectedIndex(): number | undefined {
    if (this.selectElement == null || this.selectElement.selectedIndex == -1)
      return undefined;


    var list = this.props.ctx.value;
    if (list.length <= this.selectElement.selectedIndex)
      return undefined;

    return this.selectElement.selectedIndex;
  }

  handleRemoveClick = (event: React.SyntheticEvent<any>): void => {

    event.preventDefault();

    const p = this.props;
    var list = p.ctx.value!;
    var selectedIndex = this.getSelectedIndex()!;

    (p.onRemove ? p.onRemove(list[selectedIndex].element) : Promise.resolve(true))
      .then(result => {
        if (result == false)
          return;

        list.removeAt(selectedIndex!);

        this.setValue(list, event);
      });
  };

  renderViewButton(btn: boolean, item: V): React.ReactElement | undefined {

    if (!this.canView(item))
      return undefined;

    return (
      <LinkButton className={classes("sf-line-button", "sf-view", btn ? "input-group-text" : undefined)}
        onClick={this.handleViewClick}
        title={this.props.ctx.titleLabels ? EntityControlMessage.View.niceToString() : undefined}>
        {EntityBaseController.getViewIcon()}
      </LinkButton>
    );
  }

  renderRemoveButton(btn: boolean, item: V): React.ReactElement | undefined {
    if (!this.canRemove(item))
      return undefined;

    return (
      <LinkButton className={classes("sf-line-button", "sf-remove", btn ? "input-group-text" : undefined)}
        onClick={this.handleRemoveClick}
        title={this.props.ctx.titleLabels ? EntityControlMessage.Remove.niceToString() : undefined}>
        {EntityBaseController.getRemoveIcon()}
      </LinkButton>
    );
  }

  handleViewClick = (event: React.MouseEvent<any>): void => {

    event.preventDefault();

    const ctx = this.props.ctx;
    const selectedIndex = this.getSelectedIndex()!;
    const list = ctx.value!;
    const entity = list[selectedIndex].element;

    const pr = ctx.propertyRoute!.addLambda(a => a[0]);

    const openWindow = (event.button == 1 || event.ctrlKey) && !this.props.type!.isEmbedded;

    const promise = this.props.onView ?
      this.props.onView(entity, pr) :
      this.defaultView(entity, pr);

    if (promise == null)
      return;

    promise.then(e => {
      if (e == undefined)
        return;

      this.convert(e).then(m => {
        if (is(list[selectedIndex].element as Entity, e as Entity)) {
          list[selectedIndex].element = m;
          if ((e as ModifiableEntity).modified)
            this.setValue(list, event);
        }
        else {
          list[selectedIndex] = { rowId: null, element: m };
          this.setValue(list, event);
        }
      });
    });
  }

  getTitle(e: Lite<Entity> | ModifiableEntity): string {

    const pr = this.props.ctx.propertyRoute;

    const type = pr?.member?.niceName || (e as Lite<Entity>).EntityType || (e as ModifiableEntity).Type;

    const id = (e as Lite<Entity>).id || (e as Entity).id;

    return type + (id ? " " + id : "");
  }
}


export const EntityList: <V extends Lite<Entity> | ModifiableEntity>(props: EntityListProps<V>) => React.ReactNode | null =
  genericMemo(function EntityList<V extends Lite<Entity> | ModifiableEntity>(props: EntityListProps<V>) {
  const c = useController<EntityListController<V>, EntityListProps<V>, MList<V>>(EntityListController, props);
  const p = c.props;
  const list = p.ctx.value!;

  const selectedIndex = c.getSelectedIndex();

  if (c.isHidden)
    return null;

  return (
    <FormGroup ctx={p.ctx} error={p.error} label={p.label} labelIcon={p.labelIcon}
      htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }}
      labelHtmlAttributes={p.labelHtmlAttributes}>
      {inputId => <div className="sf-entity-line">
        <div className={p.ctx.inputGroupClass}>
          <select id={inputId} className={p.ctx.formSelectClass} size={p.size ?? 30} style={{ height: "120px", overflow: "auto" }} onChange={c.handleOnSelect} ref={c.handleSelectLoad}>
            {list.map(mle => <option key={c.keyGenerator.getKey(mle)} title={p.ctx.titleLabels ? c.getTitle(mle.element) : undefined} {...EntityBaseController.entityHtmlAttributes(mle.element)}>{getToString(mle.element)}</option>)}
          </select>
          <span className="input-group-vertical">
            {c.renderCreateButton(true)}
            {c.renderFindButton(true)}
            {selectedIndex != undefined && c.renderViewButton(true, list[selectedIndex].element)}
            {selectedIndex != undefined && c.renderRemoveButton(true, list[selectedIndex].element)}
            {selectedIndex != undefined && p.move && selectedIndex != null && selectedIndex > 0 && c.renderMoveUp(true, selectedIndex!, "v")}
            {selectedIndex != undefined && p.move && selectedIndex != null && selectedIndex < list.length - 1 && c.renderMoveDown(true, selectedIndex!, "v")}
          </span>
        </div>
      </div>}
    </FormGroup>
  );
});
