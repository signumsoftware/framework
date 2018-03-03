
import * as React from 'react'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import { TypeContext, StyleOptions, EntityFrame } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { TypeInfo, getTypeInfo, parseId, GraphExplorer, PropertyRoute, ReadonlyBinding, } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import * as Operations from '../../../../Framework/Signum.React/Scripts/Operations'
import { EntityPack, Entity, Lite, JavascriptMessage, entityInfo, getToString } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { renderWidgets, renderEmbeddedWidgets, WidgetContext } from '../../../../Framework/Signum.React/Scripts/Frames/Widgets'
import ValidationErrors from '../../../../Framework/Signum.React/Scripts/Frames/ValidationErrors'
import ButtonBar from '../../../../Framework/Signum.React/Scripts/Frames/ButtonBar'
import { CaseActivityEntity, WorkflowEntity, ICaseMainEntity, CaseActivityOperation, CaseActivityQuery, WorkflowMainEntityStrategy } from '../Signum.Entities.Workflow'
import * as WorkflowClient from '../WorkflowClient'

import { UncontrolledDropdown, DropdownItem, DropdownToggle, DropdownMenu, LinkContainer } from '../../../../Framework/Signum.React/Scripts/Components'

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

    render() {

        const inboxUrl = WorkflowClient.getDefaultInboxUrl();

        return (
            <UncontrolledDropdown className="sf-workflow" id="workflowDropdown" nav inNavbar>
                <DropdownToggle nav caret>
                    {WorkflowEntity.nicePluralName()}
                </DropdownToggle>
                <DropdownMenu style={{ minWidth: "200px" }}>
                    <LinkContainer exact to={inboxUrl}><DropdownItem>{CaseActivityQuery.Inbox.niceName()}</DropdownItem></LinkContainer>
                    {this.state.starts.length > 0 && <DropdownItem divider />}
                    {this.state.starts.length > 0 && <DropdownItem disabled>{JavascriptMessage.create.niceToString()}</DropdownItem>}
                    {this.getStarts().map((val, i) =>
                        <LinkContainer key={i} to={`~/workflow/new/${val.workflow.id}/${val.mainEntityStrategy}`}>
                            <DropdownItem>{val.workflow.toStr}{val.mainEntityStrategy == "SelectByUser" ? `(${WorkflowMainEntityStrategy.niceToString(val.mainEntityStrategy)})` : ""}</DropdownItem>
                        </LinkContainer>
                    )}
                </DropdownMenu>
            </UncontrolledDropdown>
        );
    }

    getStarts() {
        return this.state.starts.flatMap((w, i) => {
            if (w.mainEntityStrategy != "Both")
                return [({ workflow: w, mainEntityStrategy: w.mainEntityStrategy! })]
            else
                return [({ workflow: w, mainEntityStrategy: ("CreateNew" as WorkflowMainEntityStrategy) })]
                    .concat([({ workflow: w, mainEntityStrategy: ("SelectByUser" as WorkflowMainEntityStrategy) })]);
        })
    }
}

