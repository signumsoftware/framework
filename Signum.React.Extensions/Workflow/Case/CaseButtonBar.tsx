
import * as React from 'react'
import * as moment from 'moment'
import { Dic } from '@framework/Globals'
import { openModal, IModalProps } from '@framework/Modals'
import { TypeContext, StyleOptions, EntityFrame } from '@framework/TypeContext'
import { TypeInfo, getTypeInfo, parseId, GraphExplorer, PropertyRoute, ReadonlyBinding, } from '@framework/Reflection'
import { ValueLine } from '@framework/Lines'
import * as Navigator from '@framework/Navigator'
import * as Operations from '@framework/Operations'
import { EntityPack, Entity, Lite, JavascriptMessage, NormalWindowMessage, entityInfo, getToString, is } from '@framework/Signum.Entities'
import ValidationErrors from '@framework/Frames/ValidationErrors'
import ButtonBar from '@framework/Frames/ButtonBar'
import { CaseActivityEntity, WorkflowEntity, ICaseMainEntity, CaseActivityOperation, CaseActivityMessage, WorkflowActivityEntity, WorkflowActivityMessage } from '../Signum.Entities.Workflow'
import * as WorkflowClient from '../WorkflowClient'
import { DynamicViewMessage } from '../../Dynamic/Signum.Entities.Dynamic'
import HtmlEditor from '../../HtmlEditor/HtmlEditor'

interface CaseButtonBarProps {
    frame: EntityFrame;
    pack: EntityPack<CaseActivityEntity>;
}

export default class CaseButtonBar extends React.Component<CaseButtonBarProps>{


    render() {

        var ca = this.props.pack.entity;

        if (ca.doneDate != null) {
            return (
                <div className="workflow-buttons">
                    {CaseActivityMessage.DoneBy0On1.niceToString().formatHtml(
                        <strong>{ca.doneBy && ca.doneBy.toStr}</strong>,
                        ca.doneDate && <strong>{moment(ca.doneDate).format("L LT")} ({moment(ca.doneDate).fromNow()})</strong>)
                    }
                </div>
            );
        }

        const ctx = new TypeContext(undefined, undefined, PropertyRoute.root(CaseActivityEntity), new ReadonlyBinding(ca, "act"));
        return (
            <div>
                <div className="workflow-buttons">
                    <ButtonBar frame={this.props.frame} pack={this.props.pack} />
                    <ValueLine ctx={ctx.subCtx(a => a.note)} formGroupStyle="None" placeholderLabels={true} />
                </div>
                {(ca.workflowActivity as WorkflowActivityEntity).userHelp &&
                    <UserHelpComponent activity={ca.workflowActivity as WorkflowActivityEntity} />}
            </div>
        );
    }
}

interface UserHelpProps {
    activity: WorkflowActivityEntity;
}

export class UserHelpComponent extends React.Component<UserHelpProps, { open: boolean }> {

    constructor(props: UserHelpProps) {
        super(props);
        this.state = { open: false };
    }

    render() {
        return (
            <div style={{ marginTop: "10px" }}>
                <a href="#" onClick={this.handleHelpClick} className="case-help-button">
                    {this.state.open ?
                        DynamicViewMessage.HideHelp.niceToString() :
                        DynamicViewMessage.ShowHelp.niceToString()}
                </a>
                {this.state.open &&
                    <div dangerouslySetInnerHTML={{ __html: this.props.activity.userHelp! }} />}
            </div>
        );
    }

    handleHelpClick = (e: React.MouseEvent<any>) => {
        e.preventDefault();
        this.setState({ open: !this.state.open });
    }


}
