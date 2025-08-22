import * as React from 'react'
import { classes, Dic, KeyGenerator } from '../Globals'
import { ModifiableEntity, Lite, Entity, MListElement, MList, EntityControlMessage, newMListElement, isLite, parseLiteList, is, liteKey, toLite } from '../Signum.Entities'
import { Finder } from '../Finder'
import { Navigator, ViewPromise } from '../Navigator'
import { Constructor } from '../Constructor'
import { FilterOption, FindOptions } from '../FindOptions'
import { TypeContext, mlistItemContext } from '../TypeContext'
import { Aprox, EntityBaseController, EntityBaseProps, AsEntity, NN } from './EntityBase'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { LineBaseController, LineBaseProps, tasks } from './LineBase'
import { getTypeInfo, IsByAll, PropertyRoute, tryGetTypeInfos } from '../Reflection'
import { isRtl, toAbsoluteUrl } from '../AppContext'
import { KeyNames } from '../Components'

export interface EntityListBaseProps<V extends ModifiableEntity | Lite<Entity>> extends LineBaseProps<MList<V>> {
  view?: boolean | ((item: NoInfer<V>) => boolean);
  viewOnCreate?: boolean;
  create?: boolean;
  createOnFind?: boolean;
  find?: boolean;
  remove?: boolean | ((item: NoInfer<V>) => boolean);
  paste?: boolean;
  move?: boolean | ((item: NoInfer<V>) => boolean);
  moveMode?: "DragIcon" | "MoveIcons";

  onView?: (entity: NoInfer<V>, pr: PropertyRoute) => Promise<Aprox<V> | undefined> | undefined;
  onCreate?: (pr: PropertyRoute) => Promise<Aprox<V> | undefined> | Aprox<V> | undefined;
  onFindMany?: () => Promise<Aprox<V>[] | undefined> | undefined;
  onRemove?: (entity: NoInfer<V>) => Promise<boolean>;
  onMove?: (list: NoInfer<MList<V>>, oldIndex: number, newIndex: IndexWithOffset) => void;
  findOptions?: FindOptions;
  findOptionsDictionary?: { [typeName: string]: FindOptions };

  liteToString?: (e: AsEntity<V>) => string;

  getComponent?: (ctx: TypeContext<AsEntity<V>>) => React.ReactElement;
  getViewPromise?: (entity: AsEntity<V>) => undefined | string | ViewPromise<ModifiableEntity>;
  
  fatLite?: boolean;

  filterRows?: (ctxs: TypeContext<V>[]) => TypeContext<V>[]; /*Not only filter, also order, skip, take is supported*/
  onAddElement?: (list: MList<V>, newItem: V) => void,
}

interface IndexWithOffset {
  index: number; 
  offset: 0 | 1;
}

export abstract class EntityListBaseController<P extends EntityListBaseProps<V>, V extends ModifiableEntity | Lite<Entity>> extends LineBaseController<P, MList<V>>
{
  dragIndex!: number | undefined;
  setDragIndex!: React.Dispatch<number | undefined>;
  dropBorderIndex!: IndexWithOffset | undefined;
  setDropBorderIndex!: React.Dispatch<IndexWithOffset | undefined>;

  init(p: P): void {
    super.init(p);
    [this.dragIndex, this.setDragIndex] = React.useState<number | undefined>(undefined);
    [this.dropBorderIndex, this.setDropBorderIndex] = React.useState<IndexWithOffset | undefined>(undefined);
  }

  keyGenerator: KeyGenerator = new KeyGenerator();
  getDefaultProps(state: P): void {
    if (state.type) {
      const type = state.type;

      state.create = EntityBaseController.defaultIsCreable(type, false);
      state.view = EntityBaseController.defaultIsViewable(type, false);
      state.find = EntityBaseController.defaultIsFindable(type);
      state.findOptions = Navigator.defaultFindOptions(type);

      state.viewOnCreate = true;
      state.remove = true;
      state.paste = (type.name == IsByAll ? true : undefined);
    }
    super.getDefaultProps(state);
    state.moveMode = "DragIcon";
  }


  overrideProps(p: P, overridenProps: P): void {

    super.overrideProps(p, overridenProps);
  }


  getMListItemContext(ctx: TypeContext<MList<V>>): TypeContext<V>[] {
    var rows = mlistItemContext(ctx);

    if (this.props.filterRows)
      return this.props.filterRows(rows);

    return rows;
  }

