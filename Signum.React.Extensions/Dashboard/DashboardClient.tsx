import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals';
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { Entity, Lite, liteKey, MList, toLite, is, EntityPack } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import * as QuickLinks from '../../../Framework/Signum.React/Scripts/QuickLinks'
import { PseudoType, QueryKey, getQueryKey, Type, isEntity } from '../../../Framework/Signum.React/Scripts/Reflection'
import { TypeContext } from '../../../Framework/Signum.React/Scripts/TypeContext'
import { WidgetContext, onEmbeddedWidgets, EmbeddedWidgetPosition } from '../../../Framework/Signum.React/Scripts/Frames/Widgets'
import { FindOptions, FilterOption, FilterOperation, OrderOption, ColumnOption,
    FilterRequest, QueryRequest, Pagination, QueryTokenType, QueryToken, FilterType, SubTokensOptions, ResultTable, OrderRequest } from '../../../Framework/Signum.React/Scripts/FindOptions'
import * as AuthClient  from '../../../Extensions/Signum.React.Extensions/Authorization/AuthClient'
import * as ChartClient from '../../../Extensions/Signum.React.Extensions/Chart/ChartClient'
import * as UserChartClient from '../../../Extensions/Signum.React.Extensions/Chart/UserChart/UserChartClient'
import * as UserQueryClient from '../../../Extensions/Signum.React.Extensions/UserQueries/UserQueryClient'
import { QueryFilterEntity, QueryColumnEntity, QueryOrderEntity } from '../UserQueries/Signum.Entities.UserQueries'

import { DashboardPermission, DashboardEntity, CountSearchControlPartEntity, LinkListPartEntity, UserChartPartEntity, UserQueryPartEntity, IPartEntity, DashboardMessage, DashboardEmbedededInEntity } from './Signum.Entities.Dashboard'
import { QueryTokenEntity } from '../UserAssets/Signum.Entities.UserAssets'
import * as UserAssetClient from '../UserAssets/UserAssetClient'


export interface PanelPartContentProps<T extends IPartEntity> {
    part: T;
    entity: Lite<Entity>;
}

export interface PartRenderer<T extends IPartEntity>{
    component: () => Promise<React.ComponentClass<PanelPartContentProps<T>>>;
    handleTitleClick?: (part: T, entity: Lite<Entity> | undefined, e: React.MouseEvent) => void;
    handleFullScreenClick?: (part: T, entity: Lite<Entity> | undefined, e: React.MouseEvent) => void;
}


export const partRenderers : { [typeName:string] : PartRenderer<IPartEntity>} = {};

