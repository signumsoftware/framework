import * as React from 'react'
import { Button } from 'react-bootstrap';
import { PropertyRouteEntity } from '@framework/Signum.Basics';
import { Operations } from '@framework/Operations'
import { TypeContext, ButtonsContext, IRenderButtons } from '@framework/TypeContext'
import { EntityLine, AutoLine, Binding, FormGroup } from '@framework/Lines'
import { AuthAdminClient } from '../AuthAdminClient'
import { PropertyRulePack, PropertyAllowedRule, PropertyAllowed, AuthAdminMessage, TypeConditionSymbol, WithConditionsModel, ConditionRuleModel } from './Signum.Authorization.Rules'
import { ColorRadio, GrayCheckbox } from './ColoredRadios'
import "./AuthAdmin.css"
import { is } from '@framework/Signum.Entities';
import { useDragAndDrop } from './TypeRulePackControl';
import { GraphExplorer } from '@framework/Reflection';
import SelectorModal from '../../../Signum/React/SelectorModal';
import { AccessibleTable, AccessibleRow } from '../../../Signum/React/Basics/AccessibleTable';

export default function PropertyRulesPackControl({ ctx, initialTypeConditions, innerRef }: { ctx: TypeContext<PropertyRulePack>, initialTypeConditions: TypeConditionSymbol[] | undefined, innerRef?: React.Ref<IRenderButtons> }): React.JSX.Element {


  const [typeConditions, setTypeConditions] = React.useState<TypeConditionSymbol[] | undefined>(initialTypeConditions);

  function handleSaveClick(bc: ButtonsContext) {
    let pack = ctx.value;

    AuthAdminClient.API.savePropertyRulePack(pack)
      .then(() => fetchPropertyRulePack(pack, bc));
  }

  function handleResetChangesClick(bc: ButtonsContext) {
    fetchPropertyRulePack(ctx.value, bc);
  }

  function fetchPropertyRulePack(pack: PropertyRulePack, bc: ButtonsContext) {
    return AuthAdminClient.API.fetchPropertyRulePack(pack.type.cleanName!, pack.role.id!)
      .then(newPack => {
        Operations.notifySuccess();
        bc.frame.onReload({ entity: newPack, canExecute: {} });
      });
  }

  function renderButtons(bc: ButtonsContext) {

    const hasChanges = GraphExplorer.hasChanges(bc.pack.entity); 

    return [
      { button: <Button variant="primary" disabled={!hasChanges || ctx.readOnly} onClick={() => handleSaveClick(bc)}>{AuthAdminMessage.Save.niceToString()}</Button> },
      { button: <Button variant="warning" disabled={!hasChanges || ctx.readOnly} onClick={() => handleResetChangesClick(bc)}>{AuthAdminMessage.ResetChanges.niceToString()}</Button> }
    ];
  }

  React.useImperativeHandle(innerRef, () => ({ renderButtons }), [ctx.value]);

  function updateFrame() {
    ctx.frame!.frameComponent.forceUpdate();
  }


  function handleHeaderClick(e: React.MouseEvent<HTMLAnchorElement>, hc: PropertyAllowed) {

    ctx.value.rules.forEach(mle => {
      const value = PropertyAllowed.min(getBinding(mle.element.coerced, typeConditions).getValue(), hc);
      getBinding(mle.element.allowed, typeConditions).setValue(value);
      mle.element.modified = true;
    });

    updateFrame();
  }

  function hasOverrides(tcs: TypeConditionSymbol[] | undefined): undefined | "fw-bold" {
    return ctx.value.rules.some(r => getBinding(r.element.allowed, tcs).getValue() != getBinding(r.element.allowedBase, tcs).getValue()) ? "fw-bold" : undefined;
  }

  return (
    <div>
      <div className="form-compact">
        <EntityLine ctx={ctx.subCtx(f => f.role)} />
        <AutoLine ctx={ctx.subCtx(f => f.strategy)} />
        <EntityLine ctx={ctx.subCtx(f => f.type)} />
        <FormGroup ctx={ctx} label={AuthAdminMessage.TypeConditions.niceToString()}>
          {id =>
            <div id={id}>
              <select className={hasOverrides(typeConditions)} value={typeConditions?.map(a => a.key).join(" & ")} onChange={e => {
                if (e.currentTarget.value == "Fallback")
                  setTypeConditions(undefined);
                else {
                  var tcs = ctx.value.availableTypeConditions.single(arr => arr.map(a => a.key).join(" & ") == e.currentTarget.value);
                  setTypeConditions(tcs);
                }
              }} >
                <option value="Fallback" className={hasOverrides(undefined)}>{AuthAdminMessage.Fallback.niceToString()}</option>
                {ctx.value.availableTypeConditions.map((arr, i) => <option value={arr.map(a => a.key).join(" & ")} className={hasOverrides(arr)}>
                  {arr.map(a => a.key.after(".")).join(" & ")}
                </option>)}
              </select>

              {ctx.value.availableTypeConditions.length > 1 && 
                <button className="btn btn-xs btn-primary mx-1" onClick={e => {
                  var options = ["Fallback", ...ctx.value.availableTypeConditions.map(a => a.map(a => a.key).join(" & "))]
                    .filter(o => o != (typeConditions?.map(a => a.key).join(" & ") ?? "Fallback"))
                    .map(o => o.tryAfter(".") ?? o);

                  SelectorModal.chooseElement(options)
                    .then(option => {
                      if (!option)
                        return;

                      var tcs = option == "Fallback" ? undefined : ctx.value.availableTypeConditions.single(arr => arr.map(a => a.key.after(".")).join(" & ") == option);
                      Promise.resolve(() => ctx.mlistItemCtxs(a => a.rules).forEach(tctx => {
                        const allowedBinding = getBinding(tctx.value.allowed, typeConditions);
                        const coercedBinding = getBinding(tctx.value.coerced, typeConditions);
                        const allowed = getBinding(tctx.value.allowed, tcs).getValue();

                        if (PropertyAllowed.index(coercedBinding.getValue()) < PropertyAllowed.index(allowed))
                          return;

                        allowedBinding.setValue(allowed);
                      }))
                        .then(f => f())
                        .then(() => updateFrame());
                    });
                }}>{AuthAdminMessage.CopyFrom.niceToString()}…</button>}
            </div>}
        </FormGroup>
      </div>
      <AccessibleTable
        caption={AuthAdminMessage.PropertyRuleOverview.niceToString()}
        className="table table-sm sf-auth-rules "
        mapCustomComponents={new Map([[PropertyRow, "tr"]])}
        multiselectable={false}>
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
          {ctx.mlistItemCtxs(a => a.rules).map((tctx, i) => <PropertyRow key={i} tctx={tctx} updateFrame={updateFrame} getBidning={tac => getBinding(tac, typeConditions)} />)}
        </tbody>
      </AccessibleTable>
    </div>
  );
}