  renderCreateButton(btn: boolean, createMessage?: string): React.ReactElement | undefined {
    if (!this.props.create || this.props.ctx.readOnly)
      return undefined;

    return (
      <a href="#" className={classes("sf-line-button", "sf-create", btn ? "input-group-text" : undefined)}
        onClick={this.handleCreateClick}
        title={this.props.ctx.titleLabels ? createMessage ?? EntityControlMessage.Create.niceToString() : undefined}>
        {EntityBaseController.getCreateIcon()}
      </a>
    );
  }

  renderFindButton(btn: boolean): React.ReactElement | undefined {
    if (!this.props.find || this.props.ctx.readOnly)
      return undefined;

    return (
      <a href="#" className={classes("sf-line-button", "sf-find", btn ? "input-group-text" : undefined)}
        onClick={this.handleFindClick}
        title={this.props.ctx.titleLabels ? EntityControlMessage.Find.niceToString() : undefined}>
        {EntityBaseController.getFindIcon()}
      </a>
    );
  }

  moveUp(index: number): void {
    const list = this.props.ctx.value!;
    list.moveUp(index);
    this.setValue(list);
  }

  renderMoveUp(btn: boolean, index: number, orientation: "h" | "v"): React.ReactElement | undefined {
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

  moveDown(index: number): void {
    const list = this.props.ctx.value!;
    list.moveDown(index);
    this.setValue(list);
  }

  renderMoveDown(btn: boolean, index: number, orientation: "h" | "v"): React.ReactElement | undefined {
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



  doView(entity: V): Promise<NoInfer<V extends Entity ? V | Lite<V> : V extends Lite<infer E extends Entity> ? E | Lite<E> : V extends ModifiableEntity ? V : never> | undefined> | undefined {
    const pr = this.props.ctx.propertyRoute?.addLambda(a => a[0])!;
    return this.props.onView ?
      this.props.onView(entity, pr) :
      this.defaultView(entity, pr);
  }

  defaultView(value: V, propertyRoute: PropertyRoute): Promise<Aprox<V> | undefined> {
    return Navigator.view(value!, {
      propertyRoute: propertyRoute,
      getViewPromise: this.getGetViewPromise() as (undefined | ((entity: ModifiableEntity) => undefined | string | ViewPromise<ModifiableEntity>)),
      allowExchangeEntity: false,
    }) as Promise<Aprox<V> | undefined>;
  }

  getGetViewPromise(): undefined | ((entity: AsEntity<V>) => undefined | string | ViewPromise<AsEntity<V>>) {
    var getComponent = this.props.getComponent;
    if (getComponent)
      return e => ViewPromise.resolve(getComponent!);

    var getViewPromise = this.props.getViewPromise;
    if (getViewPromise)
      return e => getViewPromise!(e);

    return undefined;
  }

  handleViewElement = (event: React.MouseEvent<any>, index: number): void => {

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
      window.open(toAbsoluteUrl(route));
    }
    else {
      const pr = ctx.propertyRoute?.addLambda(a => a[0])!;

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
            if ((e as Entity).modified)
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

  async convert(entityOrLite: Aprox<V>): Promise<V> {

    const type = this.props.type!;

    const entityType = isLite(entityOrLite) ? entityOrLite.EntityType : entityOrLite.Type;

    if (type.isEmbedded) {
      if (entityType != type.name || isLite(entityOrLite))
        throw new Error(`Impossible to convert '${entityType}' to '${type.name}'`);

      return entityOrLite as V;
    }
    else {
      if (type.name != IsByAll && !type.name.split(',').map(a => a.trim()).contains(entityType))
        throw new Error(`Impossible to convert '${entityType}' to '${type.name}'`);

      if (!!isLite(entityOrLite) == !!type.isLite)
        return entityOrLite as V;

      if (isLite(entityOrLite)) {
        const lite = entityOrLite as Lite<Entity>;
        return (await Navigator.API.fetch(lite)) as unknown as V;
      }

      const entity = entityOrLite as Entity;
      const ti = getTypeInfo(entity.Type);
      const toStr = this.props.liteToString && this.props.liteToString(entity as AsEntity<V>);
      const fatLite = this.props.fatLite || this.props.fatLite == null && (ti.entityKind == "Part" || ti.entityKind == "SharedPart" || entityOrLite.isNew);
      return toLite(entity, fatLite, toStr) as V;
    }
  }

  

  handleCreateClick = async (event: React.SyntheticEvent<any>): Promise<void> => {

    event.preventDefault();

    var pr = this.props.ctx.propertyRoute?.addLambda(a => a[0])!;
    const e = this.props.onCreate ? await this.props.onCreate(pr) :
      await this.defaultCreate(pr);

    if (!e)
      return;

    if (!this.props.viewOnCreate) {
      var value = await this.convert(e);
      this.addElement(value);

    } else {
      var conv = await this.convert(e);
      var v = await this.doView(conv);
      if (v != null) {
        var value = await this.convert(v);
        this.addElement(value);

      }
    }
  }

  getFindOptions(typeName: string): FindOptions | undefined {
    if (this.props.findOptionsDictionary)
      return this.props.findOptionsDictionary[typeName];

    return this.props.findOptions;
  }
  
  async defaultCreate(pr: PropertyRoute): Promise<Aprox<V> | undefined> {

    var typeName = await EntityBaseController.chooseType(this.props.type!, t => this.props.create /*Hack?*/ || Navigator.isCreable(t, { customComponent: !!this.props.getComponent || !!this.props.getViewPromise, isEmbedded: pr.member!.type.isEmbedded }));

    if (typeName == null)
      return undefined;

    var fo = this.getFindOptions(typeName);

    var props = await Finder.getPropsFromFindOptions(typeName, fo);

    var result = (await Constructor.construct(typeName, props, pr));

    return result as Aprox<V>;
  }

  defaultFindMany(): Promise<(ModifiableEntity | Lite<Entity>)[] | undefined> {

    if (this.props.findOptions) {
      return Finder.findMany(this.props.findOptions, { searchControlProps: { create: this.props.createOnFind } });
    }

    return EntityBaseController.chooseType(this.props.type!, ti => Finder.isFindable(ti, false))
      .then<(ModifiableEntity | Lite<Entity>)[] | undefined>(typeName => {
        if (typeName == null)
          return undefined;

        var fo: FindOptions = (this.props.findOptionsDictionary && this.props.findOptionsDictionary[typeName]) ?? Navigator.defaultFindOptions({ name: typeName }) ?? { queryName: typeName };

        return Finder.findMany(fo, { searchControlProps: { create: this.props.createOnFind } });
      });
  }

  addElement(entityOrLite: V): void {

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

  renderPasteButton(btn: boolean): React.ReactElement | undefined {
    if (!this.props.paste || this.props.ctx.readOnly)
      return undefined;

    return (
      <a href="#" className={classes("sf-line-button", "sf-paste", btn ? "input-group-text" : undefined)}
        onClick={this.handlePasteClick}
        title={EntityControlMessage.Paste.niceToString()}>
        {EntityBaseController.getPasteIcon()}
      </a>
    );
  }

  paste(text: string): Promise<void | undefined> | undefined {
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

          return Promise.all(lites.map(lite => this.convert(lite as Aprox<V>)))
            .then(entities => entities.forEach(e => this.addElement(e)))
        });
    }).first();
  }

