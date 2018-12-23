
import * as React from 'react'
import { ValueLine, EntityLine, EntityRepeater } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ValueUserQueryListPartEntity, ValueUserQueryElementEmbedded } from '../Signum.Entities.Dashboard'

export default class ValueUserQueryListPart extends React.Component<{ ctx: TypeContext<ValueUserQueryListPartEntity> }> {
  render() {
    const ctx = this.props.ctx.subCtx({ formGroupStyle: "SrOnly", placeholderLabels: true });

    return (
      <div className="form-inline">
        <EntityRepeater ctx={ctx.subCtx(p => p.userQueries)} getComponent={ctx => this.renderUserQuery(ctx as TypeContext<ValueUserQueryElementEmbedded>)} />
      </div>
    );
  }

  renderUserQuery = (tc: TypeContext<ValueUserQueryElementEmbedded>) => {
    return (
      <div className="form-inline">
        <ValueLine ctx={tc.subCtx(cuq => cuq.label)} />
        &nbsp;
        <EntityLine ctx={tc.subCtx(cuq => cuq.userQuery)} formGroupHtmlAttributes={{ style: { maxWidth: "300px" } }} />
        &nbsp;
        <ValueLine ctx={tc.subCtx(cuq => cuq.href)} />
      </div>
    );

  }
}
