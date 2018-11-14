import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { UserQueryPartEntity } from '../Signum.Entities.Dashboard'

export default class UserQueryPart extends React.Component<{ ctx: TypeContext<UserQueryPartEntity> }> {

  render() {
    const ctx = this.props.ctx;

    return (
      <div >
        <EntityLine ctx={ctx.subCtx(p => p.userQuery)} create={false} />
        <ValueLine ctx={ctx.subCtx(p => p.renderMode)} inlineCheckbox={true} />
      </div>
    );
  }
}
