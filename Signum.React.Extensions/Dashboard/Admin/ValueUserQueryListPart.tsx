
import * as React from 'react'
import { ValueLine, EntityLine, EntityRepeater } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ValueUserQueryListPartEntity, ValueUserQueryElementEmbedded } from '../Signum.Entities.Dashboard'

export default function ValueUserQueryListPart(p : { ctx: TypeContext<ValueUserQueryListPartEntity> }){
  
  const ctx = p.ctx.subCtx({ formGroupStyle: "SrOnly", placeholderLabels: true });

  return (
    <div className="form-inline">
      <EntityRepeater ctx={ctx.subCtx(p => p.userQueries)} getComponent={(tc: TypeContext<ValueUserQueryElementEmbedded>) => {
        return (
          <div className="form-inline">
            <ValueLine ctx={tc.subCtx(cuq => cuq.label)} />
            &nbsp;
            <EntityLine ctx={tc.subCtx(cuq => cuq.userQuery)} formGroupHtmlAttributes={{ style: { maxWidth: "300px" } }} />
            &nbsp;
            <ValueLine ctx={tc.subCtx(cuq => cuq.href)} />
          </div>
        );
      }} />
    </div>
  );
}
