
import * as React from 'react'
import { ValueLine, EntityRepeater } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { LinkListPartEntity, LinkElementEmbedded } from '../Signum.Entities.Dashboard'

export default class ValueSearchControlPart extends React.Component<{ ctx: TypeContext<LinkListPartEntity> }> {

  render() {
    const ctx = this.props.ctx.subCtx({ formGroupStyle: "SrOnly", placeholderLabels: true });

    return (
      <div className="form-inline">
        <EntityRepeater ctx={ctx.subCtx(p => p.links)} getComponent={this.renderLink} />
      </div>
    );
  }

  renderLink = (tc: TypeContext<LinkElementEmbedded>) => {
    return (
      <div>
        <ValueLine ctx={tc.subCtx(cuq => cuq.label)} />
        &nbsp;
                <ValueLine ctx={tc.subCtx(cuq => cuq.link)} />
      </div>
    );

  }
}
