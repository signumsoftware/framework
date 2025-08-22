import * as React from 'react'
import { Dic, classes } from '../Globals'
import { Navigator, ViewPromise } from '../Navigator'
import { Constructor } from '../Constructor'
import { Finder } from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext } from '../TypeContext'
import { PropertyRoute, tryGetTypeInfos, TypeInfo, IsByAll, TypeReference, getTypeInfo, getTypeInfos, Type } from '../Reflection'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, toLiteFat, is, entityInfo, SelectorMessage, toLite, parseLiteList, getToString, isLite } from '../Signum.Entities'
import { LineBaseController, LineBaseProps } from './LineBase'
import SelectorModal from '../SelectorModal'
import { TypeEntity } from "../Signum.Basics";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { FindOptionsAutocompleteConfig } from './AutoCompleteConfig'
import { FilterOption } from '../Search'
import { toAbsoluteUrl } from '../AppContext'
import { To } from 'react-router'

export interface EntityBaseProps<V extends ModifiableEntity | Lite<Entity> | null> extends LineBaseProps<V> {
  view?: boolean;
  viewOnCreate?: boolean;
  create?: boolean;
  createOnFind?: boolean;
  find?: boolean;
  remove?: boolean;
  paste?: boolean;

  onView?: (entity: NN<V>, pr: PropertyRoute) => Promise<Aprox<V> | undefined> | undefined;
  onCreate?: (pr: PropertyRoute) => Promise<Aprox<V> | undefined> | undefined;
  onFind?: () => Promise<Aprox<V> | undefined> | undefined;
  onRemove?: (entity: NN<V>) => Promise<boolean>;
  findOptions?: FindOptions;
  findOptionsDictionary?: { [typeName: string]: FindOptions };
  liteToString?: (e: AsEntity<V>) => string;

  getComponent?: (ctx: TypeContext<AsEntity<V>>) => React.ReactElement;
  getViewPromise?: (entity: AsEntity<V>) => undefined | string | ViewPromise<ModifiableEntity>;

  fatLite?: boolean;
}

export type NN<T> = NoInfer<NonNullable<T>>;

export type Aprox<T> = NoInfer<
  T extends Entity ? T | Lite<T> :
  T extends Lite<infer E> ? E | Lite<E> :
  T extends ModifiableEntity ? T :
  never>;

export type AsEntity<T> = NoInfer<
  T extends ModifiableEntity ? T :
  T extends Lite<infer E> ? E :
  never>;

export type AsLite<T> = NoInfer<
  T extends Entity ? Lite<T> :
  T extends Lite<infer E> ? T :
  never>;

export class EntityBaseController<P extends EntityBaseProps<V>, V extends ModifiableEntity | Lite<Entity> | null> extends LineBaseController<P, V>{

  static getCreateIcon = (): React.ReactElement => <FontAwesomeIcon icon="plus" title={EntityControlMessage.Create.niceToString()} />;
  static getFindIcon = (): React.ReactElement => <FontAwesomeIcon icon="magnifying-glass" title={EntityControlMessage.Find.niceToString()} />;
  static getRemoveIcon = (): React.ReactElement => <FontAwesomeIcon icon="xmark" title={EntityControlMessage.Remove.niceToString()} />;
  static getTrashIcon = (): React.ReactElement => <FontAwesomeIcon icon="trash-can" title={EntityControlMessage.Remove.niceToString()} />;
  static getViewIcon = (): React.ReactElement => <FontAwesomeIcon icon="arrow-right" title={EntityControlMessage.View.niceToString()} />;
  static getMoveIcon = (): React.ReactElement => <FontAwesomeIcon icon="bars" />;
  static getPasteIcon = (): React.ReactElement => <FontAwesomeIcon icon="clipboard" title={EntityControlMessage.Paste.niceToString()} />;

  static hasChildrens(element: React.ReactElement): any {
     
    return (element.props as any).children && React.Children.toArray((element.props as any).children).length;
  }

