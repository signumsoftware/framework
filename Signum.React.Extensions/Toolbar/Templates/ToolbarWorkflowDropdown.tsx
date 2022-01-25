import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import * as AppContext from '@framework/AppContext'
import { useAPI } from '@framework/Hooks'
import * as Navigator from '@framework/Navigator'
import { getTypeInfo } from '@framework/Reflection'
import * as React from 'react'
import { Nav } from 'react-bootstrap'
import { CaseActivityQuery, WorkflowEntity, WorkflowMainEntityStrategy } from '../../Workflow/Signum.Entities.Workflow'
import * as WorkflowClient from '../../Workflow/WorkflowClient'

export default function WorkflowDropdown(props: { sidebarExpanded: boolean | undefined, onRefresh?: () => void | undefined, onClose?: () => void, fullScreenExpanded?: boolean }) {

  if (!Navigator.isViewable(WorkflowEntity))
    return null;

  return <WorkflowDropdownImp sidebarExpanded={props.sidebarExpanded} onRefresh={props.onRefresh} onClose={props.onClose} fullScreenExpanded={props.fullScreenExpanded} />;
}

function WorkflowDropdownImp(p: { sidebarExpanded: boolean | undefined, onRefresh?: () => void | undefined, onClose?: () => void, fullScreenExpanded?: boolean}) {
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
      {starts.length == 1 && <Nav.Item>
        <Nav.Link style={{ paddingLeft: p.sidebarExpanded === true ? "25px" : "13px" }} key={"LS-00"}
          onClick={(e: React.MouseEvent<any>) => { AppContext.pushOrOpenInTab(Options.getInboxUrl()!, e); if (p.onRefresh) p.onRefresh(); if (p.fullScreenExpanded === true) if (p.onClose) p.onClose(); }}
          onAuxClick={(e: React.MouseEvent<any>) => { AppContext.pushOrOpenInTab(Options.getInboxUrl()!, e); if (p.onRefresh) p.onRefresh(); if (p.fullScreenExpanded === true) if (p.onClose) p.onClose();}}
          active={location.href.contains("/find/Inbox")}
          title={CaseActivityQuery.Inbox.niceName()}>

          <FontAwesomeIcon icon={"bell"} />
          <span style={{ marginLeft: "16px", verticalAlign: "middle", display: "inline-block", width: "calc(100% - 50px)", overflow: "hidden", whiteSpace: "nowrap", textOverflow: "ellipsis" }}>{CaseActivityQuery.Inbox.niceName()}</span>
          {!p.sidebarExpanded && <div className={"nav-item-float"}>{CaseActivityQuery.Inbox.niceName()}</div>}
        </Nav.Link>
      </Nav.Item>}

      {starts.length > 1 &&
        <>
          <div className="nav-item">
            <div
              className={"nav-link"}
              onClick={() => setShow(!show)}
              style={{ paddingLeft: p.sidebarExpanded === true ? "25px" : "13px", cursor: "pointer" }}>
              {!show ? <FontAwesomeIcon icon={"caret-down"} /> : <FontAwesomeIcon icon={"caret-up"} />}
              <span style={{ marginLeft: "16px", verticalAlign: "middle" }}>{WorkflowEntity.nicePluralName()}</span>
              {!p.sidebarExpanded && <div className={"nav-item-float"}>{WorkflowEntity.nicePluralName()}</div>}
            </div>
          </div>
          <div style={{ display: show ? "block" : "none" }}>
            <Nav.Item key={"LS-01"}>
              <Nav.Link style={{ paddingLeft: p.sidebarExpanded === true ? "40px" : "10px" }}
              onClick={(e: React.MouseEvent<any>) => { AppContext.pushOrOpenInTab(Options.getInboxUrl()!, e); if (p.onRefresh) p.onRefresh(); if (p.fullScreenExpanded === true) if (p.onClose) p.onClose();}}
              onAuxClick={(e: React.MouseEvent<any>) => { AppContext.pushOrOpenInTab(Options.getInboxUrl()!, e); if (p.onRefresh) p.onRefresh(); if (p.fullScreenExpanded === true) if (p.onClose) p.onClose(); }}
                active={location.href.contains("/find/Inbox")}
                title={CaseActivityQuery.Inbox.niceName()}
                key={"LS-01"}>

                <FontAwesomeIcon icon={"bell"} />
                <span style={{ marginLeft: "16px", verticalAlign: "middle", display: "inline-block", width: "calc(100% - 50px)", overflow: "hidden", whiteSpace: "nowrap", textOverflow: "ellipsis" }}>{CaseActivityQuery.Inbox.niceName()}</span>
                {!p.sidebarExpanded && <div className={"nav-item-float"}>{CaseActivityQuery.Inbox.niceName()}</div>}
              </Nav.Link>
            </Nav.Item>

            {starts.length > 1 && getStarts(starts).flatMap((kvp, i) => [
              (kvp.elements.length > 1 &&
                <Nav.Item key={i + "K-" + i}>
                  <Nav.Link style={{ paddingLeft: p.sidebarExpanded === true ? "40px" : "10px" }} disabled
                    title={kvp.elements[0].typeInfo.niceName}
                    key={i + "K-" + i}>

                    <FontAwesomeIcon icon={"angle-double-right"} />
                    <span style={{ marginLeft: "16px", verticalAlign: "middle", display: "inline-block", width: "calc(100% - 50px)", overflow: "hidden", whiteSpace: "nowrap", textOverflow: "ellipsis" }}>{kvp.elements[0].typeInfo.niceName}</span>
                    {!p.sidebarExpanded && <div className={"nav-item-float"}>{"Inicio"}</div>}
                  </Nav.Link>
                </Nav.Item>),
              ...kvp.elements.map((val, j) =>
                <Nav.Item key={i + "-" + j}>
                  <Nav.Link style={{ paddingLeft: p.sidebarExpanded === true ? "40px" : "10px" }}
                    onClick={(e: React.MouseEvent<any>) => { AppContext.pushOrOpenInTab(`~/workflow/new/${val.workflow.id}/${val.mainEntityStrategy}`, e); if (p.onRefresh) p.onRefresh(); if (p.fullScreenExpanded === true) if (p.onClose) p.onClose();}}
                    onAuxClick={(e: React.MouseEvent<any>) => { AppContext.pushOrOpenInTab(`~/workflow/new/${val.workflow.id}/${val.mainEntityStrategy}`, e); if (p.onRefresh) p.onRefresh(); if (p.fullScreenExpanded === true) if (p.onClose) p.onClose();}}
                    active={false}
                    key={i + "-" + j}
                    title={val.workflow.toStr + val.mainEntityStrategy == "CreateNew" ? "" : `(${WorkflowMainEntityStrategy.niceToString(val.mainEntityStrategy)})`}>

                    <FontAwesomeIcon icon={"tasks"} />
                    <span style={{ marginLeft: "16px", verticalAlign: "middle", display: "inline-block", width: "calc(100% - 50px)", overflow: "hidden", whiteSpace: "nowrap", textOverflow: "ellipsis" }}>{val.workflow.toStr}{val.mainEntityStrategy == "CreateNew" ? "" : `(${WorkflowMainEntityStrategy.niceToString(val.mainEntityStrategy)})`}</span>
                    {!p.sidebarExpanded && <div className={"nav-item-float"}>{val.workflow.toStr}{val.mainEntityStrategy == "CreateNew" ? "" : `(${WorkflowMainEntityStrategy.niceToString(val.mainEntityStrategy)})`}</div>}
                  </Nav.Link>
                </Nav.Item>)
            ])}
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
