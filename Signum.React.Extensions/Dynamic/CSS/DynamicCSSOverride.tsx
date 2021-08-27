import * as React from 'react'
import { ValueLine, TypeContext } from '@framework/Lines'
import CSSCodeMirror from '../../Codemirror/CSSCodeMirror'
import { DynamicCSSOverrideEntity } from '../Signum.Entities.Dynamic'
import { useForceUpdate } from '@framework/Hooks'

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
      <ValueLine ctx={ctx.subCtx(dt => dt.name)} />
      <br />
      <div className="code-container">
        <CSSCodeMirror script={ctx.value.script ?? ""} onChange={handleCodeChange} />
      </div>
    </div>
  );
}

