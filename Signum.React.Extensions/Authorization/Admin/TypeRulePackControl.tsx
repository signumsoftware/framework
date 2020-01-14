import * as React from 'react'
import { Button } from 'react-bootstrap'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { IconProp } from '@fortawesome/fontawesome-svg-core'
import { classes } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import { notifySuccess } from '@framework/Operations'
import { TypeContext, ButtonsContext, IRenderButtons, EntityFrame, ButtonBarElement } from '@framework/TypeContext'
import { EntityLine, ValueLine } from '@framework/Lines'
import SelectorModal from '@framework/SelectorModal'
import MessageModal from '@framework/Modals/MessageModal'

import { getTypeInfo, Binding, GraphExplorer } from '@framework/Reflection'
import { OperationSymbol, ModelEntity, newMListElement, NormalControlMessage } from '@framework/Signum.Entities'
import { API, properties, queries, operations } from '../AuthClient'
import {
  TypeRulePack, AuthAdminMessage, PermissionSymbol, AuthMessage, TypeAllowed, TypeAllowedRule,
  TypeAllowedAndConditions, TypeAllowedBasic, TypeConditionRuleEmbedded, AuthThumbnail, PropertyRulePack, OperationRulePack, QueryRulePack, RoleEntity
} from '../Signum.Entities.Authorization'
import { ColorRadio, GrayCheckbox } from './ColoredRadios'
import { TypeConditionSymbol } from '../../Basics/Signum.Entities.Basics'
import { QueryEntity, PropertyRouteEntity } from '@framework/Signum.Entities.Basics'


import "./AuthAdmin.css"
import { is } from '@framework/Signum.Entities';
import { useForceUpdate } from '../../../../Framework/Signum.React/Scripts/Hooks'

