import * as React from 'react'
import { EntityLine, EntityTable, ValueLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { useForceUpdate } from '@framework/Hooks';
import { ADGroupEntity, UserADQuery } from '../Signum.Authorization';
import { SearchValueLine } from '@framework/Search';

export default function ADGroup(p: { ctx: TypeContext<ADGroupEntity> }) {
  const ctx = p.ctx;
  return (
    <div>
      <ValueLine ctx={ctx.subCtx(n => n.displayName)} />
      <SearchValueLine ctx={ctx} findOptions={{ queryName: UserADQuery.ActiveDirectoryUsers, filterOptions: [{ token: "InGroup", value: ctx.value }] }} />
    </div>
  );
}
