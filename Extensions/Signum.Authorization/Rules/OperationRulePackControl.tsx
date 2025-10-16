import * as React from 'react'
import { Button } from 'react-bootstrap'
import { Operations, EntityOperationSettings } from '@framework/Operations'
import { TypeContext, ButtonsContext, IRenderButtons, ButtonBarElement } from '@framework/TypeContext'
import { EntityLine, AutoLine, FormGroup } from '@framework/Lines'
import { getToString, is } from '@framework/Signum.Entities'
import { AuthAdminClient } from '../AuthAdminClient'
import { OperationRulePack, OperationAllowed, OperationAllowedRule, AuthAdminMessage, TypeConditionSymbol, ConditionRuleModel, WithConditionsModel } from './Signum.Authorization.Rules'
import { ColorRadio, GrayCheckbox } from './ColoredRadios'
import "./AuthAdmin.css"
import { OperationSymbol } from '@framework/Signum.Operations'
import { Binding, getOperationInfo, GraphExplorer } from '@framework/Reflection'
import { useDragAndDrop } from './TypeRulePackControl'
import SelectorModal from '../../../Signum/React/SelectorModal';
import { WCAGRow, AccessibleTable } from '../../../Signum/React/Basics/AccessibleTable'



export default function OperationRulePackControl({ ctx, initialTypeConditions, innerRef }: { ctx: TypeContext<OperationRulePack>, initialTypeConditions: TypeConditionSymbol[] | undefined, innerRef: React.Ref<IRenderButtons> }): React.JSX.Element {

  const [typeConditions, setTypeConditions] = React.useState<TypeConditionSymbol[] | undefined>(initialTypeConditions);

  function handleSaveClick(bc: ButtonsContext) {
    let pack = ctx.value;

    AuthAdminClient.API.saveOperationRulePack(pack)
      .then(() => fetchOperationRulePack(pack, bc));
  }

  function handleResetChangesClick(bc: ButtonsContext) {
    fetchOperationRulePack(ctx.value, bc);
  }

  function fetchOperationRulePack(pack: OperationRulePack, bc: ButtonsContext) {
    return AuthAdminClient.API.fetchOperationRulePack(pack.type.cleanName!, pack.role.id!)
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

  function hasOverrides(tcs: TypeConditionSymbol[] | undefined): undefined | "fw-bold" {
    return ctx.value.rules.filter(a => !isConstructor(a.element)).some(r => getBinding(r.element.allowed, tcs).getValue() != getBinding(r.element.allowedBase, tcs).getValue()) ? "fw-bold" : undefined;
  }



  function isConstructor(oar: OperationAllowedRule) {
    return getOperationInfo(oar.resource.operation, ctx.value.type.cleanName).operationType == "Constructor"
  }

  return (
    <div>
      <div className="form-compact">
        <EntityLine ctx={ctx.subCtx(f => f.role)} />
        <AutoLine ctx={ctx.subCtx(f => f.strategy)} />
        <EntityLine ctx={ctx.subCtx(f => f.type)} />
      </div>
      {ctx.value.rules.some(a => isConstructor(a.element)) &&
        <OperationTable ctx={ctx} filter={oar => isConstructor(oar)} typeConditions={undefined} updateFrame={updateFrame} />}

      {ctx.value.rules.some(a => !isConstructor(a.element)) &&
        <div className="form-compact">
          <FormGroup ctx={ctx} label={AuthAdminMessage.TypeConditions.niceToString()}>
            {id =>
              <div id={id}>
                <select id={id} className={hasOverrides(typeConditions)} value={typeConditions?.map(a => a.key).join(" & ")} onChange={e => {
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

                          if (OperationAllowed.index(coercedBinding.getValue()) < OperationAllowed.index(allowed))
                            return;

                          allowedBinding.setValue(allowed);
                        }))
                          .then(f => f())
                          .then(() => updateFrame());
                      });
                  }}>{AuthAdminMessage.CopyFrom.niceToString()}…</button>}
              </div>}
          </FormGroup>
          <OperationTable ctx={ctx} filter={oar => !isConstructor(oar)} typeConditions={typeConditions} updateFrame={updateFrame} />
        </div>
      }
    </div>
  );
}

