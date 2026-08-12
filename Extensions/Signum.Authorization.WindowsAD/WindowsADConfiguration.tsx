import * as React from 'react'
import { EntityLine, EntityTable, AutoLine, CheckboxLine, EntityDetail } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { useForceUpdate } from '@framework/Hooks';
import { WindowsADConfigurationEmbedded } from './Signum.Authorization.WindowsAD';

export default function WindowsADConfiguration(p: { ctx: TypeContext<WindowsADConfigurationEmbedded> }): React.JSX.Element {
  const ctx = p.ctx;
  const forceUpdate = useForceUpdate();
  return (
    <div>
      <CheckboxLine ctx={ctx.subCtx(n => n.loginWithWindowsAuthenticator)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} />
      <CheckboxLine ctx={ctx.subCtx(n => n.loginWithActiveDirectoryRegistry)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} />
      <AutoLine ctx={ctx.subCtx(n => n.domainName)} />
      <AutoLine ctx={ctx.subCtx(ad => ad.directoryRegistry_Username)} helpText="Required for DirectoryServices if the IIS user is not in AD" />
      <AutoLine ctx={ctx.subCtx(ad => ad.directoryRegistry_Password)} />

      <div className="row">
        <div className="col-sm-4">
          <CheckboxLine ctx={ctx.subCtx(n => n.allowMatchUsersBySimpleUserName)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} />
        </div>
        <div className="col-sm-4">
          <CheckboxLine ctx={ctx.subCtx(n => n.autoUpdateUsers)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} onChange={forceUpdate} />
        </div>
        <div className="col-sm-4">
          <CheckboxLine ctx={ctx.subCtx(n => n.autoCreateUsers)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} onChange={forceUpdate} />
        </div>
      </div>

      <EntityTable ctx={ctx.subCtx(n => n.roleMapping)} />
      <EntityLine ctx={ctx.subCtx(n => n.defaultRole)} />
    </div>
  );
}
