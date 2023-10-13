import * as React from 'react'
import { EntityLine, EntityTable, AutoLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { useForceUpdate } from '@framework/Hooks';
import { ActiveDirectoryConfigurationEmbedded } from './Signum.Authorization.ActiveDirectory';

export default function ActiveDirectoryConfiguration(p: { ctx: TypeContext<ActiveDirectoryConfigurationEmbedded> }) {
  const ctx = p.ctx;
  const forceUpdate = useForceUpdate();
  const ctxb = ctx.subCtx({ formGroupStyle: "Basic" });
  return (
    <div>
      <div className="row">
        <div className="col-sm-6">
          <fieldset>
            <legend>Active Directory (Windows)</legend>
            <AutoLine ctx={ctxb.subCtx(n => n.domainName)} />
            <AutoLine ctx={ctxb.subCtx(ad => ad.directoryRegistry_Username)} helpText="Required for DirectoryServices if the IIS user is not in AD" />
            <AutoLine ctx={ctxb.subCtx(ad => ad.directoryRegistry_Password)} />
            <AutoLine ctx={ctxb.subCtx(n => n.loginWithWindowsAuthenticator)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} />
            <AutoLine ctx={ctxb.subCtx(n => n.loginWithActiveDirectoryRegistry)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} />
          </fieldset>
        </div>
        <div className="col-sm-6">
          <fieldset>
            <legend>Azure AD</legend>
            <AutoLine ctx={ctxb.subCtx(n => n.azure_ApplicationID)} />
            <AutoLine ctx={ctxb.subCtx(n => n.azure_DirectoryID)} />
            <AutoLine ctx={ctxb.subCtx(n => n.azure_ClientSecret)} helpText="Required for Microsoft Graph, not for Azure Log-in" />
            <AutoLine ctx={ctxb.subCtx(n => n.useDelegatedPermission)} helpText="Request current user groups from Azure using the accessToken" />
            <AutoLine ctx={ctxb.subCtx(n => n.loginWithAzureAD)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} />
          </fieldset>
        </div>
      </div>

      <AutoLine ctx={ctx.subCtx(n => n.allowMatchUsersBySimpleUserName)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} />
      <AutoLine ctx={ctx.subCtx(n => n.autoUpdateUsers)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} onChange={forceUpdate} />
      <AutoLine ctx={ctx.subCtx(n => n.autoCreateUsers)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} onChange={forceUpdate} />
      <EntityTable ctx={ctx.subCtx(n => n.roleMapping)} />
      <EntityLine ctx={ctx.subCtx(n => n.defaultRole)} />
    </div>
  );
}