  static defaultIsCreable(type: TypeReference, customComponent: boolean): boolean {
    return type.isEmbedded ? Navigator.isCreable(type.name, { customComponent, isEmbedded: type.isEmbedded }) :
      type.name == IsByAll ? false :
        tryGetTypeInfos(type).some(ti => ti && Navigator.isCreable(ti, { customComponent }));
  }

  static defaultIsViewable(type: TypeReference, customComponent: boolean): boolean {
    return type.isEmbedded ? Navigator.isViewable(type.name, { customComponent, isEmbedded: type.isEmbedded }) :
      type.name == IsByAll ? true :
        tryGetTypeInfos(type).some(ti => ti && Navigator.isViewable(ti, { customComponent }));
  }

  static defaultIsFindable(type: TypeReference): boolean {
    return type.isEmbedded ? false :
      type.name == IsByAll ? true :
        tryGetTypeInfos(type).some(ti => ti && Navigator.isFindable(ti));
  }

  static propEquals(prevProps: EntityBaseProps<any>, nextProps: EntityBaseProps<any>): boolean {
    if (
      nextProps.getComponent || prevProps.getComponent ||
      nextProps.extraButtons || prevProps.extraButtons ||
      nextProps.extraButtonsBefore || prevProps.extraButtonsBefore)
      return false;

    return LineBaseController.propEquals(prevProps, nextProps);
  }

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

  doView(entity: V): Promise<Aprox<V> | undefined> | undefined {
    const pr = this.props.ctx.propertyRoute!;
    return this.props.onView ?
      this.props.onView(entity!, pr) :
      this.defaultView(entity!, pr);
  }


