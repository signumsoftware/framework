import * as React from 'react'
import * as moment from 'moment'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
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
            <button className="btn btn-light float-right flip" onClick={this.handleClick}>
                <FontAwesomeIcon icon="random" color="green" /> {WorkflowActivityMessage.CaseFlow.niceToString()}
            </button>
        );
    }

}
