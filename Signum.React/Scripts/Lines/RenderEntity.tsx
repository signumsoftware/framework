import * as React from 'react'
import { Link } from 'react-router'
import { classes } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, ReadonlyBinding, LambdaMemberType } from '../Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks, } from '../Lines/LineBase'
import { EntityComponentProps, EntityFrame } from '../Lines'
import { ModifiableEntity, Lite, IEntity, Entity, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString } from '../Signum.Entities'
import Typeahead from '../Lines/Typeahead'
import { EntityBase, EntityBaseProps} from './EntityBase'



export interface RenderEntityProps {
    ctx?: TypeContext<ModifiableEntity | Lite<IEntity>>;
    getComponent?: (mod: ModifiableEntity) => Promise<React.ComponentClass<EntityComponentProps<ModifiableEntity>>>;
}

export interface RenderEntityState {
    component: React.ComponentClass<EntityComponentProps<ModifiableEntity>>;
}

export class RenderEntity extends React.Component<RenderEntityProps, RenderEntityState> {

    constructor(props) {
        super(props);

        this.state = { component: null };
    }


    componentWillMount() {
        this.loadEntity()
            .then(e => this.loadComponent(e));
    }

    componentWillReceiveProps(nextProps: RenderEntityProps) {
        if (!is(this.props.ctx.value, nextProps.ctx.value)) {
            this.setState({
                component: null
            });

            this.loadEntity()
                .then(e =>this.loadComponent(e));
        }
    }

    loadEntity(): Promise<Entity> {

        if (!this.props.ctx.value)
            return Promise.resolve(null);

        var ent = this.getEntity();
        if (ent)
            return Promise.resolve(ent);


        var lite = this.props.ctx.value as Lite<Entity>;
        return Navigator.API.fetchEntity(lite).then(e => {
            lite.entity = e;
            this.forceUpdate();
            return e;
        });
    }


    getEntity() {
        var element = this.props.ctx.value;

        if (!element)
            return null;

        var entity = element as Entity;
        if (entity.Type)
            return entity;

        var lite = element as Lite<Entity>;
        if (lite.EntityType)
            return lite.entity;

        throw new Error("Unexpected value " + lite);
    }

    loadComponent(e: Entity) {

        if (e == null)
            this.setState({ component: null });
    
        const promise = this.props.getComponent ?
            this.props.getComponent(e) :
            Navigator.getSettings(e.Type).onGetComponent(e);

        return promise
            .then(c => this.setState({ component: c }));
    }

    render() {
        var entity = this.getEntity();
        if (entity == null || this.state.component == null)
            return null;
        
        var ti = getTypeInfo(entity.Type);

        var ctx = this.props.ctx;

        var pr = !ti ? ctx.propertyRoute : PropertyRoute.root(ti);
        
        var newCtx = new TypeContext<ModifiableEntity>(ctx, null, pr, new ReadonlyBinding(entity));
        
        var frame: EntityFrame<ModifiableEntity> = {
            onClose: () => { throw new Error("Not implemented Exception"); },
            onReload: pack => { throw new Error("Not implemented Exception"); },
            setError: (modelState, initialPrefix) => { throw new Error("Not implemented Exception"); },
        }; 

        return React.createElement<EntityComponentProps<ModifiableEntity>>(this.state.component, {
            ctx: newCtx,
            frame: frame
        });
    }

}

