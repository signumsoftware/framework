
import * as React from 'react'
import { ValueLine, EntityRepeater } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { LinkListPartEntity, LinkElementEmbedded } from '../Signum.Entities.Dashboard'

export default function ValueSearchControlPart(p : { ctx: TypeContext<LinkListPartEntity> }){
  const ctx = p.ctx.subCtx({ formGroupStyle: "SrOnly", placeholderLabels: true });

  return (
    <div className="form-inline">
      <EntityRepeater ctx={ctx.subCtx(p => p.links)} getComponent={(tc: TypeContext<LinkElementEmbedded>) => {
        return (
          <div>
            <ValueLine ctx={tc.subCtx(cuq => cuq.label)} />
            &nbsp;
              <ValueLine ctx={tc.subCtx(cuq => cuq.link)} />
          </div>
        );
      }} />
    </div>
  );
}
