import * as React from 'react'
import * as moment from 'moment'
import { Link } from "react-router"
import { Button } from "react-bootstrap"
import { Binding, LambdaMemberType } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { Dic, classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { newMListElement, Lite, liteKey, Entity, is } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { CaseActivityMessage, CaseNotificationEntity, CaseNotificationOperation, CaseActivityEntity, WorkflowActivityEntity } from '../Signum.Entities.Workflow'
import { TypeContext, ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater, EnumCheckboxList, FormGroup, FormGroupStyle, FormGroupSize, ValueLineType } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl, ValueSearchControl, FilterOperation, OrderType, PaginationMode, ISimpleFilterBuilder, FilterOption, FindOptionsParsed } from '../../../../Framework/Signum.React/Scripts/Search'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Operations from '../../../../Framework/Signum.React/Scripts/Operations'
import ValueLineModal from '../../../../Framework/Signum.React/Scripts/ValueLineModal'
import * as WorkflowClient from '../WorkflowClient'


export interface ActivityWithRemarks {
    activity: Lite<WorkflowActivityEntity>;
    notification: Lite<CaseNotificationEntity>;
    remarks: string | undefined;
}

export interface ActivityWithRemarksProps extends React.Props<ActivityWithRemarks> {
    data: ActivityWithRemarks;
}

export default class ActivityWithRemarksComponent extends React.Component<ActivityWithRemarksProps, { remarks: string | undefined | null }>{

    constructor(props: ActivityWithRemarksProps) {
        super(props);
        this.state = { remarks: this.props.data.remarks };
    }
    componentWillReceiveProps(newProps: ActivityWithRemarksProps) {
        if (!is(this.props.data.notification, newProps.data.notification))
            this.setState({ remarks: newProps.data.remarks });
    }

    render() {
        return (
            <span>
                {this.props.data.activity.toStr}
                &nbsp;
                <a href="" onClick={this.handleClick} className={classes(
                        "case-remarks",
                        !this.state.remarks && "case-remarks-pencil")}>
                    <span className={classes(
                        this.state.remarks ? "glyphicon glyphicon-comment" : "glyphicon glyphicon-pencil")} />
                </a>
            </span>
        );
    }

    handleClick = (e: React.MouseEvent<any>) => {
        e.preventDefault();

        ValueLineModal.show({
            type: { name: "string" },
            valueLineType: ValueLineType.TextArea,
            title: CaseNotificationEntity.nicePropertyName(a => a.remarks),
            message: CaseActivityMessage.PersonalRemarksForThisNotification.niceToString(),
            labelText: undefined,
            initialValue: this.state.remarks,
            initiallyFocused: true
        }).then(remarks => {

            if (remarks === undefined)
                return;

            Operations.API.executeLite(this.props.data.notification, CaseNotificationOperation.SetRemarks, remarks).then(n => {
                this.setState({ remarks: n.entity.remarks });
            }).done();

        }).done();
    }
}
