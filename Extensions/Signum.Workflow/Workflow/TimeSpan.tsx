import * as React from 'react'
import { ValueLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { TimeSpanEmbedded } from '../Signum.Workflow';

export default function TimeSpan(p : { ctx: TypeContext<TimeSpanEmbedded> }){
  const e = p.ctx;
  const sc = e.subCtx({ formGroupStyle: "BasicDown" });

  return (
    <div className="row">
      <div className="col-sm-3">
        <ValueLine ctx={sc.subCtx(n => n.days)} />
      </div>
      <div className="col-sm-3">
        <ValueLine ctx={sc.subCtx(n => n.hours)} />
      </div>
      <div className="col-sm-3">
        <ValueLine ctx={sc.subCtx(n => n.minutes)} />
      </div>
      <div className="col-sm-3">
        <ValueLine ctx={sc.subCtx(n => n.seconds)} />
      </div>
    </div>
  );
}
