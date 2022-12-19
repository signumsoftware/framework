import * as React from 'react'
import { Dic, classes } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext } from '../TypeContext'
import { PropertyRoute, tryGetTypeInfos, TypeInfo, IsByAll, TypeReference, getTypeInfo, getTypeInfos, Type } from '../Reflection'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, toLiteFat, is, entityInfo, SelectorMessage, toLite, parseLiteList, getToString } from '../Signum.Entities'
import { LineBaseController, LineBaseProps } from './LineBase'
import SelectorModal from '../SelectorModal'
import { TypeEntity } from "../Signum.Entities.Basics";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { FindOptionsAutocompleteConfig } from './AutoCompleteConfig'
import { FilterOption } from '../Search'

export interface EntityBaseProps extends LineBaseProps {
  view?: boolean | ((item: any/*T*/) => boolean);
  viewOnCreate?: boolean;
  create?: boolean;
  createOnFind?: boolean;
  find?: boolean;
  remove?: boolean | ((item: any /*T*/) => boolean);
  paste?: boolean;

  onView?: (entity: any /*T*/, pr: PropertyRoute) => Promise<ModifiableEntity | undefined> | undefined;
  onCreate?: (pr: PropertyRoute) => Promise<ModifiableEntity | Lite<Entity> | undefined> | undefined;
  onFind?: () => Promise<ModifiableEntity | Lite<Entity> | undefined> | undefined;
  onRemove?: (entity: any /*T*/) => Promise<boolean>;
  findOptions?: FindOptions;
  findOptionsDictionary?: { [typeName: string]: FindOptions };
  extraButtonsBefore?: (ec: EntityBaseController<EntityBaseProps>) => React.ReactNode;
  extraButtonsAfter?: (ec: EntityBaseController<EntityBaseProps>) => React.ReactNode;
  liteToString?: (e: any /*T*/) => string;

  getComponent?: (ctx: TypeContext<any /*T*/>) => React.ReactElement<any>;
  getViewPromise?: (entity: any /*T*/) => undefined | string | Navigator.ViewPromise<ModifiableEntity>;

  fatLite?: boolean;
}

export class EntityBaseController<P extends EntityBaseProps> extends LineBaseController<P>{

