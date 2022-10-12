import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '@framework/Globals'
import { getToString, Lite } from '@framework/Signum.Entities'
import { CaseActivityMessage, CaseNotificationEntity, CaseNotificationOperation, CaseActivityEntity, WorkflowActivityEntity, CaseTagTypeEntity, CaseEntity } from '../Signum.Entities.Workflow'
import { FindOptions } from '@framework/Search'
import * as Finder from '@framework/Finder'
import * as AppContext from '@framework/AppContext'
import * as Operations from '@framework/Operations'
import ValueLineModal from '@framework/ValueLineModal'
import { AlertEntity } from '../../Alerts/Signum.Entities.Alerts'
import InlineCaseTags from './InlineCaseTags'
import { useAPI } from '@framework/Hooks'

export interface ActivityWithRemarks {
  workflowActivity: Lite<WorkflowActivityEntity>;
  case: Lite<CaseEntity>;
  caseActivity: Lite<CaseActivityEntity>;
  notification: Lite<CaseNotificationEntity>;
  remarks: string | null
  alerts: number;
  tags: Array<CaseTagTypeEntity>;
}

export interface ActivityWithRemarksProps {
  data: ActivityWithRemarks;
}

function useStateFromProps<T>(propsValue: T): [T, (newValue: T) => void] {
  var [val, setVal] = React.useState(propsValue);

  React.useEffect(() => {
    setVal(propsValue);
  }, [propsValue]);

  return [val, setVal];
}

export default function ActivityWithRemarksComponent(p: ActivityWithRemarksProps) {

  const [remarks, setRemarks] = useStateFromProps(p.data.remarks);
  const [alerts, setAlerts] = useStateFromProps(p.data.alerts);
  const tags = useAPI(() => Promise.resolve(p.data.tags), p.data.tags.map(t => t.id));

  function handleAlertsClick(e: React.MouseEvent<any>) {
    e.preventDefault();

    var fo: FindOptions = {
      queryName: AlertEntity,
      filterOptions: [
        { token: AlertEntity.token(a => a.target), value: p.data.caseActivity },
        { token: AlertEntity.token(e => e.entity.recipient), value: AppContext.currentUser },
        { token: AlertEntity.token(a => a.entity).expression("CurrentState"), value: "Alerted" }
      ],
      columnOptions: [{ token: AlertEntity.token(e => e.target) }],
      columnOptionsMode: "Remove",
    };

    Finder.exploreOrView(fo)
      .then(() => Finder.getQueryValue(fo.queryName, fo.filterOptions!.notNull()))
      .then(alerts => setAlerts(alerts));
  }

  function handleRemarksClick(e: React.MouseEvent<any>) {
    e.preventDefault();

    ValueLineModal.show({
      type: { name: "string" },
      valueLineType: "TextArea",
      title: CaseNotificationEntity.nicePropertyName(a => a.remarks),
      message: CaseActivityMessage.PersonalRemarksForThisNotification.niceToString(),
      label: undefined,
      initialValue: remarks,
      initiallyFocused: true
    }).then(remarks => {

      if (remarks === undefined)
        return;

      Operations.API.executeLite(p.data.notification, CaseNotificationOperation.SetRemarks, remarks)
        .then(n => setRemarks(n.entity.remarks));

    });
  }
  return (
    <span>
      {getToString(p.data.workflowActivity)}
      &nbsp;
      <a href="#" onClick={handleRemarksClick} className={classes(
        "case-icon",
        !remarks && "case-icon-ghost")}>
        <FontAwesomeIcon icon={remarks ? "comment-dots" : ["far", "comment"]} />
      </a>
      {alerts > 0 && " "}
      {alerts > 0 && <a href="#" onClick={handleAlertsClick} style={{ color: "orange" }}>
        <FontAwesomeIcon icon={"bell"} />
      </a>}
      &nbsp;
      <InlineCaseTags case={p.data.case} defaultTags={tags} wrap />
    </span>
  );
}

