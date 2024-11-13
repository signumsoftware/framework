import * as React from 'react'
import { Button } from 'react-bootstrap';
import { PropertyRouteEntity } from '@framework/Signum.Basics';
import { Operations } from '@framework/Operations'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { TypeContext, ButtonsContext, IRenderButtons } from '@framework/TypeContext'
import { EntityLine, AutoLine, EntityBaseController, Binding } from '@framework/Lines'
import { AuthAdminClient } from '../AuthAdminClient'
import { PropertyRulePack, PropertyAllowedRule, PropertyAllowed, AuthAdminMessage, WithConditions, ConditionRule } from './Signum.Authorization.Rules'
import { ColorRadio, GrayCheckbox } from './ColoredRadios'
import "./AuthAdmin.css"
import { useForceUpdate } from '@framework/Hooks';
import { RoleEntity } from '../Signum.Authorization';
import { getToString, Lite, newMListElement } from '@framework/Signum.Entities';
import { getRuleTitle, addConditionClick, useDragAndDrop } from './TypeRulePackControl';
import { GraphExplorer } from '@framework/Reflection';

export default function PropertyRulesPackControl({ ctx, innerRef }: { ctx: TypeContext<PropertyRulePack>, innerRef?: React.Ref<IRenderButtons> }): React.JSX.Element {

  
 
  function handleSaveClick(bc: ButtonsContext) {
    let pack = ctx.value;

    AuthAdminClient.API.savePropertyRulePack(pack)
      .then(() => AuthAdminClient.API.fetchPropertyRulePack(pack.type.cleanName!, pack.role.id!))
      .then(newPack => {
        Operations.notifySuccess();
        bc.frame.onReload({ entity: newPack, canExecute: {} });
      });
  }

  function renderButtons(bc: ButtonsContext) {
    GraphExplorer.propagateAll(bc.pack.entity);

    const hasChanges = bc.pack.entity.modified;

    return [
      { button: <Button variant="primary" disabled={!hasChanges || ctx.readOnly} onClick={() => handleSaveClick(bc)}>{AuthAdminMessage.Save.niceToString()}</Button> }
    ];
  }

  React.useImperativeHandle(innerRef, () => ({ renderButtons }), [ctx.value]);

  function updateFrame() {
    ctx.frame!.frameComponent.forceUpdate();
  }


  function handleHeaderClick(e: React.MouseEvent<HTMLAnchorElement>, hc: PropertyAllowed) {

    ctx.value.rules.forEach(mle => {
      mle.element.allowed = reduceCoerced(mle.element.coerced, hc);
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
          {ctx.mlistItemCtxs(a => a.rules).map((tctx, i) => <PropertyRow key={i} tctx={tctx} updateFrame={updateFrame} />)}
        </tbody>
      </table>

    </div>
  );


}

function max(p: WithConditions<PropertyAllowed>) {
  return [p.fallback, ...p.conditionRules.map(a => a.element.allowed)].maxBy(a => PropertyAllowed.index(a));
}

function reduceCoerced(coerced: WithConditions<PropertyAllowed>, value: PropertyAllowed) {
  return WithConditions(PropertyAllowed).New({
    fallback: PropertyAllowed.min(coerced.fallback, value),
    conditionRules: coerced.conditionRules.map(cr => newMListElement(ConditionRule(PropertyAllowed).New({
      allowed: PropertyAllowed.min(cr.element.allowed, value),
      typeConditions: JSON.parse(JSON.stringify(cr.element.typeConditions))
    })))
  });
}

function PropertyRow(p: { tctx: TypeContext<PropertyAllowedRule>, updateFrame: () => void }): React.JSX.Element {

  const getConfig = useDragAndDrop(p.tctx.value.allowed.conditionRules, () => p.updateFrame(), () => { p.tctx.value.modified = true; p.updateFrame(); });

  const rule = p.tctx.value;

  function renderRadio(b: Binding<PropertyAllowed | null>, allowed: PropertyAllowed, color: string) {

    if (rule.coerced.fallback == allowed)
      return;

    return <ColorRadio
      readOnly={p.tctx.readOnly}
      checked={b.getValue() == allowed}
      color={color}
      onClicked={e => { b.setValue(allowed); p.updateFrame(); }}
    />;
  }

  function handleRemoveConditionClick(taac: WithConditions<PropertyAllowed>, con: ConditionRule<PropertyAllowed>) {
    taac.conditionRules.remove(taac.conditionRules.single(mle => mle.element == con));
    taac.modified = true;
    p.updateFrame();
  }

  const conditions = rule.availableConditions;
  let fallback = Binding.create(rule.allowed, a => a.fallback);
  return (
      <tr>
        <td>
          {
            conditions.length > 1 || conditions.length == 1 && rule.allowed.conditionRules.length == 0 ?
              <a className="sf-condition-icon" href="#" title={AuthAdminMessage.AddCondition.niceToString()} onClick={async e => {
                e.preventDefault();
                await addConditionClick(PropertyAllowed, conditions, rule.allowed, rule.resource.rootType);
                p.updateFrame();
              }}>
                <FontAwesomeIcon icon="circle-plus" className="me-2" />
              </a> :
              <FontAwesomeIcon icon="circle" className="sf-placeholder-icon me-2"></FontAwesomeIcon>
          }
          {rule.resource.path}
          {rule.allowed.conditionRules.length > 0 && <small className="shy-text ms-1">{AuthAdminMessage.Fallback.niceToString()}</small>}
        </td>
        <td style={{ textAlign: "center" }}>
          {renderRadio(fallback, "Write", "green")}
        </td>
        <td style={{ textAlign: "center" }}>
          {renderRadio(fallback, "Read", "#FFAD00")}
        </td>
        <td style={{ textAlign: "center" }}>
          {renderRadio(fallback, "None", "red")}
        </td>
        <td style={{ textAlign: "center" }}>
          <GrayCheckbox readOnly={p.tctx.readOnly} checked={rule.allowed != rule.allowedBase} onUnchecked={() => {
            rule.allowed = JSON.parse(JSON.stringify(rule.allowedBase));
            rule.modified = true;
            p.updateFrame();
          }} />
        </td>
      </tr>
  )
}


