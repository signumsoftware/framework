import * as React from 'react'
import { ValueLine, EntityLine } from '../../../../Framework/Signum.React/Scripts/Lines'
import { Lite } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeContext } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { MoveTreeModel, TreeEntity } from '../Signum.Entities.Tree'
import * as TreeClient from '../TreeClient'
import { TypeReference, QueryTokenString } from "../../../../Framework/Signum.React/Scripts/Reflection";

export interface MoveTreeModelComponentProps {
  ctx: TypeContext<MoveTreeModel>;
  lite: Lite<TreeEntity>;
}

export default class MoveTreeModelComponent extends React.Component<MoveTreeModelComponentProps> {
  render() {
    const ctx = this.props.ctx;
    const typeName = this.props.lite.EntityType;
    const type = { name: typeName, isLite: true } as TypeReference;
    return (
      <div>
        <EntityLine ctx={ctx.subCtx(a => a.newParent)} type={type} onChange={() => this.forceUpdate()}
          findOptions={{
            queryName: typeName,
            filterOptions: [{ token: QueryTokenString.entity(), operation: "DistinctTo", value: this.props.lite, frozen: true }]
          }}
          onFind={() => TreeClient.openTree(typeName, [{ token: QueryTokenString.entity(), operation: "DistinctTo", value: this.props.lite, frozen: true }])} />

        <ValueLine ctx={ctx.subCtx(a => a.insertPlace)} onChange={() => this.forceUpdate()} />

        {(ctx.value.insertPlace == "Before" || ctx.value.insertPlace == "After") &&
          <EntityLine ctx={ctx.subCtx(a => a.sibling)} type={type}
            findOptions={{ queryName: typeName, parentToken: QueryTokenString.entity<TreeEntity>().expression("Parent"), parentValue: ctx.value.newParent }}
            onFind={() => TreeClient.openTree(typeName, [{ token: QueryTokenString.entity<TreeEntity>().expression("Parent"), value: ctx.value.newParent }])} />}
      </div>
    );
  }


}
