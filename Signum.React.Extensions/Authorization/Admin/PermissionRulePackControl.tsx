import * as React from 'react'
import { Button } from 'react-bootstrap'
import { notifySuccess } from '@framework/Operations'
import { TypeContext, ButtonsContext, IRenderButtons } from '@framework/TypeContext'
import { EntityLine, ValueLine } from '@framework/Lines'
import { API } from '../AuthClient'
import { PermissionRulePack, PermissionAllowedRule, AuthAdminMessage, PermissionSymbol, AuthMessage } from '../Signum.Entities.Authorization'
import { ColorRadio, GrayCheckbox } from './ColoredRadios'

import "./AuthAdmin.css"
import { useForceUpdate } from '../../../../Framework/Signum.React/Scripts/Hooks'

export default React.forwardRef(function PermissionRulesPackControl(p: { ctx: TypeContext<PermissionRulePack> }, ref: React.Ref<IRenderButtons>) {

  function handleSaveClick(bc: ButtonsContext) {
    let pack = p.ctx.value;

    API.savePermissionRulePack(pack)
      .then(() => API.fetchPermissionRulePack(pack.role.id!))
      .then(newPack => {
        notifySuccess();
        bc.frame.onReload({ entity: newPack, canExecute: {} });
      })
      .done();
  }

  function renderButtons(bc: ButtonsContext) {
    return [
      { button: <Button variant="primary" onClick={() => handleSaveClick(bc)}>{AuthMessage.Save.niceToString()}</Button> }
    ];
  }

  const forceUpdate = useForceUpdate();

  React.useImperativeHandle(ref, () => ({ renderButtons }), [p.ctx.value])

  let ctx = p.ctx;

  return (
    <div>
      <div className="form-compact">
        <EntityLine ctx={ctx.subCtx(f => f.role)} />
        <ValueLine ctx={ctx.subCtx(f => f.strategy)} />
      </div>
      <table className="table table-sm sf-auth-rules">
        <thead>
          <tr>
            <th>
              {PermissionSymbol.niceName()}
            </th>
            <th style={{ textAlign: "center" }}>
              {AuthAdminMessage.Allow.niceToString()}
            </th>
            <th style={{ textAlign: "center" }}>
              {AuthAdminMessage.Deny.niceToString()}
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
                {c.value.resource.key}
              </td>
              <td style={{ textAlign: "center" }}>
                {renderRadio(c.value, true, "green")}
              </td>
              <td style={{ textAlign: "center" }}>
                {renderRadio(c.value, false, "red")}
              </td>
              <td style={{ textAlign: "center" }}>
                <GrayCheckbox checked={c.value.allowed != c.value.allowedBase} onUnchecked={() => {
                  c.value.allowed = c.value.allowedBase;
                  ctx.value.modified = true;
                  forceUpdate();
                }} />
              </td>
            </tr>
          )}
        </tbody>
      </table>

    </div>
  );

  function renderRadio(c: PermissionAllowedRule, allowed: boolean, color: string) {
    return <ColorRadio checked={c.allowed == allowed} color={color} onClicked={a => { c.allowed = allowed; c.modified = true; forceUpdate() }} />;
  }
});
