import * as React from 'react'
import { EntityLine, EntityTable, AutoLine, CheckboxLine, EntityDetail } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { useForceUpdate } from '@framework/Hooks';
import { AzureADConfigurationEmbedded } from './Signum.Authorization.AzureAD';

export default function AzureADConfiguration(p: { ctx: TypeContext<AzureADConfigurationEmbedded> }): React.JSX.Element {
  const ctx = p.ctx;
  const forceUpdate = useForceUpdate();
  const ctxb = ctx.subCtx({ formGroupStyle: "Basic" });
  return (
    <div>
      <CheckboxLine ctx={ctx.subCtx(n => n.loginWithAzureAD)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} />
      <AutoLine ctx={ctx.subCtx(n => n.applicationID)} />
      <AutoLine ctx={ctx.subCtx(n => n.directoryID)} />
      <AutoLine ctx={ctx.subCtx(n => n.clientSecret)} helpText="Required for Microsoft Graph, not for Azure Log-in" />
      <CheckboxLine ctx={ctx.subCtx(n => n.useDelegatedPermission)} inlineCheckbox helpText="Request current user groups from Azure using the accessToken" />
      <EntityDetail ctx={ctx.subCtx(a => a.azureB2C)} avoidFieldSet="h6" />

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
