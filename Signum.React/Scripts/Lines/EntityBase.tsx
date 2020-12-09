import * as React from 'react'
import { Dic, classes } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext } from '../TypeContext'
import { PropertyRoute, tryGetTypeInfos, TypeInfo, IsByAll, TypeReference, getTypeInfo, getTypeInfos } from '../Reflection'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, toLiteFat, is, entityInfo, SelectorMessage, toLite } from '../Signum.Entities'
import { LineBaseController, LineBaseProps } from './LineBase'
import SelectorModal from '../SelectorModal'
import { TypeEntity } from "../Signum.Entities.Basics";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

export interface EntityBaseProps extends LineBaseProps {
  view?: boolean | ((item: any/*T*/) => boolean);
  viewOnCreate?: boolean;
  navigate?: boolean;
  create?: boolean;
  createOnFind?: boolean;
  find?: boolean;
  remove?: boolean | ((item: any /*T*/) => boolean);

  onView?: (entity: any /*T*/, pr: PropertyRoute) => Promise<ModifiableEntity | undefined> | undefined;
  onCreate?: (pr: PropertyRoute) => Promise<ModifiableEntity | Lite<Entity> | undefined> | undefined;
  onFind?: () => Promise<ModifiableEntity | Lite<Entity> | undefined> | undefined;
  onRemove?: (entity: any /*T*/) => Promise<boolean>;
  findOptions?: FindOptions;
  extraButtons?: (ec: EntityBaseController<EntityBaseProps>) => React.ReactNode;
  liteToString?: (e: any /*T*/) => string;

  getComponent?: (ctx: TypeContext<any /*T*/>) => React.ReactElement<any>;
  getViewPromise?: (entity: any /*T*/) => undefined | string | Navigator.ViewPromise<ModifiableEntity>;

  fatLite?: boolean;
}

export class EntityBaseController<P extends EntityBaseProps> extends LineBaseController<P>{

  static createIcon = <FontAwesomeIcon icon="plus" />;
  static findIcon = <FontAwesomeIcon icon="search" />;
  static removeIcon = <FontAwesomeIcon icon="times" />;
  static viewIcon = <FontAwesomeIcon icon="arrow-right" />;
  static moveIcon = <FontAwesomeIcon icon="bars" />;

  static hasChildrens(element: React.ReactElement<any>) {
    return element.props.children && React.Children.toArray(element.props.children).length;
  }

  static defaultIsCreable(type: TypeReference, customComponent: boolean) {
    return type.isEmbedded ? Navigator.isCreable(type.name, { customComponent, isEmbedded: type.isEmbedded }) :
      type.name == IsByAll ? false :
        tryGetTypeInfos(type).some(ti => ti && Navigator.isCreable(ti, { customComponent }));
  }

  static defaultIsViewable(type: TypeReference, customComponent: boolean) {
    return type.isEmbedded ? Navigator.isViewable(type.name, { customComponent, isEmbedded: type.isEmbedded }) :
      type.name == IsByAll ? true :
        tryGetTypeInfos(type).some(ti => ti && Navigator.isViewable(ti, { customComponent }));
  }

  static defaultIsFindable(type: TypeReference) {
    return type.isEmbedded ? false :
      type.name == IsByAll ? true :
        tryGetTypeInfos(type).some(ti => ti && Navigator.isFindable(ti));
  }

  static propEquals(prevProps: EntityBaseProps, nextProps: EntityBaseProps): boolean {
    if (
      nextProps.getComponent || prevProps.getComponent ||
      nextProps.extraButtons || prevProps.extraButtons)
      return false;

    return LineBaseController.propEquals(prevProps, nextProps);
  }

  getDefaultProps(state: P) {
    if (state.type) {
      const type = state.type;

      const customComponent = Boolean(state.getComponent || state.getViewPromise);

      state.create = EntityBaseController.defaultIsCreable(type, customComponent);
      state.view = EntityBaseController.defaultIsViewable(type, customComponent);
      state.find = EntityBaseController.defaultIsFindable(type);
      state.findOptions = Navigator.defaultFindOptions(type);

      state.viewOnCreate = true;
      state.remove = true;
    }
  }

  convert(entityOrLite: ModifiableEntity | Lite<Entity>): Promise<ModifiableEntity | Lite<Entity>> {

    const type = this.props.type!;

    const isLite = (entityOrLite as Lite<Entity>).EntityType != undefined;
    const entityType = (entityOrLite as Lite<Entity>).EntityType ?? (entityOrLite as ModifiableEntity).Type;

    if (type.isEmbedded) {
      if (entityType != type.name || isLite)
        throw new Error(`Impossible to convert '${entityType}' to '${type.name}'`);

      return Promise.resolve(entityOrLite as ModifiableEntity);
    }
    else {
      if (type.name != IsByAll && !type.name.split(',').map(a => a.trim()).contains(entityType))
        throw new Error(`Impossible to convert '${entityType}' to '${type.name}'`);

      if (!!isLite == !!type.isLite)
        return Promise.resolve(entityOrLite);

      if (isLite) {
        const lite = entityOrLite as Lite<Entity>;
        return Navigator.API.fetchAndForget(lite);
      }

      const entity = entityOrLite as Entity;
      const ti = getTypeInfo(entity.Type);
      const toStr = this.props.liteToString && this.props.liteToString(entity);
      const fatLite = this.props.fatLite || this.props.fatLite == null && (ti.entityKind == "Part" || ti.entityKind == "SharedPart");
      return Promise.resolve(toLite(entity, fatLite, toStr));
    }
  }

