import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Navigator } from '@framework/Navigator'
import { CaseActivityEntity, WorkflowActivityMessage } from '../Signum.Workflow'

interface CaseFlowButtonProps {
  caseActivity: CaseActivityEntity;
}

export default function CaseFlowButton(p: CaseFlowButtonProps): React.JSX.Element {
  function handleClick(e: React.MouseEvent<HTMLAnchorElement>) {
    e.preventDefault();
    var ca = p.caseActivity;
    Navigator.view(ca.case, { extraProps: { caseActivity: ca } });
  }

  return (
    <a href="#" className="btn btn-info btn-xs px-2" onClick={handleClick}>
      <FontAwesomeIcon icon="shuffle" color="green" /> {WorkflowActivityMessage.CaseFlow.niceToString()}
    </a>
  );
}
