
import * as React from 'react'
import { getTypeInfo } from '@framework/Reflection'
import { getToString, JavascriptMessage } from '@framework/Signum.Entities'
import { WorkflowEntity, CaseActivityQuery, WorkflowMainEntityStrategy } from '../Signum.Workflow'
import { WorkflowClient } from '../WorkflowClient'
import { NavDropdown, Dropdown } from 'react-bootstrap'
import { useAPI } from '@framework/Hooks';
import { LinkContainer } from '@framework/Components'
import { Navigator } from '@framework/Navigator'

export default function WorkflowDropdown(props: {}): React.JSX.Element | null {
  
  if (!Navigator.isViewable(WorkflowEntity))
    return null;

  return <WorkflowDropdownImp />;
}

function WorkflowDropdownImp(props: {}) {
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
    <NavDropdown className="sf-workflow" id="workflowDropdown" title={WorkflowEntity.nicePluralName()}>
      <LinkContainer exact to={Options.getInboxUrl()}>
        <Dropdown.Item>{CaseActivityQuery.Inbox.niceName()}</Dropdown.Item>
      </LinkContainer>
      {starts.length > 0 && <Dropdown.Divider />}
      {starts.length > 0 && <Dropdown.Item disabled>{JavascriptMessage.create.niceToString()}</Dropdown.Item>}
      {starts.length > 0 && getStarts(starts).flatMap((kvp, i) => [
        (kvp.elements.length > 1 && <Dropdown.Item key={i} disabled>{kvp.elements[0].typeInfo.niceName}</Dropdown.Item>),
        ...kvp.elements.map((val, j) =>
          <LinkContainer key={i + "-" + j} to={`/workflow/new/${val.workflow.id}/${val.mainEntityStrategy}`}>
            <Dropdown.Item>{getToString(val.workflow)}{val.mainEntityStrategy == "CreateNew" ? "" : `(${WorkflowMainEntityStrategy.niceToString(val.mainEntityStrategy)})`}</Dropdown.Item>
          </LinkContainer>)
      ])}
    </NavDropdown>
  );

}

export namespace Options {
  export function getInboxUrl(): string {
    return WorkflowClient.getDefaultInboxUrl();
  }
}