export default React.forwardRef(function TypesRulesPackControl({ ctx }: { ctx: TypeContext<TypeRulePack> }, ref: React.Ref<IRenderButtons>) {

  const [filter, setFilter] = React.useState("");

  function renderButtons(bc: ButtonsContext): ButtonBarElement[] {

    GraphExplorer.propagateAll(bc.pack.entity);

    const hasChanges = bc.pack.entity.modified;

    return [
      { button: <Button variant="primary" disabled={!hasChanges} onClick={() => handleSaveClick(bc)}>{AuthMessage.Save.niceToString()}</Button> },
      { button: <Button variant="warning" disabled={!hasChanges} onClick={() => handleResetChangesClick(bc)}>{AuthAdminMessage.ResetChanges.niceToString()}</Button> },
      { button: <Button variant="info" disabled={hasChanges} onClick={() => handleSwitchToClick(bc)}>{AuthAdminMessage.SwitchTo.niceToString()}</Button> }
    ];
  }

  React.useImperativeHandle(ref, () => ({ renderButtons }), [ctx.value])
  const forceUpdate = useForceUpdate();


  function handleSaveClick(bc: ButtonsContext) {
    let pack = ctx.value;

    API.saveTypeRulePack(pack)
      .then(() => API.fetchTypeRulePack(pack.role.id!))
      .then(newPack => {
        notifySuccess();
        bc.frame.onReload({ entity: newPack, canExecute: {} });
      })
      .done();
  }

  function handleResetChangesClick(bc: ButtonsContext) {
    let pack = ctx.value;

    API.fetchTypeRulePack(pack.role.id!)
      .then(newPack => {
        bc.frame.onReload({ entity: newPack, canExecute: {} });
      })
      .done();
  }

  function handleSwitchToClick(bc: ButtonsContext) {
    let pack = ctx.value;

    Finder.find(RoleEntity).then(r => {
      if (!r)
        return;

      API.fetchTypeRulePack(r.id!)
        .then(newPack => bc.frame.onReload({ entity: newPack, canExecute: {} }))
        .done();
    });
  }


  function updateFrame() {
    ctx.frame!.frameComponent.forceUpdate();
  }


  function handleSetFilter(e: React.FormEvent<any>) {
    setFilter((e.currentTarget as HTMLInputElement).value);
  }

  const parts = filter.match(/[+-]?((!?\w+)|\*)/g);

  function isMatch(rule: TypeAllowedRule): boolean {

    if (!parts || parts.length == 0)
      return true;

    const array = [
      rule.resource.namespace,
      rule.resource.cleanName,
      getTypeInfo(rule.resource.cleanName).niceName
    ];


    const str = array.join("|");

    for (let i = parts.length - 1; i >= 0; i--) {
      const p = parts[i];
      const pair = p.startsWith("+") ? { isPositive: true, token: p.after("+") } :
        p.startsWith("-") ? { isPositive: false, token: p.after("-") } :
          { isPositive: true, token: p };

      if (pair.token == "*")
        return pair.isPositive;

      if (pair.token.startsWith("!")) {
        if ("overriden".startsWith(pair.token.after("!")) && !typeAllowedEquals(rule.allowed, rule.allowedBase))
          return pair.isPositive;

        if ("conditions".startsWith(pair.token.after("!")) && rule.allowed.conditions.length)
          return pair.isPositive;
      }

      if (str.toLowerCase().contains(pair.token.toLowerCase()))
        return pair.isPositive;
    }

    return false;
  };


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
              <div style={{ marginBottom: "-2px" }}>
                <input type="text" className="form-control form-control-sm" id="filter" placeholder="Auth-!overriden+!conditions" value={filter} onChange={handleSetFilter} />
              </div>
            </th>

            <th style={{ textAlign: "center" }}>
              {TypeAllowed.niceToString("Write")}
            </th>
            <th style={{ textAlign: "center" }}>
              {TypeAllowed.niceToString("Read")}
            </th>
            <th style={{ textAlign: "center" }}>
              {TypeAllowed.niceToString("None")}
            </th>
            <th style={{ textAlign: "center" }}>
              {AuthAdminMessage.Overriden.niceToString()}
            </th>
            {properties && <th style={{ textAlign: "center" }}>
              {PropertyRouteEntity.niceName()}
            </th>}
            {operations && <th style={{ textAlign: "center" }}>
              {OperationSymbol.niceName()}
            </th>}
            {queries && <th style={{ textAlign: "center" }}>
              {QueryEntity.niceName()}
            </th>}
          </tr>
        </thead>
        <tbody>
          {ctx.mlistItemCtxs(a => a.rules)
            .filter((n, i) => isMatch(n.value))
            .groupBy(a => a.value.resource.namespace).orderBy(a => a.key).flatMap(gr => [
              <tr key={gr.key} className="sf-auth-namespace">
                <td colSpan={10}><b>{gr.key}</b></td>
              </tr>
            ].concat(gr.elements.orderBy(a => a.value.resource.className).flatMap(c => renderType(c))))}
        </tbody>
      </table>

    </div>
  );


  function handleAddConditionClick(remainig: TypeConditionSymbol[], taac: TypeAllowedAndConditions) {
    SelectorModal.chooseElement(remainig, { buttonDisplay: a => a.toStr.tryAfter(".") ?? a.toStr })
      .then(tc => {
        if (!tc)
          return;

        taac.conditions.push(newMListElement(TypeConditionRuleEmbedded.New({
          typeCondition: tc!,
          allowed: "None"
        })));

        updateFrame();
      })
      .done();
  }

  function handleRemoveConditionClick(taac: TypeAllowedAndConditions, con: TypeConditionRuleEmbedded) {
    taac.conditions!.remove(taac.conditions.filter(mle => mle.element == con).single());
    taac.modified = true;
    updateFrame();
  }

  function renderType(tctx: TypeContext<TypeAllowedRule>) {

    let roleId = ctx.value.role.id!;

    let used = tctx.value.allowed.conditions.map(mle => mle.element.typeCondition.id!);

    let remaining = tctx.value.availableConditions.filter(tcs => !used.contains(tcs.id!));

    var typeInfo = getTypeInfo(tctx.value.resource.cleanName);

    var masterClass = typeInfo.entityData == "Master" ? "sf-master" : undefined;

    let fallback = Binding.create(tctx.value.allowed, a => a.fallback);
    return [
      <tr key={tctx.value.resource.namespace + "." + tctx.value.resource.className} className={classes("sf-auth-type", tctx.value.allowed.conditions.length > 0 && "sf-auth-with-conditions")}>
        <td>
          {remaining.length > 0 ?
            <span className="sf-condition-icon" onClick={() => handleAddConditionClick(remaining, tctx.value.allowed)}>
              <FontAwesomeIcon icon="plus-circle" />
            </span> :
            <FontAwesomeIcon icon="circle" className="sf-placeholder-icon"></FontAwesomeIcon>}
          &nbsp;
                    {typeInfo.niceName} {typeInfo.entityData && <small title={typeInfo.entityData}>{typeInfo.entityData[0]}</small>}
        </td>
        <td style={{ textAlign: "center" }} className={masterClass}>
          {colorRadio(fallback, "Write", "green")}
        </td>
        <td style={{ textAlign: "center" }}>
          {colorRadio(fallback, "Read", "#FFAD00")}
        </td>
        <td style={{ textAlign: "center" }}>
          {colorRadio(fallback, "None", "red")}
        </td>
        <td style={{ textAlign: "center" }}>
          <GrayCheckbox checked={!typeAllowedEquals(tctx.value.allowed, tctx.value.allowedBase)} onUnchecked={() => {
            tctx.value.allowed = JSON.parse(JSON.stringify(tctx.value.allowedBase));
            tctx.value.modified = true;
            updateFrame();
          }} />
        </td>
        {properties && <td style={{ textAlign: "center" }}>
          {link("edit", tctx.value.modified ? "Invalidated" : tctx.value.properties,
            () => API.fetchPropertyRulePack(tctx.value.resource.cleanName, roleId),
            m => tctx.value.properties = m.rules.every(a => a.element.allowed == "None") ? "None" :
              m.rules.every(a => a.element.allowed == "Write") ? "All" : "Mix"
          )}
        </td>}
        {operations && <td style={{ textAlign: "center" }}>
          {link("bolt", tctx.value.modified ? "Invalidated" : tctx.value.operations,
            () => API.fetchOperationRulePack(tctx.value.resource.cleanName, roleId),
            m => tctx.value.operations = m.rules.every(a => a.element.allowed == "None") ? "None" :
              m.rules.every(a => a.element.allowed == "Allow") ? "All" : "Mix")}
        </td>}
        {queries && <td style={{ textAlign: "center" }}>
          {link("search", tctx.value.modified ? "Invalidated" : tctx.value.queries,
            () => API.fetchQueryRulePack(tctx.value.resource.cleanName, roleId),
            m => tctx.value.queries = m.rules.every(a => a.element.allowed == "None") ? "None" :
              m.rules.every(a => a.element.allowed == "Allow") ? "All" : "Mix")}
        </td>}
      </tr>
    ].concat(tctx.value.allowed!.conditions!.map(mle => mle.element).map((c, i) => {
      let b = Binding.create(c, ca => ca.allowed);
      return (
        <tr key={tctx.value.resource.namespace + "." + tctx.value.resource.className + "_" + c.typeCondition.id} className="sf-auth-condition" >
          <td>
            {"\u00A0 \u00A0".repeat(i + 1)}
            <span className="sf-condition-icon" onClick={() => handleRemoveConditionClick(tctx.value.allowed, c)}><FontAwesomeIcon icon="minus-circle" /></span>
            &nbsp;
            <small>{c.typeCondition.toStr.tryAfter(".") ?? c.typeCondition.toStr}</small>
          </td>
          <td style={{ textAlign: "center" }} className={masterClass}>
            {colorRadio(b, "Write", "green")}
          </td>
          <td style={{ textAlign: "center" }}>
            {colorRadio(b, "Read", "#FFAD00")}
          </td>
          <td style={{ textAlign: "center" }}>
            {colorRadio(b, "None", "red")}
          </td>
          <td style={{ textAlign: "center" }}>
          </td>
        </tr>
      );
    }));
  }

  function colorRadio(b: Binding<TypeAllowed | null>, basicAllowed: TypeAllowedBasic, color: string) {
    const allowed = b.getValue();

    const niceName = TypeAllowedBasic.niceToString(basicAllowed)!;

    const title = !allowed ? niceName :
      getDB(allowed) == getUI(allowed) && getUI(allowed) == basicAllowed ? niceName :
        getDB(allowed) == basicAllowed ? AuthAdminMessage._0InDB.niceToString(niceName) :
          getUI(allowed) == basicAllowed ? AuthAdminMessage._0InUI.niceToString(niceName) :
            niceName;

    const icon: IconProp | undefined = !allowed ? undefined :
      getDB(allowed) == getUI(allowed) && getUI(allowed) == basicAllowed ? undefined :
        getDB(allowed) == basicAllowed ? "database" :
          getUI(allowed) == basicAllowed ? "window-restore" :
            undefined;

    return <ColorRadio
      checked={isActive(allowed, basicAllowed)}
      title={title}
      color={color}
      icon={icon}
      onClicked={e => { b.setValue(select(b.getValue(), basicAllowed, e)); updateFrame(); }}
    />;
  }

  function link<T extends ModelEntity>(icon: IconProp, allowed: AuthThumbnail | null | "Invalidated", action: () => Promise<T>, setNewValue: (model: T) => void) {
    if (!allowed)
      return undefined;

    let onClick = () => {
      GraphExplorer.propagateAll(ctx.value);

      if (ctx.value.modified) {
        MessageModal.show({
          title: NormalControlMessage.SaveChangesFirst.niceToString(),
          message: AuthAdminMessage.PleaseSaveChangesFirst.niceToString(),
          buttons: "ok",
          style: "warning",
          icon: "warning"
        }).done();
      }
      else {
        action()
          .then(m => Navigator.navigate(m))
          .then(() => action())
          .then(m => {
            setNewValue(m);
            updateFrame();
          })
          .done();
      }
    };

    return (

      <a onClick={onClick} title={allowed}>
        <FontAwesomeIcon icon={icon}
          className="sf-auth-link"
          color={allowed == "Invalidated" ? "gray" :
            allowed == "All" ? "green" :
              allowed == "Mix" ? "#FFAD00" : "red"} />
      </a>
    );
  }
});

