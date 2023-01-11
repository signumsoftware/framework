import * as React from 'react'
import { ValueLine, EntityCombo } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { EmailConfigurationEmbedded, MicrosoftGraphEmailServiceEntity } from '../../Signum.Entities.Mailing'
import { useForceUpdate } from '@framework/Hooks';

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