  static createIcon = <FontAwesomeIcon icon="plus" />;
  static findIcon = <FontAwesomeIcon icon="magnifying-glass" />;
  static removeIcon = <FontAwesomeIcon icon="xmark" />;
  static viewIcon = <FontAwesomeIcon icon="arrow-right" />;
  static moveIcon = <FontAwesomeIcon icon="bars" />;
  static pasteIcon = <FontAwesomeIcon icon="clipboard" />;

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
      nextProps.extraButtonsAfter || prevProps.extraButtonsAfter ||
      nextProps.extraButtonsBefore || prevProps.extraButtonsBefore)
      return false;

    return LineBaseController.propEquals(prevProps, nextProps);
  }

  getDefaultProps(state: P) {
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
        return Navigator.API.fetch(lite);
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
        this.convert(e).then(m => this.setValue(m, event));
      });
    }
  }

  renderViewButton(btn: boolean, item: ModifiableEntity | Lite<Entity>) {

    if (!this.canView(item))
      return undefined;

    return (
      <a href="#" className={classes("sf-line-button", "sf-view", btn ?  "input-group-text" : undefined)}
        onClick={this.handleViewClick}
        title={this.props.ctx.titleLabels ? EntityControlMessage.View.niceToString() : undefined}>
        {EntityBaseController.viewIcon}
      </a>
    );
  }

  chooseType(predicate: (ti: TypeInfo) => boolean): Promise<string | undefined> {
    const t = this.props.type!;

    if (t.isEmbedded)
      return Promise.resolve(t.name);

    if (t.name == IsByAll)
      return Finder.find(TypeEntity, { title: SelectorMessage.PleaseSelectAType.niceToString() }).then(t => getToString(t) /*CleanName*/);

    const tis = tryGetTypeInfos(t).notNull().filter(ti => predicate(ti));

    return SelectorModal.chooseType(tis)
      .then(ti => ti ? ti.name : undefined);
  }

  getFindOptions(typeName: string) {
    if (this.props.findOptionsDictionary)
      return this.props.findOptionsDictionary[typeName];

    return this.props.findOptions;
  }

  defaultCreate(pr: PropertyRoute): Promise<ModifiableEntity | Lite<Entity> | undefined> {

    return this.chooseType(t => this.props.create /*Hack?*/ || Navigator.isCreable(t, { customComponent: !!this.props.getComponent || !!this.props.getViewPromise, isEmbedded: pr.member!.type.isEmbedded }))
      .then(typeName => {
        if (!typeName)
          return Promise.resolve(undefined);

        var fo = this.getFindOptions(typeName);

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

      this.convert(e).then(m => this.setValue(m, event));
    });
  };

  paste(text: string) {
    var lites = parseLiteList(text);
    if (lites.length == 0)
      return;

    var tis = getTypeInfos(this.props.type!);
    lites = lites.filter(lite => tis.length == 0 || tis.singleOrNull(ti => ti.name == lite.EntityType) != null);
    if (lites.length == 0)
      return;

    tis = lites.map(lite => lite.EntityType).distinctBy().map(tn => getTypeInfo(tn));
    return SelectorModal.chooseType(tis)
      .then(ti => {
        if (!ti)
          return;

        lites = lites.filter(lite => lite.EntityType == ti.name);
        return Navigator.API.fillLiteModels(...lites)
          .then(() => SelectorModal.chooseLite(ti.name, lites));
      })
      .then(lite => {
        if (!lite)
          return;

        const typeName = lite.EntityType;
        const fo = this.getFindOptions(typeName) ?? { queryName: typeName };
        const fos = (fo.filterOptions ?? []).concat([{ token: "Entity", operation: "EqualTo", value: lite }]);
        return Finder.fetchLites({ queryName: typeName, filterOptions: fos })
          .then(lites => {
            if (lites.length == 0)
              return;

            return this.convert(lites[0]).then(m => this.setValue(m));
          });
      });
  }

  handlePasteClick = (event: React.SyntheticEvent<any>) => {

    event.preventDefault();

    navigator.clipboard.readText()
      .then(text => this.paste(text));
  }

  renderCreateButton(btn: boolean, createMessage?: string) {
    if (!this.props.create || this.props.ctx.readOnly)
      return undefined;

    return (
      <a href="#" className={classes("sf-line-button", "sf-create", btn ? "input-group-text" : undefined)}
        onClick={this.handleCreateClick}
        title={this.props.ctx.titleLabels ? createMessage ?? EntityControlMessage.Create.niceToString() : undefined}>
        {EntityBaseController.createIcon}
      </a>
    );
  }

  renderPasteButton(btn: boolean) {
    if (!this.props.paste || this.props.ctx.readOnly)
      return undefined;

    return (
      <a href="#" className={classes("sf-line-button", "sf-paste", btn ? "input-group-text" : undefined)}
        onClick={this.handlePasteClick}
        title={EntityControlMessage.Paste.niceToString()}>
        {EntityBaseController.pasteIcon}
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
      .then<ModifiableEntity | Lite<Entity> | undefined>(typeName => {
        if (typeName == null)
          return undefined;

        var fo: FindOptions = (this.props.findOptionsDictionary && this.props.findOptionsDictionary[typeName]) ?? Navigator.defaultFindOptions({ name: typeName }) ?? { queryName: typeName };

        return Finder.find(fo, { searchControlProps: { create: this.props.createOnFind } })
      });
  }

  handleFindClick = (event: React.SyntheticEvent<any>) => {

    event.preventDefault();

    const promise = this.props.onFind ? this.props.onFind() : this.defaultFind();

    if (!promise)
      return;

    promise.then(entity => {
      if (!entity)
        return;

      this.convert(entity).then(e => this.setValue(e, event));
    });
  };

  renderFindButton(btn: boolean) {
    if (!this.props.find || this.props.ctx.readOnly)
      return undefined;

    return (
      <a href="#" className={classes("sf-line-button", "sf-find", btn ? "input-group-text" : undefined)}
        onClick={this.handleFindClick}
        title={this.props.ctx.titleLabels ? EntityControlMessage.Find.niceToString() : undefined}>
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

        this.setValue(null, event);
      });
  };

  renderRemoveButton(btn: boolean, item: ModifiableEntity | Lite<Entity>) {
    if (!this.canRemove(item) || this.props.ctx.readOnly)
      return undefined;

    return (
      <a href="#" className={classes("sf-line-button", "sf-remove", btn ? "input-group-text" : undefined)}
        onClick={this.handleRemoveClick}
        title={this.props.ctx.titleLabels ? EntityControlMessage.Remove.niceToString() : undefined}>
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
