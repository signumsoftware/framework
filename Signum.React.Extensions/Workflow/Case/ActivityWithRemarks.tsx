import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '@framework/Globals'
import { Lite } from '@framework/Signum.Entities'
import { CaseActivityMessage, CaseNotificationEntity, CaseNotificationOperation, CaseActivityEntity, WorkflowActivityEntity, CaseTagTypeEntity, CaseEntity } from '../Signum.Entities.Workflow'
import { FindOptions } from '@framework/Search'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import * as Operations from '@framework/Operations'
import ValueLineModal from '@framework/ValueLineModal'
import { AlertEntity } from '../../Alerts/Signum.Entities.Alerts'
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
          <FontAwesomeIcon icon={this.state.remarks ? "comment-dots" : ["far", "comment"]} />
        </a>
        {this.state.alerts > 0 && " "}
        {this.state.alerts > 0 && <a href="#" onClick={this.handleAlertsClick} style={{ color: "orange" }}>
          <FontAwesomeIcon icon={"bell"} />
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
        { token: AlertEntity.token(a => a.target), value: this.props.data.caseActivity },
        { token: AlertEntity.token().entity(e => e.recipient), value: Navigator.currentUser },
        { token: AlertEntity.token().entity().expression("CurrentState"), value: "Alerted" }
      ],
      columnOptions: [{ token: AlertEntity.token(e => e.target) }],
      columnOptionsMode: "Remove",
    };

    Finder.exploreOrNavigate(fo)
      .then(() => Finder.getQueryValue(fo.queryName, fo.filterOptions!))
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

