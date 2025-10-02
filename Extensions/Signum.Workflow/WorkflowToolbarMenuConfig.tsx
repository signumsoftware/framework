import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import * as AppContext from '@framework/AppContext'
import { Location } from 'react-router'
import { useAPI } from '@framework/Hooks'
import { Navigator } from '@framework/Navigator'
import { getTypeInfo } from '@framework/Reflection'
import { Entity, getToString, is, Lite } from '@framework/Signum.Entities'
import * as React from 'react'
import { ToolbarNavItem } from '../Signum.Toolbar/Renderers/ToolbarRenderer'
import { ToolbarClient, ToolbarResponse } from '../Signum.Toolbar/ToolbarClient'
import { IconColor, ToolbarConfig, ToolbarContext } from '../Signum.Toolbar/ToolbarConfig'
import { CaseActivityQuery, WorkflowEntity, WorkflowMainEntityStrategy, WorkflowPermission } from './Signum.Workflow'
import { WorkflowClient } from './WorkflowClient'
import { PermissionSymbol } from '@framework/Signum.Basics'
import { IconProp } from '@fortawesome/fontawesome-svg-core';

export default class WorkflowToolbarMenuConfig extends ToolbarConfig<PermissionSymbol> {

  constructor() {
    var type = PermissionSymbol;
    super(type);
  }

  getDefaultIcon(): IconProp {
    return "shuffle";
  }

  isApplicableTo(element: ToolbarResponse<PermissionSymbol>): boolean {
    return is(element.content, WorkflowPermission.WorkflowToolbarMenu);
  }

  getMenuItem(res: ToolbarResponse<PermissionSymbol>, key: number | string, ctx: ToolbarContext): React.JSX.Element {
    return <WorkflowDropdownImp key={key} />;
  }

  isCompatibleWithUrlPrio(res: ToolbarResponse<PermissionSymbol>, location: Location, query: any): { prio: number, inferredEntity?: Lite<Entity> } | null {
    return null;
  }

  navigateTo(): Promise<string> {
    return Promise.resolve("");
  }
}


function WorkflowDropdownImp() {
  var [show, setShow] = React.useState(false);

  var starts = useAPI(signal => WorkflowClient.API.starts(), []);

  function getStarts(starts: WorkflowEntity[]) {
    return starts.flatMap(w => {
      const typeInfo = getTypeInfo(w.mainEntityType!.cleanName);

      return w.mainEntityStrategies.flatMap(ws => [({ workflow: w, typeInfo, mainEntityStrategy: ws.element! })]);
    }).filter(kvp => !!kvp.typeInfo)
      .groupBy(kvp => kvp.typeInfo.name);
  }

  if (!starts)
    return null;

  return (
    <div>
      {starts.length == 0 &&
        <ToolbarNavItem title={CaseActivityQuery.Inbox.niceName()}
          active={location.href.contains("/find/Inbox")}
          onClick={(e: React.MouseEvent<any>) => { AppContext.pushOrOpenInTab(Options.getInboxUrl()!, e); }}
          icon={ToolbarConfig.coloredIcon("inbox", "steelblue")} />
      }

      {starts.length > 0 &&
        <>
          <ToolbarNavItem
            title={WorkflowEntity.nicePluralName()}
            onClick={() => setShow(!show)}
            icon={
              <div style={{ display: 'inline-block', position: 'relative' }}>
                <div className="nav-arrow-icon" style={{ position: 'absolute' }}><FontAwesomeIcon icon={show ? "caret-down" : "caret-right"} className="icon" /></div>
                <div className="nav-icon-with-arrow">
                  {ToolbarConfig.coloredIcon("random", "mediumvioletred")}
                </div>
              </div>
            } />

          <div style={{ display: show ? "block" : "none" }}>

            <ToolbarNavItem title={CaseActivityQuery.Inbox.niceName()}
              active={location.href.contains("/find/Inbox")}
              onClick={(e: React.MouseEvent<any>) => { AppContext.pushOrOpenInTab(Options.getInboxUrl()!, e); }}
              icon={ToolbarConfig.coloredIcon("inbox", "steelblue")} />

            {getStarts(starts).flatMap((kvp, i) => kvp.elements.map((val, j) =>
              <ToolbarNavItem key={i + "-" + j} title={getToString(val.workflow) + (val.mainEntityStrategy == "CreateNew" ? "" : ` (${WorkflowMainEntityStrategy.niceToString(val.mainEntityStrategy)})`)}
                onClick={(e: React.MouseEvent<any>) => { AppContext.pushOrOpenInTab(`/workflow/new/${val.workflow.id}/${val.mainEntityStrategy}`, e); }}
                active={false}
                icon={ToolbarConfig.coloredIcon("square-plus", "seagreen")}
              />)
            )}
          </div>
        </>
      }
    </div>
  );
}

export namespace Options {
  export function getInboxUrl(): string {
    return WorkflowClient.getDefaultInboxUrl();
  }
}
