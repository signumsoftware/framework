import * as React from 'react'
import { Dic, classes } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, EntityFrame } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, TypeReference } from '../Reflection'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, JavascriptMessage, toLiteFat, is, liteKey, isLite, isEntity, entityInfo, SelectorMessage } from '../Signum.Entities'
import { LineBase, LineBaseProps, runTasks } from './LineBase'
import { FormGroup } from './FormGroup'
import { FormControlReadonly } from './FormControlReadonly'
import SelectorModal from '../SelectorModal'
import { TypeEntity } from "../Signum.Entities.Basics";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';


export interface EntityBaseProps extends LineBaseProps {
    view?: boolean | ((item: any/*T*/) => boolean);
    viewOnCreate?: boolean;
    navigate?: boolean;
    create?: boolean;
    find?: boolean;
    remove?: boolean | ((item: any /*T*/) => boolean);

    onView?: (entity: any /*T*/, pr: PropertyRoute) => Promise<ModifiableEntity | undefined> | undefined;
    onCreate?: () => Promise<ModifiableEntity | Lite<Entity> | undefined> | undefined;
    onFind?: () => Promise<ModifiableEntity | Lite<Entity> | undefined> | undefined;
    onRemove?: (entity: any /*T*/) => Promise<boolean>;
    findOptions?: FindOptions;

    getComponent?: (ctx: TypeContext<any /*T*/>) => React.ReactElement<any>;
    getViewPromise?: (entity: any /*T*/) => undefined | string | Navigator.ViewPromise<ModifiableEntity>;
}

export abstract class EntityBase<T extends EntityBaseProps, S extends EntityBaseProps> extends LineBase<T, S>
{
    static hasChildrens(element: React.ReactElement<any>) {
        return element.props.children && React.Children.toArray(element.props.children).length;
    }

    static defaultIsCreable(type: TypeReference, customComponent: boolean) {
        return type.isEmbedded ? Navigator.isCreable(type.name, customComponent , false) :
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
            const promise = this.props.onView ?
                this.props.onView(entity, ctx.propertyRoute) :
                this.defaultView(entity, ctx.propertyRoute);

            if (!promise)
                return;

            promise.then(e => {
                if (e == undefined)
                    return;

                if (e.modified || !is(e, entity))
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
                title={EntityControlMessage.View.niceToString()}>
                <FontAwesomeIcon icon="arrow-right" />
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

    defaultCreate(): Promise<ModifiableEntity | Lite<Entity> | undefined> {

        return this.chooseType(t => this.props.create /*Hack?*/ || Navigator.isCreable(t, !!this.props.getComponent || !!this.props.getViewPromise, false))
            .then(typeName => typeName ? Constructor.construct(typeName) : undefined)
            .then(e => {
                if (!e)
                    return Promise.resolve(undefined);

                var fo = this.state.findOptions;
                if (!fo || !fo.filterOptions)
                    return e.entity as Entity;

                return Finder.getQueryDescription(fo.queryName)
                    .then(qd => Finder.parseFilterOptions(fo!.filterOptions || [], false, qd))
                    .then(filters => Finder.setFilters(e!.entity as Entity, filters));
            });
    }

    handleCreateClick = (event: React.SyntheticEvent<any>) => {

        event.preventDefault();

        const promise = this.props.onCreate ?
            this.props.onCreate() : this.defaultCreate();

        if (!promise)
            return;

        promise.then<ModifiableEntity | Lite<Entity> | undefined>(e => {

            if (e == undefined)
                return undefined;

            if (!this.state.viewOnCreate)
                return Promise.resolve(e);

            return this.props.onView ?
                this.props.onView(e, this.state.ctx.propertyRoute) :
                this.defaultView(e, this.state.ctx.propertyRoute);

        }).then(e => {

            if (!e)
                return;

            this.convert(e).then(m => this.setValue(m)).done();
        }).done();
    };

    renderCreateButton(btn: boolean) {
        if (!this.state.create || this.state.ctx.readOnly)
            return undefined;

        return (
            <a href="#" className={classes("sf-line-button", "sf-create", btn ? "btn input-group-text" : undefined) }
                onClick={this.handleCreateClick}
                title={EntityControlMessage.Create.niceToString()}>
                <FontAwesomeIcon icon="plus" className="sf-create" />
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
            <a href="#" className={classes("sf-line-button", "sf-find", btn ? "btn input-group-text" : undefined) }
                onClick={this.handleFindClick}
                title={EntityControlMessage.Find.niceToString()}>
                <FontAwesomeIcon icon="search" />
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
            <a href="#" className={classes("sf-line-button", "sf-remove", btn ? "btn input-group-text" : undefined) }
                onClick={this.handleRemoveClick}
                title={EntityControlMessage.Remove.niceToString()}>
                <FontAwesomeIcon icon="times" />
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

