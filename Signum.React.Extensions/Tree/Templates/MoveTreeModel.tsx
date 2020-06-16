import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { Lite } from '@framework/Signum.Entities'
import { TypeContext } from '@framework/TypeContext'
import { MoveTreeModel, TreeEntity } from '../Signum.Entities.Tree'
import * as TreeClient from '../TreeClient'
import { TypeReference, QueryTokenString } from "@framework/Reflection";
import { useForceUpdate } from '@framework/Hooks'

export interface MoveTreeModelComponentProps {
  ctx: TypeContext<MoveTreeModel>;
  lite: Lite<TreeEntity>;
}

export default function MoveTreeModelComponent(p : MoveTreeModelComponentProps){
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
        onFind={() => TreeClient.openTree(typeName, [{ token: QueryTokenString.entity(), operation: "DistinctTo", value: p.lite, frozen: true }])} />

      <ValueLine ctx={ctx.subCtx(a => a.insertPlace)} onChange={() => forceUpdate()} />

      {(ctx.value.insertPlace == "Before" || ctx.value.insertPlace == "After") &&
        <EntityLine ctx={ctx.subCtx(a => a.sibling)} type={type}
          findOptions={{ queryName: typeName, parentToken: QueryTokenString.entity<TreeEntity>().expression("Parent"), parentValue: ctx.value.newParent }}
          onFind={() => TreeClient.openTree(typeName, [{ token: QueryTokenString.entity<TreeEntity>().expression("Parent"), value: ctx.value.newParent }])} />}
    </div>
  );
}
