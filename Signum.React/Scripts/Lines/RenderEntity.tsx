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
import { ModifiableEntity, Lite, Entity, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString } from '../Signum.Entities'
import Typeahead from '../Lines/Typeahead'
import { EntityBase, EntityBaseProps} from './EntityBase'



export interface RenderEntityProps {
    ctx?: TypeContext<ModifiableEntity | Lite<Entity>>;
    getComponent?: (ctx: TypeContext<ModifiableEntity>, frame: EntityFrame<ModifiableEntity>) => React.ReactElement<any>;
}

export interface RenderEntityState {
    getComponent?: (ctx: TypeContext<ModifiableEntity>, frame: EntityFrame<ModifiableEntity>) => React.ReactElement<any>;
    lastLoadedType?: string;
}

export class RenderEntity extends React.Component<RenderEntityProps, RenderEntityState> {

    constructor(props) {
        super(props);

        this.state = { getComponent: null, lastLoadedType: null };
    }


    componentWillMount() {
        this.loadEntity()
            .then(() => this.loadComponent())
            .done();
    }

    componentWillReceiveProps(nextProps: RenderEntityProps) {
        this.loadEntity()
            .then(() => this.loadComponent())
            .done();
    }

    loadEntity(): Promise<void> {

        if (!this.props.ctx.value)
            return Promise.resolve(null);

        var ent = this.getEntity();
        if (ent)
            return Promise.resolve(null);


        var lite = this.props.ctx.value as Lite<Entity>;
        return Navigator.API.fetchAndRemember(lite).then(a => null);
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

    loadComponent(): Promise<void> {

        var e = this.getEntity();

        if (this.props.getComponent)
            return Promise.resolve(null);


        if (e == null) {
            this.setState({ getComponent: null, lastLoadedType: null });
            return Promise.resolve(null);
        }
        
        if (this.state.lastLoadedType == e.Type)
            return Promise.resolve(null);

        return Navigator.getComponent(e).then(c => {
            this.setState({
                getComponent: (ctx, frame) => React.createElement<EntityComponentProps<ModifiableEntity>>(c, {
                    ctx: ctx,
                    frame: frame
                }),
                lastLoadedType: e.Type
            });
        });
    }

    render() {
        var entity = this.getEntity();
        var getComponent = this.props.getComponent;

        if (getComponent == null) {

            if (entity.Type != this.state.lastLoadedType)
                return <span>entity.Type is {entity.Type} but lastLoadedType is {this.state.lastLoadedType}</span>;

            getComponent = this.state.getComponent;
        }

        if (entity == null || getComponent == null)
            return null;
        
        var ti = getTypeInfo(entity.Type);

        var ctx = this.props.ctx;

        var pr = !ti ? ctx.propertyRoute : PropertyRoute.root(ti);
        
        var newCtx = new TypeContext<ModifiableEntity>(ctx, null, pr, new ReadonlyBinding(entity, ""));
        
        var frame: EntityFrame<ModifiableEntity> = {
            onClose: () => { throw new Error("Not implemented Exception"); },
            onReload: pack => { throw new Error("Not implemented Exception"); },
            setError: (modelState, initialPrefix) => { throw new Error("Not implemented Exception"); },
        }; 

        return getComponent(newCtx, frame);
    }

}

