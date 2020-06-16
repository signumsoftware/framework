import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { SearchControl } from '@framework/Search';
import { SMSSendPackageEntity, SMSMessageEntity } from '../Signum.Entities.SMS'

export default function SMSSendPackage(p: { ctx: TypeContext<SMSSendPackageEntity> }) {

  return (
    <div>
      <ValueLine ctx={p.ctx.subCtx(a => a.name)} />
      <SearchControl
        findOptions={{
          queryName: SMSMessageEntity,
          parentToken: "Entity.SendPackage",
          parentValue: p.ctx.value,
        }} />
    </div>);
}
