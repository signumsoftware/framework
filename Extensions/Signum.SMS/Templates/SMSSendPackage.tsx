import * as React from 'react'
import { AutoLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { SearchControl } from '@framework/Search';
import { SMSSendPackageEntity, SMSMessageEntity } from '../Signum.SMS'

export default function SMSSendPackage(p: { ctx: TypeContext<SMSSendPackageEntity> }) {

  return (
    <div>
      <AutoLine ctx={p.ctx.subCtx(a => a.name)} />
      <SearchControl
        findOptions={{
          queryName: SMSMessageEntity,
          filterOptions: [{ token: "Entity.SendPackage", value: p.ctx.value}],
        }} />
    </div>);
}
