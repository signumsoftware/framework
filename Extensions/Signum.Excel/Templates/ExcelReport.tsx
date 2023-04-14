import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { FileLine } from '../../Signum.Files/Components/FileLine'
import { ExcelReportEntity } from '../Signum.Excel'

export default function ExcelReport(p : { ctx: TypeContext<ExcelReportEntity> }){
  const e = p.ctx;

  return (
    <div>
      <EntityLine ctx={e.subCtx(f => f.query)} />
      <ValueLine ctx={e.subCtx(f => f.displayName)} />
      <FileLine ctx={e.subCtx(f => f.file)} />
    </div>
  );
}

