import * as React from 'react'
import { AutoLine, EntityRepeater, EntityDetail } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { SmtpNetworkDeliveryEmbedded, ClientCertificationFileEmbedded, SmtpEmailServiceEntity, } from '../../Signum.Mailing'
export default function SmtpEmailService(p: { ctx: TypeContext<SmtpEmailServiceEntity> }) {
  const sc = p.ctx;

  return (
    <div>
      <AutoLine ctx={sc.subCtx(s => s.deliveryFormat)} />
      <AutoLine ctx={sc.subCtx(s => s.deliveryMethod)} />
      <AutoLine ctx={sc.subCtx(s => s.pickupDirectoryLocation)} />
      <EntityDetail ctx={sc.subCtx(s => s.network)} getComponent={(net: TypeContext<SmtpNetworkDeliveryEmbedded>) =>
        <div>
          <AutoLine ctx={net.subCtx(s => s.port)} />
          <AutoLine ctx={net.subCtx(s => s.host)} />
          <AutoLine ctx={net.subCtx(s => s.useDefaultCredentials)} />
          <AutoLine ctx={net.subCtx(s => s.username)} />
          <AutoLine ctx={net.subCtx(s => s.password)} valueLineType="Password" />
          <AutoLine ctx={net.subCtx(s => s.enableSSL)} />
          <EntityRepeater ctx={net.subCtx(s => s.clientCertificationFiles)} getComponent={(cert: TypeContext<ClientCertificationFileEmbedded>) =>
            <div>
              <AutoLine ctx={cert.subCtx(s => s.certFileType)} />
              <AutoLine ctx={cert.subCtx(s => s.fullFilePath)} />
            </div>
          } />
        </div>
      } />
    </div>
  );
}