  handlePasteClick = (event: React.SyntheticEvent<any>): void => {

    event.preventDefault();

    navigator.clipboard.readText()
      .then(text => this.paste(text));
  }

  handleFindClick = async (event: React.SyntheticEvent<any>): Promise<void> => {

    event.preventDefault();

    const lites = this.props.onFindMany ?
      await this.props.onFindMany() :
      await this.defaultFindMany();

    if (!lites)
      return;

    var converted = await Promise.all(lites.map(a => this.convert(a as Aprox<V>)));

    converted.forEach(e => this.addElement(e));
  };

  handleRemoveElementClick = async (event: React.SyntheticEvent<any>, index: number): Promise<void> => {

    event.preventDefault();

    const list = this.props.ctx.value!;
    const mle = list[index];

    var result = this.props.onRemove ? await this.props.onRemove(mle.element) : await Promise.resolve(true);

    if (result)
      this.removeElement(mle)
  }

  removeElement(mle: MListElement<V>): void {
    const list = this.props.ctx.value!;
    list.remove(mle);
    this.setValue(list);
  }

  canMove(item: V): boolean | undefined {

    const move = this.props.move;

    if (move == undefined)
      return undefined;

    if (typeof move === "function")
      return move(item);

    return move;
  }


  handleDragStart = (de: React.DragEvent<any>, index: number): void => {
    de.dataTransfer.setData('text', "start"); //cannot be empty string
    de.dataTransfer.effectAllowed = "move";
    this.setDragIndex(index);
  }

  handleDragEnd = (de: React.DragEvent<any>): void => {
    this.setDragIndex(undefined);
    this.setDropBorderIndex(undefined);
    this.forceUpdate();
  }

  getOffsetHorizontal(dragEvent: DragEvent, rect: DOMRect): 0 | 1 | undefined {

    const margin = Math.min(50, rect.width / 2);

    const width = rect.width;
    const offsetX = dragEvent.x - rect.left;

    if (offsetX < margin)
      return 0;

    if (offsetX > (width - margin))
      return 1;

    return undefined;
  }