function PropertyRow(p: { tctx: TypeContext<PropertyAllowedRule>, updateFrame: () => void, getBidning: (e: WithConditionsModel<PropertyAllowed>) => Binding<PropertyAllowed> }): React.JSX.Element {
  
  const getConfig = useDragAndDrop(p.tctx.value.allowed.conditionRules, () => p.updateFrame(), () => { p.tctx.value.modified = true; p.updateFrame(); });

  const allowedBinding = p.getBidning(p.tctx.value.allowed);
  const allowedBaseBinding = p.getBidning(p.tctx.value.allowedBase);
  const coercedBinding = p.getBidning(p.tctx.value.coerced);

  function renderRadio(allowed: PropertyAllowed, color: string) {
    if (PropertyAllowed.index(coercedBinding.getValue()) < PropertyAllowed.index(allowed))
      return null;

    return (
      <ColorRadio
        readOnly={p.tctx.readOnly}
        checked={allowedBinding.getValue() == allowed}
        color={color}
        onClicked={e => {
          allowedBinding.setValue(allowed);
          p.updateFrame();
        }}
      />
    );
  }

  return (
    <AccessibleRow>
      <td>
        {p.tctx.value.resource.path}
      </td>
      <td style={{ textAlign: "center" }}>
        {renderRadio("Write", "green")}
      </td>
      <td style={{ textAlign: "center" }}>
        {renderRadio("Read", "#FFAD00")}
      </td>
      <td style={{ textAlign: "center" }}>
        {renderRadio("None", "red")}
      </td>
      <td style={{ textAlign: "center" }}>
        <GrayCheckbox readOnly={p.tctx.readOnly} checked={allowedBinding.getValue() != allowedBaseBinding.getValue()} onUnchecked={() => {
          allowedBinding.setValue(allowedBaseBinding.getValue());
          p.updateFrame();
        }} />
      </td>
    </AccessibleRow>
  )
}



function matches(r: ConditionRuleModel<PropertyAllowed>, typeConditions: TypeConditionSymbol[]) {
  return r.typeConditions.length == typeConditions.length && r.typeConditions.every((tc, i) => is(tc.element, typeConditions[i]));
}

function getBinding(pac: WithConditionsModel<PropertyAllowed>, typeConditions: TypeConditionSymbol[] | undefined): Binding<PropertyAllowed> {
  if (typeConditions == undefined)
    return new Binding(pac, "fallback");
  else {
    var cr = pac.conditionRules.single(a => matches(a.element, typeConditions)).element;
    return new Binding(cr, "allowed");
  }
}
