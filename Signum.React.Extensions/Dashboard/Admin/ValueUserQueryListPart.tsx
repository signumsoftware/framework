
import * as React from 'react'
import { ValueLine, EntityLine, EntityRepeater, EntityTable } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ValueUserQueryListPartEntity, ValueUserQueryElementEmbedded } from '../Signum.Entities.Dashboard'

export default function ValueUserQueryListPart(p : { ctx: TypeContext<ValueUserQueryListPartEntity> }){
  
  const ctx = p.ctx;

  return (
      <EntityTable ctx={ctx.subCtx(p => p.userQueries)} />
  );
}
