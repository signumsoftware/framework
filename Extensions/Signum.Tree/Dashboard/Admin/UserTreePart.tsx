import * as React from 'react'
import { EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { UserTreePartEntity } from '../../Signum.Tree';

export default function UserTreePart(p: { ctx: TypeContext<UserTreePartEntity> }) {

  const ctx = p.ctx;

  return (
    <EntityLine ctx={ctx.subCtx(a => a.userQuery)} />
  );
}


