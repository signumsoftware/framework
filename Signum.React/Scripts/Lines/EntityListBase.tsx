import * as React from 'react'
import { classes, Dic, KeyGenerator } from '../Globals'
import { ModifiableEntity, Lite, Entity, MListElement, MList, EntityControlMessage, newMListElement, isLite, parseLiteList, is, liteKey } from '../Signum.Entities'
import * as Finder from '../Finder'
import * as Navigator from '../Navigator'
import { FilterOption, FindOptions } from '../FindOptions'
import { TypeContext, mlistItemContext } from '../TypeContext'
import { EntityBaseController, EntityBaseProps } from './EntityBase'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { LineBaseController, LineBaseProps, tasks } from './LineBase'
import { FindOptionsAutocompleteConfig, LiteAutocompleteConfig } from './AutoCompleteConfig'
import { tryGetTypeInfos } from '../Reflection'
import { KeyCodes } from '../Components'
import { isRtl } from '../AppContext'

export interface EntityListBaseProps extends EntityBaseProps {
  move?: boolean | ((item: ModifiableEntity | Lite<Entity>) => boolean);
  moveMode?: "DragIcon" | "MoveIcons";
  onFindMany?: () => Promise<(ModifiableEntity | Lite<Entity>)[] | undefined> | undefined;
  filterRows?: (ctxs: TypeContext<any /*T*/>[]) => TypeContext<any /*T*/>[]; /*Not only filter, also order, skip, take is supported*/
  onAddElement?: (list: MList<Lite<Entity> | ModifiableEntity /*T*/>, newItem: Lite<Entity> | ModifiableEntity /*T*/) => void,
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
    super.getDefaultProps(state);
    state.moveMode = "DragIcon";
  }

  overrideProps(p: T, overridenProps: T) {
    if (overridenProps.onFind) {
      throw new Error(`'onFind' property is not applicable to ${this.constructor.name.before("Controller")} (ctx = ${p.ctx.propertyPath}). Use 'onFindMany' instead`);
    }

    super.overrideProps(p, overridenProps);
  }

  setValue(list: MList<Lite<Entity> | ModifiableEntity>, event?: React.SyntheticEvent) {
    super.setValue(list as any, event);
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

  renderMoveUp(btn: boolean, index: number, orientation: "h" | "v") {
    if (!this.canMove(this.props.ctx.value[index].element) || this.props.ctx.readOnly)
      return undefined;

    return (
      <a href="#" className={classes("sf-line-button", "sf-move", "sf-move-step", btn ? "input-group-text" : undefined)}
        onClick={e => { e.preventDefault(); this.moveUp(index); }}
        title={this.props.ctx.titleLabels ? (orientation == "v" ? EntityControlMessage.MoveUp : (isRtl() ? EntityControlMessage.MoveRight : EntityControlMessage.MoveLeft)).niceToString() : undefined}>
        <FontAwesomeIcon icon={orientation == "v" ? "chevron-up" : (isRtl() ? "chevron-right" : "chevron-left")} />
      </a>
    );
  }

  moveDown(index: number) {
    const list = this.props.ctx.value!;
    list.moveDown(index);
    this.setValue(list);
  }

  renderMoveDown(btn: boolean, index: number, orientation: "h" | "v") {
    if (!this.canMove(this.props.ctx.value[index].element) || this.props.ctx.readOnly)
      return undefined;

    return (
      <a href="#" className={classes("sf-line-button", "sf-move", "sf-move-step", btn ? "input-group-text" : undefined)}
        onClick={e => { e.preventDefault(); this.moveDown(index); }}
        title={this.props.ctx.titleLabels ? (orientation == "v" ? EntityControlMessage.MoveDown : (isRtl() ? EntityControlMessage.MoveLeft : EntityControlMessage.MoveRight)).niceToString() : undefined}>
        <FontAwesomeIcon icon={orientation == "v" ? "chevron-down" : (isRtl() ? "chevron-left" : "chevron-right")} />
      </a>
    );
  }

  doView(entity: ModifiableEntity | Lite<Entity>) {
    const pr = this.props.ctx.propertyRoute!.addLambda(a => a[0]);
    return this.props.onView ?
      this.props.onView(entity, pr) :
      this.defaultView(entity, pr);
  }

  handleViewElement = (event: React.MouseEvent<any>, index: number) => {

    event.preventDefault();

    const p = this.props;
    const ctx = p.ctx;
    const list = ctx.value!;
    const mle = list[index];
    const entity = mle.element;

    const openWindow = (event.button == 1 || event.ctrlKey) && !p.type!.isEmbedded;
    if (openWindow) {
      event.preventDefault();
      const route = Navigator.navigateRoute(entity as Lite<Entity> /*or Entity*/);
      window.open(route);
    }
    else {
      const pr = ctx.propertyRoute!.addLambda(a => a[0]);

      const promise = p.onView ?
        p.onView(entity, pr) :
        this.defaultView(entity, pr);

      if (promise == null)
        return;

      promise.then(e => {
        if (e == undefined)
          return;

        this.convert(e).then(m => {
          if (is(list[index].element as Entity, e as Entity)) {
            list[index].element = m;
            if (e.modified)
              this.setValue(list);
            this.forceUpdate();
          } else {
            list[index] = { rowId: null, element: m };
            this.setValue(list);
          }

        });
      });
    }
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
          .then(m => this.addElement(m));
      });
  };

  defaultFindMany(): Promise<(ModifiableEntity | Lite<Entity>)[] | undefined> {

    if (this.props.findOptions) {
      return Finder.findMany(this.props.findOptions, { searchControlProps: { create: this.props.createOnFind } });
    }

    return this.chooseType(ti => Finder.isFindable(ti, false))
      .then<(ModifiableEntity | Lite<Entity>)[] | undefined>(typeName => {
        if (typeName == null)
          return undefined;

        var fo: FindOptions = (this.props.findOptionsDictionary && this.props.findOptionsDictionary[typeName]) ?? Navigator.defaultFindOptions({ name: typeName }) ?? { queryName: typeName };

        return Finder.findMany(fo, { searchControlProps: { create: this.props.createOnFind } });
      });
  }

  addElement(entityOrLite: Lite<Entity> | ModifiableEntity) {

    if (isLite(entityOrLite) != (this.props.type!.isLite || false))
      throw new Error("entityOrLite should be already converted");

    const list = this.props.ctx.value!;
    if (this.props.onAddElement)
      this.props.onAddElement(list, entityOrLite);
    else {
      list.push(newMListElement(entityOrLite));
    }
    this.setValue(list);
  }

  paste(text: string) {
    var lites = parseLiteList(text);
    if (lites.length == 0)
      return;

    const tis = tryGetTypeInfos(this.props.type!);
    lites = lites.filter(lite => tis.length == 0 || tis.notNull().singleOrNull(ti => ti.name == lite.EntityType) != null);
    if (lites.length == 0)
      return;

    const dic = lites.groupBy(lite => lite.EntityType);
    return dic.map(kvp => {
      const fo = this.getFindOptions(kvp.key) ?? { queryName: kvp.key };
      const fos = (fo.filterOptions ?? []).concat([{ token: "Entity", operation: "IsIn", value: kvp.elements }]);
      return Finder.fetchLites({ queryName: kvp.key, filterOptions: fos })
        .then(lites => {
          if (lites.length == 0)
            return;

          return Promise.all(lites.map(lite => this.convert(lite)))
            .then(entities => entities.forEach(e => this.addElement(e)))
        });
    }).first();
  }

  handlePasteClick = (event: React.SyntheticEvent<any>) => {

    event.preventDefault();

    navigator.clipboard.readText()
      .then(text => this.paste(text));
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
      });
    });
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
      });
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
    const offsetX = dragEvent.x - rect.left;

    if (offsetX < margin)
      return 0;

    if (offsetX > (width - margin))
      return 1;

    return undefined;
  }

  getOffsetVertical(dragEvent: DragEvent, rect: ClientRect) {

    var margin = Math.min(50, rect.height / 2);

    const height = rect.height;
    const offsetY = dragEvent.y - rect.top;

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
      onKeyDown: e => this.handleMoveKeyDown(e, index),
      onDragOver: e => this.handlerDragOver(e, index, orientation),
      onDrop: this.handleDrop,
      title: !this.props.ctx.titleLabels ? undefined :
        orientation == "h" ? EntityControlMessage.MoveWithDragAndDropOrCtrlLeftRight.niceToString() :
          EntityControlMessage.MoveWithDragAndDropOrCtrlUpDown.niceToString()
    };
  }

  getMoveConfig(btn: boolean, index: number, orientation: "h" | "v") {
    return {
      renderMoveUp: () => this.renderMoveUp(false, index, orientation)!,
      renderMoveDown: () => this.renderMoveDown(false, index, orientation)
    }
  }

  dropClass(index: number, orientation: "h" | "v") {
    const dropBorderIndex = this.dropBorderIndex;

    return dropBorderIndex != null && index == dropBorderIndex ? (orientation == "h" ? "drag-left" : "drag-top") :
      dropBorderIndex != null && index == dropBorderIndex - 1 ? (orientation == "h" ? "drag-right" : "drag-bottom") :
        undefined;
  }



  handleMoveKeyDown = (ke: React.KeyboardEvent<any>, index : number) => {
    ke.preventDefault();

    if (ke.ctrlKey) {
      var direction =
        ke.keyCode == KeyCodes.down || ke.keyCode == KeyCodes.right ? +1 :
          ke.keyCode == KeyCodes.up || ke.keyCode == KeyCodes.left ? -1 :
            null;

      if (direction != null) {
        const list = this.props.ctx.value!;
        if (index + direction < 0 || list.length <= index + direction)
          return;

        var temp = list[index + direction];
        list[index + direction] = list[index];
        list[index] = temp;

        this.setValue(list);
        this.setDropBorderIndex(undefined);
        this.setDragIndex(undefined);
        this.forceUpdate();
      }
    }
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
  onKeyDown?: React.KeyboardEventHandler<any>;
  dropClass?: string;
  title?: string;
}

export interface MoveConfig {
  renderMoveUp: () => (JSX.Element | undefined);
  renderMoveDown: () => (JSX.Element | undefined);
}


tasks.push(taskSetMove);
export function taskSetMove(lineBase: LineBaseController<any>, state: LineBaseProps) {
  if (lineBase instanceof EntityListBaseController &&
    (state as EntityListBaseProps).move == undefined &&
    state.ctx.propertyRoute &&
    state.ctx.propertyRoute.propertyRouteType == "Field" &&
    state.ctx.propertyRoute.member!.preserveOrder) {
    (state as EntityListBaseProps).move = true;
  }
}


