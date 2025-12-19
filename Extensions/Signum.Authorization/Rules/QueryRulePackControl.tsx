import * as React from 'react'
import { Operations } from '@framework/Operations'
import { TypeContext, ButtonsContext, IRenderButtons, ButtonBarElement } from '@framework/TypeContext'
import { EntityLine, AutoLine } from '@framework/Lines'
import { AuthAdminClient } from '../AuthAdminClient'
import { QueryRulePack, QueryAllowedRule, QueryAllowed, AuthAdminMessage } from './Signum.Authorization.Rules'
import { ColorRadio, GrayCheckbox } from './ColoredRadios'
import { Button } from 'react-bootstrap'
import "./AuthAdmin.css"
import { useForceUpdate } from '@framework/Hooks';
import { getToString } from '@framework/Signum.Entities';
import { QueryEntity } from '@framework/Signum.Basics'
import { AccessibleTable } from '../../../Signum/React/Basics/AccessibleTable'
import { LinkButton } from '../../../Signum/React/Basics/LinkButton'

export default function QueryRulesPackControl({ ctx, innerRef }: { ctx: TypeContext<QueryRulePack>, innerRef: React.Ref<IRenderButtons> }): React.JSX.Element {

  function updateFrame() {
    ctx.frame!.frameComponent.forceUpdate();
  }
  function handleSaveClick(bc: ButtonsContext) {
    let pack = ctx.value;

    AuthAdminClient.API.saveQueryRulePack(pack)
      .then(() => AuthAdminClient.API.fetchQueryRulePack(pack.type.cleanName!, pack.role.id!))
      .then(newPack => {
        Operations.notifySuccess();
        bc.frame.onReload({ entity: newPack, canExecute: {} });
      });
  }

  function renderButtons(bc: ButtonsContext): ButtonBarElement[] {
    return [
      { button: <Button type="button" variant="primary" disabled={ctx.readOnly} onClick={() => handleSaveClick(bc)}>{AuthAdminMessage.Save.niceToString()}</Button> },
    ];
  }

  React.useImperativeHandle(innerRef, () => ({ renderButtons }), [ctx.value]);

  function handleHeaderClick(e: React.MouseEvent<HTMLAnchorElement>, hc: QueryAllowed) {

    ctx.value.rules.forEach(mle => {
      mle.element.allowed = QueryAllowed.min(hc, mle.element.coerced);
      mle.element.modified = true;
    });

    updateFrame();
  }

  return (
    <div>
      <div className="form-compact">
        <EntityLine ctx={ctx.subCtx(f => f.role)} />
        <AutoLine ctx={ctx.subCtx(f => f.strategy)} />
        <EntityLine ctx={ctx.subCtx(f => f.type)} />
      </div>
      <AccessibleTable
        aria-label={AuthAdminMessage.QueryPermissionsOverview.niceToString()}
        className="table table-sm sf-auth-rules "
        multiselectable={false}>
        <thead>
          <tr>
            <th>
              {QueryEntity.niceName()}
            </th>
            <th style={{ textAlign: "center" }}>
              <LinkButton title={undefined} onClick={e => handleHeaderClick(e, "Allow")} style={{ color: "inherit" }}>{QueryAllowed.niceToString("Allow")}</LinkButton>
            </th>
            <th style={{ textAlign: "center" }}>
              <LinkButton title={undefined} onClick={e => handleHeaderClick(e, "EmbeddedOnly")} style={{ color: "inherit" }}>{QueryAllowed.niceToString("EmbeddedOnly")}</LinkButton>
            </th>
            <th style={{ textAlign: "center" }}>
              <LinkButton title={undefined} onClick={e => handleHeaderClick(e, "None")} style={{ color: "inherit" }}>{QueryAllowed.niceToString("None")}</LinkButton>
            </th>
            <th style={{ textAlign: "center" }}>
              {AuthAdminMessage.Overriden.niceToString()}
            </th>
          </tr>
        </thead>
        <tbody>
          {ctx.mlistItemCtxs(a => a.rules).orderBy(a => a.value.resource.key).map((c, i) =>
            <tr key={i}>
              <td>
                {getToString(c.value.resource)}
              </td>
              <td style={{ textAlign: "center" }}>
                {renderRadio(c.value, "Allow", "green")}
              </td>
              <td style={{ textAlign: "center" }}>
                {renderRadio(c.value, "EmbeddedOnly", "#FFAD00")}
              </td>
              <td style={{ textAlign: "center" }}>
                {renderRadio(c.value, "None", "red")}
              </td>
              <td style={{ textAlign: "center" }}>
                <GrayCheckbox readOnly={ctx.readOnly} checked={c.value.allowed != c.value.allowedBase} onUnchecked={() => {
                  c.value.allowed = c.value.allowedBase;
                  ctx.value.modified = true;
                  updateFrame();
                }} />
              </td>
            </tr>
          )
          }
        </tbody>
      </AccessibleTable>
    </div>
  );


  function renderRadio(c: QueryAllowedRule, allowed: QueryAllowed, color: string) {

    if (QueryAllowed.index(c.coerced) < QueryAllowed.index(allowed))
      return;

    return <ColorRadio readOnly={ctx.readOnly} checked={c.allowed == allowed} color={color} onClicked={a => {
      c.allowed = allowed;
      c.modified = true;
      updateFrame()
    }} />;
  }
}
