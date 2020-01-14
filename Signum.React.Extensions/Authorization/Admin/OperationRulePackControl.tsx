import * as React from 'react'
import { Button } from 'react-bootstrap'
import { notifySuccess } from '@framework/Operations'
import { TypeContext, ButtonsContext, IRenderButtons, ButtonBarElement } from '@framework/TypeContext'
import { EntityLine, ValueLine } from '@framework/Lines'
import { OperationSymbol } from '@framework/Signum.Entities'
import { API } from '../AuthClient'
import { OperationRulePack, OperationAllowed, OperationAllowedRule, AuthAdminMessage, PermissionSymbol, AuthMessage } from '../Signum.Entities.Authorization'
import { ColorRadio, GrayCheckbox } from './ColoredRadios'
import "./AuthAdmin.css"
import { useForceUpdate } from '../../../../Framework/Signum.React/Scripts/Hooks'

export default React.forwardRef(function OperationRulePackControl({ ctx }: { ctx: TypeContext<OperationRulePack> }, ref: React.Ref<IRenderButtons>) {

  function handleSaveClick(bc: ButtonsContext) {
    let pack = ctx.value;

    API.saveOperationRulePack(pack)
      .then(() => API.fetchOperationRulePack(pack.type.cleanName!, pack.role.id!))
      .then(newPack => {
        notifySuccess();
        bc.frame.onReload({ entity: newPack, canExecute: {} });
      })
      .done();
  }

  const forceUpdate = useForceUpdate();

  function renderButtons(bc: ButtonsContext): ButtonBarElement[] {
    return [
      { button: <Button variant="primary" onClick={() => handleSaveClick(bc)}>{AuthMessage.Save.niceToString()}</Button> }
    ];
  }

  React.useImperativeHandle(ref, () => ({ renderButtons }), [ctx.value])

  function handleRadioClick(e: React.MouseEvent<HTMLAnchorElement>, hc: OperationAllowed) {

    ctx.value.rules.forEach(mle => {
      if (!mle.element.coercedValues!.contains(hc)) {
        mle.element.allowed = hc;
        mle.element.modified = true;
      }
    });

    forceUpdate();
  }

  return (
    <div>
      <div className="form-compact">
        <EntityLine ctx={ctx.subCtx(f => f.role)} />
        <ValueLine ctx={ctx.subCtx(f => f.strategy)} />
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
          {ctx.mlistItemCtxs(a => a.rules).map((c, i) =>
            <tr key={i}>
              <td>
                {c.value.resource!.operation!.toStr}
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
                <GrayCheckbox checked={c.value.allowed != c.value.allowedBase} onUnchecked={() => {
                  c.value.allowed = c.value.allowedBase;
                  ctx.value.modified = true;
                  forceUpdate();
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

    if (c.coercedValues!.contains(allowed))
      return;

    return <ColorRadio checked={c.allowed == allowed} color={color} onClicked={a => { c.allowed = allowed; c.modified = true; forceUpdate() }} />;
  }
});
