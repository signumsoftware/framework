import * as React from 'react'
import { classes, KeyGenerator } from '../Globals'
import { ModifiableEntity, Lite, Entity, MListElement, MList, EntityControlMessage, newMListElement, isLite } from '../Signum.Entities'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, mlistItemContext } from '../TypeContext'
import { EntityBaseController, EntityBaseProps } from './EntityBase'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

export interface EntityListBaseProps extends EntityBaseProps {
  move?: boolean | ((item: ModifiableEntity | Lite<Entity>) => boolean);
  onFindMany?: () => Promise<(ModifiableEntity | Lite<Entity>)[] | undefined> | undefined;
  filterRows?: (ctxs: TypeContext<any /*T*/>[]) => TypeContext<any /*T*/>[]; /*Not only filter, also order, skip, take is supported*/
  ctx: TypeContext<MList<any /*Lite<Entity> | ModifiableEntity*/>>;
}

export interface EntityListBaseState extends EntityListBaseProps {
  dragIndex?: number,
  dropBorderIndex?: number,
}

export abstract class EntityListBaseController<T extends EntityListBaseProps> extends EntityBaseController<T>
{
  dragIndex!: number | undefined;
  setDragIndex!: React.Dispatch<number | undefined>;
  dropBorderIndex!: number | undefined
  setDropBorderIndex!: React.Dispatch<number | undefined>;

  init(p: T) {
    super.init(p);
    [this.dragIndex, this.setDragIndex] = React.useState<number | undefined>(undefined);
    [this.dropBorderIndex, this.setDropBorderIndex] = React.useState<number | undefined>(undefined);
  }

  keyGenerator = new KeyGenerator();
  getDefaultProps(state: T) {

    if (state.onFind)
      throw new Error(`'onFind' property is not applicable to '${this}'. Use 'onFindMany' instead`);

    super.getDefaultProps(state);
  }

  setValue(list: MList<Lite<Entity> | ModifiableEntity>) {
    super.setValue(list as any);
  }

  getMListItemContext<T>(ctx: TypeContext<MList<T>>): TypeContext<T>[] {
    var rows = mlistItemContext(ctx);

    if (this.props.filterRows)
      return this.props.filterRows(rows);

    return rows;
  }

  moveUp(index: number) {
    const list = this.props.ctx.value!;
    list.moveUp(index);
    this.setValue(list);
  }

  renderMoveUp(btn: boolean, index: number) {
    if (!this.canMove(this.props.ctx.value[index].element) || this.props.ctx.readOnly)
      return undefined;

    return (
      <a href="#" className={classes("sf-line-button", "sf-move", "sf-move-step", btn ? "btn input-group-text" : undefined)}
        onClick={e => { e.preventDefault(); this.moveUp(index); }}
        title={this.props.ctx.titleLabels ? EntityControlMessage.MoveUp.niceToString() : undefined}>
        <FontAwesomeIcon icon="chevron-up" />
      </a>
    );
  }

  doView(entity: ModifiableEntity | Lite<Entity>) {
    const pr = this.props.ctx.propertyRoute!.addLambda(a => a[0]);
    return this.props.onView ?
      this.props.onView(entity, pr) :
      this.defaultView(entity, pr);
  }

  moveDown(index: number) {
    const list = this.props.ctx.value!;
    list.moveDown(index);
    this.setValue(list);
  }

  renderMoveDown(btn: boolean, index: number) {
    if (!this.canMove(this.props.ctx.value[index].element) || this.props.ctx.readOnly)
      return undefined;

    return (
      <a href="#" className={classes("sf-line-button", "sf-move", "sf-move-step", btn ? "btn input-group-text" : undefined)}
        onClick={e => { e.preventDefault(); this.moveDown(index); }}
        title={this.props.ctx.titleLabels ? EntityControlMessage.MoveUp.niceToString() : undefined}>
        <FontAwesomeIcon icon="chevron-down" />
      </a>);
  }


  handleCreateClick = (event: React.SyntheticEvent<any>) => {

    event.preventDefault();
    event.stopPropagation();
    var pr = this.props.ctx.propertyRoute!.addLambda(a => a[0]);

    const promise = this.props.onCreate ? this.props.onCreate(pr) : this.defaultCreate(pr);

    if (promise == null)
      return;

    promise
      .then<ModifiableEntity | Lite<Entity> | undefined>(e => {

        if (e == undefined)
          return undefined;

        if (!this.props.viewOnCreate)
          return Promise.resolve(e);

        return this.doView(e);

      }).then(e => {

        if (!e)
          return;

        this.convert(e)
          .then(m => this.addElement(m))
          .done();
      }).done();
  };

  defaultFindMany(): Promise<(ModifiableEntity | Lite<Entity>)[] | undefined> {

    if (this.props.findOptions) {
      return Finder.findMany(this.props.findOptions, { searchControlProps: { create: this.props.createOnFind } });
    }

    return this.chooseType(ti => Finder.isFindable(ti, false))
      .then<(ModifiableEntity | Lite<Entity>)[] | undefined>(qn => qn == undefined ? undefined :
        Finder.findMany({ queryName: qn } as FindOptions, { searchControlProps: { create: this.props.createOnFind } }));
  }

