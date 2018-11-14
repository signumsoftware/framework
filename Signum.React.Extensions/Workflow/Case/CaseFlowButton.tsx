import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import * as Navigator from '@framework/Navigator'
import { CaseActivityEntity, WorkflowActivityMessage } from '../Signum.Entities.Workflow'

interface CaseFlowButtonProps {
  caseActivity: CaseActivityEntity;
}

export default class CaseFlowButton extends React.Component<CaseFlowButtonProps>{
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
