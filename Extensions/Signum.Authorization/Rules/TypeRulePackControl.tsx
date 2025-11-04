import * as React from 'react'
import { Button } from 'react-bootstrap'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { IconProp } from '@fortawesome/fontawesome-svg-core'
import { classes } from '@framework/Globals'
import { Finder } from '@framework/Finder'
import { Navigator } from '@framework/Navigator'
import { Operations } from '@framework/Operations'
import { TypeContext, ButtonsContext, IRenderButtons, EntityFrame, ButtonBarElement } from '@framework/TypeContext'
import { EntityLine, AutoLine, EntityBaseController } from '@framework/Lines'
import SelectorModal from '@framework/SelectorModal'
import MessageModal from '@framework/Modals/MessageModal'

import { getTypeInfo, Binding, GraphExplorer, EnumType } from '@framework/Reflection'
import { ModelEntity, newMListElement, NormalControlMessage, getToString, toMList, Lite, EntityControlMessage, MList, MListElement } from '@framework/Signum.Entities'
import { AuthAdminClient } from '../AuthAdminClient'
import {
  TypeRulePack, TypeAllowed, TypeAllowedRule,
  TypeAllowedBasic, AuthThumbnail,
  PropertyRulePack, OperationRulePack, QueryRulePack, TypeConditionSymbol, AuthAdminMessage,
  WithConditionsModel,
  ConditionRuleModel,
  PropertyAllowed,
  OperationAllowed,
} from './Signum.Authorization.Rules'

import { ColorRadio, GrayCheckbox } from './ColoredRadios'
import { PropertyRouteEntity, TypeEntity } from '@framework/Signum.Basics'

import "./AuthAdmin.css"
import { is } from '@framework/Signum.Entities';
import { RoleEntity } from '../Signum.Authorization'
import { OperationSymbol } from '@framework/Signum.Operations'
import { QueryEntity } from '@framework/Signum.Basics'
import { KeyNames } from '@framework/Components'
import { useForceUpdate } from '@framework/Hooks'
import { AccessibleRow, AccessibleTable } from '../../../Signum/React/Basics/AccessibleTable'
import { LinkButton } from '@framework/Basics/LinkButton'

