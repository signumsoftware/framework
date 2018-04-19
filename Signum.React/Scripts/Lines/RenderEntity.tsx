import * as React from 'react'
import { classes } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, EntityFrame } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, ReadonlyBinding, LambdaMemberType } from '../Reflection'
import { LineBase, LineBaseProps, runTasks, } from '../Lines/LineBase'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, JavascriptMessage, toLite, is, isEntity, isLite, isModifiableEntity, liteKey, getToString } from '../Signum.Entities'
import { EntityBase, EntityBaseProps} from './EntityBase'
import { ViewPromise } from "../Navigator";
import { ErrorBoundary } from '../Components';



export interface RenderEntityProps {
    ctx: TypeContext<ModifiableEntity | Lite<Entity> | undefined | null>;
    getComponent?: (ctx: TypeContext<any /*T*/>) => React.ReactElement<any>;
    getViewPromise?: (e: any /*T*/) => undefined | string | Navigator.ViewPromise<any>;
}

export interface RenderEntityState {
    getComponent?: (ctx: TypeContext<ModifiableEntity>) => React.ReactElement<any>;
    lastLoadedType?: string;
    lastLoadedViewName?: string;
}

const Anonymous = "__Anonymous__";

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
            if (this.state.getComponent != undefined || this.state.lastLoadedType != undefined || this.state.lastLoadedViewName != undefined)
                this.setState({ getComponent: undefined, lastLoadedType: undefined, lastLoadedViewName: undefined });
            return Promise.resolve(undefined);
        }

        var result = nextProps.getViewPromise && nextProps.getViewPromise(e);

        if (this.state.lastLoadedType == e.Type && this.state.lastLoadedViewName == RenderEntity.toViewName(result))
            return Promise.resolve(undefined);
        
        var viewPromise = result == undefined || typeof result == "string" ? Navigator.getViewPromise(e, result) : result;

        return viewPromise.promise.then(c => {
            this.setState({
                getComponent: c,
                lastLoadedType: e.Type,
                lastLoadedViewName: RenderEntity.toViewName(result)
            });
        });
    }

    static toViewName(result: undefined | string | Navigator.ViewPromise<ModifiableEntity>)  : string | undefined{
        return (result instanceof ViewPromise ? Anonymous : result);
    }

    entityComponent?: React.Component<any, any> | null;

    setComponent(c: React.Component<any, any> | null) {
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
            if (this.state.lastLoadedType != entity.Type)
                return null;

            var result = this.props.getViewPromise && this.props.getViewPromise(entity);
            if (this.state.lastLoadedViewName != RenderEntity.toViewName(result))
                return null

            getComponent = this.state.getComponent;

            if (getComponent == undefined)
                return null;
        }
       
        const ti = getTypeInfo(entity.Type);

        const ctx = this.props.ctx;

        const pr = !ti ? ctx.propertyRoute : PropertyRoute.root(ti);
        

        const frame: EntityFrame = {
            frameComponent: this,
            entityComponent: this.entityComponent,
            revalidate: () => this.props.ctx.frame && this.props.ctx.frame.revalidate(),
            onClose: () => { throw new Error("Not implemented Exception"); },
            onReload: pack => { throw new Error("Not implemented Exception"); },
            setError: (modelState, initialPrefix) => { throw new Error("Not implemented Exception"); },
            refreshCount: 0,
        }; 

        const newCtx = new TypeContext<ModifiableEntity>(ctx, { frame }, pr, new ReadonlyBinding(entity, ""));

        return (
            <div data-property-path={ctx.propertyPath}>
                <ErrorBoundary>
                    {React.cloneElement(getComponent(newCtx), { ref: (c: React.Component<any, any> | null) => this.setComponent(c) })}
                </ErrorBoundary>
            </div>
        );
    }

}

