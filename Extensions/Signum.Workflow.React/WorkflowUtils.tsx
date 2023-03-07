import type { FunctionalFrameComponent } from '../../Signum.React/Scripts/TypeContext'
import type { IHasCaseActivity } from './WorkflowClient'

export function isWorkflowFrame(frameComponent: FunctionalFrameComponent | React.Component): frameComponent is FunctionalFrameComponent & IHasCaseActivity  {
  return (frameComponent as FunctionalFrameComponent & IHasCaseActivity).getCaseActivity != null;
} 


export function assertWorkflowFrame(frameComponent: FunctionalFrameComponent | React.Component): FunctionalFrameComponent & IHasCaseActivity {
  if (!isWorkflowFrame(frameComponent))
    throw new Error("No getCaseActivity found!");
  return frameComponent;
} 
