import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { UserChartPartEntity } from '../Signum.Entities.Dashboard'

export default class UserChartPart extends React.Component<{ ctx: TypeContext<UserChartPartEntity> }> {
  render() {
    const ctx = this.props.ctx;

    return (
      <div >
        <EntityLine ctx={ctx.subCtx(p => p.userChart)} create={false} />
        <ValueLine ctx={ctx.subCtx(p => p.showData)} inlineCheckbox={true} formGroupHtmlAttributes={{ style: { display: "block" } }} />
        <ValueLine ctx={ctx.subCtx(p => p.allowChangeShowData)} inlineCheckbox={true} formGroupHtmlAttributes={{ style: { display: "block" } }} />
      </div>
    );
  }
}
