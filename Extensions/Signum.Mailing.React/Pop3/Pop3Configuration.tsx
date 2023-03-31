import * as React from 'react'
import { ValueLine, EntityRepeater } from '@framework/Lines'
import { SearchValueLine } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import { Binding } from '@framework/Reflection'
import { Pop3ConfigurationEntity, EmailMessageEntity } from '../Signum.Mailing'
import { DoublePassword } from '../../Authorization/Templates/DoublePassword'

export default function Pop3Configuration(p: { ctx: TypeContext<Pop3ConfigurationEntity> }) {
  const sc = p.ctx.subCtx({ formGroupStyle: "Basic" });

  return (
    <div>
      <div className="row">
        <div className="col-sm-auto">
          <ValueLine ctx={sc.subCtx(s => s.active)} />
        </div>
        <div className="col-sm-auto">
          <ValueLine ctx={sc.subCtx(s => s.fullComparation)} />
        </div>
      </div>
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
        {!sc.readOnly && sc.subCtx(a => a.password).propertyRoute?.canModify() && <div className="col-sm-auto">
          <DoublePassword ctx={new TypeContext<string>(sc, { formGroupStyle: "Basic" }, undefined as any, Binding.create(sc.value, v => v.newPassword))} initialOpen={sc.value.isNew ?? false} mandatory={false} />
        </div>}
      </div>
      
      <div className="row">
        <div className="col-sm-auto">
          <ValueLine ctx={sc.subCtx(s => s.readTimeout)} />
        </div>
        <div className="col-sm-auto">
          <ValueLine ctx={sc.subCtx(s => s.deleteMessagesAfter)} />
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

