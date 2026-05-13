import * as React from 'react'
import { EntityLine, EntityTable, AutoLine, CheckboxLine, TextBoxLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { useForceUpdate } from '@framework/Hooks'
import { OpenIDConfigurationEmbedded } from './Signum.Authorization.OpenID'

const roleClaimPathSuggestions = [
  "roles",
  "groups",
  "realm_access.roles",
  "resource_access.{clientId}.roles",
];

export default function OpenIDConfiguration(p: { ctx: TypeContext<OpenIDConfigurationEmbedded> }): React.JSX.Element {
  const ctx = p.ctx;
  const forceUpdate = useForceUpdate();

  const datalistId = React.useId();

  return (
    <div>
      <div className="row">
        <div className="col-sm-10 offset-sm-2">
          <CheckboxLine ctx={ctx.subCtx(n => n.enabled)} inlineCheckbox onChange={forceUpdate} formGroupHtmlAttributes={{ style: { display: "block" } }} />
        </div>
      </div>

      <AutoLine ctx={ctx.subCtx(n => n.authority)} helpText="Base URL of the OpenID provider (e.g. https://keycloak.example.com/realms/myrealm)" />
      <AutoLine ctx={ctx.subCtx(n => n.clientId)} />
      <AutoLine ctx={ctx.subCtx(n => n.clientSecret)} />
      <AutoLine ctx={ctx.subCtx(n => n.scopes)} helpText='Space-separated scopes (default: "openid profile email")' />

      <datalist id={datalistId}>
        {roleClaimPathSuggestions.map(s => <option key={s} value={s} />)}
      </datalist>
      <TextBoxLine ctx={ctx.subCtx(n => n.roleClaimPath)}
        helpText="Claim path for roles (e.g. roles, groups, realm_access.roles)"
        valueHtmlAttributes={{ list: datalistId }} />

      <div className="row my-2">
        <div className="col-sm-6">
          <CheckboxLine ctx={ctx.subCtx(n => n.allowMatchUsersBySimpleUserName)} inlineCheckbox formGroupHtmlAttributes={{ style: { display: "block" } }} />
        </div>
      </div>

      <div className="row my-2">
        <div className="col-sm-6">
          <CheckboxLine ctx={ctx.subCtx(n => n.autoUpdateUsers)} inlineCheckbox onChange={forceUpdate} formGroupHtmlAttributes={{ style: { display: "block" } }} />
        </div>
        <div className="col-sm-6">
          <CheckboxLine ctx={ctx.subCtx(n => n.autoCreateUsers)} inlineCheckbox onChange={forceUpdate} formGroupHtmlAttributes={{ style: { display: "block" } }} />
        </div>
      </div>

      {(ctx.value.autoCreateUsers || ctx.value.autoUpdateUsers) && (
        <div>
          <div className="row">
            <div className="col-sm-10 offset-sm-2">
              <EntityTable ctx={ctx.subCtx(n => n.roleMapping)} avoidFieldSet="h3" />
            </div>
          </div>
          <EntityLine ctx={ctx.subCtx(n => n.defaultRole)} />
        </div>
      )}
    </div>
  );
}
