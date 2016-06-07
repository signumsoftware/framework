import * as React from 'react'
import { Link } from 'react-router'
import { classes } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, EntityFrame } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, ReadonlyBinding, LambdaMemberType } from '../Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks, } from '../Lines/LineBase'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, JavascriptMessage, toLite, is, isEntity, isLite, isModifiableEntity, liteKey, getToString } from '../Signum.Entities'
import Typeahead from '../Lines/Typeahead'
import { EntityBase, EntityBaseProps} from './EntityBase'



export interface RenderEntityProps {
    ctx?: TypeContext<ModifiableEntity | Lite<Entity>>;
    getComponent?: (ctx: TypeContext<ModifiableEntity>) => React.ReactElement<any>;
}

export interface RenderEntityState {
    getComponent?: (ctx: TypeContext<ModifiableEntity>) => React.ReactElement<any>;
    lastLoadedType?: string;
}

export class RenderEntity extends React.Component<RenderEntityProps, RenderEntityState> {

    constructor(props) {
        super(props);

        this.state = { getComponent: null, lastLoadedType: null };
    }


    componentWillMount() {
        this.loadEntity(this.props)
            .then(() => this.loadComponent(this.props))
            .then(() => this.forceUpdate())
            .done();
    }

    componentWillReceiveProps(nextProps: RenderEntityProps) {
        this.loadEntity(nextProps)
            .then(() => this.loadComponent(nextProps))
            .then(() => this.forceUpdate())
            .done();
    }

    loadEntity(nextProps: RenderEntityProps): Promise<void> {

        if (!nextProps.ctx.value)
            return Promise.resolve(null);

        var ent = this.toEntity(nextProps.ctx.value);
        if (ent)
            return Promise.resolve(null);

        var lite = nextProps.ctx.value as Lite<Entity>;
        return Navigator.API.fetchAndRemember(lite).then(a => null);
    }


    toEntity(entityOrLite: ModifiableEntity | Lite<Entity>): ModifiableEntity {

        if (!entityOrLite)
            return null;

        if (isLite(entityOrLite))
            return entityOrLite.entity;
        
        if (isModifiableEntity(entityOrLite))
            return entityOrLite;

        throw new Error("Unexpected value " + entityOrLite);
    }

    loadComponent(nextProps: RenderEntityProps): Promise<void> {

        var e = this.toEntity(nextProps.ctx.value);

        if (nextProps.getComponent)
            return Promise.resolve(null);

        if (e == null) {
            this.setState({ getComponent: null, lastLoadedType: null });
            return Promise.resolve(null);
        }
        
        if (this.state.lastLoadedType == e.Type)
            return Promise.resolve(null);


        return Navigator.getComponent(e).then(c => {
            this.setState({
                getComponent: (ctx) => React.createElement<{ ctx: TypeContext<ModifiableEntity> }>(c, {
                    ctx: ctx
                }),
                lastLoadedType: e.Type
            });
        });
    }

    render() {
        var entity = this.toEntity(this.props.ctx.value);

        if (entity == null)
            return null;

        var getComponent = this.props.getComponent;

        if (getComponent == null) {
            if (entity.Type != this.state.lastLoadedType)
                return null;

            getComponent = this.state.getComponent;

            if (getComponent == null)
                return null;
        }

       
        var ti = getTypeInfo(entity.Type);

        var ctx = this.props.ctx;

        var pr = !ti ? ctx.propertyRoute : PropertyRoute.root(ti);
        

        var frame: EntityFrame<ModifiableEntity> = {
            frameComponent: this,
            entityComponent: null,
            onClose: () => { throw new Error("Not implemented Exception"); },
            onReload: pack => { throw new Error("Not implemented Exception"); },
            setError: (modelState, initialPrefix) => { throw new Error("Not implemented Exception"); },
        }; 

        var newCtx = new TypeContext<ModifiableEntity>(ctx, { frame }, pr, new ReadonlyBinding(entity, ""));

        return (
            <div data-propertypath={ctx.propertyPath}>
                {getComponent(newCtx) }
            </div>
        );
    }

}

