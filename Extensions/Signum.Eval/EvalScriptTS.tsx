import * as React from 'react'
import CollapsableCard from '@framework/Components/CollapsableCard'
import { useAPI } from '@framework/Hooks'
import { EvalClient } from './EvalClient'

//The TypeScript twin of a C# script, compiled by the Altea application that shares the database
//(EvalEmbedded.ScriptTS, WorkflowScriptEval.CustomTypesTS). Only edited here (plain text), never compiled.
//Shown only when EvalLogic.AlteaMigration is on.
export default function EvalScriptTS(p: {
  value: string | null | undefined,
  onChange: (newValue: string) => void,
  label?: string,
}): React.JSX.Element | null {

  const alteaMigration = useAPI(() => EvalClient.API.alteaMigration(), []);

  if (!alteaMigration)
    return null;

  return (
    <CollapsableCard header={p.label ?? "TypeScript (Altea)"} size="xs" defaultOpen={false}>
      <textarea className="form-control font-monospace" rows={12} spellCheck={false}
        value={p.value ?? ""} onChange={e => p.onChange(e.currentTarget.value)} />
    </CollapsableCard>
  );
}
