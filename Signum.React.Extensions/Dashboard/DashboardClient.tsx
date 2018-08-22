import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { IconProp} from '@fortawesome/fontawesome-svg-core'
import { Route } from 'react-router'
import { Dic, classes } from '@framework/Globals';
import { ajaxPost, ajaxGet } from '@framework/Services';
import { EntitySettings, ViewPromise } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { EntityOperationSettings } from '@framework/Operations'
import { Entity, Lite, liteKey, MList, toLite, is, EntityPack } from '@framework/Signum.Entities'
import * as Constructor from '@framework/Constructor'
import * as Operations from '@framework/Operations'
import * as QuickLinks from '@framework/QuickLinks'
import { PseudoType, QueryKey, getQueryKey, Type, isTypeEntity } from '@framework/Reflection'
import { TypeContext } from '@framework/TypeContext'
import { WidgetContext, onEmbeddedWidgets, EmbeddedWidgetPosition } from '@framework/Frames/Widgets'
import { FindOptions, FilterOption, FilterOperation, OrderOption, ColumnOption,
    FilterRequest, QueryRequest, Pagination, QueryTokenType, QueryToken, FilterType, SubTokensOptions, ResultTable, OrderRequest } from '@framework/FindOptions'
import * as AuthClient  from '../Authorization/AuthClient'
import * as ChartClient from '../Chart/ChartClient'
import * as UserChartClient from '../Chart/UserChart/UserChartClient'
import * as UserQueryClient from '../UserQueries/UserQueryClient'
import { QueryFilterEmbedded, QueryColumnEmbedded, QueryOrderEmbedded } from '../UserQueries/Signum.Entities.UserQueries'

import { DashboardPermission, DashboardEntity, ValueUserQueryListPartEntity, LinkListPartEntity, UserChartPartEntity, UserQueryPartEntity, IPartEntity, DashboardMessage, DashboardEmbedededInEntity } from './Signum.Entities.Dashboard'
import { QueryTokenEmbedded } from '../UserAssets/Signum.Entities.UserAssets'
import * as UserAssetClient from '../UserAssets/UserAssetClient'
import { ImportRoute, ComponentModule } from "@framework/AsyncImport";
import { ModifiableEntity } from "@framework/Signum.Entities";


export interface PanelPartContentProps<T extends IPartEntity> {
    part: T;
    entity?: Lite<Entity>;
}

interface IconColor {
    icon: IconProp;
    iconColor: string;
}

export interface PartRenderer<T extends IPartEntity>{
    component: () => Promise<React.ComponentClass<PanelPartContentProps<T>>>;
    defaultIcon: (element: T) => IconColor;
    withPanel?: (element: T) => boolean;
    handleTitleClick?: (part: T, entity: Lite<Entity> | undefined, e: React.MouseEvent<any>) => void;
    handleEditClick?: (part: T, entity: Lite<Entity> | undefined, e: React.MouseEvent<any>) => void;
}


export const partRenderers : { [typeName:string] : PartRenderer<IPartEntity>} = {};

