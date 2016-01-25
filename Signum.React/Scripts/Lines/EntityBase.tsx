import * as React from 'react'
import { Link } from 'react-router'
import { Dic, classes } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll } from '../Reflection'
import { ModifiableEntity, Lite, IEntity, Entity, EntityControlMessage, JavascriptMessage, toLite, is, liteKey } from '../Signum.Entities'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks} from '../Lines/LineBase'
import Typeahead from '../Lines/Typeahead'
import SelectorPopup from '../SelectorPopup'


export interface EntityBaseProps extends LineBaseProps {
    view?: boolean;
    viewOnCreate?: boolean;
    navigate?: boolean;
    create?: boolean;
    find?: boolean;
    remove?: boolean;

    onView?: (entity: ModifiableEntity | Lite<IEntity>, pr: PropertyRoute) => Promise<ModifiableEntity>;
    onCreate?: () => Promise<ModifiableEntity | Lite<IEntity>>;
    onFind?: () => Promise<ModifiableEntity | Lite<IEntity>>;
    onRemove?: (entity: ModifiableEntity| Lite<IEntity>) => Promise<boolean>;

    component?: Navigator.EntityComponent<any>;
}


export abstract class EntityBase<T extends EntityBaseProps> extends LineBase<T>
{
    calculateDefaultState(state: EntityBaseProps) {

        var type = state.type;

        state.create = type.isEmbedded ? Navigator.isCreable(type.name, false) :
            type.name == IsByAll ? false :
                getTypeInfos(type).some(ti=> Navigator.isCreable(ti, false));

        state.view = type.isEmbedded ? Navigator.isViewable(type.name, !!state.component) :
            type.name == IsByAll ? true :
                getTypeInfos(type).some(ti=> Navigator.isViewable(ti, !!state.component));

        state.navigate = type.isEmbedded ? Navigator.isNavigable(type.name, !!state.component) :
            type.name == IsByAll ? true :
                getTypeInfos(type).some(ti=> Navigator.isNavigable(ti, !!state.component));

        state.find = type.isEmbedded ? false :
            type.name == IsByAll ? false :
                getTypeInfos(type).some(ti=> Navigator.isFindable(ti));

        state.viewOnCreate = true;
        state.remove = true;
    }
    
    convert(entityOrLite: ModifiableEntity | Lite<IEntity>): Promise<ModifiableEntity | Lite<IEntity>> {

        var tr = this.state.type;

        var isLite = (entityOrLite as Lite<IEntity>).EntityType != null;
        var entityType = (entityOrLite as Lite<IEntity>).EntityType || (entityOrLite as ModifiableEntity).Type;


        if (tr.isEmbedded) {
            if (entityType != tr.name || isLite)
                throw new Error(`Impossible to convert '${entityType}' to '${tr.name}'`);

            return Promise.resolve(entityOrLite as ModifiableEntity);
        } else {
            if (tr.name != IsByAll && !tr.name.split(',').contains(entityType))
                throw new Error(`Impossible to convert '${entityType}' to '${tr.name}'`);

            if (isLite == tr.isLite)
                return Promise.resolve(entityOrLite);

            if (isLite)
                return Navigator.API.fetchEntity(entityOrLite as Lite<IEntity>);
            
            var entity = entityOrLite as Entity; 

            return Promise.resolve(toLite(entity, true));
        }
    }


    defaultView(value: ModifiableEntity | Lite<IEntity>): Promise<ModifiableEntity> {
        return Navigator.view({ entity: value, propertyRoute: this.state.ctx.propertyRoute });
    }
    

    handleViewClick = (event: React.SyntheticEvent) => {

        var ctx = this.state.ctx;
        var entity = ctx.value;

        var onView = this.state.onView ?
            this.state.onView(entity, ctx.propertyRoute) :
            this.defaultView(entity);

        onView.then(e => {
            if (e == null)
                return;

            this.convert(e).then(m => this.setValue(m));
        });
    }

    renderViewButton(btn: boolean) {
        if (!this.state.view)
            return null;

        return <a className={classes("sf-line-button", "sf-view", btn ? "btn btn-default" : null) }
            onClick={this.handleViewClick}
            title={EntityControlMessage.View.niceToString() }>
            <span className="glyphicon glyphicon-arrow-right"/>
            </a>;
    }

    chooseType(predicate: (ti: TypeInfo) => boolean): Promise<string> {
        var t = this.state.type;

        if (t.isEmbedded)
            return Promise.resolve(t.name);

        var tis = getTypeInfos(t).filter(predicate);

        return SelectorPopup.chooseType<TypeInfo>(tis)
            .then(ti=> ti ? ti.name : null);
    }

    defaultCreate(): Promise<ModifiableEntity | Lite<IEntity>> {

        return this.chooseType(Navigator.isCreable)
            .then(typeName => typeName ? Constructor.construct(typeName) : null);
    }

    handleCreateClick = (event: React.SyntheticEvent) =>
    {
        var onCreate = this.props.onCreate ?
            this.props.onCreate() : this.defaultCreate();

        onCreate.then(e=> {

            if (e == null)
                return null;

            if (!this.state.viewOnCreate)
                return Promise.resolve(e);

            return this.state.onView ?
                this.state.onView(e, this.state.ctx.propertyRoute) :
                this.defaultView(e);
        }).then(e=> {

            if (!e)
                return;

            this.convert(e).then(m => this.setValue(m));
        });
    };

    renderCreateButton(btn: boolean) {
        if (!this.state.create || this.state.ctx.readOnly)
            return null;

        return <a className={classes("sf-line-button", "sf-create", btn ? "btn btn-default" : null) }
            onClick={this.handleCreateClick}
            title={EntityControlMessage.Create.niceToString() }>
            <span className="glyphicon glyphicon-plus"/>
            </a>;
    }


    defaultFind(): Promise<ModifiableEntity | Lite<IEntity>> {
        return this.chooseType(Finder.isFindable)
            .then(qn=> qn == null ? null : Finder.find({ queryName: qn } as FindOptions));
    }
    handleFindClick = (event: React.SyntheticEvent) => {
        var result = this.state.onFind ? this.state.onFind() : this.defaultFind();

        result.then(entity=> {
            if (!entity)
                return;

            this.convert(entity).then(e=> this.setValue(e));
        });
    };
    renderFindButton(btn: boolean) {
        if (!this.state.find || this.state.ctx.readOnly)
            return null;

        return <a className={classes("sf-line-button", "sf-find", btn ? "btn btn-default" : null) }
            onClick={this.handleFindClick}
            title={EntityControlMessage.Find.niceToString() }>
            <span className="glyphicon glyphicon-search"/>
            </a>;
    }

    handleRemoveClick = (event: React.SyntheticEvent) => {
        (this.state.onRemove ? this.state.onRemove(this.props.ctx.value) : Promise.resolve(true))
            .then(result=> {
                if (result == false)
                    return;

                this.setValue(null);
            });
    };
    renderRemoveButton(btn: boolean) {
        if (!this.state.remove || this.state.ctx.readOnly)
            return null;

        return <a className={classes("sf-line-button", "sf-remove", btn ? "btn btn-default" : null) }
            onClick={this.handleRemoveClick}
            title={EntityControlMessage.Remove.niceToString() }>
            <span className="glyphicon glyphicon-remove"/>
            </a>;
    }
}



