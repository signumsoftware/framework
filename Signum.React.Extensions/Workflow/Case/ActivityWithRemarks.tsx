import * as React from 'react'
import * as moment from 'moment'
import { Binding, LambdaMemberType } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { Dic, classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { newMListElement, Lite, liteKey, Entity, is } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import {
    CaseActivityMessage, CaseNotificationEntity, CaseNotificationOperation, CaseActivityEntity, WorkflowActivityEntity, CaseTagTypeEntity,
    CaseTagsModel, CaseOperation, CaseEntity
} from '../Signum.Entities.Workflow'
import { TypeContext, ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater, EnumCheckboxList, FormGroup, FormGroupStyle, ValueLineType } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl, ValueSearchControl, FilterOperation, OrderType, PaginationMode, ISimpleFilterBuilder, FilterOption, FindOptions  } from '../../../../Framework/Signum.React/Scripts/Search'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Operations from '../../../../Framework/Signum.React/Scripts/Operations'
import ValueLineModal from '../../../../Framework/Signum.React/Scripts/ValueLineModal'
import { AlertEntity, AlertState } from '../../Alerts/Signum.Entities.Alerts'
import * as WorkflowClient from '../WorkflowClient'
import { Color } from '../../Basics/Color'
import InlineCaseTags from './InlineCaseTags'


export interface ActivityWithRemarks {
    workflowActivity: Lite<WorkflowActivityEntity>;
    case: Lite<CaseEntity>;
    caseActivity: Lite<CaseActivityEntity>;
    notification: Lite<CaseNotificationEntity>;
    remarks: string | undefined;
    alerts: number;
    tags: Array<CaseTagTypeEntity>;
}

export interface ActivityWithRemarksProps extends React.Props<ActivityWithRemarks> {
    data: ActivityWithRemarks;
}

export interface ActivityWithRemarksState {
    remarks: string | undefined | null;
    alerts: number;
    tags: Array<CaseTagTypeEntity>;
}

export default class ActivityWithRemarksComponent extends React.Component<ActivityWithRemarksProps, ActivityWithRemarksState>{

    constructor(props: ActivityWithRemarksProps) {
        super(props);
        this.state = {
            remarks: this.props.data.remarks,
            alerts: this.props.data.alerts,
            tags: this.props.data.tags,
        };
    }
    componentWillReceiveProps(newProps: ActivityWithRemarksProps) {
        if ((this.props.data.remarks != newProps.data.remarks) ||
            (this.props.data.alerts != newProps.data.alerts) ||
            (this.props.data.tags.map(a => a.id).join(",") != newProps.data.tags.map(a => a.id).join(","))) {
            this.setState({
                remarks: newProps.data.remarks,
                alerts: newProps.data.alerts,
                tags: newProps.data.tags,
            });
        }
    }

    render() {
        return (
            <span>
                {this.props.data.workflowActivity.toStr}
                &nbsp;
                <a href="#" onClick={this.handleRemarksClick} className={classes(
                        "case-icon",
                        !this.state.remarks && "case-icon-ghost")}>
                    <span className={classes(
                        this.state.remarks ? "fa fa-comment" : "fa fa-comment-o")} />
                </a>
                {this.state.alerts > 0 && " "}
                {this.state.alerts > 0 && <a href="#" onClick={this.handleAlertsClick} style={{ color: "orange" }}>
                    <span className={"fa fa-bell"} />
                </a>}
                &nbsp;
               <InlineCaseTags case={this.props.data.case} defaultTags={this.state.tags} />
            </span>
        );
    }

   
    handleAlertsClick = (e: React.MouseEvent<any>) => {
        e.preventDefault();

        var fo: FindOptions = {
            queryName: AlertEntity,
            filterOptions: [
                { columnName: "Target", value: this.props.data.caseActivity },
                { columnName: "Entity.Recipient", value: Navigator.currentUser },
                { columnName: "Entity.CurrentState", value: "Alerted" }
            ],
            columnOptions: [{ columnName: "Target" }],
            columnOptionsMode: "Remove",
        }; 

        Finder.exploreOrNavigate(fo)
            .then(() => Finder.getCount(fo.queryName, fo.filterOptions!))
            .then(alerts => this.setState({ alerts: alerts }))
            .done();
    }

    handleRemarksClick = (e: React.MouseEvent<any>) => {
        e.preventDefault();

        ValueLineModal.show({
            type: { name: "string" },
            valueLineType: "TextArea",
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

