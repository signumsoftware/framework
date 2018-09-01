
import * as React from 'react'
import { Dic } from '@framework/Globals'
import { TypeContext, StyleOptions, EntityFrame } from '@framework/TypeContext'
import { TypeInfo, getTypeInfo, parseId, GraphExplorer, PropertyRoute, ReadonlyBinding } from '@framework/Reflection'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import * as Operations from '@framework/Operations'
import { EntityPack, Entity, Lite, JavascriptMessage, entityInfo, getToString } from '@framework/Signum.Entities'
import { renderWidgets, renderEmbeddedWidgets, WidgetContext } from '@framework/Frames/Widgets'
import ValidationErrors from '@framework/Frames/ValidationErrors'
import ButtonBar from '@framework/Frames/ButtonBar'
import { CaseActivityEntity, WorkflowEntity, ICaseMainEntity, CaseActivityOperation, CaseActivityQuery, WorkflowMainEntityStrategy } from '../Signum.Entities.Workflow'
import * as WorkflowClient from '../WorkflowClient'

import { UncontrolledDropdown, DropdownItem, DropdownToggle, DropdownMenu, LinkContainer } from '@framework/Components'

export default class WorkflowDropdown extends React.Component<{}, { starts: Array<WorkflowEntity> }>
{
    constructor(props: any) {
        super(props);
        this.state = { starts: [] };
    } 

    componentWillMount() {
        WorkflowClient.API.starts()
            .then(starts => this.setState({ starts }))
            .done();
    }

    static getInboxUrl(): string {
        return WorkflowClient.getDefaultInboxUrl();
    }

    render() {

        return (
            <UncontrolledDropdown className="sf-workflow" id="workflowDropdown" nav inNavbar>
                <DropdownToggle nav caret>
                    {WorkflowEntity.nicePluralName()}
                </DropdownToggle>
                <DropdownMenu style={{ minWidth: "200px" }}>
                    <LinkContainer exact to={WorkflowDropdown.getInboxUrl()}><DropdownItem>{CaseActivityQuery.Inbox.niceName()}</DropdownItem></LinkContainer>
                    {this.state.starts.length > 0 && <DropdownItem divider />}
                    {this.state.starts.length > 0 && <DropdownItem disabled>{JavascriptMessage.create.niceToString()}</DropdownItem>}
                    {this.state.starts.length > 0 && this.getStarts().flatMap((kvp, i) => [
                        (kvp.elements.length > 1 && <DropdownItem key={i} disabled>{kvp.elements[0].typeInfo.niceName}</DropdownItem>),
                        ...kvp.elements.map((val, j) =>
                            <LinkContainer key={i + "-" + j} to={`~/workflow/new/${val.workflow.id}/${val.mainEntityStrategy}`}>
                                <DropdownItem>{val.workflow.toStr}{val.mainEntityStrategy == "SelectByUser" ? `(${WorkflowMainEntityStrategy.niceToString(val.mainEntityStrategy)})` : ""}</DropdownItem>
                            </LinkContainer>)
                    ])}
                </DropdownMenu>
            </UncontrolledDropdown>
        );
            }
        
    getStarts() {
        return this.state.starts.flatMap(w => {
            const typeInfo = getTypeInfo(w.mainEntityType!.cleanName);

            if (w.mainEntityStrategy != "Both")
                return [({ workflow: w, typeInfo, mainEntityStrategy: w.mainEntityStrategy! })]
            else
                return [({ workflow: w, typeInfo, mainEntityStrategy: ("CreateNew" as WorkflowMainEntityStrategy) })]
                    .concat([({ workflow: w, typeInfo, mainEntityStrategy: ("SelectByUser" as WorkflowMainEntityStrategy) })]);
        }).filter(kvp => !!kvp.typeInfo)
            .groupBy(kvp => kvp.typeInfo.name);
    }
}

