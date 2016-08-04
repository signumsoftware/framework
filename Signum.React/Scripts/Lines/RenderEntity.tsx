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
    ctx: TypeContext<ModifiableEntity | Lite<Entity> | undefined | null>;
    getComponent?: (ctx: TypeContext<ModifiableEntity>) => React.ReactElement<any>;
}

export interface RenderEntityState {
    getComponent?: (ctx: TypeContext<ModifiableEntity>) => React.ReactElement<any>;
    lastLoadedType?: string;
}

export class RenderEntity extends React.Component<RenderEntityProps, RenderEntityState> {

    constructor(props: RenderEntityProps) {
        super(props);

        this.state = { getComponent: undefined, lastLoadedType: undefined };
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
            return Promise.resolve(undefined);

        const ent = this.toEntity(nextProps.ctx.value);
        if (ent)
            return Promise.resolve(undefined);

        const lite = nextProps.ctx.value as Lite<Entity>;
        return Navigator.API.fetchAndRemember(lite).then(a => undefined);
    }


    toEntity(entityOrLite: ModifiableEntity | Lite<Entity> | undefined | null): ModifiableEntity | undefined {

        if (!entityOrLite)
            return undefined;

        if (isLite(entityOrLite))
            return entityOrLite.entity;
        
        if (isModifiableEntity(entityOrLite))
            return entityOrLite;

        throw new Error("Unexpected value " + entityOrLite);
    }

    loadComponent(nextProps: RenderEntityProps): Promise<void> {

        const e = this.toEntity(nextProps.ctx.value);

        if (nextProps.getComponent)
            return Promise.resolve(undefined);

        if (e == undefined) {
            this.setState({ getComponent: undefined, lastLoadedType: undefined });
            return Promise.resolve(undefined);
        }
        
        if (this.state.lastLoadedType == e.Type)
            return Promise.resolve(undefined);


        return Navigator.getComponent(e).then(c => {
            this.setState({
                getComponent: (ctx) => React.createElement<{ ctx: TypeContext<ModifiableEntity> }>(c, {
                    ctx: ctx
                }),
                lastLoadedType: e.Type
            });
        });
    }

    entityComponent: React.Component<any, any>;

    setComponent(c: React.Component<any, any>) {
        if (c && this.entityComponent != c) {
            this.entityComponent = c;
            this.forceUpdate();
        }
    }

    render() {
        const entity = this.toEntity(this.props.ctx.value);

        if (entity == undefined)
            return null;

        let getComponent = this.props.getComponent;

        if (getComponent == undefined) {
            if (entity.Type != this.state.lastLoadedType)
                return null;

            getComponent = this.state.getComponent;

            if (getComponent == undefined)
                return null;
        }

       
        const ti = getTypeInfo(entity.Type);

        const ctx = this.props.ctx;

        const pr = !ti ? ctx.propertyRoute : PropertyRoute.root(ti);
        

        const frame: EntityFrame<ModifiableEntity> = {
            frameComponent: this,
            entityComponent: this.entityComponent,
            revalidate: () => this.props.ctx.frame && this.props.ctx.frame.revalidate(),
            onClose: () => { throw new Error("Not implemented Exception"); },
            onReload: pack => { throw new Error("Not implemented Exception"); },
            setError: (modelState, initialPrefix) => { throw new Error("Not implemented Exception"); },
        }; 

        const newCtx = new TypeContext<ModifiableEntity>(ctx, { frame }, pr, new ReadonlyBinding(entity, ""));

        return (
            <div data-propertypath={ctx.propertyPath}>
                {React.cloneElement(getComponent(newCtx), { ref: (c: React.Component<any, any>) => this.setComponent(c) }) }
            </div>
        );
    }

}

