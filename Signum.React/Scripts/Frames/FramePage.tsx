import * as React from 'react'
import { RouteComponentProps } from 'react-router'
import { Dic } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import ButtonBar from './ButtonBar'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../FindOptions'
import { Entity, Lite, is, toLite, LiteMessage, getToString, EntityPack, ModelState, JavascriptMessage, entityInfo } from '../Signum.Entities'
import { TypeContext, StyleOptions, EntityFrame } from '../TypeContext'
import { getTypeInfo, TypeInfo, PropertyRoute, ReadonlyBinding, getTypeInfos, GraphExplorer, parseId } from '../Reflection'
import { renderWidgets, renderEmbeddedWidgets, WidgetContext } from './Widgets'
import ValidationErrors from './ValidationErrors'
import * as QueryString from 'query-string'
import "./Frames.css"
import { ErrorBoundary } from '../Components';

interface FramePageProps extends RouteComponentProps<{ type: string; id?: string, waitData?: string }> {
}


interface FramePageState {
    pack?: EntityPack<Entity>;
    getComponent?: (ctx: TypeContext<Entity>) => React.ReactElement<any>;
    refreshCount: number;
}

export default class FramePage extends React.Component<FramePageProps, FramePageState> {

    constructor(props: FramePageProps) {
        super(props);
        this.state = this.calculateState(props);

    }

    componentWillMount() {
        this.load(this.props);
    }

    getTypeInfo(): TypeInfo {
        return getTypeInfo(this.props.match.params.type);
    }

    calculateState(props: FramePageProps): FramePageState {
        return {
            getComponent: undefined,
            pack: undefined,
            refreshCount: 0
        };
    }

    componentWillReceiveProps(newProps: FramePageProps) {
        this.setState(this.calculateState(newProps), () => {
            this.load(newProps);
        });
    }

    componentWillUnmount() {
        Navigator.setTitle();
    }

    load(props: FramePageProps) {
        this.loadEntity(props)
            .then(() => Navigator.setTitle(this.state.pack!.entity.toStr))
            .then(() => this.loadComponent())
            .done();
    }

    loadEntity(props: FramePageProps): Promise<void> {

        if (QueryString.parse(this.props.location.search).waitData) {
            if (window.opener.dataForChildWindow == undefined) {
                throw new Error("No dataForChildWindow in parent found!")
            }

            var pack = window.opener.dataForChildWindow;
            window.opener.dataForChildWindow = undefined;
            this.setState({ pack: pack });
            return Promise.resolve();
        }

        const ti = this.getTypeInfo();

        if (this.props.match.params.id) {

            const lite: Lite<Entity> = {
                EntityType: ti.name,
                id: parseId(ti, props.match.params.id!),
            };

            return Navigator.API.fetchEntityPack(lite)
                .then(pack => this.setState({ pack }));

        } else {

            return Constructor.construct(ti.name)
                .then(pack => this.setState({ pack: pack as EntityPack<Entity> }));
        }
    }


    loadComponent(): Promise<void> {
        return Navigator.getViewPromise(this.state.pack!.entity).promise
            .then(c => this.setState({ getComponent: c }));
    }

    onClose() {
        if (Finder.isFindable(this.state.pack!.entity.Type, true))
            Navigator.history.push(Finder.findOptionsPath({ queryName: this.state.pack!.entity.Type }));
        else
            Navigator.history.push("~/");
    }


    entityComponent?: React.Component<any, any> | null;

    setComponent(c: React.Component<any, any> | null) {
        if (c && this.entityComponent != c) {
            this.entityComponent = c;
            this.forceUpdate();
        }
    }

    render() {

        if (!this.state.pack) {
            return (
                <div className="normal-control">
                    {this.renderTitle()}
                </div>
            );
        }

        const entity = this.state.pack.entity;

        const frame: EntityFrame = {
            frameComponent: this,
            entityComponent: this.entityComponent,
            onReload: pack => {

                var packEntity = (pack || this.state.pack) as EntityPack<Entity>;

                if (packEntity.entity.id != null && entity.id == null)
                    Navigator.history.push(Navigator.navigateRoute(packEntity.entity));
                else
                    this.setState({ pack: packEntity, refreshCount: this.state.refreshCount + 1 });
            },
            onClose: () => this.onClose(),
            revalidate: () => this.validationErrors && this.validationErrors.forceUpdate(),
            setError: (ms, initialPrefix) => {
                GraphExplorer.setModelState(entity, ms, initialPrefix || "");
                this.forceUpdate()
            },
            refreshCount: this.state.refreshCount,
        };

        const ti = this.getTypeInfo();

        const styleOptions: StyleOptions = {
            readOnly: Navigator.isReadOnly(this.state.pack),
            frame: frame
        };

        const ctx = new TypeContext<Entity>(undefined, styleOptions, PropertyRoute.root(ti), new ReadonlyBinding(entity, ""));

        const wc: WidgetContext<Entity> = {
            ctx: ctx,
            pack: this.state.pack,
        };

        const embeddedWidgets = renderEmbeddedWidgets(wc);

        return (
            <div className="normal-control">
                {this.renderTitle()}
                {renderWidgets(wc)}
                {this.entityComponent && <ButtonBar frame={frame} pack={this.state.pack} />}
                <ValidationErrors entity={this.state.pack.entity} ref={ve => this.validationErrors = ve} />
                {embeddedWidgets.top}
                <div className="sf-main-control" data-test-ticks={new Date().valueOf()} data-main-entity={entityInfo(ctx.value)}>
                    <ErrorBoundary>
                        {this.state.getComponent && React.cloneElement(this.state.getComponent(ctx), { ref: (c: React.Component<any, any> | null) => this.setComponent(c) })}
                    </ErrorBoundary>
                </div>
                {embeddedWidgets.bottom}
            </div>
        );
    }

    validationErrors?: ValidationErrors | null;

    renderTitle() {

        if (!this.state.pack)
            return <h3 className="display-6">{JavascriptMessage.loading.niceToString()}</h3>;

        const entity = this.state.pack.entity;

        return (
            <h4>
                <span className="display-6">{getToString(entity)}</span>
                <br />
                <small className="sf-type-nice-name">{Navigator.getTypeTitle(entity, undefined)}</small>
            </h4>
        );
    }
}




