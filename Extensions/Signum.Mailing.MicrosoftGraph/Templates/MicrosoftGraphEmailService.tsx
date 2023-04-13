import * as React from 'react'
import { ValueLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { useForceUpdate } from '@framework/Hooks';
import { MicrosoftGraphEmailServiceEntity } from '../Signum.Mailing.MicrosoftGraph';

export default function MicrosoftGraphEmailService(p: { ctx: TypeContext<MicrosoftGraphEmailServiceEntity> }) {
  const sc = p.ctx;
  const forceUpdate = useForceUpdate();

  return (
    <div>
      <ValueLine ctx={sc.subCtx(ca => ca.useActiveDirectoryConfiguration)} onChange={forceUpdate} />
      {
        !sc.value.useActiveDirectoryConfiguration && <div>
          <ValueLine ctx={sc.subCtx(ca => ca.azure_DirectoryID)} />
          <ValueLine ctx={sc.subCtx(ca => ca.azure_ApplicationID)} />
          <ValueLine ctx={sc.subCtx(ca => ca.azure_ClientSecret)} />
        </div>
      }
    </div>
  );
}

