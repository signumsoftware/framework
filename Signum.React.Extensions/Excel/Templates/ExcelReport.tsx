import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import FileLine from '../../Files/FileLine'
import { ExcelReportEntity } from '../Signum.Entities.Excel'

export default class ExcelReport extends React.Component<{ ctx: TypeContext<ExcelReportEntity> }> {
  render() {
    const e = this.props.ctx;

    return (
      <div>
        <EntityLine ctx={e.subCtx(f => f.query)} />
        <ValueLine ctx={e.subCtx(f => f.displayName)} />
        <FileLine ctx={e.subCtx(f => f.file)} />
      </div>
    );
  }
}

