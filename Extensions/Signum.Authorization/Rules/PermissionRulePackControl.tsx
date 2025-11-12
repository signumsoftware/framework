import * as React from 'react'
import { Button } from 'react-bootstrap'
import { Operations } from '@framework/Operations'
import { TypeContext, ButtonsContext, IRenderButtons } from '@framework/TypeContext'
import { EntityLine, AutoLine } from '@framework/Lines'
import { Finder } from '@framework/Finder'

import { AuthAdminClient } from '../AuthAdminClient'
import { PermissionRulePack, PermissionAllowedRule, AuthAdminMessage } from './Signum.Authorization.Rules'
import { ColorRadio, GrayCheckbox } from './ColoredRadios'

import "./AuthAdmin.css"
import { GraphExplorer } from '@framework/Reflection'
import { RoleEntity } from '../Signum.Authorization'
import { AccessibleTable } from '../../../Signum/React/Basics/AccessibleTable'

export default function PermissionRulesPackControl(p: { ctx: TypeContext<PermissionRulePack>, innerRef: React.Ref<IRenderButtons> }): React.JSX.Element {

  function renderButtons(bc: ButtonsContext) {

    const hasChanges = GraphExplorer.hasChanges(bc.pack.entity); 

    return [
      { button: <Button type="button" variant="primary" disabled={!hasChanges || p.ctx.readOnly} onClick={() => handleSaveClick(bc)}>{AuthAdminMessage.Save.niceToString()}</Button> },
      { button: <Button type="button" variant="warning" disabled={!hasChanges || p.ctx.readOnly} onClick={() => handleResetChangesClick(bc)}>{AuthAdminMessage.ResetChanges.niceToString()}</Button> },
      { button: <Button type="button" variant="info" disabled={hasChanges} onClick={() => handleSwitchToClick(bc)}>{AuthAdminMessage.SwitchTo.niceToString()}</Button> }
    ];
  }

  function handleSaveClick(bc: ButtonsContext) {
    let pack = p.ctx.value;

    AuthAdminClient.API.savePermissionRulePack(pack)
      .then(() => AuthAdminClient.API.fetchPermissionRulePack(pack.role.id!))
      .then(newPack => {
        Operations.notifySuccess();
        bc.frame.onReload({ entity: newPack, canExecute: {} });
      });
  }

  function handleResetChangesClick(bc: ButtonsContext) {
    let pack = ctx.value;

    AuthAdminClient.API.fetchPermissionRulePack(pack.role.id!)
      .then(newPack => { bc.frame.onReload({ entity: newPack, canExecute: {} }); });
  }

  function handleSwitchToClick(bc: ButtonsContext) {

    Finder.find(RoleEntity).then(r => {
      if (!r)
        return;

      AuthAdminClient.API.fetchPermissionRulePack(r.id!)
        .then(newPack => bc.frame.onReload({ entity: newPack, canExecute: {} }));
    });
  }

  const [filter, setFilter] = React.useState("");

  React.useImperativeHandle(p.innerRef, () => ({ renderButtons }), [p.ctx.value])

  function updateFrame() {
    ctx.frame!.frameComponent.forceUpdate();
  }

  function handleSetFilter(e: React.FormEvent<any>) {
    setFilter((e.currentTarget as HTMLInputElement).value);
  }

  const parts = filter.match(/(!?\w+)/g);

  function isMatch(rule: PermissionAllowedRule): boolean {

    if (!parts || parts.length == 0)
      return true;

    for (let i = parts.length - 1; i >= 0; i--) {
      const p = parts[i];

      if (p.startsWith("!")) {
        if ("overriden".startsWith(p.after("!")) && rule.allowed != rule.allowedBase)
          return true;
      }

      if (rule.resource.key.toLowerCase().contains(p.toLowerCase()))
        return true;
    }

    return false;
  };

  let ctx = p.ctx;

  return (
    <div>
      <div className="form-compact">
        <EntityLine ctx={ctx.subCtx(f => f.role)} />
        <AutoLine ctx={ctx.subCtx(f => f.strategy)} />
      </div>
      <AccessibleTable
        aria-label={AuthAdminMessage.PermissionRulesOverview.niceToString()}
        className="table table-sm sf-auth-rules">
        <thead>
          <tr>
            <th>
              <div style={{ marginBottom: "-2px" }}>
                <input
                  type="text"
                  className="form-control form-control-sm"
                  id="filter"
                  placeholder={AuthAdminMessage.PermissionOverriden.niceToString()}
                  value={filter}
                  onChange={handleSetFilter}
                />
              </div>
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
          {ctx.mlistItemCtxs(a => a.rules)
            .filter(a => isMatch(a.value))
            .orderBy(a => a.value.resource.key).map((c, i) =>
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
                  <GrayCheckbox readOnly={ctx.readOnly} checked={c.value.allowed != c.value.allowedBase} onUnchecked={() => {
                    c.value.allowed = c.value.allowedBase;
                    ctx.value.modified = true;
                    updateFrame();
                  }} />
                </td>
              </tr>
            )}
        </tbody>
      </AccessibleTable>
    </div>
  );

  function renderRadio(c: PermissionAllowedRule, allowed: boolean, color: string) {
    return <ColorRadio readOnly={ctx.readOnly} checked={c.allowed == allowed} color={color} onClicked={a => {
      c.allowed = allowed;
      c.modified = true;
      updateFrame()
    }} />;
  }
}
