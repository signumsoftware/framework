import * as React from 'react'
import { RouteObject } from 'react-router'
import { DateTime } from 'luxon'
import { Link } from 'react-router-dom';
import { ajaxGet } from '@framework/Services';
import { EntitySettings } from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { Lite, Entity, getToString, isEntity, JavascriptMessage } from '@framework/Signum.Entities'
import { OperationLogEntity } from '@framework/Signum.Entities.Basics'
import * as QuickLinks from '@framework/QuickLinks'
import { TimeMachineMessage, TimeMachinePermission } from './Signum.DiffLog';
import { ImportComponent } from '@framework/ImportComponent'
import { getTypeInfo, getTypeInfos, QueryTokenString } from '@framework/Reflection';
import { EntityLink, SearchControl, SearchControlLoaded } from '@framework/Search';
import { liteKey } from '@framework/Signum.Entities';
import { EntityControlMessage } from '@framework/Signum.Entities';
import { tryGetTypeInfos } from '@framework/Reflection';
import { CellFormatter } from '@framework/Finder';
import { TypeReference } from '@framework/Reflection';
import { isPermissionAuthorized } from '../Authorization/AuthClient';
import { SearchControlOptions } from '@framework/SearchControl/SearchControl';
import { TimeMachineCompareModal, TimeMachineModal } from './Templates/TimeMachinePage';
import { QueryString } from '@framework/QueryString';
import * as Widgets from '@framework/Frames/Widgets';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { getTimeMachineIcon, TimeMachineColors } from '@framework/Lines/TimeMachineIcon';

