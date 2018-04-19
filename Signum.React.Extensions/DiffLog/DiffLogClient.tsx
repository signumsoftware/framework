import * as React from 'react'
import { Route } from 'react-router'
import { Link } from 'react-router-dom';
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals';
import { ajaxPost, ajaxPostRaw, ajaxGet, saveFile } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { QueryRequest } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From, registerToString, JavascriptMessage, toLite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { OperationLogEntity } from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { PseudoType, QueryKey, GraphExplorer, OperationType, Type, getTypeName  } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import * as QuickLinks from '../../../Framework/Signum.React/Scripts/QuickLinks'
import { TimeMachineMessage } from './Signum.Entities.DiffLog';
import { ImportRoute } from '../../../Framework/Signum.React/Scripts/AsyncImport';
import { getTypeInfo } from '../../../Framework/Signum.React/Scripts/Reflection';
import { EntityLink } from '../../../Framework/Signum.React/Scripts/Search';
import { liteKey } from '../../../Framework/Signum.React/Scripts/Signum.Entities';
import { EntityControlMessage } from '../../../Framework/Signum.React/Scripts/Signum.Entities';
import { getTypeInfos } from '../../../Framework/Signum.React/Scripts/Reflection';
import { CellFormatter } from '../../../Framework/Signum.React/Scripts/Finder';
import { TypeReference } from '../../../Framework/Signum.React/Scripts/Reflection';

export function start(options: { routes: JSX.Element[], timeMachine: boolean }) {

    Navigator.addSettings(new EntitySettings(OperationLogEntity, e => import('./Templates/OperationLog')));

    if (options.timeMachine) {

        QuickLinks.registerGlobalQuickLink(ctx => getTypeInfo(ctx.lite.EntityType).isSystemVersioned ? new QuickLinks.QuickLinkLink("TimeMachine",
            TimeMachineMessage.TimeMachine.niceToString(),
            timeMachineRoute(ctx.lite), {
            icon: "fa fa-history",
            iconColor: "blue"
        }) : undefined);

        options.routes.push(<ImportRoute path="~/timeMachine/:type/:id" onImportModule={() => import("./Templates/TimeMachinePage")} />);

        Finder.entityFormatRules.push(
            {
                name: "ViewHistory",
                isApplicable: (row, sc) => sc != null && sc.props.findOptions.systemTime != null && isSystemVersioned(sc.props.queryDescription.columns["Entity"].type),
                formatter: (row, columns, sc) => !row.entity || !Navigator.isNavigable(row.entity.EntityType, undefined, true)  ? undefined :
                    <TimeMachineLink lite={row.entity}
                        inSearch={true}>
                        {EntityControlMessage.View.niceToString()}
                    </TimeMachineLink>
            });

        Finder.formatRules.push(
            {
                name: "Lite",
                isApplicable: (col, sc) => col.token!.filterType == "Lite" && sc != null && sc.props.findOptions.systemTime != null && isSystemVersioned(col.token!.type),
                formatter: col => new CellFormatter((cell: Lite<Entity>, ctx) => !cell ? undefined : <TimeMachineLink lite={cell} />)
            },);
    }
}

function isSystemVersioned(tr?: TypeReference) {
    return tr != null && getTypeInfos(tr).some(ti => ti.isSystemVersioned == true)
}

export function timeMachineRoute(lite: Lite<Entity>) {
    return "~/timeMachine/" + lite.EntityType + "/" + lite.id;
}

export namespace API {

    export function diffLog(id: string | number): Promise<DiffLogResult> {
        return ajaxGet<DiffLogResult>({ url: "~/api/diffLog/" + id });
    }

    export function retrieveVersion(lite: Lite<Entity>, asOf: string,): Promise<Entity> {
        return ajaxGet<Entity>({ url: `~/api/retrieveVersion/${lite.EntityType}/${lite.id}?asOf=${asOf}`});
    }

    export function diffVersions(lite: Lite<Entity>, from: string, to: string): Promise<DiffBlock> {
        return ajaxGet<DiffBlock>({ url: `~/api/diffVersions/${lite.EntityType}/${lite.id}?from=${from}&to=${to}` });
    }
}

export interface DiffLogResult {
    prev: Lite<OperationLogEntity>;
    diffPrev: DiffBlock;
    diff: DiffBlock;
    diffNext: DiffBlock;
    next: Lite<OperationLogEntity>;
}

export type DiffBlock = Array<DiffPair<Array<DiffPair<string>>>>;

export interface DiffPair<T> {
    Action: "Equal" | "Added" | "Removed";
    Value: T ;
}

export interface TimeMachineLinkProps extends React.HTMLAttributes<HTMLAnchorElement>, React.Props<EntityLink> {
    lite: Lite<Entity>;
    inSearch?: boolean;
}

export default class TimeMachineLink extends React.Component<TimeMachineLinkProps>{

    render() {
        const { lite, inSearch, children, ...htmlAtts } = this.props;

        if (!Navigator.isNavigable(lite.EntityType, undefined, this.props.inSearch || false))
            return <span data-entity={liteKey(lite)}>{this.props.children || lite.toStr}</span>;


        return (
            <Link
                to={timeMachineRoute(lite)}
                title={lite.toStr}
                onClick={this.handleClick}
                data-entity={liteKey(lite)}
                {...(htmlAtts as React.HTMLAttributes<HTMLAnchorElement>)}>
                {children || lite.toStr}
            </Link>
        );
    }

    handleClick = (event: React.MouseEvent<any>) => {

        const lite = this.props.lite;
        
        event.preventDefault();

        window.open(Navigator.toAbsoluteUrl(timeMachineRoute(lite)));
    }
}
