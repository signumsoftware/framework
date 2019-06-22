import * as React from 'react'
import { Dic, classes } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext } from '../TypeContext'
import { PropertyRoute, getTypeInfos, TypeInfo, IsByAll, TypeReference } from '../Reflection'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, toLiteFat, is, entityInfo, SelectorMessage } from '../Signum.Entities'
import { LineBase, LineBaseProps } from './LineBase'
import SelectorModal from '../SelectorModal'
import { TypeEntity } from "../Signum.Entities.Basics";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

export let TitleManager = { useTitle: true };

export interface EntityBaseProps extends LineBaseProps {
  view?: boolean | ((item: any/*T*/) => boolean);
  viewOnCreate?: boolean;
  navigate?: boolean;
  create?: boolean;
  find?: boolean;
  remove?: boolean | ((item: any /*T*/) => boolean);

  onView?: (entity: any /*T*/, pr: PropertyRoute) => Promise<ModifiableEntity | undefined> | undefined;
  onCreate?: (pr: PropertyRoute) => Promise<ModifiableEntity | Lite<Entity> | undefined> | undefined;
  onFind?: () => Promise<ModifiableEntity | Lite<Entity> | undefined> | undefined;
  onRemove?: (entity: any /*T*/) => Promise<boolean>;
  findOptions?: FindOptions;
  extraButtons?: (ec: EntityBase<EntityBaseProps, EntityBaseProps>) => React.ReactNode;

  getComponent?: (ctx: TypeContext<any /*T*/>) => React.ReactElement<any>;
  getViewPromise?: (entity: any /*T*/) => undefined | string | Navigator.ViewPromise<ModifiableEntity>;
}

export abstract class EntityBase<T extends EntityBaseProps, S extends EntityBaseProps> extends LineBase<T, S>
{

  static createIcon = <FontAwesomeIcon icon="plus"  />;
  static findIcon = <FontAwesomeIcon icon="search" />;
  static removeIcon = <FontAwesomeIcon icon="times" />;
  static viewIcon = <FontAwesomeIcon icon="arrow-right" />;
  static moveIcon = <FontAwesomeIcon icon="bars" />;

  static hasChildrens(element: React.ReactElement<any>) {
    return element.props.children && React.Children.toArray(element.props.children).length;
  }

  static defaultIsCreable(type: TypeReference, customComponent: boolean) {
    return type.isEmbedded ? Navigator.isCreable(type.name, customComponent, false) :
      type.name == IsByAll ? false :
        getTypeInfos(type).some(ti => Navigator.isCreable(ti, customComponent, false));
  }

  static defaultIsViewable(type: TypeReference, customComponent: boolean) {
    return type.isEmbedded ? Navigator.isViewable(type.name, customComponent) :
      type.name == IsByAll ? true :
        getTypeInfos(type).some(ti => Navigator.isViewable(ti, customComponent));
  }

  static defaultIsFindable(type: TypeReference) {
    return type.isEmbedded ? false :
      type.name == IsByAll ? true :
        getTypeInfos(type).some(ti => Navigator.isFindable(ti));
  }

  shouldComponentUpdate(nextProps: T, nextState: S): boolean {
    if (
      nextState.getComponent || this.state.getComponent ||
      nextState.extraButtons || this.state.extraButtons)
      return true;

    return super.shouldComponentUpdate(nextProps, nextState);
  }
  
  calculateDefaultState(state: S) {

    const type = state.type!;

    state.create = EntityBase.defaultIsCreable(type, !!this.props.getComponent || !!this.props.getViewPromise);
    state.view = EntityBase.defaultIsViewable(type, !!this.props.getComponent || !!this.props.getViewPromise);
    state.find = EntityBase.defaultIsFindable(type);
    state.findOptions = Navigator.defaultFindOptions(type);


    state.viewOnCreate = true;
    state.remove = true;
  }

