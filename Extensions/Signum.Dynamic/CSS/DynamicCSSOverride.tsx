import * as React from 'react'
import { AutoLine, TypeContext } from '@framework/Lines'
import CSSCodeMirror from '../../Signum.CodeMirror/CSSCodeMirror'
import { useForceUpdate } from '@framework/Hooks'
import { DynamicCSSOverrideEntity } from '../Signum.Dynamic.CSS';

export default function DynamicCSSOverrideComponent(p : { ctx: TypeContext<DynamicCSSOverrideEntity> }){
  const forceUpdate = useForceUpdate();
  function handleCodeChange(newScript: string) {
    const entity = p.ctx.value;
    entity.script = newScript;
    entity.modified = true;
    forceUpdate();
  }

  var ctx = p.ctx;

  return (
    <div>
      <AutoLine ctx={ctx.subCtx(dt => dt.name)} />
      <br />
      <div className="code-container">
        <CSSCodeMirror script={ctx.value.script ?? ""} onChange={handleCodeChange} />
      </div>
    </div>
  );
}

