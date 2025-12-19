import * as React from 'react'
import { RouteObject } from 'react-router'
import { DateTime } from 'luxon'
import { Link } from 'react-router-dom';
import { ajaxGet } from '@framework/Services';
import { Navigator, EntitySettings } from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import { Finder } from '@framework/Finder'
import { Lite, Entity, getToString, isEntity, JavascriptMessage } from '@framework/Signum.Entities'
import { OperationLogEntity } from '@framework/Signum.Operations'
import { QuickLinkClient, QuickLinkLink } from '@framework/QuickLinkClient'
import { ImportComponent } from '@framework/ImportComponent'
import { getTypeInfo, getTypeInfos, QueryTokenString } from '@framework/Reflection';
import { EntityLink, SearchControl, SearchControlLoaded } from '@framework/Search';
import { liteKey } from '@framework/Signum.Entities';
import { EntityControlMessage } from '@framework/Signum.Entities';
import { tryGetTypeInfos } from '@framework/Reflection';
import { TypeReference } from '@framework/Reflection';
import { SearchControlOptions } from '@framework/SearchControl/SearchControl';
import { TimeMachineCompareModal, TimeMachineModal } from './TimeMachinePage';
import { QueryString } from '@framework/QueryString';
import * as Widgets from '@framework/Frames/Widgets';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { getTimeMachineIcon, TimeMachineColors } from '@framework/Lines/TimeMachineIcon';
import { TimeMachineMessage, TimeMachinePermission } from './Signum.TimeMachine';
import { ChangeLogClient } from '@framework/Basics/ChangeLogClient';
import { LinkButton } from '@framework/Basics/LinkButton';

export namespace TimeMachineClient {

  export function start(options: { routes: RouteObject[] }): void {

    ChangeLogClient.registerChangeLogModule("Signum.TimeMachine", () => import("./Changelog"));

    if (AppContext.isPermissionAuthorized(TimeMachinePermission.ShowTimeMachine))
      QuickLinkClient.registerGlobalQuickLink(entityType => Promise.resolve(!getTypeInfo(entityType).isSystemVersioned ? [] :
        [
          new QuickLinkLink("TimeMachine", () => TimeMachineMessage.TimeMachine.niceToString(), ctx => timeMachineRoute(ctx.lite), {
            isVisible: getTypeInfo(entityType) && getTypeInfo(entityType).operations && Finder.isFindable(OperationLogEntity, false),
            icon: "clock-rotate-left",
            iconColor: "blue",
            color: "success",
          })
        ]));


    SearchControlOptions.showSystemTimeButton = sc => AppContext.isPermissionAuthorized(TimeMachinePermission.ShowTimeMachine);

    options.routes.push({ path: "/timeMachine/:type/:id", element: <ImportComponent onImport={() => import("./TimeMachinePage")} /> });

    Finder.entityFormatRules.push({
      name: "ViewHistory",
      isApplicable: (sc) => sc != null && sc.props.findOptions.systemTime != null && Finder.isSystemVersioned(sc.props.queryDescription.columns["Entity"].type),
      formatter: new Finder.EntityFormatter(({ row, columns, searchControl: sc }) => {

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
              {created && <FontAwesomeIcon aria-hidden={true} icon="plus" color={TimeMachineColors.created} />}
              {deleted && <FontAwesomeIcon aria-hidden={true} icon="minus" color={TimeMachineColors.removed} className={created ? "ms-1" : undefined} />}
              {!created && !deleted && fop.systemTime.mode == "Between" && <FontAwesomeIcon aria-hidden={true} icon="equals" color={TimeMachineColors.noChange} />}
            </span>;
          }
        }

        if (sc?.state.resultFindOptions?.groupResults) {
          return (
            <LinkButton
              className="sf-line-button sf-view"
              onClick={e => { sc!.openRowGroup(row, e); }}
              title={JavascriptMessage.ShowGroup.niceToString()}
              style={{ whiteSpace: "nowrap", opacity: deleted ? .5 : undefined }} >
              <FontAwesomeIcon aria-hidden={true} icon="layer-group" />
              {icon}
            </LinkButton>
          );
        }

        if (!row.entity || !Navigator.isViewable(row.entity.EntityType, { isSearch: "main" }))
          return icon;

        return (
          <TimeMachineLink lite={row.entity} inSearch="main" style={{ whiteSpace: "nowrap", opacity: deleted ? .5 : undefined }} >
            {EntityControlMessage.View.niceToString()}
            {icon}
          </TimeMachineLink >
        );

      })
    });

    Finder.formatRules.push({
      name: "Lite_TM",
      isApplicable: (qt, sc) => qt.filterType == "Lite" && sc != null && sc.props.findOptions.systemTime != null && Finder.isSystemVersioned(qt.type),
      formatter: qt => new Finder.CellFormatter((cell: Lite<Entity>, ctx) => !cell ? undefined : <TimeMachineLink lite={cell} />, true)
    });

    Finder.formatRules.push({
      name: "LiteNoFill_TM",
      isApplicable: (qt, sc) => {
        return qt.filterType == "Lite" && sc != null && sc.props.findOptions.systemTime != null && Finder.isSystemVersioned(qt.type) &&
          tryGetTypeInfos(qt.type)?.every(ti => ti && Navigator.getSettings(ti)?.avoidFillSearchColumnWidth);
      },
      formatter: qt => new Finder.CellFormatter((cell: Lite<Entity> | undefined, ctx) => !cell ? undefined : <TimeMachineLink lite={cell} />, false)
    });
  }



  export interface EntityDump {
    entity: Entity;
    dump: string;
  }



  export namespace API {


    export function getEntityDump(lite: Lite<Entity>, asOf: string,): Promise<EntityDump> {
      return ajaxGet({ url: `/api/retrieveVersion/${lite.EntityType}/${lite.id}?` + QueryString.stringify({ asOf }) });
    }
  }

  export function timeMachineRoute(lite: Lite<Entity>): string {
    return "/timeMachine/" + lite.EntityType + "/" + lite.id;
  }
}

export interface TimeMachineLinkProps extends React.HTMLAttributes<HTMLAnchorElement> {
  lite: Lite<Entity>;
  inSearch?: "main" | "related";
}

export function TimeMachineLink(p: TimeMachineLinkProps): React.JSX.Element {

  function handleClick(event: React.MouseEvent<any>) {
    const lite = p.lite;

    event.preventDefault();

    window.open(AppContext.toAbsoluteUrl(TimeMachineClient.timeMachineRoute(lite)));
  }
  const { lite, inSearch, children, ...htmlAtts } = p;

  if (!Navigator.isViewable(lite.EntityType, { isSearch: p.inSearch }))
    return <span data-entity={liteKey(lite)}>{p.children ?? getToString(lite)}</span>;


  return (
    <Link
      to={TimeMachineClient.timeMachineRoute(lite)}
      title={getToString(lite)}
      onClick={handleClick}
      data-entity={liteKey(lite)}
      {...(htmlAtts as React.HTMLAttributes<HTMLAnchorElement>)}>
      {children ?? getToString(lite)}
    </Link>
  );
}