export function start(options: { routes: RouteObject[], timeMachine: boolean }) {
  Navigator.addSettings(new EntitySettings(OperationLogEntity, e => import('./Templates/OperationLog')));

  if (options.timeMachine) {

    QuickLinks.registerGlobalQuickLink(ctx => getTypeInfo(ctx.lite.EntityType).isSystemVersioned && isPermissionAuthorized(TimeMachinePermission.ShowTimeMachine) ?
      new QuickLinks.QuickLinkAction(TimeMachineMessage.TimeMachine.niceToString(),
        () => TimeMachineMessage.TimeMachine.niceToString(),
        e => {
          if (e.ctrlKey)
            window.open(AppContext.toAbsoluteUrl(timeMachineRoute(ctx.lite)));
          else
            TimeMachineModal.show(ctx.lite);
        }, {
          icon: "clock-rotate-left",
          color: "info",
          iconColor: "blue",
          group: null,
      }) : undefined);

    QuickLinks.registerGlobalQuickLink(ctx => {
      if (!getTypeInfo(ctx.lite.EntityType).isSystemVersioned && isPermissionAuthorized(TimeMachinePermission.ShowTimeMachine))
        return undefined;

      if (!(ctx.contextualContext?.container instanceof SearchControlLoaded))
        return undefined;

      var sc = ctx.contextualContext?.container;
      if (sc.props.findOptions.systemTime == null ||
        sc.state.selectedRows == null ||
        sc.state.selectedRows.length != 2 ||
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
        e => TimeMachineCompareModal.show(lite, versions), {
        icon: "not-equal",
        iconColor: "blue",
      });
    }, { allowsMultiple: true });

    SearchControlOptions.showSystemTimeButton = sc => isPermissionAuthorized(TimeMachinePermission.ShowTimeMachine);

    options.routes.push({ path: "/timeMachine/:type/:id", element: <ImportComponent onImport={() => import("./Templates/TimeMachinePage")} /> });

    Finder.entityFormatRules.push({
      name: "ViewHistory",
      isApplicable: (sc) => sc != null && sc.props.findOptions.systemTime != null && Finder.isSystemVersioned(sc.props.queryDescription.columns["Entity"].type),
      formatter: new Finder.EntityFormatter((row, columns, sc) => {

        var icon: undefined | React.ReactElement = undefined;

        const fop = sc?.state.resultFindOptions;
        let created = false;
        let deleted = false;
        if (fop && fop.systemTime) {
          var validFromIndex = columns.indexOf("Entity.SystemValidFrom");
          var validToIndex = columns.indexOf("Entity.SystemValidTo");
          if (validFromIndex != -1 && validToIndex != -1) {
            var validFrom = DateTime.fromISO(row.columns[validFromIndex]);
            var validTo = DateTime.fromISO(row.columns[validToIndex]);

            created = fop.systemTime.mode == "Between" ? DateTime.fromISO(fop.systemTime.startDate!) <= validFrom : true;
            deleted = fop.systemTime.mode == "Between" ? validTo <= DateTime.fromISO(fop.systemTime.endDate!) : validTo.year < 9999;

            var title = created && deleted ? TimeMachineMessage.ThisVersionWasCreatedAndDeleted.niceToString() :
              created ? TimeMachineMessage.ThisVersionWasCreated.niceToString() :
                deleted ? TimeMachineMessage.ThisVersionWasDeleted.niceToString() :
                  TimeMachineMessage.ThisVersionDidNotChange.niceToString(); 

            icon = <span className="ms-2" title={title + (fop.systemTime.mode == "Between" ? (" " + TimeMachineMessage.BetweenThisTimeRange.niceToString()) : "")}>
              {created && <FontAwesomeIcon icon="plus" color={TimeMachineColors.created} />}
              {deleted && <FontAwesomeIcon icon="minus" color={TimeMachineColors.removed} className={created ? "ms-1" : undefined} />}
              {!created && !deleted && fop.systemTime.mode == "Between" && <FontAwesomeIcon icon="equals" color={TimeMachineColors.noChange} />}
            </span>;
          }
        }

        if (sc?.state.resultFindOptions?.groupResults) {
          return (
            <a href="#" className="sf-line-button sf-view" onClick={e => { e.preventDefault(); sc!.openRowGroup(row); }}
              style={{ whiteSpace: "nowrap", opacity: deleted ? .5 : undefined }} >
              <span title={JavascriptMessage.ShowGroup.niceToString()}>
                <FontAwesomeIcon icon="layer-group" />
              </span>
              {icon}
            </a>
          );
        }

        if (!row.entity || !Navigator.isViewable(row.entity.EntityType, { isSearch: true }))
          return icon;

        return (
          <TimeMachineLink lite={row.entity} inSearch={true} style={{ whiteSpace: "nowrap", opacity: deleted ? .5 : undefined }} >
            {EntityControlMessage.View.niceToString()}
            {icon}
          </TimeMachineLink >
        );

      })
    });

    Finder.formatRules.push({
      name: "Lite_TM",
      isApplicable: (qt, sc) => qt.filterType == "Lite" && sc != null && sc.props.findOptions.systemTime != null && Finder.isSystemVersioned(qt.type),
      formatter: qt => new CellFormatter((cell: Lite<Entity>, ctx) => !cell ? undefined : <TimeMachineLink lite={cell} />, true)
    });

    Finder.formatRules.push({
      name: "LiteNoFill_TM",
      isApplicable: (qt, sc) => {
        return qt.filterType == "Lite" && sc != null && sc.props.findOptions.systemTime != null && Finder.isSystemVersioned(qt.type) &&
          tryGetTypeInfos(qt.type)?.every(ti => ti && Navigator.getSettings(ti)?.avoidFillSearchColumnWidth);
      },
      formatter: qt => new CellFormatter((cell: Lite<Entity> | undefined, ctx) => !cell ? undefined : <TimeMachineLink lite={cell} />, false)
    });
  }
}

export function timeMachineRoute(lite: Lite<Entity>) {
  return "/timeMachine/" + lite.EntityType + "/" + lite.id;
}

export namespace API {

  export function getPreviousOperationLog(id: string | number): Promise<PreviousLog> {
    return ajaxGet({ url: "/api/diffLog/previous/" + id });
  }

  export function getNextOperationLog(id: string | number): Promise<NextLog> {
    return ajaxGet({ url: "/api/diffLog/next/" + id });
  }

  export function getEntityDump(lite: Lite<Entity>, asOf: string,): Promise<EntityDump> {
    return ajaxGet({ url: `/api/retrieveVersion/${lite.EntityType}/${lite.id}?` + QueryString.stringify({asOf}) });
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

    window.open(AppContext.toAbsoluteUrl(timeMachineRoute(lite)));
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
