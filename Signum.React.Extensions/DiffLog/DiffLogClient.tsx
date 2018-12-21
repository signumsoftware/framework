import * as React from 'react'
import { Link } from 'react-router-dom';
import { ajaxGet } from '@framework/Services';
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { Lite, Entity } from '@framework/Signum.Entities'
import { OperationLogEntity } from '@framework/Signum.Entities.Basics'
import * as QuickLinks from '@framework/QuickLinks'
import { TimeMachineMessage, TimeMachinePermission } from './Signum.Entities.DiffLog';
import { ImportRoute } from '@framework/AsyncImport';
import { getTypeInfo } from '@framework/Reflection';
import { EntityLink, SearchControl } from '@framework/Search';
import { liteKey } from '@framework/Signum.Entities';
import { EntityControlMessage } from '@framework/Signum.Entities';
import { getTypeInfos } from '@framework/Reflection';
import { CellFormatter } from '@framework/Finder';
import { TypeReference } from '@framework/Reflection';
import { isPermissionAuthorized } from '../Authorization/AuthClient';

export function start(options: { routes: JSX.Element[], timeMachine: boolean }) {
  Navigator.addSettings(new EntitySettings(OperationLogEntity, e => import('./Templates/OperationLog')));

  if (options.timeMachine) {
    QuickLinks.registerGlobalQuickLink(ctx => getTypeInfo(ctx.lite.EntityType).isSystemVersioned && isPermissionAuthorized(TimeMachinePermission.ShowTimeMachine) ?
      new QuickLinks.QuickLinkLink("TimeMachine",
        TimeMachineMessage.TimeMachine.niceToString(),
        timeMachineRoute(ctx.lite), {
          icon: "history",
          iconColor: "blue"
        }) : undefined);

    SearchControl.showSystemTimeButton = sc => isPermissionAuthorized(TimeMachinePermission.ShowTimeMachine);

    options.routes.push(<ImportRoute path="~/timeMachine/:type/:id" onImportModule={() => import("./Templates/TimeMachinePage")} />);

    Finder.entityFormatRules.push(
      {
        name: "ViewHistory",
        isApplicable: (row, sc) => sc != null && sc.props.findOptions.systemTime != null && isSystemVersioned(sc.props.queryDescription.columns["Entity"].type),
        formatter: (row, columns, sc) => !row.entity || !Navigator.isNavigable(row.entity.EntityType, undefined, true) ? undefined :
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
      });
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

  export function retrieveVersion(lite: Lite<Entity>, asOf: string, ): Promise<Entity> {
    return ajaxGet<Entity>({ url: `~/api/retrieveVersion/${lite.EntityType}/${lite.id}?asOf=${asOf}` });
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
  action: "Equal" | "Added" | "Removed";
  value: T;
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
