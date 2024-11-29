import * as React from 'react'
import { Button } from 'react-bootstrap'
import { Operations, EntityOperationSettings } from '@framework/Operations'
import { TypeContext, ButtonsContext, IRenderButtons, ButtonBarElement } from '@framework/TypeContext'
import { EntityLine, AutoLine } from '@framework/Lines'
import { getToString } from '@framework/Signum.Entities'
import { AuthAdminClient } from '../AuthAdminClient'
import { OperationRulePack, OperationAllowed, OperationAllowedRule, AuthAdminMessage } from './Signum.Authorization.Rules'
import { ColorRadio, GrayCheckbox } from './ColoredRadios'
import "./AuthAdmin.css"
import { useForceUpdate } from '@framework/Hooks'
import { OperationSymbol } from '@framework/Signum.Operations'
import { GraphExplorer } from '@framework/Reflection'

export default function OperationRulePackControl({ ctx, innerRef }: { ctx: TypeContext<OperationRulePack>; innerRef: React.Ref<IRenderButtons> }): React.JSX.Element {

  function handleSaveClick(bc: ButtonsContext) {
    let pack = ctx.value;

    AuthAdminClient.API.saveOperationRulePack(pack)
      .then(() => AuthAdminClient.API.fetchOperationRulePack(pack.type.cleanName!, pack.role.id!))
      .then(newPack => {
        Operations.notifySuccess();
        bc.frame.onReload({ entity: newPack, canExecute: {} });
      });
  }

  function updateFrame() {
    ctx.frame!.frameComponent.forceUpdate();
  }


  function renderButtons(bc: ButtonsContext): ButtonBarElement[] {
    GraphExplorer.propagateAll(bc.pack.entity);

    const hasChanges = bc.pack.entity.modified;

    return [
      { button: <Button variant="primary" disabled={!hasChanges || ctx.readOnly} onClick={() => handleSaveClick(bc)}>{AuthAdminMessage.Save.niceToString()}</Button> }
    ];
  }

  React.useImperativeHandle(innerRef, () => ({ renderButtons }), [ctx.value])

  function handleRadioClick(e: React.MouseEvent<HTMLAnchorElement>, hc: OperationAllowed) {

    ctx.value.rules.forEach(mle => {
      mle.element.allowed = OperationAllowed.min(hc, mle.element.coerced);
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
      <table className="table table-sm sf-auth-rules">
        <thead>
          <tr>
            <th>
              {OperationSymbol.niceName()}
            </th>
            <th style={{ textAlign: "center" }}>
              <a onClick={e => handleRadioClick(e, "Allow")}>{OperationAllowed.niceToString("Allow")}</a>
            </th>
            <th style={{ textAlign: "center" }}>
              <a onClick={e => handleRadioClick(e, "DBOnly")}>{OperationAllowed.niceToString("DBOnly")}</a>
            </th>
            <th style={{ textAlign: "center" }}>
              <a onClick={e => handleRadioClick(e, "None")}>{OperationAllowed.niceToString("None")}</a>
            </th>
            <th style={{ textAlign: "center" }}>
              {AuthAdminMessage.Overriden.niceToString()}
            </th>
          </tr>
        </thead>
        <tbody>
          {ctx.mlistItemCtxs(a => a.rules).filter(c => {
            var os = Operations.getSettings(c.value.resource!.operation);

            if (os instanceof EntityOperationSettings && os.isVisibleOnlyType && !os.isVisibleOnlyType(ctx.value.type.cleanName))
              return false;

            return true;
          }).map((c, i) =>
            <tr key={i}>
              <td>
                {getToString(c.value.resource!.operation)}
              </td>
              <td style={{ textAlign: "center" }}>
                {renderRadio(c.value, "Allow", "green")}
              </td>
              <td style={{ textAlign: "center" }}>
                {renderRadio(c.value, "DBOnly", "#FFAD00")}
              </td>
              <td style={{ textAlign: "center" }}>
                {renderRadio(c.value, "None", "red")}
              </td>
              <td style={{ textAlign: "center" }}>
                <GrayCheckbox readOnly={c.readOnly} checked={c.value.allowed != c.value.allowedBase} onUnchecked={() => {
                  c.value.allowed = c.value.allowedBase;
                  ctx.value.modified = true;
                  updateFrame();
                }} />
              </td>
            </tr>
          )
          }
        </tbody>
      </table>

    </div>
  );

  function renderRadio(c: OperationAllowedRule, allowed: OperationAllowed, color: string) {

    if (OperationAllowed.index(c.coerced) < OperationAllowed.index(allowed))
      return;

    return <ColorRadio readOnly={ctx.readOnly} checked={c.allowed == allowed} color={color}
      onClicked={a => {
        c.allowed = allowed;
        c.modified = true;
        updateFrame();
      }} />;
  }
}