function typeAllowedEquals(allowed: TypeAllowedAndConditions, allowedBase: TypeAllowedAndConditions) {
  return allowed.fallback == allowedBase.fallback
    && allowed.conditions!.length == allowedBase.conditions!.length
    && allowed.conditions!.map(mle => mle.element)
      .every((c, i) => {
        let b = allowedBase.conditions![i].element;
        return c.allowed == b.allowed && is(c.typeCondition, b.typeCondition);
      });
}

function getDB(allowed: TypeAllowed): TypeAllowedBasic {
  if (allowed.contains("DB"))
    return allowed.after("DB").before("UI") as TypeAllowedBasic;

  return allowed as TypeAllowedBasic;
}

function getUI(allowed: TypeAllowed): TypeAllowedBasic {
  if (allowed.contains("UI"))
    return allowed.after("UI") as TypeAllowedBasic;

  return allowed as TypeAllowedBasic;
}

let values: TypeAllowedBasic[] = ["Write", "Read", "None"];

function combine(val1: TypeAllowedBasic, val2: TypeAllowedBasic): TypeAllowed {

  let db: TypeAllowedBasic;
  let ui: TypeAllowedBasic;
  if (values.indexOf(val1) < values.indexOf(val2)) {
    db = val1;
    ui = val2;
  } else {
    db = val2;
    ui = val1;
  }

  return "DB" + db + "UI" + ui as TypeAllowed;
}

function isActive(allowed: TypeAllowed | null, basicAllowed: TypeAllowedBasic) {
  if (!allowed)
    return false;

  return getDB(allowed) == basicAllowed || getUI(allowed) == basicAllowed;
}


function select(current: TypeAllowed | null, basicAllowed: TypeAllowedBasic, e: React.MouseEvent<any>) {
  if (!(e.shiftKey || e.ctrlKey) || current == null)
    return basicAllowed as TypeAllowedBasic;

  let db = getDB(current);
  let ui = getUI(current);

  if (db != ui) {
    if (basicAllowed == ui)
      return db;

    if (basicAllowed == db)
      return ui;
  } else {
    if (basicAllowed != db)
      return combine(db, basicAllowed);
  }

  return current;
}
