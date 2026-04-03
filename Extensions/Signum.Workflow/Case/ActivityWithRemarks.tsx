import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '@framework/Globals'
import { getToString, Lite } from '@framework/Signum.Entities'
import { CaseActivityMessage, CaseNotificationEntity, CaseNotificationOperation, CaseActivityEntity, WorkflowActivityEntity, CaseTagTypeEntity, CaseEntity } from '../Signum.Workflow'
import { FindOptions } from '@framework/Search'
import { Finder } from '@framework/Finder'
import * as AppContext from '@framework/AppContext'
import { Operations } from '@framework/Operations'
import AutoLineModal from '@framework/AutoLineModal'
import { AlertEntity } from '../../Signum.Alerts/Signum.Alerts'
import InlineCaseTags from './InlineCaseTags'
import { useAPI } from '@framework/Hooks'
import { TextAreaLine } from '../../../Signum/React/Lines'
import { LinkButton } from '@framework/Basics/LinkButton'

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

export default function ActivityWithRemarksComponent(p: ActivityWithRemarksProps): React.JSX.Element {

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

    AutoLineModal.show({
      type: { name: "string" },
      customComponent: props => <TextAreaLine {...props} />,
      title: CaseNotificationEntity.nicePropertyName(a => a.remarks),
      message: CaseActivityMessage.PersonalRemarksForThisNotification.niceToString(),
      label: undefined,
      initialValue: remarks,
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
      <LinkButton onClick={handleRemarksClick} title={CaseNotificationEntity.nicePropertyName(a => a.remarks)} className={classes(
        "case-icon",
        !remarks && "case-icon-ghost")}        
      >
        <FontAwesomeIcon icon={remarks ? "comment-dots" : ["far", "comment"]} />
      </LinkButton>
      {alerts > 0 && " "}
      {alerts > 0 && <LinkButton title={AlertEntity.nicePluralName()} onClick={handleAlertsClick} style={{ color: "orange" }} >
        <FontAwesomeIcon icon={"bell"} aria-hidden={true}/>
      </LinkButton>}
      &nbsp;
      <InlineCaseTags case={p.data.case} defaultTags={tags} wrap />
    </span>
  );
}

