import * as React from 'react'
import { EntityLine, EntityTable, AutoLine, CheckboxLine, EntityDetail } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { useForceUpdate } from '@framework/Hooks';
import { ActiveDirectoryConfigurationEmbedded } from './Signum.Authorization.ActiveDirectory';

export default function ActiveDirectoryConfiguration(p: { ctx: TypeContext<ActiveDirectoryConfigurationEmbedded> }): React.JSX.Element {
  const ctx = p.ctx;
  const forceUpdate = useForceUpdate();
  const ctxb = ctx.subCtx({ formGroupStyle: "Basic" });
  return (
    <div>
      <div className="row">
        <div className="col-sm-6">
          <EntityDetail ctx={ctxb.subCtx(a => a.windowsAD)} avoidFieldSet="h5"
            getComponent={(wac) => <div>
              <CheckboxLine ctx={wac.subCtx(n => n.loginWithWindowsAuthenticator)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} />
              <CheckboxLine ctx={wac.subCtx(n => n.loginWithActiveDirectoryRegistry)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} />
              <AutoLine ctx={wac.subCtx(n => n.domainName)} />
              <AutoLine ctx={wac.subCtx(ad => ad.directoryRegistry_Username)} helpText="Required for DirectoryServices if the IIS user is not in AD" />
              <AutoLine ctx={wac.subCtx(ad => ad.directoryRegistry_Password)} />
            </div>} />
        </div>
        <div className="col-sm-6">
          <EntityDetail ctx={ctxb.subCtx(a => a.azureAD)} avoidFieldSet="h5"
            getComponent={(wac) => <div>
              <CheckboxLine ctx={wac.subCtx(n => n.loginWithAzureAD)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} />
              <AutoLine ctx={wac.subCtx(n => n.applicationID)} />
              <AutoLine ctx={wac.subCtx(n => n.directoryID)} />
              <AutoLine ctx={wac.subCtx(n => n.clientSecret)} helpText="Required for Microsoft Graph, not for Azure Log-in" />
              <CheckboxLine ctx={wac.subCtx(n => n.useDelegatedPermission)} inlineCheckbox helpText="Request current user groups from Azure using the accessToken" />
              <EntityDetail ctx={wac.subCtx(a => a.azureB2C)} avoidFieldSet="h6" />
            </div>} />
        </div>
      </div>

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