  getOffsetVertical(dragEvent: DragEvent, rect: DOMRect): 0 | 1 | undefined {

    var margin = Math.min(50, rect.height / 2);

    const height = rect.height;
    const offsetY = dragEvent.y - rect.top;

    if (offsetY < margin)
      return 0;

    if (offsetY > (height - margin))
      return 1;

    return undefined;
  }

  handlerDragOver = (de: React.DragEvent<any>, index: number, orientation: "h" | "v"): void => {
    if (this.dragIndex == null)
      return;

    de.preventDefault();

    const th = de.currentTarget as HTMLElement;
    
    const offset = orientation == "v" ?
      this.getOffsetVertical((de.nativeEvent as DragEvent), th.getBoundingClientRect()) :
      this.getOffsetHorizontal((de.nativeEvent as DragEvent), th.getBoundingClientRect());

    let dropBorderIndex: IndexWithOffset | undefined = offset == undefined ? undefined :
      { index, offset };

    if (dropBorderIndex != null && dropBorderIndex.index == this.dragIndex)
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
      renderMoveUp: (): React.ReactElement => this.renderMoveUp(false, index, orientation)!,
      renderMoveDown: (): React.ReactElement | undefined => this.renderMoveDown(false, index, orientation)
    }
  }

  dropClass(index: number, orientation: "h" | "v"): "drag-left" | "drag-top" | "drag-right" | "drag-bottom" | undefined {
    const dropBorderIndex = this.dropBorderIndex;


    if (dropBorderIndex != null) {

      if (index == dropBorderIndex.index) {
        if (dropBorderIndex.offset == 0)
          return (orientation == "h" ? "drag-left" : "drag-top");
        else
          return (orientation == "h" ? "drag-right" : "drag-bottom")
      }

      if (!this.props.filterRows) {
        if (dropBorderIndex.index == (index  -1) && dropBorderIndex.offset == 1)
          return (orientation == "h" ? "drag-left" : "drag-top");
        else if (dropBorderIndex.index == (index + 1) && dropBorderIndex.offset == 0)
          return (orientation == "h" ? "drag-right" : "drag-bottom")
      }
    }

    return undefined;
  }

  canRemove(item: V): boolean | undefined {

    const remove = this.props.remove;

    if (remove == undefined)
      return undefined;

    if (typeof remove === "function")
      return remove(item);

    return remove;
  }

  canView(item: V): boolean | undefined {

    const view = this.props.view;

    if (view == undefined)
      return undefined;

    if (typeof view === "function")
      return view(item);

    return view;
  }


  handleMoveKeyDown = (ke: React.KeyboardEvent<any>, index : number): void => {

    if (ke.ctrlKey) {

      if (ke.key == KeyNames.arrowDown || ke.key == KeyNames.arrowRight) {
        ke.preventDefault();
        this.onMoveElement(index, ({ index: index + 1, offset: 1 }));
      } else {
        ke.preventDefault();
        this.onMoveElement(index, ({ index: index - 1, offset : 0}));
      }
    }
  }

  handleDrop = (de: React.DragEvent<any>): void => {

    de.preventDefault();
    const dropBorderIndex = this.dropBorderIndex;
    const dragIndex = this.dragIndex;
    if (dropBorderIndex == null || dragIndex == null)
      return;

    this.onMoveElement(dragIndex, dropBorderIndex);
  }

  onMoveElement(oldIndex: number, newIndex: IndexWithOffset): void {
    const list = this.props.ctx.value!;

    if (this.props.onMove) {
      this.props.onMove(list, oldIndex, newIndex);
    }
    else {
      const temp = list[oldIndex];
      list.removeAt(oldIndex);
      var completeNewIndex = newIndex.index + newIndex.offset;
      const rebasedDropIndex = newIndex.index > oldIndex ? completeNewIndex - 1 : completeNewIndex;
      list.insertAt(rebasedDropIndex, temp);
    }

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
  renderMoveUp: () => (React.ReactElement | undefined);
  renderMoveDown: () => (React.ReactElement | undefined);
}


tasks.push(taskSetMove);
export function taskSetMove(lineBase: LineBaseController<LineBaseProps, unknown>, state: LineBaseProps): void {
  if (lineBase instanceof EntityListBaseController &&
    (state as EntityListBaseProps<any>).move == undefined &&
    state.ctx.propertyRoute &&
    state.ctx.propertyRoute.propertyRouteType == "Field" &&
    state.ctx.propertyRoute.member!.preserveOrder) {
    (state as EntityListBaseProps<any>).move = true;
  }
}
