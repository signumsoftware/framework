import * as React from 'react'
import { UserTreePartEntity } from '../Signum.Dashboard'
import { EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'

export default function UserTreePart(p: { ctx: TypeContext<UserTreePartEntity> }) {

  const ctx = p.ctx;

  return (
    <EntityLine ctx={ctx.subCtx(a => a.userQuery)} />
  );
}


