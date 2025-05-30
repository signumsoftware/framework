import * as React from 'react'
import { AutoLine, EntityLine } from '@framework/Lines'
import { Lite } from '@framework/Signum.Entities'
import { TypeContext } from '@framework/TypeContext'
import { MoveTreeModel, TreeEntity } from '../Signum.Tree'
import { TreeClient } from '../TreeClient'
import { TypeReference, QueryTokenString } from "@framework/Reflection";
import { useForceUpdate } from '@framework/Hooks'

export interface MoveTreeModelComponentProps {
  ctx: TypeContext<MoveTreeModel>;
  lite: Lite<TreeEntity>;
}

export default function MoveTreeModelComponent(p : MoveTreeModelComponentProps): React.JSX.Element {
  const forceUpdate = useForceUpdate();
  const ctx = p.ctx;
  const typeName = p.lite.EntityType;
  const type = { name: typeName, isLite: true } as TypeReference;
  return (
    <div>
      <EntityLine ctx={ctx.subCtx(a => a.newParent)} type={type} onChange={() => forceUpdate()}
        findOptions={{
          queryName: typeName,
          filterOptions: [{ token: QueryTokenString.entity(), operation: "DistinctTo", value: p.lite, frozen: true }]
        }}
        onFind={() => TreeClient.openTree({ typeName, filterOptions: [{ token: QueryTokenString.entity(), operation: "DistinctTo", value: p.lite, frozen: true }] })} />

      <AutoLine ctx={ctx.subCtx(a => a.insertPlace)} onChange={() => forceUpdate()} />

      {(ctx.value.insertPlace == "Before" || ctx.value.insertPlace == "After") &&
        <EntityLine ctx={ctx.subCtx(a => a.sibling)} type={type}
          findOptions={{ queryName: typeName, filterOptions: [{ token: QueryTokenString.entity<TreeEntity>().expression("Parent"), value: ctx.value.newParent }] }}
          onFind={() => TreeClient.openTree({ typeName, filterOptions: [{ token: QueryTokenString.entity<TreeEntity>().expression("Parent"), value: ctx.value.newParent }] })} />}
    </div>
  );
}
