import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import * as Navigator from '@framework/Navigator'
import { CaseActivityEntity, WorkflowActivityMessage } from '../Signum.Entities.Workflow'

interface CaseFlowButtonProps {
  caseActivity: CaseActivityEntity;
}

export default function CaseFlowButton(p : CaseFlowButtonProps){
  function handleClick(e: React.MouseEvent<HTMLButtonElement>) {
    e.preventDefault();
    var ca = p.caseActivity;
    Navigator.navigate(ca.case, { extraProps: { caseActivity: ca } }).done();
  }

  return (
    <button className="btn btn-light float-right flip" onClick={handleClick}>
      <FontAwesomeIcon icon="random" color="green" /> {WorkflowActivityMessage.CaseFlow.niceToString()}
    </button>
  );
}
