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

export default function QueryRulesPackControl({ ctx, innerRef }: { ctx: TypeContext<QueryRulePack>, innerRef: React.Ref<IRenderButtons> }): React.JSX.Element {

  const forceUpdate = useForceUpdate();

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
      { button: <Button variant="primary" disabled={ctx.readOnly} onClick={() => handleSaveClick(bc)}>{AuthAdminMessage.Save.niceToString()}</Button> },
    ];
  }

  React.useImperativeHandle(innerRef, () => ({ renderButtons }), [ctx.value]);

  function handleHeaderClick(e: React.MouseEvent<HTMLAnchorElement>, hc: QueryAllowed) {

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
        <AutoLine ctx={ctx.subCtx(f => f.strategy)} />
        <EntityLine ctx={ctx.subCtx(f => f.type)} />
      </div>
      <table className="table table-sm sf-auth-rules">
        <thead>
          <tr>
            <th>
              {QueryEntity.niceName()}
            </th>
            <th style={{ textAlign: "center" }}>
              <a onClick={e => handleHeaderClick(e, "Allow")}>{QueryAllowed.niceToString("Allow")}</a>
            </th>
            <th style={{ textAlign: "center" }}>
              <a onClick={e => handleHeaderClick(e, "EmbeddedOnly")}>{QueryAllowed.niceToString("EmbeddedOnly")}</a>
            </th>
            <th style={{ textAlign: "center" }}>
              <a onClick={e => handleHeaderClick(e, "None")}>{QueryAllowed.niceToString("None")}</a>
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


  function renderRadio(c: QueryAllowedRule, allowed: QueryAllowed, color: string) {

    if (c.coercedValues.contains(allowed))
      return;

    return <ColorRadio readOnly={ctx.readOnly} checked={c.allowed == allowed} color={color} onClicked={a => { c.allowed = allowed; c.modified = true; forceUpdate() }} />;
  }
}