  convert(entityOrLite: ModifiableEntity | Lite<Entity>): Promise<ModifiableEntity | Lite<Entity>> {

    const type = this.state.type!;

    const isLite = (entityOrLite as Lite<Entity>).EntityType != undefined;
    const entityType = (entityOrLite as Lite<Entity>).EntityType || (entityOrLite as ModifiableEntity).Type;


    if (type.isEmbedded) {
      if (entityType != type.name || isLite)
        throw new Error(`Impossible to convert '${entityType}' to '${type.name}'`);

      return Promise.resolve(entityOrLite as ModifiableEntity);
    } else {
      if (type.name != IsByAll && !type.name.split(',').map(a => a.trim()).contains(entityType))
        throw new Error(`Impossible to convert '${entityType}' to '${type.name}'`);

      if (!!isLite == !!type.isLite)
        return Promise.resolve(entityOrLite);

      if (isLite) {
        const lite = entityOrLite as Lite<Entity>;
        return Navigator.API.fetchAndRemember(lite);
      }

      const entity = entityOrLite as Entity;

      return Promise.resolve(toLiteFat(entity));
    }
  }

  doView(entity: ModifiableEntity | Lite<Entity>): Promise<ModifiableEntity | undefined> | undefined {
    const pr = this.state.ctx.propertyRoute;
    return this.props.onView ?
      this.props.onView(entity, pr) :
      this.defaultView(entity, pr);
  }


  defaultView(value: ModifiableEntity | Lite<Entity>, propertyRoute: PropertyRoute): Promise<ModifiableEntity | undefined> {
    return Navigator.view(value, {
      propertyRoute: propertyRoute,
      getViewPromise: this.getGetViewPromise(value)
    });
  }

  getGetViewPromise(value: ModifiableEntity | Lite<Entity>): undefined | ((entity: ModifiableEntity) => undefined | string | Navigator.ViewPromise<ModifiableEntity>) {
    var getComponent = this.props.getComponent;
    if (getComponent)
      return e => Navigator.ViewPromise.resolve(getComponent!);

    var getViewPromise = this.props.getViewPromise;
    if (getViewPromise)
      return e => getViewPromise!(e);

    return undefined;
  }

  handleViewClick = (event: React.MouseEvent<any>) => {

    event.preventDefault();

    const ctx = this.state.ctx;
    const entity = ctx.value;

    const openWindow = (event.button == 1 || event.ctrlKey) && !this.state.type!.isEmbedded;
    if (openWindow) {
      event.preventDefault();
      const route = Navigator.navigateRoute(entity as Lite<Entity> /*or Entity*/);
      window.open(route);
    }
    else {
      const promise = this.doView(entity);

      if (!promise)
        return;

      promise.then(e => {
        if (e == undefined)
          return;

        //Modifying the sub entity, saving and coming back should change the entity in the UI (ToString, or EntityDetails), 
        //the parent entity is not really modified, but I'm not sure it his is a real problem in practice, till then the line is commented out
        //if (e.modified || !is(e, entity)) 
          this.convert(e).then(m => this.setValue(m)).done();
      }).done();
    }
  }

  renderViewButton(btn: boolean, item: ModifiableEntity | Lite<Entity>) {

    if (!this.canView(item))
      return undefined;

    return (
      <a href="#" className={classes("sf-line-button", "sf-view", btn ? "btn input-group-text" : undefined)}
        onClick={this.handleViewClick}
        title={TitleManager.useTitle ? EntityControlMessage.View.niceToString() : undefined}>
        {EntityBase.viewIcon}
      </a>
    );
  }

  chooseType(predicate: (ti: TypeInfo) => boolean): Promise<string | undefined> {
    const t = this.state.type!;

    if (t.isEmbedded)
      return Promise.resolve(t.name);

    if (t.name == IsByAll)
      return Finder.find(TypeEntity, { title: SelectorMessage.PleaseSelectAType.niceToString() }).then(t => t && t.toStr /*CleanName*/);

    const tis = getTypeInfos(t).filter(ti => predicate(ti));

    return SelectorModal.chooseType(tis)
      .then(ti => ti ? ti.name : undefined);
  }