export function start(options: { routes: JSX.Element[] }) {

    UserAssetClient.start({ routes: options.routes });
    UserAssetClient.registerExportAssertLink(DashboardEntity);

    Navigator.addSettings(new EntitySettings(DashboardEntity, e => import('./Admin/Dashboard')));

    Navigator.addSettings(new EntitySettings(ValueUserQueryListPartEntity, e => import('./Admin/ValueUserQueryListPart')));
    Navigator.addSettings(new EntitySettings(LinkListPartEntity, e => import('./Admin/LinkListPart')));
    Navigator.addSettings(new EntitySettings(UserChartPartEntity, e => import('./Admin/UserChartPart')));
    Navigator.addSettings(new EntitySettings(UserQueryPartEntity, e => import('./Admin/UserQueryPart')));

    Finder.addSettings({ queryName: DashboardEntity, defaultOrderColumn: "DashboardPriority", defaultOrderType: "Descending" });

    options.routes.push(<ImportRoute path="~/dashboard/:dashboardId" onImportModule={() => import("./View/DashboardPage")} />);

    registerRenderer(ValueUserQueryListPartEntity, {
        component: () => import('./View/ValueUserQueryListPart').then(a => a.default),
        defaultIcon: () => ({ icon: ["far", "list-alt"], iconColor: "lightblue" })
    });
    registerRenderer(LinkListPartEntity, {
        component: () => import('./View/LinkListPart').then(a => a.default),
        defaultIcon: () => ({ icon: ["far", "list-alt"], iconColor: "forestgreen" })
    });
    registerRenderer(UserChartPartEntity, {
        component: () => import('./View/UserChartPart').then(a => a.default),
        defaultIcon: () => ({ icon: "chart-bar", iconColor: "violet" }),
        handleEditClick: (p, e, ev) => {
            ev.preventDefault();
            Navigator.pushOrOpenInTab(Navigator.navigateRoute(p.userChart!), ev);
        },
        handleTitleClick: (p, e, ev) => {
            ev.preventDefault();
            ev.persist();
            UserChartClient.Converter.toChartRequest(p.userChart!, e)
                .then(cr => Navigator.pushOrOpenInTab(ChartClient.Encoder.chartPath(cr, toLite(p.userChart!)), ev))
                .done();
        },

    });
    
    registerRenderer(UserQueryPartEntity, {
        component: () => import('./View/UserQueryPart').then((a: any) => a.default),
        defaultIcon: () => ({ icon: ["far", "list-alt"], iconColor: "dodgerblue" }),
        withPanel: p => p.renderMode != "BigValue",
        handleEditClick: (p, e, ev) => {
            ev.preventDefault();
            Navigator.pushOrOpenInTab(Navigator.navigateRoute(p.userQuery!), ev);
        },
        handleTitleClick: (p, e, ev) => {
            ev.preventDefault();
            ev.persist();
            UserQueryClient.Converter.toFindOptions(p.userQuery!, e)
                .then(cr => Navigator.pushOrOpenInTab(Finder.findOptionsPath(cr, { userQuery: liteKey(toLite(p.userQuery!)) }), ev))
                .done()
        }
    });

    onEmbeddedWidgets.push(ctx => ctx.pack.embeddedDashboard &&
    {
        position: ctx.pack.embeddedDashboard.embeddedInEntity as "Top" | "Bottom",
        embeddedWidget: <DashboardWidget dashboard={ctx.pack.embeddedDashboard} pack={ctx.pack as EntityPack<Entity>} />
    });

    QuickLinks.registerGlobalQuickLink(ctx => {
        if (!AuthClient.isPermissionAuthorized(DashboardPermission.ViewDashboard))
            return undefined;

        var promise = ctx.widgetContext ?
            Promise.resolve(ctx.widgetContext.pack.dashboards || []) :
            API.forEntityType(ctx.lite.EntityType);

        return promise.then(das =>
            das.map(d => new QuickLinks.QuickLinkAction(liteKey(d), d.toStr || "", e => {
                Navigator.pushOrOpenInTab(dashboardUrl(d, ctx.lite), e)
            }, { icon: "tachometer-alt", iconColor: "darkslateblue" })));
    });

    QuickLinks.registerQuickLink(DashboardEntity, ctx => new QuickLinks.QuickLinkAction("preview", DashboardMessage.Preview.niceToString(),
        e => Navigator.API.fetchAndRemember(ctx.lite)
            .then(db => {
                if (db.entityType == undefined)
                    Navigator.pushOrOpenInTab(dashboardUrl(ctx.lite), e);
                else
                    Navigator.API.fetchAndRemember(db.entityType)
                        .then(t => Finder.find({ queryName: t.cleanName }))
                        .then(entity => {
                            if (!entity)
                                return;

                            Navigator.pushOrOpenInTab(dashboardUrl(ctx.lite, entity), e);
                        }).done();
            }).done()));
}

export function defaultIcon<T extends IPartEntity>(part: T) {
    return partRenderers[part.Type].defaultIcon(part);
}

export function dashboardUrl(lite: Lite<DashboardEntity>, entity?: Lite<Entity>) {
    return "~/dashboard/" + lite.id + (!entity ? "" : "?entity=" + liteKey(entity)); 
}

export function registerRenderer<T extends IPartEntity>(type: Type<T>, renderer: PartRenderer<T>) {
    partRenderers[type.typeName] = renderer as PartRenderer<any> as PartRenderer<IPartEntity>;
} 

export module API {
    export function forEntityType(type: string): Promise<Lite<DashboardEntity>[]> {
        return ajaxGet<Lite<DashboardEntity>[]>({ url: `~/api/dashboard/forEntityType/${type}` });
    }

    export function home(): Promise<Lite<DashboardEntity> | null> {
        return ajaxGet<Lite<DashboardEntity> | null>({ url: "~/api/dashboard/home" });
    }
}

declare module '@framework/Signum.Entities' {

    export interface EntityPack<T extends ModifiableEntity> {
        dashboards?: Array<Lite<DashboardEntity>>;
        embeddedDashboard?: DashboardEntity;
    }
}

export interface DashboardWidgetProps {
    pack: EntityPack<Entity>;
    dashboard: DashboardEntity;
}

export interface DashboardWidgetState {
    component?: React.ComponentClass<{ dashboard: DashboardEntity, entity?: Entity}>
}

export class DashboardWidget extends React.Component<DashboardWidgetProps, DashboardWidgetState> {

    state = { component: undefined } as DashboardWidgetState;
    
    componentWillMount() {
        this.load(this.props);
    }


    load(props: DashboardWidgetProps) {      

        import("./View/DashboardView")
            .then(mod => this.setState({ component: mod.default }))
            .done();
    }

    render() {

        if (!this.state.component)
            return null;

        return React.createElement(this.state.component, {
            dashboard: this.props.dashboard,
            entity: this.props.pack.entity
        });
    }
}

