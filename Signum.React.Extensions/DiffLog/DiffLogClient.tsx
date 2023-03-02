import * as React from 'react'
import { Link } from 'react-router-dom';
import { ajaxGet } from '@framework/Services';
import { EntitySettings } from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { Lite, Entity, getToString } from '@framework/Signum.Entities'
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
import { QueryString } from '@framework/QueryString';

export function start(options: { routes: JSX.Element[], timeMachine: boolean }) {
  Navigator.addSettings(new EntitySettings(OperationLogEntity, e => import('./Templates/OperationLog')));

  if (options.timeMachine) {
    QuickLinks.registerGlobalQuickLink(ctx => getTypeInfo(ctx.lite.EntityType).isSystemVersioned && isPermissionAuthorized(TimeMachinePermission.ShowTimeMachine) ?
      new QuickLinks.QuickLinkLink("TimeMachine",
        () => TimeMachineMessage.TimeMachine.niceToString(),
        timeMachineRoute(ctx.lite), {
          icon: "clock-rotate-left",
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
      var versions = sc.state.selectedRows.map(r => r.columns[index] as string);

      return new QuickLinks.QuickLinkAction("CompareTimeMachine",
        () => TimeMachineMessage.CompareVersions.niceToString(),
        e => TimeMachineModal.show(lite, versions), {
        icon: "not-equal",
        iconColor: "blue",
      });
    }, { allowsMultiple : true });

    SearchControlOptions.showSystemTimeButton = sc => isPermissionAuthorized(TimeMachinePermission.ShowTimeMachine);

    options.routes.push(<ImportRoute path="~/timeMachine/:type/:id" onImportModule={() => import("./Templates/TimeMachinePage")} />);

    Finder.entityFormatRules.push({
      name: "ViewHistory",
      isApplicable: (sc) => sc != null && sc.props.findOptions.systemTime != null && isSystemVersioned(sc.props.queryDescription.columns["Entity"].type),
      formatter: new Finder.EntityFormatter((row, columns, sc) => !row.entity || !Navigator.isViewable(row.entity.EntityType, { isSearch: true }) ? undefined :
        <TimeMachineLink lite={row.entity}
          inSearch={true}>
          {EntityControlMessage.View.niceToString()}
        </TimeMachineLink>
      )
    });

    Finder.formatRules.push({
      name: "Lite_TM",
      isApplicable: (qt, sc) => qt.filterType == "Lite" && sc != null && sc.props.findOptions.systemTime != null && isSystemVersioned(qt.type),
      formatter: qt => new CellFormatter((cell: Lite<Entity>, ctx) => !cell ? undefined : <TimeMachineLink lite={cell} />, true)
    });

    Finder.formatRules.push({
      name: "LiteNoFill_TM",
      isApplicable: (qt, sc) => {
        return qt.filterType == "Lite" && sc != null && sc.props.findOptions.systemTime != null && isSystemVersioned(qt.type) &&
          tryGetTypeInfos(qt.type)?.every(ti => ti && Navigator.getSettings(ti)?.avoidFillSearchColumnWidth);
      },
      formatter: qt => new CellFormatter((cell: Lite<Entity> | undefined, ctx) => !cell ? undefined : <TimeMachineLink lite={cell} />, false)
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

  export function getPreviousOperationLog(id: string | number): Promise<PreviousLog> {
    return ajaxGet({ url: "~/api/diffLog/previous/" + id });
  }

  export function getNextOperationLog(id: string | number): Promise<NextLog> {
    return ajaxGet({ url: "~/api/diffLog/next/" + id });
  }

  export function getEntityDump(lite: Lite<Entity>, asOf: string,): Promise<EntityDump> {
    return ajaxGet({ url: `~/api/retrieveVersion/${lite.EntityType}/${lite.id}?` + QueryString.stringify({asOf}) });
  }
}

export interface PreviousLog {
  operationLog: Lite<OperationLogEntity>;
  dump: string;
}

export interface NextLog {
  operationLog?: Lite<OperationLogEntity>;
  dump: string;
}

export interface EntityDump {
  entity: Entity;
  dump: string;
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

  if (!Navigator.isViewable(lite.EntityType, { isSearch: p.inSearch }))
    return <span data-entity={liteKey(lite)}>{p.children ?? getToString(lite)}</span>;


  return (
    <Link
      to={timeMachineRoute(lite)}
      title={getToString(lite)}
      onClick={handleClick}
      data-entity={liteKey(lite)}
      {...(htmlAtts as React.HTMLAttributes<HTMLAnchorElement>)}>
      {children ?? getToString(lite)}
    </Link>
  );
}