  defaultView(value: NonNullable<V>, propertyRoute: PropertyRoute): Promise<Aprox<V> | undefined> {
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

  handleViewClick = async (event: React.MouseEvent<any>): Promise<void> => {

    event.preventDefault();

    const ctx = this.props.ctx;
    const entity = ctx.value as V;

    const openWindow = (event.button == 1 || event.ctrlKey) && !this.props.type!.isEmbedded;
    if (openWindow) {
      event.preventDefault();
      const route = Navigator.navigateRoute(entity as Lite<Entity> /*or Entity*/);
      window.open(toAbsoluteUrl(route));
    }
    else {
      const e = await this.doView(entity);

      if (!e)
        return;

      //Modifying the sub entity, saving and coming back should change the entity in the UI (ToString, or EntityDetails),
      //the parent entity is not really modified, but I'm not sure it his is a real problem in practice, till then the line is commented out
      //if (e.modified || !is(e, entity)) 
      // return;

      this.setValue(await this.convert(e), event);
    }
  }

  renderViewButton(btn: boolean): React.ReactElement | undefined {

    if (!this.props.view)
      return undefined;

    return (
      <a href="#" className={classes("sf-line-button", "sf-view", btn ?  "input-group-text" : undefined)}
        onClick={this.handleViewClick}
        title={this.props.ctx.titleLabels ? EntityControlMessage.View.niceToString() : undefined}>
        {EntityBaseController.getViewIcon()}
      </a>
    );
  }

  static chooseType(t: TypeReference, predicate: (ti: TypeInfo) => boolean): Promise<string | undefined> {

    if (t.isEmbedded)
      return Promise.resolve(t.name);

    if (t.name == IsByAll)
      return Finder.find(TypeEntity, { title: SelectorMessage.PleaseSelectAType.niceToString() }).then(t => t && getToString(t) /*CleanName*/);

    const tis = tryGetTypeInfos(t).notNull().filter(ti => predicate(ti));

    return SelectorModal.chooseType(tis)
      .then(ti => ti ? ti.name : undefined);
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

  handleCreateClick = async (event: React.SyntheticEvent<any>): Promise<void> => {

    event.preventDefault();

    var pr = this.props.ctx.propertyRoute!;
    const e = this.props.onCreate ? await this.props.onCreate(pr) :
      await this.defaultCreate(pr);

    if (!e)
      return;

    if (!this.props.viewOnCreate) {
      var value = await this.convert(e);
      this.setValue(value);

    } else {
      var conv = await this.convert(e);
      var v = await this.doView(conv);
      if (v != null) {
        var value = await this.convert(v);
        this.setValue(value);

      }
    }
  }

  async paste(text: string): Promise<void> {

    var lites = parseLiteList(text);
    if (lites.length == 0)
      return;

    var tis = getTypeInfos(this.props.type!);
    lites = lites.filter(lite => tis.length == 0 || tis.singleOrNull(ti => ti.name == lite.EntityType) != null);
    if (lites.length == 0)
      return;

    tis = lites.map(lite => lite.EntityType).distinctBy().map(tn => getTypeInfo(tn));
    var ti = await SelectorModal.chooseType(tis);

    if (!ti)
      return;

    lites = lites.filter(lite => lite.EntityType == ti!.name);

    await Navigator.API.fillLiteModels(...lites);

    var lite = await SelectorModal.chooseLite(ti.name, lites);
    if (!lite)
      return;

    const typeName = lite.EntityType;
    const fo = this.getFindOptions(typeName) ?? { queryName: typeName };
    const fos = (fo.filterOptions ?? []).concat([{ token: "Entity", operation: "EqualTo", value: lite }]);
    var lites = await Finder.fetchLites({ queryName: typeName, filterOptions: fos });
    if (lites.length == 0)
      return;


    var value = await this.convert(lites.single() as Aprox<V>);
    this.setValue(value);
  }

  handlePasteClick = (event: React.SyntheticEvent<any>): void => {

    event.preventDefault();

    navigator.clipboard.readText()
      .then(text => this.paste(text));
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

  static entityHtmlAttributes(entity: ModifiableEntity | Lite<Entity> | undefined | null): React.HTMLAttributes<any> {

    return {
      'data-entity': entityInfo(entity)
    } as any;
  }

  async defaultFind(): Promise<Aprox<V> | undefined> {

    if (this.props.findOptions) {
      var lite = await Finder.find(this.props.findOptions, { searchControlProps: { create: this.props.createOnFind } });

      return lite as Aprox<V> | undefined;

    } else {

      var typeName = await EntityBaseController.chooseType(this.props.type!, ti => Finder.isFindable(ti, false));

      if (typeName == null)
        return undefined;

      var fo: FindOptions = (this.props.findOptionsDictionary && this.props.findOptionsDictionary[typeName]) ?? Navigator.defaultFindOptions({ name: typeName }) ?? { queryName: typeName };

      var lite = await Finder.find(fo, { searchControlProps: { create: this.props.createOnFind } });

      return lite as Aprox<V> | undefined;
    }
  
  }

  handleFindClick = async (event: React.SyntheticEvent<any>): Promise<void> => {

    event.preventDefault();

    const lite = this.props.onFind ?
      await this.props.onFind() :
      await this.defaultFind();

    if (lite != null) {
      var value = await this.convert(lite);
      this.setValue(value);
    }
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

  handleRemoveClick = (event: React.SyntheticEvent<any>): void => {

    event.preventDefault();

    (this.props.onRemove ? this.props.onRemove(this.props.ctx.value!) : Promise.resolve(true))
      .then(result => {
        if (result == false)
          return;

        this.setValue(null!, event);
      });
  };

  renderRemoveButton(btn: boolean): React.ReactElement | undefined {
    if (!this.props.remove || this.props.ctx.readOnly)
      return undefined;

    return (
      <a href="#" className={classes("sf-line-button", "sf-remove", btn ? "input-group-text" : undefined)}
        onClick={this.handleRemoveClick}
        title={this.props.ctx.titleLabels ? EntityControlMessage.Remove.niceToString() : undefined}>
        {EntityBaseController.getRemoveIcon()}
      </a>
    );
  }
}