export function start(options: { routes: JSX.Element[] }) {

    UserAssetClient.start({ routes: options.routes });
    UserAssetClient.registerExportAssertLink(DashboardEntity);

    Navigator.addSettings(new EntitySettings(DashboardEntity, e => new Promise(resolve => require(['./Admin/Dashboard'], resolve))));

    Navigator.addSettings(new EntitySettings(CountSearchControlPartEntity, e => new Promise(resolve => require(['./Admin/CountSearchControlPart'], resolve))));
    Navigator.addSettings(new EntitySettings(LinkListPartEntity, e => new Promise(resolve => require(['./Admin/LinkListPart'], resolve))));
    Navigator.addSettings(new EntitySettings(UserChartPartEntity, e => new Promise(resolve => require(['./Admin/UserChartPart'], resolve))));
    Navigator.addSettings(new EntitySettings(UserQueryPartEntity, e => new Promise(resolve => require(['./Admin/UserQueryPart'], resolve))));

    options.routes.push(<Route path="dashboard">
        <Route path=":dashboardId" getComponent={ (loc, cb) => require(["./View/DashboardPage"], (Comp) => cb(undefined, Comp.default)) } />
    </Route>);

    registerRenderer(CountSearchControlPartEntity, { 
        component: () => new Promise(resolve => require(['./View/CountSearchControlPart'], resolve)).then((a : any) => a.default)
    });
    registerRenderer(LinkListPartEntity, {
        component: () =>new Promise(resolve => require(['./View/LinkListPart'], resolve)).then((a: any) => a.default)
    });
    registerRenderer(UserChartPartEntity, {
        component: () => new Promise(resolve => require(['./View/UserChartPart'], resolve)).then((a: any) => a.default),
        handleTitleClick: (p, e, ev) => {
            ev.preventDefault();
            navigateOrWindowsOpen(ev, Navigator.navigateRoute(p.userChart!));
        },
        handleFullScreenClick: (p, e, ev) => {
            ev.preventDefault();
            UserChartClient.Converter.toChartRequest(p.userChart!, e)
                .then(cr => navigateOrWindowsOpen(ev, ChartClient.Encoder.chartRequestPath(cr, { userChart: liteKey(toLite(p.userChart!)) })))
                .done();
        }
    });


    registerRenderer(UserQueryPartEntity, { 
        component: () => new Promise(resolve => require(['./View/UserQueryPart'], resolve)).then((a: any) => a.default),
        handleTitleClick: (p, e, ev) => {
            ev.preventDefault();
            navigateOrWindowsOpen(ev, Navigator.navigateRoute(p.userQuery!));
        },
        handleFullScreenClick: (p, e, ev) => {
            ev.preventDefault();
            UserQueryClient.Converter.toFindOptions(p.userQuery!, e)
                .then(cr => navigateOrWindowsOpen(ev, Finder.findOptionsPath(cr, { userQuery: liteKey(toLite(p.userQuery!)) })))
                .done()
        }
    });

    onEmbeddedWidgets.push(ctx => !isEntity(ctx.pack.entity.Type) || ctx.pack.entity.isNew || !AuthClient.isPermissionAuthorized(DashboardPermission.ViewDashboard) ? undefined :
        { position: "Top", embeddedWidget: <DashboardWidget position="Top" pack={ ctx.pack as EntityPack<Entity> } /> });

    onEmbeddedWidgets.push(ctx => !isEntity(ctx.pack.entity.Type) || ctx.pack.entity.isNew || !AuthClient.isPermissionAuthorized(DashboardPermission.ViewDashboard)? undefined :
        { position: "Bottom", embeddedWidget: <DashboardWidget position="Bottom" pack={ ctx.pack as EntityPack<Entity> } /> });

    QuickLinks.registerGlobalQuickLink(ctx => {
        if (!AuthClient.isPermissionAuthorized(DashboardPermission.ViewDashboard))
            return undefined;

        return API.forEntityType(ctx.lite.EntityType).then(das =>
            das.map(d => new QuickLinks.QuickLinkAction(liteKey(d), d.toStr || "", e => {
                navigateOrWindowsOpen(e, "~/dashboard/" + d.id + "?entity=" + liteKey(ctx.lite))
            }, { glyphicon: "glyphicon-th-large", glyphiconColor: "darkslateblue" })));
    });

    QuickLinks.registerQuickLink(DashboardEntity, ctx => new QuickLinks.QuickLinkAction("preview", DashboardMessage.Preview.niceToString(),
        e => Navigator.API.fetchAndRemember(ctx.lite)
            .then(db => {
                if (db.entityType == undefined)
                    navigateOrWindowsOpen(e, "~/dashboard/" + ctx.lite.id);
                else
                    Navigator.API.fetchAndRemember(db.entityType)
                        .then(t => Finder.find({ queryName: t.cleanName }))
                        .then(lite => {
                            if (!lite)
                                return;

                            navigateOrWindowsOpen(e, "~/dashboard/" + ctx.lite.id + "?entity=" + liteKey(lite));
                        }).done();
            }).done()));
}

function navigateOrWindowsOpen(e: React.MouseEvent, url: string){
    if (e.ctrlKey || e.button == 2) {
        window.open(url);
    } else {
        Navigator.currentHistory.push(url);
    }
}

export function registerRenderer<T extends IPartEntity>(type: Type<T>, renderer : PartRenderer<T>){
    partRenderers[type.typeName] = renderer;    
} 

export module API {
    export function forEntityType(type: string): Promise<Lite<DashboardEntity>[]> {
        return ajaxGet<Lite<DashboardEntity>[]>({ url: "~/api/dashboard/forEntityType/" + type });
    }

    export function embedded(type: string, position: DashboardEmbedededInEntity): Promise<DashboardEntity> {
        return ajaxGet<DashboardEntity>({ url: "~/api/dashboard/embedded/" + type + "/" + position });
    }
}

export interface DashboardWidgetProps {
    pack: EntityPack<Entity>,
    position: DashboardEmbedededInEntity;  
}

export interface DashboardWidgetState {
    dashboard?: DashboardEntity;
    component?: React.ComponentClass<{ dashboard: DashboardEntity, entity: Entity}>
}

export class DashboardWidget extends React.Component<DashboardWidgetProps, DashboardWidgetState> {

    state = { dashboard: undefined } as DashboardWidgetState;
    
    componentWillMount() {
        this.load(this.props);
    }

    componentWillReceiveProps(newProps: DashboardWidgetProps) {
        if (!is(newProps.pack.entity as Entity, this.props.pack.entity as Entity)) {
            this.load(newProps);
        }
    }

    load(props: DashboardWidgetProps) {      

        if (props.pack.entity.isNew) {
            this.setState({ dashboard: undefined });

        } else {
            this.setState({ dashboard: undefined });

            API.embedded(props.pack.entity.Type, props.position)
                .then(d => {
                    this.setState({ dashboard: d });
                    if (d && !this.state.component)
                        require(["./View/DashboardView"], mod => this.setState({ component: mod.default }));
                }).done();
        }
    }

    render() {

        if (!this.state.dashboard || !this.state.component)
            return null;

        return React.createElement(this.state.component, {
            dashboard: this.state.dashboard,
            entity: this.props.pack.entity
        });
    }
}