function OperationTable(p: {
  ctx: TypeContext<OperationRulePack>,
  typeConditions: TypeConditionSymbol[] | undefined,
  filter: (operation: OperationAllowedRule) => boolean,
  updateFrame: () => void
}) {

  function handleHeaderClick(e: React.MouseEvent<HTMLAnchorElement>, hc: OperationAllowed) {

    p.ctx.value.rules.filter(a => p.filter(a.element)).forEach(mle => {
      const value = OperationAllowed.min(getBinding(mle.element.coerced, p.typeConditions).getValue(), hc);
      getBinding(mle.element.allowed, p.typeConditions).setValue(value);
      mle.element.modified = true;
    });

    p.updateFrame();
  }

  return (
    <AccessibleTable
      caption={AuthAdminMessage.AuthRuleOverview.niceToString()}
      className="table table-sm sf-auth-rules"
      mapCustomComponents={new Map([[OperationRow, "tr"]])}
      multiselectable={false}>
      <thead>
        <tr>
          <th style={{ width: "50%" }}>
            {OperationSymbol.niceName()}
          </th>
          <th style={{ textAlign: "center" }}>
            <a onClick={e => handleHeaderClick(e, "Allow")}>{OperationAllowed.niceToString("Allow")}</a>
          </th>
          <th style={{ textAlign: "center" }}>
            <a onClick={e => handleHeaderClick(e, "DBOnly")}>{OperationAllowed.niceToString("DBOnly")}</a>
          </th>
          <th style={{ textAlign: "center" }}>
            <a onClick={e => handleHeaderClick(e, "None")}>{OperationAllowed.niceToString("None")}</a>
          </th>
          <th style={{ textAlign: "center" }}>
            {AuthAdminMessage.Overriden.niceToString()}
          </th>
        </tr>
      </thead>
      <tbody>
        {p.ctx.mlistItemCtxs(a => a.rules).filter(a => p.filter(a.value)).map((tctx, i) => <OperationRow key={i} tctx={tctx} updateFrame={p.updateFrame} getBidning={tac => getBinding(tac, p.typeConditions)} />)}
      </tbody>
    </AccessibleTable>
  )
}

function OperationRow(p: { tctx: TypeContext<OperationAllowedRule>, updateFrame: () => void, getBidning: (e: WithConditionsModel<OperationAllowed>) => Binding<OperationAllowed> }): React.JSX.Element {
  const getConfig = useDragAndDrop(p.tctx.value.allowed.conditionRules, () => p.updateFrame(), () => { p.tctx.value.modified = true; p.updateFrame(); });

  const allowedBinding = p.getBidning(p.tctx.value.allowed);
  const allowedBaseBinding = p.getBidning(p.tctx.value.allowedBase);
  const coercedBinding = p.getBidning(p.tctx.value.coerced);

  function renderRadio(allowed: OperationAllowed, color: string) {
    if (OperationAllowed.index(coercedBinding.getValue()) < OperationAllowed.index(allowed))
      return;

    return <ColorRadio
      readOnly={p.tctx.readOnly}
      checked={allowedBinding.getValue() == allowed}
      color={color}
      onClicked={e => {
        allowedBinding.setValue(allowed);
        p.updateFrame();
      }}
    />;
  }

  return (
    <WCAGRow>
      <td>
        {getToString(p.tctx.value.resource.operation)}
      </td>
      <td style={{ textAlign: "center" }}>
        {renderRadio("Allow", "green")}
      </td>
      <td style={{ textAlign: "center" }}>
        {renderRadio("DBOnly", "#FFAD00")}
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
    </WCAGRow>
  )
}


function matches(r: ConditionRuleModel<OperationAllowed>, typeConditions: TypeConditionSymbol[]) {
  return r.typeConditions.length == typeConditions.length && r.typeConditions.every((tc, i) => is(tc.element, typeConditions[i]));
}

function getBinding(pac: WithConditionsModel<OperationAllowed>, typeConditions: TypeConditionSymbol[] | undefined): Binding<OperationAllowed> {
  if (typeConditions == undefined)
    return new Binding(pac, "fallback");
  else {
    var cr = pac.conditionRules.single(a => matches(a.element, typeConditions)).element;
    return new Binding(cr, "allowed");
  }
}
