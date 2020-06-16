import * as React from 'react'
import { Button } from 'react-bootstrap';
import { PropertyRouteEntity } from '@framework/Signum.Entities.Basics';
import { notifySuccess } from '@framework/Operations'
import { TypeContext, ButtonsContext, IRenderButtons } from '@framework/TypeContext'
import { EntityLine, ValueLine } from '@framework/Lines'
import { API } from '../AuthClient'
import { PropertyRulePack, PropertyAllowedRule, PropertyAllowed, AuthAdminMessage, AuthMessage } from '../Signum.Entities.Authorization'
import { ColorRadio, GrayCheckbox } from './ColoredRadios'
import "./AuthAdmin.css"
import { useForceUpdate } from '../../../../Framework/Signum.React/Scripts/Hooks';

export default React.forwardRef(function PropertyRulesPackControl({ ctx }: { ctx: TypeContext<PropertyRulePack> }, ref: React.Ref<IRenderButtons>) {

  function handleSaveClick(bc: ButtonsContext) {
    let pack = ctx.value;

    API.savePropertyRulePack(pack)
      .then(() => API.fetchPropertyRulePack(pack.type.cleanName!, pack.role.id!))
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

  React.useImperativeHandle(ref, () => ({ renderButtons }), [ctx.value]);
  const forceUpdate = useForceUpdate();

  function handleHeaderClick(e: React.MouseEvent<HTMLAnchorElement>, hc: PropertyAllowed) {

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
              {PropertyRouteEntity.niceName()}
            </th>
            <th style={{ textAlign: "center" }}>
              <a onClick={e => handleHeaderClick(e, "Write")}>{PropertyAllowed.niceToString("Write")}</a>
            </th>
            <th style={{ textAlign: "center" }}>
              <a onClick={e => handleHeaderClick(e, "Read")}>{PropertyAllowed.niceToString("Read")}</a>
            </th>
            <th style={{ textAlign: "center" }}>
              <a onClick={e => handleHeaderClick(e, "None")}>{PropertyAllowed.niceToString("None")}</a>
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
                {c.value.resource.path}
              </td>
              <td style={{ textAlign: "center" }}>
                {renderRadio(c.value, "Write", "green")}
              </td>
              <td style={{ textAlign: "center" }}>
                {renderRadio(c.value, "Read", "#FFAD00")}
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

  function renderRadio(c: PropertyAllowedRule, allowed: PropertyAllowed, color: string) {

    if (c.coercedValues!.contains(allowed))
      return;

    return <ColorRadio
      checked={c.allowed == allowed}
      color={color}
      onClicked={a => { c.allowed = allowed; c.modified = true; forceUpdate() }}
    />;
  }
});
