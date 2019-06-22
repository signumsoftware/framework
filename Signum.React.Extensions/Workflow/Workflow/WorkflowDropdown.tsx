
import * as React from 'react'
import { getTypeInfo } from '@framework/Reflection'
import { JavascriptMessage } from '@framework/Signum.Entities'
import { WorkflowEntity, CaseActivityQuery, WorkflowMainEntityStrategy } from '../Signum.Entities.Workflow'
import * as WorkflowClient from '../WorkflowClient'
import { UncontrolledDropdown, DropdownItem, DropdownToggle, DropdownMenu, LinkContainer } from '@framework/Components'
import { useAPI } from '@framework/Hooks';

export default function WorkflowDropdown(props: {}) {
  var starts = useAPI(undefined, [], signal => WorkflowClient.API.starts());

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
    <UncontrolledDropdown className="sf-workflow" id="workflowDropdown" nav inNavbar>
      <DropdownToggle nav caret>
        {WorkflowEntity.nicePluralName()}
      </DropdownToggle>
      <DropdownMenu style={{ minWidth: "200px" }}>
        <LinkContainer exact to={Options.getInboxUrl()}><DropdownItem>{CaseActivityQuery.Inbox.niceName()}</DropdownItem></LinkContainer>
        {starts.length > 0 && <DropdownItem divider />}
        {starts.length > 0 && <DropdownItem disabled>{JavascriptMessage.create.niceToString()}</DropdownItem>}
        {starts.length > 0 && getStarts(starts).flatMap((kvp, i) => [
          (kvp.elements.length > 1 && <DropdownItem key={i} disabled>{kvp.elements[0].typeInfo.niceName}</DropdownItem>),
          ...kvp.elements.map((val, j) =>
            <LinkContainer key={i + "-" + j} to={`~/workflow/new/${val.workflow.id}/${val.mainEntityStrategy}`}>
              <DropdownItem>{val.workflow.toStr}{val.mainEntityStrategy == "CreateNew" ? "" : `(${WorkflowMainEntityStrategy.niceToString(val.mainEntityStrategy)})`}</DropdownItem>
            </LinkContainer>)
        ])}
      </DropdownMenu>
    </UncontrolledDropdown>
  );

}

export namespace Options {
  export function getInboxUrl(): string {
    return WorkflowClient.getDefaultInboxUrl();
  }
}