export default function TypesRulesPackControl({ ctx, innerRef }: { ctx: TypeContext<TypeRulePack>, innerRef?: React.Ref<IRenderButtons> }): React.JSX.Element {

  const [filter, setFilter] = React.useState("");

  function renderButtons(bc: ButtonsContext): ButtonBarElement[] {


    const hasChanges = GraphExplorer.hasChanges(bc.pack.entity); 
    return [
      { button: <Button type="button" variant="primary" disabled={!hasChanges || ctx.readOnly} onClick={() => handleSaveClick(bc)}>{AuthAdminMessage.Save.niceToString()}</Button> },
      { button: <Button type="button" variant="warning" disabled={!hasChanges || ctx.readOnly} onClick={() => handleResetChangesClick(bc)}>{AuthAdminMessage.ResetChanges.niceToString()}</Button> },
      { button: <Button type="button" variant="info" disabled={hasChanges} onClick={() => handleSwitchToClick(bc)}>{AuthAdminMessage.SwitchTo.niceToString()}</Button> }
    ];
  }

  React.useImperativeHandle(innerRef, () => ({ renderButtons }), [ctx.value])

  function handleSaveClick(bc: ButtonsContext) {
    let pack = ctx.value;

    AuthAdminClient.API.saveTypeRulePack(pack)
      .then(() => AuthAdminClient.API.fetchTypeRulePack(pack.role.id!))
      .then(newPack => {
        Operations.notifySuccess();
        bc.frame.onReload({ entity: newPack, canExecute: {} });
      });
  }

  function handleResetChangesClick(bc: ButtonsContext) {
    let pack = ctx.value;

    AuthAdminClient.API.fetchTypeRulePack(pack.role.id!)
      .then(newPack => {
        bc.frame.onReload({ entity: newPack, canExecute: {} });
      });
  }

  function handleSwitchToClick(bc: ButtonsContext) {
    let pack = ctx.value;

    Finder.find(RoleEntity).then(r => {
      if (!r)
        return;

      AuthAdminClient.API.fetchTypeRulePack(r.id!)
        .then(newPack => bc.frame.onReload({ entity: newPack, canExecute: {} }));
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
        if ("overriden".startsWith(pair.token.after("!")) && !withConditionsEquals(rule.allowed, rule.allowedBase))
          return pair.isPositive;

        if ("conditions".startsWith(pair.token.after("!")) && rule.availableConditions.length)
          return pair.isPositive;

        if ("error".startsWith(pair.token.after("!")) && rule.allowed.fallback == null)
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
        <AutoLine ctx={ctx.subCtx(f => f.strategy)} />
      </div>
      <AccessibleTable
        aria-label={AuthAdminMessage.TypePermissionOverview.niceToString()}
        mapCustomComponents={new Map([[TypeRow, "tr"]])}
        className="table table-sm sf-auth-rules">
        <thead>
          <tr>
            <th>
              <div style={{ marginBottom: "-2px" }}>
                <input type="text" className="form-control form-control-sm" id="filter" placeholder="Auth-!overriden+!conditions+!error" value={filter} onChange={handleSetFilter} />
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
            {AuthAdminClient.properties && <th style={{ textAlign: "center" }}>
              {PropertyRouteEntity.niceName()}
            </th>}
            {AuthAdminClient.operations && <th style={{ textAlign: "center" }}>
              {OperationSymbol.niceName()}
            </th>}
            {AuthAdminClient.queries && <th style={{ textAlign: "center" }}>
              {QueryEntity.niceName()}
            </th>}
          </tr>
        </thead>
        <tbody>
          {ctx.mlistItemCtxs(a => a.rules)
            .filter((n, i) => isMatch(n.value))
            .groupBy(a => a.value.resource.namespace).orderBy(a => a.key).map(gr =>
              <>
                <AccessibleRow key={gr.key} className="sf-auth-namespace">
                  <td colSpan={10}><b>{gr.key}</b></td>
                </AccessibleRow>
                {gr.elements.orderBy(a => a.value.resource.className)
                  .map(c => <TypeRow tctx={c} role={ctx.value.role} updateFrame={updateFrame} />)}
              </>)
          }
        </tbody>
      </AccessibleTable>
    </div>
  );
}

function withConditionsEquals<A extends string>(allowed: WithConditionsModel<A>, allowedBase: WithConditionsModel<A>) {
  return allowed.fallback == allowedBase.fallback
    && allowed.conditionRules.length == allowedBase.conditionRules.length
    && allowed.conditionRules.map(mle => mle.element)
      .every((c, i) => {
        let b = allowedBase.conditionRules[i].element;

        if (c.allowed != b.allowed)
          return false;

        if (c.typeConditions.length != b.typeConditions.length)
          return false;

        var cKeys = c.typeConditions.map(a => a.element.key);
        var bKeys = b.typeConditions.map(a => a.element.key);

        return cKeys.every(k => bKeys.contains(k));
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

export function TypeRow(p: { tctx: TypeContext<TypeAllowedRule>, role: Lite<RoleEntity>, updateFrame: () => void }): React.ReactElement {

  const getConfig = useDragAndDrop(p.tctx.value.allowed.conditionRules, () => p.updateFrame(), () => { p.tctx.value.modified = true; p.updateFrame(); });

  let roleId = p.role.id!;

  const rule = p.tctx.value;

  const typeInfo = getTypeInfo(rule.resource.cleanName);
  const conditions = rule.availableConditions;

  const masterClass = typeInfo.entityData == "Master" ? "sf-master" : undefined;

  function handleRemoveConditionClick(taac: WithConditionsModel<TypeAllowed>, con: ConditionRuleModel<TypeAllowed>) {
    taac.conditionRules.remove(taac.conditionRules.single(mle => mle.element == con));
    taac.modified = true;
    p.updateFrame();
  }

  function renderRadio(b: Binding<TypeAllowed | null>, basicAllowed: TypeAllowedBasic, color: string) {
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
      readOnly={p.tctx.readOnly}
      onClicked={e => { b.setValue(select(b.getValue(), basicAllowed, e)); p.updateFrame(); }}
    />;
  }


  function link<T extends ModelEntity>(icon: IconProp, allowed: AuthThumbnail | null | "Invalidated", title: string, fetch: () => Promise<T>, setNewValue: (model: T) => void, extraProps?: {}) {
    if (!allowed)
      return undefined;

    let onClick = () => {
      if (GraphExplorer.hasChanges(p.tctx.value)) {
        MessageModal.show({
          title: NormalControlMessage.SaveChangesFirst.niceToString(),
          message: AuthAdminMessage.PleaseSaveChangesFirst.niceToString(),
          buttons: "ok",
          style: "warning",
          icon: "warning"
        });
      }
      else {
        fetch()
          .then(m => Navigator.view(m, { buttons: "close", readOnly: p.tctx.readOnly, extraProps: extraProps }))
          .then(() => fetch())
          .then(m => {
            setNewValue(m);
            p.updateFrame();
          });
      }
    };

    return (

      <LinkButton onClick={onClick} aria-label={allowed} title={`${title} (${allowed})`}>
        <FontAwesomeIcon aria-hidden="true" icon={icon}
          className="sf-auth-link"
          color={allowed == "Invalidated" ? "gray" :
            allowed == "All" ? "green" :
              allowed == "Mix" ? "#FFAD00" : "red"} />
      </LinkButton>
    );
  }


  let fallback = Binding.create(rule.allowed, a => a.fallback);
  return (
    <>
      <AccessibleRow key={rule.resource.namespace + "." + rule.resource.className} className={classes("sf-auth-type", rule.allowed.conditionRules.length > 0 && "sf-auth-with-conditions")}>
        <td>
          {conditions.length > 1 || conditions.length == 1 && rule.allowed.conditionRules.length == 0 ?
            <LinkButton className="sf-condition-icon" title={AuthAdminMessage.AddCondition.niceToString()} onClick={async e => {
              await addConditionClick(TypeAllowed, conditions, rule.allowed, rule.resource);
              p.updateFrame();
            }}>
              <FontAwesomeIcon aria-hidden="true" icon="circle-plus" className="me-2" />
            </LinkButton> :
            <FontAwesomeIcon aria-hidden="true" icon="circle" className="sf-placeholder-icon me-2"></FontAwesomeIcon>
          }
          {typeInfo.niceName}
          {typeInfo.entityData && <small className="ms-1" title={typeInfo.entityData}>{typeInfo.entityData[0]}</small>}
          {rule.allowed.conditionRules.length > 0 && <small className="shy-text ms-1">{AuthAdminMessage.Fallback.niceToString()}</small>}
        </td>
        <td style={{ textAlign: "center" }} className={masterClass}>
          {renderRadio(fallback, "Write", "green")}
        </td>
        <td style={{ textAlign: "center" }}>
          {renderRadio(fallback, "Read", "#FFAD00")}
        </td>
        <td style={{ textAlign: "center" }}>
          {renderRadio(fallback, "None", "red")}
        </td>
        <td style={{ textAlign: "center" }}>
          <GrayCheckbox readOnly={p.tctx.readOnly} checked={!withConditionsEquals(rule.allowed, rule.allowedBase)} onUnchecked={() => {
            rule.allowed = JSON.parse(JSON.stringify(rule.allowedBase));
            rule.modified = true;
            p.updateFrame();
          }} />
        </td>
        {AuthAdminClient.properties && <td style={{ textAlign: "center" }}>
          {link("edit", rule.modified ? "Invalidated" : rule.properties?.fallback ?? null, PropertyRouteEntity.nicePluralName(), 
            () => AuthAdminClient.API.fetchPropertyRulePack(rule.resource.cleanName, roleId),
            m => rule.properties = collapsePropertyRules(m, rule.allowed)
          )}
        </td>}
        {AuthAdminClient.operations && <td style={{ textAlign: "center" }}>
          {link("bolt", rule.modified ? "Invalidated" : rule.operations?.fallback ?? null, OperationSymbol.nicePluralName(),
            () => AuthAdminClient.API.fetchOperationRulePack(rule.resource.cleanName, roleId),
            m => rule.operations = collapseOperationRules(m, rule.allowed)
          )}
        </td>}
        {AuthAdminClient.queries && <td style={{ textAlign: "center" }}>
          {link("search", rule.modified ? "Invalidated" : rule.queries, QueryEntity.nicePluralName(),
            () => AuthAdminClient.API.fetchQueryRulePack(rule.resource.cleanName, roleId),
            m => rule.queries = m.rules.every(a => a.element.allowed == "None") ? "None" :
              m.rules.every(a => a.element.allowed == "Allow") ? "All" : "Mix")}
        </td>}
      </AccessibleRow>
      {rule.allowed.conditionRules.map(mle => mle.element).map((cr, i) => {
        let b = Binding.create(cr, ca => ca.allowed);

        var drag = rule.allowed.conditionRules.length > 1 ? getConfig(i) : null;

        return (
          <AccessibleRow key={rule.resource.namespace + "." + rule.resource.className + "_" + cr.typeConditions.map(c => c.element.id).join("_")}
            className={classes("sf-auth-condition", drag?.dropClass)}
            onDragEnter={drag?.onDragOver}
            onDragOver={drag?.onDragOver}
            onDrop={drag?.onDrop}
          >
            <td>
              <LinkButton className="sf-condition-icon me-1 ms-3" title={AuthAdminMessage.RemoveCondition.niceToString()} aria-label={AuthAdminMessage.RemoveCondition.niceToString()} onClick={e => {
                handleRemoveConditionClick(rule.allowed, cr);
              }}>
                <FontAwesomeIcon aria-hidden="true" icon="circle-minus" title={AuthAdminMessage.RemoveCondition.niceToString()} />
              </LinkButton>
              {drag && <LinkButton className="sf-condition-icon me-1" aria-label={drag.title} title={drag.title}
                onClick={e => { e.stopPropagation(); }}
                draggable={true}
                onKeyDown={drag.onKeyDown}
                onDragStart={drag.onDragStart}
                onDragEnd={drag.onDragEnd}
              >
                {EntityBaseController.getMoveIcon()}
              </LinkButton>}
              {cr.typeConditions.map((tc, j) => <>
                <small className="mx-1" key={j}>{getToString(tc.element).after(".")}</small>
                {j < cr.typeConditions.length - 1 ? <small className="and" key={j + "$"}>&</small> : null}
              </>)}
              <small className="shy-text ms-1">{(getRuleTitle(i, rule.allowed.conditionRules.length))}</small>
            </td>
            <td style={{ textAlign: "center" }} className={masterClass}>
              {renderRadio(b, "Write", "green")}
            </td>
            <td style={{ textAlign: "center" }}>
              {renderRadio(b, "Read", "#FFAD00")}
            </td>
            <td style={{ textAlign: "center" }}>
              {renderRadio(b, "None", "red")}
            </td>
            <td>
            </td>
            {AuthAdminClient.properties && <td style={{ textAlign: "center" }}>
              {link("edit", rule.modified ? "Invalidated" : rule.properties?.conditionRules.singleOrNull(a => matches(a.element.typeConditions, cr.typeConditions))?.element.allowed ?? null,
                PropertyRouteEntity.nicePluralName(),
                () => AuthAdminClient.API.fetchPropertyRulePack(rule.resource.cleanName, roleId),
                m => rule.properties = collapsePropertyRules(m, rule.allowed),
                { initialTypeConditions: cr.typeConditions.map(a=>a.element) }
              )}
            </td>}
            {AuthAdminClient.operations && <td style={{ textAlign: "center" }}>
              {link("bolt", rule.modified ? "Invalidated" : rule.operations?.conditionRules.singleOrNull(a => matches(a.element.typeConditions, cr.typeConditions))?.element.allowed ?? null,
                OperationSymbol.nicePluralName(),
                () => AuthAdminClient.API.fetchOperationRulePack(rule.resource.cleanName, roleId),
                m => rule.operations = collapseOperationRules(m, rule.allowed),
                { initialTypeConditions: cr.typeConditions.map(a=>a.element) }                
              )}
            </td>}
            <td style={{ textAlign: "center" }} colSpan={1 + Number(AuthAdminClient.properties) + Number(AuthAdminClient.operations) + Number(AuthAdminClient.queries)}>
            </td>
          </AccessibleRow>
        );
      })}
    </>
  );
}

function collapsePropertyRules(pack: PropertyRulePack, tar: WithConditionsModel<TypeAllowed>): WithConditionsModel<AuthThumbnail> {

  function collapse(properties: PropertyAllowed[]): AuthThumbnail {
    if (properties.every(a => a == "None"))
      return "None";

    if (properties.every(a => a == "Write"))
      return "All";

    return "Mix";
  }



  return WithConditionsModel(AuthThumbnail).New({
    fallback: collapse(pack.rules.map(mle => mle.element.allowed.fallback)),
    conditionRules: tar.conditionRules.map(cr => newMListElement(ConditionRuleModel(AuthThumbnail).New({
      typeConditions: cr.element.typeConditions,
      allowed: collapse(pack.rules.map(mle => mle.element.allowed.conditionRules.singleOrNull(a => matches(a.element.typeConditions, cr.element.typeConditions))?.element.allowed).notNull())
    })))
  });
}

function collapseOperationRules(pack: OperationRulePack, tar: WithConditionsModel<TypeAllowed>): WithConditionsModel<AuthThumbnail> {

  function collapse(operations: OperationAllowed[]): AuthThumbnail {
    if (operations.every(a => a == "None"))
      return "None";

    if (operations.every(a => a == "Allow"))
      return "All";

    return "Mix";
  }

  return WithConditionsModel(AuthThumbnail).New({
    fallback: collapse(pack.rules.map(mle => mle.element.allowed.fallback)),
    conditionRules: tar.conditionRules.map(cr => newMListElement(ConditionRuleModel(AuthThumbnail).New({
      typeConditions: cr.element.typeConditions,
      allowed: collapse(pack.rules.map(mle => mle.element.allowed.conditionRules.singleOrNull(a => matches(a.element.typeConditions, cr.element.typeConditions))?.element.allowed).notNull())
    })))
  });
}

function matches(a: MList<TypeConditionSymbol>, b: MList<TypeConditionSymbol>) {
  return a.length == b.length && a.every((tc, i) => is(tc.element, b[i].element));
}

export async function addConditionClick<A extends string>(allowedType: EnumType<A>, conditions: TypeConditionSymbol[], taac: WithConditionsModel<A>, type: TypeEntity): Promise<undefined> {

  const tc = await SelectorModal.chooseManyElement(conditions, {
    buttonDisplay: a => getToString(a).after("."),
    title: AuthAdminMessage.SelectTypeConditions.niceToString(),
    message: <div>
      <p>{AuthAdminMessage.ThereAre0TypeConditionsDefinedFor1.niceToString().formatHtml(<strong>{conditions.length}</strong>, <strong>{getTypeInfo(type.cleanName).niceName}</strong>)}</p>
      <p>{AuthAdminMessage.SelectOneToOverrideTheAccessFor0ThatSatisfyThisCondition.niceToString().formatHtml(<strong>{getTypeInfo(type.cleanName).nicePluralName}</strong>)}</p>
      <p>{AuthAdminMessage.SelectMoreThanOneToOverrideAccessFor0ThatSatisfyAllTheConditionsAtTheSameTime.niceToString().formatHtml(<strong>{getTypeInfo(type.cleanName).nicePluralName}</strong>)}</p>
    </div>,
    size: "md"
  });
  if (!tc)
    return;

  var combinedKey = tc.orderBy(a => a.key).map(a => a.key.after(".")).join(" & ");

  if (taac.conditionRules.some(cr => cr.element.typeConditions.orderBy(a => a.element.key).map(a => a.element.key.after(".")).join(" & ") == combinedKey)) {
    return MessageModal.showError(<div>
      <p>{AuthAdminMessage.TheFollowingTypeConditionsHaveAlreadyBeenUsed.niceToString()}</p>
      <p><strong>{combinedKey}</strong></p>
    </div>,
      AuthAdminMessage.RepeatedTypeCondition.niceToString());
  }

  taac.conditionRules.push(newMListElement(ConditionRuleModel(allowedType).New({
    typeConditions: tc.map(t => newMListElement(t)),
    allowed: allowedType.values().first(),
  })));
}

export function getRuleTitle(i: number, total: number): string {

  const j = total - i;

  return j == 1 ? AuthAdminMessage.FirstRule.niceToString() :
    j == 2 ? AuthAdminMessage.SecondRule.niceToString() :
      j == 3 ? AuthAdminMessage.ThirdRule.niceToString() : AuthAdminMessage.NthRule.niceToString(j);
}

export interface IndexWithOffset {
  index: number;
  offset: 0 | 1;
}

export function useDragAndDrop(list: MList<any>, forceUpdate: () => void, onChanged: () => void): (index: number) => DragConfig {

  const [dragIndex, setDragIndex] = React.useState<number | undefined>();
  const [dropBorderIndex, setDropBorderIndex] = React.useState<IndexWithOffset | undefined>();

  function handleDragStart(de: React.DragEvent<any>, index: number) {
    de.dataTransfer.setData('text', "start"); //cannot be empty string
    de.dataTransfer.effectAllowed = "move";
    setDragIndex(index);
  }

  function handleDragEnd(de: React.DragEvent<any>){
    setDragIndex(undefined);
    setDropBorderIndex(undefined);
    forceUpdate();
  }

  function handlerDragOver(de: React.DragEvent<any>, index: number) {
    if (dragIndex == null)
      return;

    de.preventDefault();

    const th = de.currentTarget as HTMLElement;

    const offset = getOffsetVertical((de.nativeEvent as DragEvent), th.getBoundingClientRect());

    let dbi: IndexWithOffset | undefined = offset == undefined ? undefined :
      { index, offset };

    if (dbi != null && dbi.index == dragIndex)
      dbi = undefined;

    if (dropBorderIndex != dbi) {
      setDropBorderIndex(dbi);
    }
  }

  function getOffsetVertical(dragEvent: DragEvent, rect: DOMRect): 0 | 1 | undefined {

    var margin = Math.min(50, rect.height / 2);

    const height = rect.height;
    const offsetY = dragEvent.y - rect.top;

    if (offsetY < margin)
      return 0;

    if (offsetY > (height - margin))
      return 1;

    return undefined;
  }

  function dropClass(index: number): "drag-left" | "drag-top" | "drag-right" | "drag-bottom" | undefined {

    if (dropBorderIndex != null) {

      if (index == dropBorderIndex.index) {
        if (dropBorderIndex.offset == 0)
          return "drag-top";
        else
          return "drag-bottom"
      }

      if (dropBorderIndex.index == (index - 1) && dropBorderIndex.offset == 1)
        return "drag-top";
      else if (dropBorderIndex.index == (index + 1) && dropBorderIndex.offset == 0)
        return "drag-bottom";
    }

    return undefined;
  }

  function handleDrop(de: React.DragEvent<any>) {

    de.preventDefault();
    if (dropBorderIndex == null || dragIndex == null)
      return;

    onMoveElement(dragIndex, dropBorderIndex);
  }

  function onMoveElement(oldIndex: number, newIndex: IndexWithOffset): void {

    const temp = list[oldIndex];
    list.removeAt(oldIndex);
    var completeNewIndex = newIndex.index + newIndex.offset;
    const rebasedDropIndex = newIndex.index > oldIndex ? completeNewIndex - 1 : completeNewIndex;
    list.insertAt(rebasedDropIndex, temp);


    setDropBorderIndex(undefined);
    setDragIndex(undefined);
    onChanged();
  }

  function handleMoveKeyDown(ke: React.KeyboardEvent<any>, index: number): void {

    if (ke.ctrlKey) {

      if (ke.key == KeyNames.arrowDown || ke.key == KeyNames.arrowRight) {
        ke.preventDefault();
        onMoveElement(index, ({ index: index + 1, offset: 1 }));
      } else {
        ke.preventDefault();
        onMoveElement(index, ({ index: index - 1, offset: 0 }));
      }
    }
  }

  return index => ({
    dropClass: classes(
      index == dragIndex && "sf-dragging",
      dropClass(index)),
    onDragStart: e => handleDragStart(e, index),
    onDragEnd: handleDragEnd,
    onDragOver: e => handlerDragOver(e, index),
    onKeyDown: e => handleMoveKeyDown(e, index),
    onDrop: handleDrop,
    title: EntityControlMessage.MoveWithDragAndDropOrCtrlUpDown.niceToString()
  });
}

export interface DragConfig {
  onDragStart?: React.DragEventHandler<any>;
  onDragEnd?: React.DragEventHandler<any>;
  onDragOver?: React.DragEventHandler<any>;
  onDrop?: React.DragEventHandler<any>;
  onKeyDown?: React.KeyboardEventHandler<any>;
  dropClass?: string;
  title?: string;
}