  defaultCreate(pr: PropertyRoute): Promise<ModifiableEntity | Lite<Entity> | undefined> {

    return this.chooseType(t => this.props.create /*Hack?*/ || Navigator.isCreable(t, !!this.props.getComponent || !!this.props.getViewPromise, false))
      .then(typeName => {
        if (!typeName)
          return Promise.resolve(undefined);

        var fo = this.state.findOptions;

        return Finder.getPropsFromFindOptions(typeName, fo)
          .then(props => Constructor.construct(typeName, props, pr));
      });
  }

  handleCreateClick = (event: React.SyntheticEvent<any>) => {

    event.preventDefault();

    var pr = this.state.ctx.propertyRoute;
    const promise = this.props.onCreate ?
      this.props.onCreate(pr) : this.defaultCreate(pr);

    if (!promise)
      return;

    promise.then<ModifiableEntity | Lite<Entity> | undefined>(e => {

      if (e == undefined)
        return undefined;

      if (!this.state.viewOnCreate)
        return Promise.resolve(e);

      return this.doView(e);

    }).then(e => {

      if (!e)
        return;

      this.convert(e).then(m => this.setValue(m)).done();
    }).done();
  };

  renderCreateButton(btn: boolean, createMessage?: string) {
    if (!this.state.create || this.state.ctx.readOnly)
      return undefined;

    return (
      <a href="#" className={classes("sf-line-button", "sf-create", btn ? "btn input-group-text" : undefined)}
        onClick={this.handleCreateClick}
        title={TitleManager.useTitle ? createMessage || EntityControlMessage.Create.niceToString() : undefined}>
        {EntityBase.createIcon}
      </a>
    );
  }

  static entityHtmlAttributes(entity: ModifiableEntity | Lite<Entity> | undefined | null): React.HTMLAttributes<any> {

    return {
      'data-entity': entityInfo(entity)
    } as any;
  }

  defaultFind(): Promise<ModifiableEntity | Lite<Entity> | undefined> {

    if (this.state.findOptions) {
      return Finder.find(this.state.findOptions);
    }

    return this.chooseType(ti => Finder.isFindable(ti, false))
      .then<ModifiableEntity | Lite<Entity> | undefined>(qn =>
        qn == undefined ? undefined : Finder.find({ queryName: qn } as FindOptions));
  }

  handleFindClick = (event: React.SyntheticEvent<any>) => {

    event.preventDefault();

    const promise = this.props.onFind ? this.props.onFind() : this.defaultFind();

    if (!promise)
      return;

    promise.then(entity => {
      if (!entity)
        return;

      this.convert(entity).then(e => this.setValue(e)).done();
    }).done();
  };
  renderFindButton(btn: boolean) {
    if (!this.state.find || this.state.ctx.readOnly)
      return undefined;

    return (
      <a href="#" className={classes("sf-line-button", "sf-find", btn ? "btn input-group-text" : undefined)}
        onClick={this.handleFindClick}
        title={TitleManager.useTitle ? EntityControlMessage.Find.niceToString() : undefined}>
        {EntityBase.findIcon}
      </a>
    );
  }

  handleRemoveClick = (event: React.SyntheticEvent<any>) => {

    event.preventDefault();

    (this.props.onRemove ? this.props.onRemove(this.props.ctx.value) : Promise.resolve(true))
      .then(result => {
        if (result == false)
          return;

        this.setValue(null);
      }).done();
  };

  renderRemoveButton(btn: boolean, item: ModifiableEntity | Lite<Entity>) {
    if (!this.canRemove(item) || this.state.ctx.readOnly)
      return undefined;

    return (
      <a href="#" className={classes("sf-line-button", "sf-remove", btn ? "btn input-group-text" : undefined)}
        onClick={this.handleRemoveClick}
        title={TitleManager.useTitle ? EntityControlMessage.Remove.niceToString() : undefined}>
        {EntityBase.removeIcon}
      </a>
    );
  }

  canRemove(item: ModifiableEntity | Lite<Entity>): boolean | undefined {

    const remove = this.state.remove;

    if (remove == undefined)
      return undefined;

    if (typeof remove === "function")
      return remove(item);

    return remove;
  }

  canView(item: ModifiableEntity | Lite<Entity>): boolean | undefined {

    const view = this.state.view;

    if (view == undefined)
      return undefined;

    if (typeof view === "function")
      return view(item);

    return view;
  }
}

