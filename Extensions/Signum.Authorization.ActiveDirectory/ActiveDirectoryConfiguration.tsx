import * as React from 'react'
import { EntityLine, EntityTable, ValueLine } from '@framework/Lines'
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
            <ValueLine ctx={ctxb.subCtx(n => n.domainName)} />
            <ValueLine ctx={ctxb.subCtx(ad => ad.directoryRegistry_Username)} helpText="Required for DirectoryServices if the IIS user is not in AD" />
            <ValueLine ctx={ctxb.subCtx(ad => ad.directoryRegistry_Password)} />
            <ValueLine ctx={ctxb.subCtx(n => n.loginWithWindowsAuthenticator)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} />
            <ValueLine ctx={ctxb.subCtx(n => n.loginWithActiveDirectoryRegistry)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} />
          </fieldset>
        </div>
        <div className="col-sm-6">
          <fieldset>
            <legend>Azure AD</legend>
            <ValueLine ctx={ctxb.subCtx(n => n.azure_ApplicationID)} />
            <ValueLine ctx={ctxb.subCtx(n => n.azure_DirectoryID)} />
            <ValueLine ctx={ctxb.subCtx(n => n.azure_ClientSecret)} helpText="Required for Microsoft Graph, not for Azure Log-in" />
            <ValueLine ctx={ctxb.subCtx(n => n.loginWithAzureAD)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} />
          </fieldset>
        </div>
      </div>

      <ValueLine ctx={ctx.subCtx(n => n.allowMatchUsersBySimpleUserName)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} />
      <ValueLine ctx={ctx.subCtx(n => n.autoUpdateUsers)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} onChange={forceUpdate} />
      <ValueLine ctx={ctx.subCtx(n => n.autoCreateUsers)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} onChange={forceUpdate} />
      <EntityTable ctx={ctx.subCtx(n => n.roleMapping)} />
      <EntityLine ctx={ctx.subCtx(n => n.defaultRole)} />
    </div>
  );
}
