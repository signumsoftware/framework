import * as React from 'react'
import { ValueLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { DateSpanEmbedded } from '../Signum.Entities.Basics'

export default function DateSpan(p : { ctx: TypeContext<DateSpanEmbedded> }){
  const e = p.ctx;
  const sc = e.subCtx({ formGroupStyle: "BasicDown" });

  return (
    <div className="row">
      <div className="col-sm-4">
        <ValueLine ctx={sc.subCtx(n => n.years)} />
      </div>
      <div className="col-sm-4">
        <ValueLine ctx={sc.subCtx(n => n.months)} />
      </div>
      <div className="col-sm-4">
        <ValueLine ctx={sc.subCtx(n => n.days)} />
      </div>
    </div>
  );
}
