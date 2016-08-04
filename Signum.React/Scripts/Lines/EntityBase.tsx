import * as React from 'react'
import { Link } from 'react-router'
import { Dic, classes } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, EntityFrame } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll } from '../Reflection'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, JavascriptMessage, toLiteFat, is, liteKey, isLite, isEntity, entityInfo } from '../Signum.Entities'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks } from '../Lines/LineBase'
import Typeahead from '../Lines/Typeahead'
import SelectorModal from '../SelectorModal'


export interface EntityBaseProps extends LineBaseProps {
    view?: boolean;
    viewOnCreate?: boolean;
    navigate?: boolean;
    create?: boolean;
    find?: boolean;
    remove?: boolean;

    onView?: (entity: ModifiableEntity | Lite<Entity>, pr: PropertyRoute) => Promise<ModifiableEntity | undefined> | undefined;
    onCreate?: () => Promise<ModifiableEntity | Lite<Entity> | undefined> | undefined;
    onFind?: () => Promise<ModifiableEntity | Lite<Entity> | undefined> | undefined;
    onRemove?: (entity: ModifiableEntity | Lite<Entity>) => Promise<boolean>;

    getComponent?: (ctx: TypeContext<ModifiableEntity>) => React.ReactElement<any>;
}


export interface EntityBaseState extends LineBaseProps {
    view?: boolean;
    viewOnCreate?: boolean;
    navigate?: boolean;
    create?: boolean;
    find?: boolean;
    remove?: boolean;
}


export abstract class EntityBase<T extends EntityBaseProps, S extends EntityBaseState> extends LineBase<T, S>
{
    calculateDefaultState(state: S) {

        const type = state.type!;

        state.create = type.isEmbedded ? Navigator.isCreable(type.name, !!this.props.getComponent, false) :
            type.name == IsByAll ? false :
                getTypeInfos(type).some(ti => Navigator.isCreable(ti, !!this.props.getComponent, false));

        state.view = type.isEmbedded ? Navigator.isViewable(type.name, !!this.props.getComponent) :
            type.name == IsByAll ? true :
                getTypeInfos(type).some(ti => Navigator.isViewable(ti, !!this.props.getComponent));

        state.navigate = type.isEmbedded ? Navigator.isNavigable(type.name, !!this.props.getComponent) :
            type.name == IsByAll ? true :
                getTypeInfos(type).some(ti => Navigator.isNavigable(ti, !!this.props.getComponent));

        state.find = type.isEmbedded ? false :
            type.name == IsByAll ? false :
                getTypeInfos(type).some(ti => Navigator.isFindable(ti));

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


    defaultView(value: ModifiableEntity | Lite<Entity>, propertyRoute: PropertyRoute): Promise<ModifiableEntity> {

        return Navigator.view(value, { propertyRoute: propertyRoute, component: this.props.getComponent } as Navigator.ViewOptions);
    }


    handleViewClick = (event: React.MouseEvent) => {

        event.preventDefault();

        const ctx = this.state.ctx;
        const entity = ctx.value;

        const openWindow = (event.button == 2 || event.ctrlKey) && !this.state.type!.isEmbedded;
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

                this.convert(e).then(m => this.setValue(m)).done();
            }).done();
        }
    }

    renderViewButton(btn: boolean) {
        if (!this.state.view)
            return undefined;

        return (
            <a className={classes("sf-line-button", "sf-view", btn ? "btn btn-default" : undefined) }
                onClick={this.handleViewClick}
                title={EntityControlMessage.View.niceToString() }>
                <span className="glyphicon glyphicon-arrow-right"/>
            </a>
        );
    }

    chooseType(predicate: (ti: TypeInfo) => boolean): Promise<string> {
        const t = this.state.type!;

        if (t.isEmbedded)
            return Promise.resolve(t.name);

        const tis = getTypeInfos(t).filter(ti => predicate(ti));

        return SelectorModal.chooseType(tis)
            .then(ti => ti ? ti.name : undefined);
    }

    defaultCreate(): Promise<ModifiableEntity | Lite<Entity> | undefined> {

        return this.chooseType(t => Navigator.isCreable(t, !!this.props.getComponent, false))
            .then(typeName => typeName ? Constructor.construct(typeName) : undefined)
            .then(e => e ? e.entity : undefined);
    }

    handleCreateClick = (event: React.SyntheticEvent) => {

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
            <a className={classes("sf-line-button", "sf-create", btn ? "btn btn-default" : undefined) }
                onClick={this.handleCreateClick}
                title={EntityControlMessage.Create.niceToString() }>
                <span className="glyphicon glyphicon-plus"/>
            </a>
        );
    }

    static entityHtmlProps(entity: ModifiableEntity | Lite<Entity> | undefined | null): React.HTMLAttributes {

        return {
            'data-entity': entityInfo(entity)
        } as any;
    }


    defaultFind(): Promise<ModifiableEntity | Lite<Entity> | undefined> {
        return this.chooseType(Finder.isFindable)
            .then<ModifiableEntity | Lite<Entity> | undefined>(qn =>
                qn == undefined ? undefined : Finder.find({ queryName: qn } as FindOptions));
    }
    handleFindClick = (event: React.SyntheticEvent) => {

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
            <a className={classes("sf-line-button", "sf-find", btn ? "btn btn-default" : undefined) }
                onClick={this.handleFindClick}
                title={EntityControlMessage.Find.niceToString() }>
                <span className="glyphicon glyphicon-search"/>
            </a>
        );
    }

    handleRemoveClick = (event: React.SyntheticEvent) => {

        event.preventDefault();

        (this.props.onRemove ? this.props.onRemove(this.props.ctx.value) : Promise.resolve(true))
            .then(result => {
                if (result == false)
                    return;

                this.setValue(null);
            }).done();
    };

    renderRemoveButton(btn: boolean) {
        if (!this.state.remove || this.state.ctx.readOnly)
            return undefined;

        return (
            <a className={classes("sf-line-button", "sf-remove", btn ? "btn btn-default" : undefined) }
                onClick={this.handleRemoveClick}
                title={EntityControlMessage.Remove.niceToString() }>
                <span className="glyphicon glyphicon-remove"/>
            </a>
        );
    }
}



