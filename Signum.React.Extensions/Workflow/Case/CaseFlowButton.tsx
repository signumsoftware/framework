
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

interface CaseFlowButtonProps {
    caseActivity: CaseActivityEntity;
}

export default class CaseFlowButton extends React.Component <CaseFlowButtonProps>{

    handleClick = (e: React.MouseEvent<HTMLButtonElement>) => {
        e.preventDefault();
        var ca = this.props.caseActivity;
        Navigator.navigate(ca.case, { extraComponentProps: { caseActivity: ca } }).done();
    }

    render() {
        return (
            <button className="btn btn-light pull-right flip" onClick={this.handleClick}>
                <i className="fa fa-random" style={{ color: "green" }} /> {WorkflowActivityMessage.CaseFlow.niceToString()}
            </button>
        );
    }

}
