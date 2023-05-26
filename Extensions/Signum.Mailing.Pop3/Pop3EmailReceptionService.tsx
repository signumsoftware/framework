import * as React from 'react'
import { ValueLine, EntityRepeater } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { Pop3EmailReceptionServiceEntity } from './Signum.Mailing.Pop3';

export default function Pop3EmailReceptionService(p: { ctx: TypeContext<Pop3EmailReceptionServiceEntity> }) {
  const sc = p.ctx.subCtx({ formGroupStyle: "Basic" });

  return (
    <div>
      <div className="row">
        <div className="col-sm-auto">
          <ValueLine ctx={sc.subCtx(s => s.host)} />
        </div>
        <div className="col-sm-auto">
          <ValueLine ctx={sc.subCtx(s => s.port)} />
        </div>
        <div className="col-sm-auto">
          <ValueLine ctx={sc.subCtx(s => s.enableSSL)} />
        </div>
      </div>

      <div className="row">
        <div className="col-sm-auto">
          <ValueLine ctx={sc.subCtx(s => s.username)} />
        </div>
        <div className="col-sm-auto">
          <ValueLine ctx={sc.subCtx(s => s.password)} valueLineType="Password" />
        </div>
      </div>
      
      <div className="row">
        <div className="col-sm-auto">
          <ValueLine ctx={sc.subCtx(s => s.readTimeout)} />
        </div>
      </div>

      <div className="row">
        <div className="col-sm-auto">
          <EntityRepeater ctx={sc.subCtx(s => s.clientCertificationFiles)} />
        </div>
      </div>
    </div>
  );
}

