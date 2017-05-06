import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals';
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { Entity, Lite, liteKey, MList, toLite, is, EntityPack } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import * as QuickLinks from '../../../Framework/Signum.React/Scripts/QuickLinks'
import { PseudoType, QueryKey, getQueryKey, Type, isTypeEntity } from '../../../Framework/Signum.React/Scripts/Reflection'
import { TypeContext } from '../../../Framework/Signum.React/Scripts/TypeContext'
import { WidgetContext, onEmbeddedWidgets, EmbeddedWidgetPosition } from '../../../Framework/Signum.React/Scripts/Frames/Widgets'
import { FindOptions, FilterOption, FilterOperation, OrderOption, ColumnOption,
    FilterRequest, QueryRequest, Pagination, QueryTokenType, QueryToken, FilterType, SubTokensOptions, ResultTable, OrderRequest } from '../../../Framework/Signum.React/Scripts/FindOptions'
import * as AuthClient  from '../../../Extensions/Signum.React.Extensions/Authorization/AuthClient'
import * as ChartClient from '../../../Extensions/Signum.React.Extensions/Chart/ChartClient'
import * as UserChartClient from '../../../Extensions/Signum.React.Extensions/Chart/UserChart/UserChartClient'
import * as UserQueryClient from '../../../Extensions/Signum.React.Extensions/UserQueries/UserQueryClient'
import { QueryFilterEmbedded, QueryColumnEmbedded, QueryOrderEmbedded } from '../UserQueries/Signum.Entities.UserQueries'

import { DashboardPermission, DashboardEntity, ValueUserQueryListPartEntity, LinkListPartEntity, UserChartPartEntity, UserQueryPartEntity, IPartEntity, DashboardMessage, DashboardEmbedededInEntity } from './Signum.Entities.Dashboard'
import { QueryTokenEmbedded } from '../UserAssets/Signum.Entities.UserAssets'
import * as UserAssetClient from '../UserAssets/UserAssetClient'
import { ImportRoute, ComponentModule } from "../../../Framework/Signum.React/Scripts/AsyncImport";


export interface PanelPartContentProps<T extends IPartEntity> {
    part: T;
    entity: Lite<Entity>;
}

export interface PartRenderer<T extends IPartEntity>{
    component: () => Promise<React.ComponentClass<PanelPartContentProps<T>>>;
    handleTitleClick?: (part: T, entity: Lite<Entity> | undefined, e: React.MouseEvent<any>) => void;
    handleFullScreenClick?: (part: T, entity: Lite<Entity> | undefined, e: React.MouseEvent<any>) => void;
}


export const partRenderers : { [typeName:string] : PartRenderer<IPartEntity>} = {};

