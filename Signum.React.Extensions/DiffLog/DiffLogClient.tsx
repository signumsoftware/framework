import * as React from 'react'
import { Link } from 'react-router-dom';
import { ajaxGet } from '@framework/Services';
import { EntitySettings } from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { Lite, Entity } from '@framework/Signum.Entities'
import { OperationLogEntity } from '@framework/Signum.Entities.Basics'
import * as QuickLinks from '@framework/QuickLinks'
import { TimeMachineMessage, TimeMachinePermission } from './Signum.Entities.DiffLog';
import { ImportRoute } from '@framework/AsyncImport';
import { getTypeInfo, getTypeInfos, QueryTokenString } from '@framework/Reflection';
import { EntityLink, SearchControl, SearchControlLoaded } from '@framework/Search';
import { liteKey } from '@framework/Signum.Entities';
import { EntityControlMessage } from '@framework/Signum.Entities';
import { tryGetTypeInfos } from '@framework/Reflection';
import { CellFormatter } from '@framework/Finder';
import { TypeReference } from '@framework/Reflection';
import { isPermissionAuthorized } from '../Authorization/AuthClient';
import { SearchControlOptions } from '@framework/SearchControl/SearchControl';
import { TimeMachineModal } from './Templates/TimeMachinePage';
import { asUTC } from '../../../Framework/Signum.React/Scripts/SearchControl/SystemTimeEditor';

export function start(options: { routes: JSX.Element[], timeMachine: boolean }) {
  Navigator.addSettings(new EntitySettings(OperationLogEntity, e => import('./Templates/OperationLog')));

  if (options.timeMachine) {
    QuickLinks.registerGlobalQuickLink(ctx => getTypeInfo(ctx.lite.EntityType).isSystemVersioned && isPermissionAuthorized(TimeMachinePermission.ShowTimeMachine) ?
      new QuickLinks.QuickLinkLink("TimeMachine",
        () => TimeMachineMessage.TimeMachine.niceToString(),
        timeMachineRoute(ctx.lite), {
          icon: "history",
          iconColor: "blue",
      }) : undefined);

    QuickLinks.registerGlobalQuickLink(ctx => {
      if (!getTypeInfo(ctx.lite.EntityType).isSystemVersioned && isPermissionAuthorized(TimeMachinePermission.ShowTimeMachine))
        return undefined;

      if (!(ctx.contextualContext?.container instanceof SearchControlLoaded))
        return undefined;

      var sc = ctx.contextualContext?.container;
      if (sc.props.findOptions.systemTime == null ||
        sc.state.selectedRows == null ||
        sc.state.selectedRows.length <= 1 ||
        sc.state.selectedRows.some(a => a.entity == null) ||
        sc.state.selectedRows.distinctBy(a => a.entity!.id!.toString()).length > 1)
        return undefined;

      var systemValidFromKey = QueryTokenString.entity().systemValidFrom().toString();

      var index = sc.props.findOptions.columnOptions.findIndex(co => co.token?.fullKey == systemValidFromKey);

      if (index == -1)
        return undefined;

      var lite = sc.state.selectedRows[0].entity!;
      var versions = sc.state.selectedRows.map(r => asUTC(r.columns[index] as string));

      return new QuickLinks.QuickLinkAction("CompareTimeMachine",
        () => TimeMachineMessage.CompareVersions.niceToString(),
        e => TimeMachineModal.show(lite, versions).done(), {
        icon: "not-equal",
        iconColor: "blue",
      });
    }, { allowsMultiple : true });

    SearchControlOptions.showSystemTimeButton = sc => isPermissionAuthorized(TimeMachinePermission.ShowTimeMachine);

    options.routes.push(<ImportRoute path="~/timeMachine/:type/:id" onImportModule={() => import("./Templates/TimeMachinePage")} />);

    Finder.entityFormatRules.push(
      {
        name: "ViewHistory",
        isApplicable: (sc) => sc != null && sc.props.findOptions.systemTime != null && isSystemVersioned(sc.props.queryDescription.columns["Entity"].type),
        formatter: new Finder.EntityFormatter((row, columns, sc) => !row.entity || !Navigator.isNavigable(row.entity.EntityType, { isSearch: true }) ? undefined :
          <TimeMachineLink lite={row.entity}
            inSearch={true}>
            {EntityControlMessage.View.niceToString()}
          </TimeMachineLink>
        )
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
  return AppContext.toAbsoluteUrl("~/timeMachine/" + lite.EntityType + "/" + lite.id);
}

export namespace API {

  export function diffLog(id: string | number): Promise<DiffLogResult> {
    return ajaxGet({ url: "~/api/diffLog/" + id });
  }

  export function retrieveVersion(lite: Lite<Entity>, asOf: string, ): Promise<Entity> {
    return ajaxGet({ url: `~/api/retrieveVersion/${lite.EntityType}/${lite.id}?asOf=${asOf}` });
  }

  export function diffVersions(lite: Lite<Entity>, from: string, to: string): Promise<DiffBlock> {
    return ajaxGet({ url: `~/api/diffVersions/${lite.EntityType}/${lite.id}?from=${from}&to=${to}` });
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

export interface TimeMachineLinkProps extends React.HTMLAttributes<HTMLAnchorElement> {
  lite: Lite<Entity>;
  inSearch?: boolean;
}

export default function TimeMachineLink(p : TimeMachineLinkProps){

  function handleClick(event: React.MouseEvent<any>) {
    const lite = p.lite;

    event.preventDefault();

    window.open(timeMachineRoute(lite));
  }
  const { lite, inSearch, children, ...htmlAtts } = p;

  if (!Navigator.isNavigable(lite.EntityType, { isSearch: p.inSearch }))
    return <span data-entity={liteKey(lite)}>{p.children ?? lite.toStr}</span>;


  return (
    <Link
      to={timeMachineRoute(lite)}
      title={lite.toStr}
      onClick={handleClick}
      data-entity={liteKey(lite)}
      {...(htmlAtts as React.HTMLAttributes<HTMLAnchorElement>)}>
      {children ?? lite.toStr}
    </Link>
  );
}
