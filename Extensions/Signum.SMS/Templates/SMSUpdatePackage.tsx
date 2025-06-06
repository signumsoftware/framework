import * as React from 'react'
import { AutoLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { SearchControl } from '@framework/Search';
import { SMSUpdatePackageEntity, SMSMessageEntity } from '../Signum.SMS'

export default function SMSSendPackage(p: { ctx: TypeContext<SMSUpdatePackageEntity> }): React.JSX.Element {

  return (
    <div>
      <AutoLine ctx={p.ctx.subCtx(a => a.name)} />
      <SearchControl
        searchOnLoad={true}
        findOptions={{
          queryName: SMSMessageEntity,
          filterOptions: [{ token: "Entity.UpdatePackage", value: p.ctx.value}],
        }} />
    </div>);
}