export function start(options: { routes: JSX.Element[] }) {

    UserAssetClient.start({ routes: options.routes });
    UserAssetClient.registerExportAssertLink(DashboardEntity);

    Navigator.addSettings(new EntitySettings(DashboardEntity, e => _import('./Admin/Dashboard')));

    Navigator.addSettings(new EntitySettings(ValueUserQueryListPartEntity, e => _import('./Admin/ValueUserQueryListPart')));
    Navigator.addSettings(new EntitySettings(LinkListPartEntity, e => _import('./Admin/LinkListPart')));
    Navigator.addSettings(new EntitySettings(UserChartPartEntity, e => _import('./Admin/UserChartPart')));
    Navigator.addSettings(new EntitySettings(UserQueryPartEntity, e => _import('./Admin/UserQueryPart')));

    Finder.addSettings({ queryName: DashboardEntity, defaultOrderColumn: "DashboardPriority", defaultOrderType: "Descending" });

    options.routes.push(<ImportRoute path="~/dashboard/:dashboardId" onImportModule={() => _import("./View/DashboardPage")} />);

    registerRenderer(ValueUserQueryListPartEntity, {
        component: () => _import<ComponentModule>('./View/ValueUserQueryListPart').then(a => a.default)
    });
    registerRenderer(LinkListPartEntity, {
        component: () => _import<ComponentModule>('./View/LinkListPart').then(a => a.default)
    });
    registerRenderer(UserChartPartEntity, {
        component: () => _import<ComponentModule>('./View/UserChartPart').then(a => a.default),
        handleTitleClick: (p, e, ev) => {
            ev.preventDefault();
            Navigator.pushOrOpen(Navigator.navigateRoute(p.userChart!), ev);
        },
        handleFullScreenClick: (p, e, ev) => {
            ev.preventDefault();
            UserChartClient.Converter.toChartRequest(p.userChart!, e)
                .then(cr => Navigator.pushOrOpen(ChartClient.Encoder.chartRequestPath(cr, { userChart: liteKey(toLite(p.userChart!)) }), ev))
                .done();
        }
    });


    registerRenderer(UserQueryPartEntity, {
        component: () => _import('./View/UserQueryPart').then((a: any) => a.default),
        handleTitleClick: (p, e, ev) => {
            ev.preventDefault();
            Navigator.pushOrOpen(Navigator.navigateRoute(p.userQuery!), ev);
        },
        handleFullScreenClick: (p, e, ev) => {
            ev.preventDefault();
            UserQueryClient.Converter.toFindOptions(p.userQuery!, e)
                .then(cr => Navigator.pushOrOpen(Finder.findOptionsPath(cr, { userQuery: liteKey(toLite(p.userQuery!)) }), ev))
                .done()
        }
    });

    onEmbeddedWidgets.push(ctx => !isTypeEntity(ctx.pack.entity.Type) || ctx.pack.entity.isNew || !AuthClient.isPermissionAuthorized(DashboardPermission.ViewDashboard) ? undefined :
        { position: "Top", embeddedWidget: <DashboardWidget position="Top" pack={ ctx.pack as EntityPack<Entity> } /> });

    onEmbeddedWidgets.push(ctx => !isTypeEntity(ctx.pack.entity.Type) || ctx.pack.entity.isNew || !AuthClient.isPermissionAuthorized(DashboardPermission.ViewDashboard)? undefined :
        { position: "Bottom", embeddedWidget: <DashboardWidget position="Bottom" pack={ ctx.pack as EntityPack<Entity> } /> });

    QuickLinks.registerGlobalQuickLink(ctx => {
        if (!AuthClient.isPermissionAuthorized(DashboardPermission.ViewDashboard))
            return undefined;

        return API.forEntityType(ctx.lite.EntityType).then(das =>
            das.map(d => new QuickLinks.QuickLinkAction(liteKey(d), d.toStr || "", e => {
                Navigator.pushOrOpen(dashboardUrl(d, ctx.lite), e)
            }, { icon: "glyphicon glyphicon-th-large", iconColor: "darkslateblue" })));
    });

    QuickLinks.registerQuickLink(DashboardEntity, ctx => new QuickLinks.QuickLinkAction("preview", DashboardMessage.Preview.niceToString(),
        e => Navigator.API.fetchAndRemember(ctx.lite)
            .then(db => {
                if (db.entityType == undefined)
                    Navigator.pushOrOpen(dashboardUrl(ctx.lite), e);
                else
                    Navigator.API.fetchAndRemember(db.entityType)
                        .then(t => Finder.find({ queryName: t.cleanName }))
                        .then(entity => {
                            if (!entity)
                                return;

                            Navigator.pushOrOpen(dashboardUrl(ctx.lite, entity), e);
                        }).done();
            }).done()));
}

export function dashboardUrl(lite: Lite<DashboardEntity>, entity?: Lite<Entity>) {
    return "~/dashboard/" + lite.id + (!entity ? "" : "?entity=" + liteKey(entity)); 
}

export function registerRenderer<T extends IPartEntity>(type: Type<T>, renderer : PartRenderer<T>){
    partRenderers[type.typeName] = renderer;    
} 

export module API {
    export function forEntityType(type: string): Promise<Lite<DashboardEntity>[]> {
        return ajaxGet<Lite<DashboardEntity>[]>({ url: `~/api/dashboard/forEntityType/${type}` });
    }

    export function embedded(type: string, position: DashboardEmbedededInEntity): Promise<DashboardEntity> {
        return ajaxGet<DashboardEntity>({ url: `~/api/dashboard/embedded/${type}/${position}` });
    }

    export function home(): Promise<Lite<DashboardEntity> | null> {
        return ajaxGet<Lite<DashboardEntity> | null>({ url: "~/api/dashboard/home" });
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
                        _import<ComponentModule>("./View/DashboardView")
                            .then(mod => this.setState({ component: mod.default }))
                            .done();
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

