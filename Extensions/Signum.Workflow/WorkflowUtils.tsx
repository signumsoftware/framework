import type { FunctionalFrameComponent } from '@framework/TypeContext'
import type { WorkflowClient } from './WorkflowClient'

export function isWorkflowFrame(frameComponent: FunctionalFrameComponent | React.Component): frameComponent is FunctionalFrameComponent & WorkflowClient.IHasCaseActivity  {
  return (frameComponent as FunctionalFrameComponent & WorkflowClient.IHasCaseActivity).getCaseActivity != null;
} 


export function assertWorkflowFrame(frameComponent: FunctionalFrameComponent | React.Component): FunctionalFrameComponent & WorkflowClient.IHasCaseActivity {
  if (!isWorkflowFrame(frameComponent))
    throw new Error("No getCaseActivity found!");
  return frameComponent;
} 