  addElement(entityOrLite: Lite<Entity> | ModifiableEntity) {

    if (isLite(entityOrLite) != (this.props.type!.isLite || false))
      throw new Error("entityOrLite should be already converted");

    const list = this.props.ctx.value!;
    list.push(newMListElement(entityOrLite));
    this.setValue(list);
  }


  handleFindClick = (event: React.SyntheticEvent<any>) => {

    event.preventDefault();

    const promise = this.props.onFindMany ? this.props.onFindMany() : this.defaultFindMany();

    if (promise == null)
      return;

    promise.then(lites => {
      if (!lites)
        return;

      Promise.all(lites.map(a => this.convert(a))).then(entites => {
        entites.forEach(e => this.addElement(e));
      }).done();
    }).done();
  };

  handleRemoveElementClick = (event: React.SyntheticEvent<any>, index: number) => {

    event.preventDefault();

    const list = this.props.ctx.value!;
    const mle = list[index];

    (this.props.onRemove ? this.props.onRemove(mle.element) : Promise.resolve(true))
      .then(result => {
        if (result == false)
          return;

        this.removeElement(mle)
      }).done();
  };

  removeElement(mle: MListElement<ModifiableEntity | Lite<Entity>>) {
    const list = this.props.ctx.value!;
    list.remove(mle);
    this.setValue(list);
  }

  canMove(item: ModifiableEntity | Lite<Entity>): boolean | undefined {

    const move = this.props.move;

    if (move == undefined)
      return undefined;

    if (typeof move === "function")
      return move(item);

    return move;
  }


  handleDragStart = (de: React.DragEvent<any>, index: number) => {
    de.dataTransfer.setData('text', "start"); //cannot be empty string
    de.dataTransfer.effectAllowed = "move";
    this.setDragIndex(index);
  }

  handleDragEnd = (de: React.DragEvent<any>) => {
    this.setDragIndex(undefined);
    this.setDropBorderIndex(undefined);
    this.forceUpdate();
  }

  getOffsetHorizontal(dragEvent: DragEvent, rect: ClientRect) {

    const margin = Math.min(50, rect.width / 2);

    const width = rect.width;
    const offsetX = dragEvent.pageX - rect.left;

    if (offsetX < margin)
      return 0;

    if (offsetX > (width - margin))
      return 1;

    return undefined;
  }

  getOffsetVertical(dragEvent: DragEvent, rect: ClientRect) {

    var margin = Math.min(50, rect.height / 2);

    const height = rect.height;
    const offsetY = dragEvent.pageY - rect.top;

    if (offsetY < margin)
      return 0;

    if (offsetY > (height - margin))
      return 1;

    return undefined;
  }

  handlerDragOver = (de: React.DragEvent<any>, index: number, orientation: "h" | "v") => {
    if (this.dragIndex == null)
      return;

    de.preventDefault();

    const th = de.currentTarget as HTMLElement;
    
    const offset = orientation == "v" ?
      this.getOffsetVertical((de.nativeEvent as DragEvent), th.getBoundingClientRect()) :
      this.getOffsetHorizontal((de.nativeEvent as DragEvent), th.getBoundingClientRect());

    let dropBorderIndex = offset == undefined ? undefined : index + offset;

    if (dropBorderIndex == this.dragIndex || dropBorderIndex == this.dragIndex! + 1)
      dropBorderIndex = undefined;

    if (this.dropBorderIndex != dropBorderIndex) {
      this.setDropBorderIndex(dropBorderIndex);
      this.forceUpdate();
    }
  }

  getDragConfig(index: number, orientation: "h" | "v"): DragConfig {
    return {
      dropClass: classes(
        index == this.dragIndex && "sf-dragging",
        this.dropClass(index, orientation)),
      onDragStart: e => this.handleDragStart(e, index),
      onDragEnd: this.handleDragEnd,
      onDragOver: e => this.handlerDragOver(e, index, orientation),
      onDrop: this.handleDrop,
    };
  }

  dropClass(index: number, orientation: "h" | "v") {
    const dropBorderIndex = this.dropBorderIndex;

    return dropBorderIndex != null && index == dropBorderIndex ? (orientation == "h" ? "drag-left" : "drag-top") :
      dropBorderIndex != null && index == dropBorderIndex - 1 ? (orientation == "h" ? "drag-right" : "drag-bottom") :
        undefined;
  }

  handleDrop = (de: React.DragEvent<any>) => {

    de.preventDefault();
    const dropBorderIndex = this.dropBorderIndex!;
    if (dropBorderIndex == null)
      return;

    const dragIndex = this.dragIndex!;
    const list = this.props.ctx.value!;
    const temp = list[dragIndex!];
    list.removeAt(dragIndex!);
    const rebasedDropIndex = dropBorderIndex > dragIndex ? dropBorderIndex - 1 : dropBorderIndex;
    list.insertAt(rebasedDropIndex, temp);

    this.setValue(list);
    this.setDropBorderIndex(undefined);
    this.setDragIndex(undefined);
    this.forceUpdate();
  }
}

export interface DragConfig {
  onDragStart?: React.DragEventHandler<any>;
  onDragEnd?: React.DragEventHandler<any>;
  onDragOver?: React.DragEventHandler<any>;
  onDrop?: React.DragEventHandler<any>;
  dropClass?: string;
}
