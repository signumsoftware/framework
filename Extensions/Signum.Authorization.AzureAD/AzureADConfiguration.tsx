import * as React from 'react'
import { EntityLine, EntityTable, AutoLine, CheckboxLine, EntityDetail, TextBoxLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { useForceUpdate } from '@framework/Hooks';
import { AzureADConfigurationEmbedded } from './Signum.Authorization.AzureAD';

export default function AzureADConfiguration(p: { ctx: TypeContext<AzureADConfigurationEmbedded> }): React.JSX.Element {
  const ctx = p.ctx;
  const forceUpdate = useForceUpdate();
  const ctxb = ctx.subCtx({ formGroupStyle: "Basic" });
  return (
    <div>
      <div className="row">
        <div className="col-sm-10 offset-sm-2">
          <CheckboxLine ctx={ctx.subCtx(n => n.enabled)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} />
        </div>
      </div>

      <AutoLine ctx={ctx.subCtx(n => n.type)} onChange={() => {
        if (ctx.value.type == "AzureAD") {
          ctx.value.tenantName = null;
          ctx.value.signInSignUp_UserFlow = null;
        }
        ctx.value.signIn_UserFlow = null;
        ctx.value.signUp_UserFlow = null;
        ctx.value.editProfile_UserFlow = null;
        ctx.value.resetPassword_UserFlow = null;
        forceUpdate();
      }} />
      <AutoLine ctx={ctx.subCtx(n => n.applicationID)} />
      <AutoLine ctx={ctx.subCtx(n => n.directoryID)} />

      {
        ctx.value.type == "ExternalID" && <div>
          <TextBoxLine ctx={ctx.subCtx(n => n.tenantName)} mandatory valueHtmlAttributes={{ placeholder: "southwind.ciamlogin.com" }} />
          <TextBoxLine ctx={ctx.subCtx(n => n.signInSignUp_UserFlow)} mandatory valueHtmlAttributes={{ placeholder: "https://southwind.ciamlogin.com/southwind.onmicrosoft.com" }} />
        </div>
      }

      {
        ctx.value.type == "B2C" && <div>
          <TextBoxLine ctx={ctx.subCtx(n => n.tenantName)} mandatory />
          <TextBoxLine ctx={ctx.subCtx(n => n.signInSignUp_UserFlow)} mandatory={ctx.value.signIn_UserFlow ? undefined : "warning"} onChange={forceUpdate} />
          <TextBoxLine ctx={ctx.subCtx(n => n.signIn_UserFlow)} mandatory={ctx.value.signInSignUp_UserFlow ? undefined : "warning"} onChange={forceUpdate} />
          <TextBoxLine ctx={ctx.subCtx(n => n.signUp_UserFlow)} />
          <TextBoxLine ctx={ctx.subCtx(n => n.editProfile_UserFlow)} />
          <TextBoxLine ctx={ctx.subCtx(n => n.resetPassword_UserFlow)} />
        </div>
      }

      <AutoLine ctx={ctx.subCtx(n => n.clientSecret)} helpText="Required for Microsoft Graph, not for Azure Log-in" />

      <div className="row my-2">
        <div className="col-sm-6">
          <CheckboxLine ctx={ctx.subCtx(n => n.allowMatchUsersBySimpleUserName)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} />
        </div>
        <div className="col-sm-6">
          <CheckboxLine ctx={ctx.subCtx(n => n.useDelegatedPermission)} inlineCheckbox helpText="Request current user groups from Azure using the accessToken" />
        </div>
      </div>

      <div className="row my-2">
        <div className="col-sm-6">
          <CheckboxLine ctx={ctx.subCtx(n => n.autoUpdateUsers)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} onChange={forceUpdate} />
        </div>
        <div className="col-sm-6">
          <CheckboxLine ctx={ctx.subCtx(n => n.autoCreateUsers)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} onChange={forceUpdate} />
        </div>

      </div>

      {(ctx.value.autoCreateUsers || ctx.value.autoUpdateUsers) && <div>
        <div className="row">
          <div className="col-sm-10 offset-sm-2">
            <EntityTable ctx={ctx.subCtx(n => n.roleMapping)} avoidFieldSet="h3" />
          </div>
        </div>

        <EntityLine ctx={ctx.subCtx(n => n.defaultRole)} />
      </div>
      }
    </div>
  );
}
