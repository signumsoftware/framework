import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { SearchControl } from '@framework/Search';
import { SMSUpdatePackageEntity, SMSMessageEntity } from '../Signum.Entities.SMS'

export default function SMSSendPackage(p: { ctx: TypeContext<SMSUpdatePackageEntity> }) {

  return (
    <div>
      <ValueLine ctx={p.ctx.subCtx(a => a.name)} />
      <SearchControl
        searchOnLoad={true}
        findOptions={{
          queryName: SMSMessageEntity,
          parentToken: "Entity.UpdatePackage",
          parentValue: p.ctx.value,
        }} />
    </div>);
}
