import * as React from 'react'
import { AutoLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { useForceUpdate } from '@framework/Hooks';
import { MicrosoftGraphEmailServiceEntity } from '../Signum.Mailing.MicrosoftGraph';

export default function MicrosoftGraphEmailService(p: { ctx: TypeContext<MicrosoftGraphEmailServiceEntity> }): React.JSX.Element {
  const sc = p.ctx;
  const forceUpdate = useForceUpdate();

  return (
    <div>
      <AutoLine ctx={sc.subCtx(ca => ca.useActiveDirectoryConfiguration)} onChange={forceUpdate} />
      {
        !sc.value.useActiveDirectoryConfiguration && <div>
          <AutoLine ctx={sc.subCtx(ca => ca.azure_DirectoryID)} />
          <AutoLine ctx={sc.subCtx(ca => ca.azure_ApplicationID)} />
          <AutoLine ctx={sc.subCtx(ca => ca.azure_ClientSecret)} />
        </div>
      }
    </div>
  );
}

