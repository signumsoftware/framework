import * as React from 'react'
import { AutoLine, TypeContext } from '@framework/Lines'
import { WorkflowScriptRetryStrategyEntity } from '../Signum.Workflow';

export default function WorkflowScriptRetryStrategy(p: { ctx: TypeContext<WorkflowScriptRetryStrategyEntity> }): React.JSX.Element {
  const ctx = p.ctx;

  return (
    <div>
      <AutoLine ctx={ctx.subCtx(e => e.rule)} />
    </div>
  );
}