  doView(entity: ModifiableEntity | Lite<Entity>): Promise<ModifiableEntity | undefined> | undefined {
    const pr = this.props.ctx.propertyRoute!;
    return this.props.onView ?
      this.props.onView(entity, pr) :
      this.defaultView(entity, pr);
  }


  defaultView(value: ModifiableEntity | Lite<Entity>, propertyRoute: PropertyRoute): Promise<ModifiableEntity | undefined> {
    return Navigator.view(value, {
      propertyRoute: propertyRoute,
      getViewPromise: this.getGetViewPromise(value),
      allowExchangeEntity: false,
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

    const ctx = this.props.ctx;
    const entity = ctx.value;

    const openWindow = (event.button == 1 || event.ctrlKey) && !this.props.type!.isEmbedded;
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
        title={this.props.titleLabels ? EntityControlMessage.View.niceToString() : undefined}>
        {EntityBaseController.viewIcon}
      </a>
    );
  }

  chooseType(predicate: (ti: TypeInfo) => boolean): Promise<string | undefined> {
    const t = this.props.type!;

    if (t.isEmbedded)
      return Promise.resolve(t.name);

    if (t.name == IsByAll)
      return Finder.find(TypeEntity, { title: SelectorMessage.PleaseSelectAType.niceToString() }).then(t => t?.toStr /*CleanName*/);

    const tis = tryGetTypeInfos(t).notNull().filter(ti => predicate(ti));

    return SelectorModal.chooseType(tis)
      .then(ti => ti ? ti.name : undefined);
  }

  defaultCreate(pr: PropertyRoute): Promise<ModifiableEntity | Lite<Entity> | undefined> {

    return this.chooseType(t => this.props.create /*Hack?*/ || Navigator.isCreable(t, { customComponent: !!this.props.getComponent || !!this.props.getViewPromise, isEmbedded: pr.member!.type.isEmbedded }))
      .then(typeName => {
        if (!typeName)
          return Promise.resolve(undefined);

        var fo = this.props.findOptions;

        return Finder.getPropsFromFindOptions(typeName, fo)
          .then(props => Constructor.construct(typeName, props, pr));
      });
  }

  handleCreateClick = (event: React.SyntheticEvent<any>) => {

    event.preventDefault();

    var pr = this.props.ctx.propertyRoute!;
    const promise = this.props.onCreate ?
      this.props.onCreate(pr) : this.defaultCreate(pr);

    if (!promise)
      return;

    promise.then<ModifiableEntity | Lite<Entity> | undefined>(e => {

      if (e == undefined)
        return undefined;

      if (!this.props.viewOnCreate)
        return Promise.resolve(e);

      return this.doView(e);

    }).then(e => {

      if (!e)
        return;

      this.convert(e).then(m => this.setValue(m)).done();
    }).done();
  };

  renderCreateButton(btn: boolean, createMessage?: string) {
    if (!this.props.create || this.props.ctx.readOnly)
      return undefined;

    return (
      <a href="#" className={classes("sf-line-button", "sf-create", btn ? "btn input-group-text" : undefined)}
        onClick={this.handleCreateClick}
        title={this.props.titleLabels ? createMessage ?? EntityControlMessage.Create.niceToString() : undefined}>
        {EntityBaseController.createIcon}
      </a>
    );
  }

  static entityHtmlAttributes(entity: ModifiableEntity | Lite<Entity> | undefined | null): React.HTMLAttributes<any> {

    return {
      'data-entity': entityInfo(entity)
    } as any;
  }

  defaultFind(): Promise<ModifiableEntity | Lite<Entity> | undefined> {

    if (this.props.findOptions) {
      return Finder.find(this.props.findOptions, { searchControlProps: { create: this.props.createOnFind } });
    }

    return this.chooseType(ti => Finder.isFindable(ti, false))
      .then<ModifiableEntity | Lite<Entity> | undefined>(qn =>
        qn == undefined ? undefined : Finder.find({ queryName: qn } as FindOptions, { searchControlProps: { create: this.props.createOnFind } }));
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
    if (!this.props.find || this.props.ctx.readOnly)
      return undefined;

    return (
      <a href="#" className={classes("sf-line-button", "sf-find", btn ? "btn input-group-text" : undefined)}
        onClick={this.handleFindClick}
        title={this.props.titleLabels ? EntityControlMessage.Find.niceToString() : undefined}>
        {EntityBaseController.findIcon}
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
    if (!this.canRemove(item) || this.props.ctx.readOnly)
      return undefined;

    return (
      <a href="#" className={classes("sf-line-button", "sf-remove", btn ? "btn input-group-text" : undefined)}
        onClick={this.handleRemoveClick}
        title={this.props.titleLabels ? EntityControlMessage.Remove.niceToString() : undefined}>
        {EntityBaseController.removeIcon}
      </a>
    );
  }

  canRemove(item: ModifiableEntity | Lite<Entity>): boolean | undefined {

    const remove = this.props.remove;

    if (remove == undefined)
      return undefined;

    if (typeof remove === "function")
      return remove(item);

    return remove;
  }

  canView(item: ModifiableEntity | Lite<Entity>): boolean | undefined {

    const view = this.props.view;

    if (view == undefined)
      return undefined;

    if (typeof view === "function")
      return view(item);

    return view;
  }
}
