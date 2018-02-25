
import * as React from 'react'
import * as moment from 'moment'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import { openModal, IModalProps } from '../../../../Framework/Signum.React/Scripts/Modals'
import { TypeContext, StyleOptions, EntityFrame } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { TypeInfo, getTypeInfo, parseId, GraphExplorer, PropertyRoute, ReadonlyBinding, } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { ValueLine } from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Operations from '../../../../Framework/Signum.React/Scripts/Operations'
import { EntityPack, Entity, Lite, JavascriptMessage, NormalWindowMessage, entityInfo, getToString, is } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import ValidationErrors from '../../../../Framework/Signum.React/Scripts/Frames/ValidationErrors'
import ButtonBar from '../../../../Framework/Signum.React/Scripts/Frames/ButtonBar'
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

        var a = this.props.pack.entity;

        if (a.doneDate != null) {
            return (
                <div className="workflow-buttons">
                    {CaseActivityMessage.DoneBy0On1.niceToString().formatHtml(
                        <strong>{a.doneBy && a.doneBy.toStr}</strong>,
                        a.doneDate && <strong>{moment(a.doneDate).format("L LT")} ({moment(a.doneDate).fromNow()})</strong>)
                    }
                </div>
            );
        }

        const ctx = new TypeContext(undefined, undefined, PropertyRoute.root(CaseActivityEntity), new ReadonlyBinding(a, "act"));
        return (
            <div>
                <div className="workflow-buttons">
                    <ButtonBar frame={this.props.frame} pack={this.props.pack} />
                    <ValueLine ctx={ctx.subCtx(a => a.note)} formGroupStyle="None" placeholderLabels={true} />
                </div>
                {a.workflowActivity.userHelp &&
                    <UserHelpComponent activity={a.workflowActivity} />}
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
