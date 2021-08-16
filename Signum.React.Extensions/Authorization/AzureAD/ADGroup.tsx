import * as React from 'react'
import { EntityLine, EntityTable, ValueLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { useForceUpdate } from '@framework/Hooks';
import { ADGroupEntity, UserADQuery } from '../Signum.Entities.Authorization';
import { ValueSearchControlLine } from '../../../../Framework/Signum.React/Scripts/Search';

export default function ADGroup(p: { ctx: TypeContext<ADGroupEntity> }) {
  const ctx = p.ctx;
  return (
    <div>
      <ValueLine ctx={ctx.subCtx(n => n.displayName)} />
      <ValueSearchControlLine ctx={ctx} findOptions={{ queryName: UserADQuery.ActiveDirectoryUsers, filterOptions: [{ token: "InGroup", value: ctx.value }] }} />
    </div>
  );
}
