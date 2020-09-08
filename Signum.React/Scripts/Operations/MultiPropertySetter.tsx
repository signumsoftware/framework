import * as React from 'react'
import { Dic, areEqual, classes } from '../Globals'
import { tryGetTypeInfos, TypeReference, TypeInfo, tryGetTypeInfo, getTypeName, Binding, getTypeInfos, IsByAll, getTypeInfo, MemberInfo, OperationInfo } from '../Reflection'
import { ModifiableEntity, SearchMessage, External, JavascriptMessage, Lite, Entity, OperationMessage, PropertyOperation } from '../Signum.Entities'
import * as Navigator from '../Navigator'
import { ViewReplacer } from '../Frames/ReactVisitor'
import { ValueLine, EntityLine, EntityCombo, EntityDetail, EntityStrip, TypeContext, EntityCheckboxList, EnumCheckboxList, EntityTable, PropertyRoute, StyleContext } from '../Lines'
import { Type } from '../Reflection';
import { EntityRepeater } from '../Lines/EntityRepeater';
import { MultiValueLine } from '../Lines/MultiValueLine';
import { ValueLineController } from '../Lines/ValueLine';
import { API, Defaults } from '../Operations';
import { useForceUpdate } from '../Hooks'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { DropdownList } from 'react-widgets'
import { QueryTokenMessage } from '../Signum.Entities.DynamicQuery'
import { getTypeNiceName } from '../Finder'
import { openModal, IModalProps } from '../Modals'
import { Modal } from 'react-bootstrap'
import { ErrorBoundary } from '../Components'
import './MultiPropertySetter.css';
import SelectorModal from '../SelectorModal'



interface MultiPropertySetterModalProps extends IModalProps<boolean | undefined> {
  typeInfo: TypeInfo;
  lites: Lite<Entity>[];
  operationInfo: OperationInfo;
  setters: API.PropertySetter[];
}

export function MultiPropertySetterModal(p: MultiPropertySetterModalProps) {

  const [show, setShow] = React.useState(true);
  const answerRef = React.useRef<boolean | undefined>(undefined);
  const forceUpdate = useForceUpdate();

  function handleOkClicked() {
    answerRef.current = true;
    setShow(false);
  }

  function handleCancelClicked() {
    setShow(false);
  }

  function handleOnExited() {
    p.onExited!(answerRef.current);
  }

  return (
    <Modal onHide={handleCancelClicked} show={show} className="message-modal" size="xl" onExited={handleOnExited}>
      <div className="modal-header">
        <h5 className="modal-title">{OperationMessage.BulkModifications.niceToString()}</h5>
        <button type="button" className="close" data-dismiss="modal" aria-label="Close" onClick={handleCancelClicked}>
          <span aria-hidden="true">&times;</span>
        </button>
      </div>
      <div className="modal-body">
        <ErrorBoundary>
          <MultiPropertySetter setters={p.setters} root={PropertyRoute.root(p.typeInfo)} onChange={forceUpdate} />
        </ErrorBoundary>
      </div>
      <div className="modal-footer">
          <p>
            {OperationMessage.PleaseConfirmThatYouDLineToApplyTheAboveChangesAndExecute0Over12.niceToString().formatHtml(
              <strong>{p.operationInfo.niceName}</strong>,
              <strong>{p.lites.length}</strong>,
              <strong>{p.lites.length == 1 ? p.typeInfo.niceName : p.typeInfo.nicePluralName}</strong>
            )}
        </p>
        <br />
        <button className="btn btn-primary sf-entity-button sf-ok-button" disabled={p.setters.some(s => !isValid(s)) || Defaults.defaultSetterConfig(p.operationInfo) == "Mandatory" && p.setters.length == 0} onClick={handleOkClicked}>
          {JavascriptMessage.ok.niceToString()}
        </button>
        <button className="btn btn-light sf-entity-button sf-close-button" onClick={handleCancelClicked}>
          {JavascriptMessage.cancel.niceToString()}
        </button>
      </div>
    </Modal>
  );

  function isValid(setter: API.PropertySetter) {
    return setter.property != null;
  }
}

MultiPropertySetterModal.show = (typeInfo: TypeInfo, lites: Lite<Entity>[], operationInfo: OperationInfo, setters?: API.PropertySetter[]): Promise<API.PropertySetter[] | undefined> => {
  var settersOrDefault = setters ?? [];
  return openModal<boolean | undefined>(<MultiPropertySetterModal typeInfo={typeInfo} lites={lites} operationInfo={operationInfo} setters={settersOrDefault} />).then(a => a ? settersOrDefault : undefined);
};

export function MultiPropertySetter({ root, setters, onChange }: { root: PropertyRoute, setters: API.PropertySetter[], onChange: () => void }) {

  function handleNewPropertySetter(e: React.MouseEvent) {
    e.preventDefault();
    setters.push({ property: null!, operation: null! });
    onChange();
  }

  function handleDeletePropertySetter(ps: API.PropertySetter) {
    setters.remove(ps);
    onChange();
  }

  return (
    <table className="table-sm">
      <thead>
        <tr>
          <th style={{ minWidth: "24px" }}></th>
          <th>{SearchMessage.Field.niceToString()}</th>
          <th>{SearchMessage.Operation.niceToString()}</th>
          <th style={{ paddingRight: "20px" }}>{SearchMessage.Value.niceToString()}</th>
        </tr>
      </thead>
      <tbody>
        {setters.map((ps, i) =>
          <PropertySetterComponent
            key={i} setter={ps} onDeleteSetter={handleDeletePropertySetter}
            prefixRoute={undefined}
            root={root}
            isPredicate={false}
            onSetterChanged={() => onChange()} />
        )}
        {
          <tr className="sf-property-create">
            <td colSpan={4}>
              <a href="#" title={StyleContext.default.titleLabels ? SearchMessage.AddFilter.niceToString() : undefined}
                className="sf-line-button sf-create sf-create-condition"
                onClick={e => handleNewPropertySetter(e)}>
                <FontAwesomeIcon icon="plus" className="sf-create mr-1" />{SearchMessage.AddFilter.niceToString()}
              </a>
            </td>
          </tr>
        }
      </tbody>
    </table>
  );
}


export function getPropertyOperations(type: TypeReference): PropertyOperation[] {
  if (type.isCollection)
    return ["AddElement", "ChangeElements", "RemoveElements"];

  if (type.isEmbedded)
    return ["Set", "CreateNewEntiy", "ModifyEntity"]

  if (type.name == IsByAll)
    return ["Set"];

  var typeInfos = tryGetTypeInfos(type.name);
  if (typeInfos.length == 0)
    return [];

  if (typeInfos[0] == null)
    return ["Set"];

  return ["Set", "CreateNewEntiy", "ModifyEntity"];
}

export interface PropertySetterComponentProps {
  root: PropertyRoute;
  setter: API.PropertySetter;
  prefixRoute: PropertyRoute | undefined;
  onDeleteSetter: (pi: API.PropertySetter) => void;
  isPredicate: boolean;
  onSetterChanged: () => void;
}


export function PropertySetterComponent(p: PropertySetterComponentProps) {

  const forceUpdate = useForceUpdate();

  function handleDeleteSetter(e: React.MouseEvent<any>) {
    e.preventDefault();
    p.onDeleteSetter(p.setter);
  }

  function handlePropertyChanged(newProperty: PropertyRoute | null | undefined) {
    const s = p.setter;
    s.property = newProperty?.propertyPath() ?? undefined!;
    s.operation = newProperty == null ? null! : getPropertyOperations(newProperty.typeReference()).firstOrNull()!;
    s.value = undefined;
    fixOperation(s, newProperty).then(() => {
      p.onSetterChanged();
      forceUpdate();
    }).done();
  }

  function handleChangeOperation(event: React.FormEvent<HTMLSelectElement>) {
    const operation = (event.currentTarget as HTMLSelectElement).value as PropertyOperation;
    const s = p.setter;
    s.operation = operation;
    fixOperation(s, pr!).then(() => {
      p.onSetterChanged();
      forceUpdate();
    }).done();
  }

  function fixOperation(p: API.PropertySetter, pr: PropertyRoute | null | undefined): Promise<void> {

    p.value = undefined;
    p.predicate = showPredicate(p.operation) ? [] : undefined;
    p.setters = showSetters(p.operation) ? [] : undefined;
    if (pr && (p.setters || p.predicate)) {
      var infos = tryGetTypeInfos(pr.typeReference());
      var promise = infos.length == 1 && infos[0] ? Promise.resolve(undefined) :
        SelectorModal.chooseType(infos.notNull().filter(tr => tr!.entityKind == "Part" || tr!.entityKind == "SharedPart"));

      return promise.then(type => { p.entityType = type?.name; });
    }

    p.entityType = undefined;
    return Promise.resolve(undefined);
  }

  function showValue(o: PropertyOperation) {
    return o == "Set";
  }

  function showPredicate(o: PropertyOperation) {
    return o == "ChangeElements" || o == "RemoveElements";
  }

  function showSetters(o: PropertyOperation) {
    return o == "AddElement" || o == "ChangeElements" || o == "CreateNewEntiy" || o == "ModifyEntity";
  }

  const pr = React.useMemo(() => p.setter.property == null ? null : PropertyRoute.parse(p.root.findRootType(), p.setter.property),
    [p.root, p.setter.property]);

  var operations = pr == null ? [] : getPropertyOperations(pr.typeReference());

  var subRoot = pr && (pr.typeReference().isEmbedded ? (pr.typeReference().isCollection ? pr.addMember("Indexer", "Item", true) :  pr) :
    p.setter.entityType != null ? PropertyRoute.root(getTypeInfo(p.setter.entityType)) : null); 

  return (
    <>
      <tr className="sf-property-setter">
        <td>
          {<a href="#" title={StyleContext.default.titleLabels ? SearchMessage.DeleteFilter.niceToString() : undefined}
            className="sf-line-button sf-remove"
            onClick={handleDeleteSetter}>
            <FontAwesomeIcon icon="times" />
          </a>}
        </td>
        <td>
          <div className="rw-widget-xs">
            <PropertySelector
              property={pr}
              prefixRoute={p.prefixRoute}
              root={p.root}
              onPropertyChanged={handlePropertyChanged} />
          </div>
        </td>
        <td>
          {
            operations.length == 0 ? null :
              <select className="form-control form-control-xs" value={p.setter.operation} disabled={operations.length == 1} onChange={handleChangeOperation}>
                {operations.map((op, i) => <option key={i} value={op}>{PropertyOperation.niceToString(op)}</option>)}
              </select>
          }
        </td>
        <td className="sf-filter-value">
          {p.setter.property && showValue(p.setter.operation) && renderValue()}
          {subRoot && showPredicate(p.setter.operation) && pr && <div>
            <h3>{OperationMessage.Predictate.niceToString()}</h3>
            <MultiPropertySetter onChange={p.onSetterChanged} setters={p.setter.setters!} root={subRoot} />
          </div>
          }
          {subRoot && showSetters(p.setter.operation) && pr && <div>
            <h3>{OperationMessage.Setters.niceToString()}</h3>
            <MultiPropertySetter onChange={p.onSetterChanged} setters={p.setter.setters!} root={subRoot} />
          </div>
          }
        </td>
      </tr>
    </>
  );

  function renderValue() {

    const ctx = new TypeContext<any>(undefined, { formGroupStyle: "None", formSize: "ExtraSmall" }, pr!, Binding.create(p.setter, a => a.value));

    return createSetterValueControl(ctx, handleValueChange);
  }

  function handleValueChange() {
    p.onSetterChanged();
  }
}

export function createSetterValueControl(ctx: TypeContext<any>, handleValueChange: () => void): React.ReactElement<any> {
  var tr = ctx.propertyRoute!.typeReference();

  var vlt = ValueLineController.getValueLineType(tr)
  if (vlt)
    return <ValueLine ctx={ctx} onChange={handleValueChange} />;

  if (tr.isEmbedded)
    return <EntityLine ctx={ctx} autocomplete={null} onChange={handleValueChange} />;

  if (tr.isLite)
    return <EntityLine ctx={ctx} onChange={handleValueChange} />;

  var tis = tryGetTypeInfos(tr.name);

  if (tis[0]) {

    if (tis[0].kind == "Enum") {
      const ti = tis.single()!;
      const members = Dic.getValues(ti.members).filter(a => !a.isIgnoredEnum);
      return <ValueLine ctx={ctx} comboBoxItems={members} onChange={handleValueChange} />;
    }

    if (tr.name == IsByAll || tis.some(ti => !ti!.isLowPopulation))
      return <EntityLine ctx={ctx} onChange={handleValueChange} />;
    else
      return <EntityCombo ctx={ctx} onChange={handleValueChange} />
  }

  return <span className="text-alert">Not supported</span>
}

interface PropertySelectorProps {
  root: PropertyRoute;
  prefixRoute?: PropertyRoute | undefined;
  property: PropertyRoute | undefined | null;
  onPropertyChanged: (newProperty: PropertyRoute | undefined) => void;
}

export default function PropertySelector(p: PropertySelectorProps) {
  var lastTokenChanged = React.useRef<string | undefined>(undefined);

  var rootList = p.root.allParents();

  let propertyList: (PropertyRoute | undefined)[] = p.property ? p.property.allParents().filter((pr, i) => i >= rootList.length) : [];

  propertyList.push(undefined);

  return (
    <div className={classes("sf-property-selector", p.property == null ? "has-error" : null)}>
      {propertyList.map((a, i) => <PropertyPart
        key={i == 0 ? "__first__" : propertyList[i - 1]!.propertyPath()}
        onRouteSelected={pr => {
          p.onPropertyChanged && p.onPropertyChanged(pr);
        }}
        defaultOpen={lastTokenChanged.current && i > 0 && lastTokenChanged.current == propertyList[i - 1]!.propertyPath() ? true : false}
        parentRoute={i == 0 ? p.root : propertyList[i - 1]!}
        selectedRoute={a} />)}
    </div>
  );
}

interface PropertyPartProps {
  parentRoute: PropertyRoute;
  selectedRoute: PropertyRoute | undefined;
  onRouteSelected: (newRoute: PropertyRoute | undefined) => void;
  defaultOpen: boolean;
}

export function PropertyPart(p: PropertyPartProps) {

  var tr = p.parentRoute.typeReference();

  if (tr.name.contains(",") || tr.name == IsByAll)
    return null;

  var ti = tryGetTypeInfo(tr.name);
  if (p.parentRoute.propertyRouteType != "Root" && ti != null && (ti.entityKind == "Part" || ti?.entityKind != "SharedPart"))
    return null;

  const subMembers = Dic.getValues(p.parentRoute.subMembers());

  if (subMembers.length == 0)
    return null;

  return (
    <div className="sf-property-part" onKeyUp={handleKeyUp} onKeyDown={handleKeyUp}>
      <DropdownList
        filter="contains"
        data={subMembers}
        value={p.selectedRoute?.member}
        onChange={handleOnChange}
        valueField="name"
        textField="niceName"
        valueComponent={PropertyItem}
        itemComponent={PropertyItemOptional}
        defaultOpen={p.defaultOpen}
      />
    </div>
  );


  function handleOnChange(value: MemberInfo) {
    p.onRouteSelected(PropertyRoute.parse(p.parentRoute.findRootType(), value.name));
  }

  function handleKeyUp(e: React.KeyboardEvent<any>) {
    if (e.key == "Enter") {
      e.preventDefault();
      e.stopPropagation();
    }
  }
}

export function PropertyItem(p: { item: MemberInfo | null }) {

  const item = p.item;

  if (item == null)
    return null;

  return (
    <span
      style={{ color: getTypeColor(item.type) }}
      title={StyleContext.default.titleLabels ? getNiceTypeName(item.type) : undefined}>
      {item.niceName ?? " - no member - "}
    </span>
  );
}

export function PropertyItemOptional(p: { item: MemberInfo | null }) {

  const item = p.item;

  if (item == null)
    return <span> - </span>;


  return (
    <span data-member={item.name}
      style={{ color: getTypeColor(item.type) }}
      title={StyleContext.default.titleLabels ? getNiceTypeName(item.type) : undefined}>
      {item.niceName ?? "- no member - "}
    </span>
  );
}


export function getTypeColor(type: TypeReference) {

  if (type.isCollection)
    return "#CE6700";

  switch (type.name) {
    case "number":
    case "string":
    case "Guid":
    case "boolean": return "#000000";
    case "Date": return "#5100A1";
    case "DateTime": return "#5100A1";
    default:
      {
        if (type.isEmbedded)
          return "#156F8A";

        if (type.isLite)
          return "#2B91AF";

        var tis = tryGetTypeInfos(type.name);

        if (tis[0]) {

          if (tis[0].kind == "Enum")
            return "#800046";

          return "#2B91AF";
        }

        return "#7D7D7D";
      }
  }
}

export function getNiceTypeName(tr: TypeReference) {
  if (tr.isCollection)
    return QueryTokenMessage.ListOf0.niceToString(getTypeNiceName({ ...tr, isCollection: false }));

  switch (tr.name) {
    case "number": return QueryTokenMessage.Number.niceToString();
    case "string": return QueryTokenMessage.Text.niceToString();
    case "Guid": return QueryTokenMessage.GlobalUniqueIdentifier.niceToString();
    case "boolean": return QueryTokenMessage.Check.niceToString();
    case "Date": return QueryTokenMessage.Date.niceToString();
    case "DateTime": return QueryTokenMessage.DateTime.niceToString();
    default:
      {
        if (tr.isEmbedded)
          return QueryTokenMessage.Embedded0.niceToString().formatWith(tr.typeNiceName);

        if (tr.isLite)
          return "#2B91AF";

        if (tr.name == IsByAll)
          return QueryTokenMessage.AnyEntity.niceToString();

        var tis = tryGetTypeInfos(tr.name);
        if (tis[0]) {
          return tis.map(a => a!.niceName).joinComma(External.CollectionMessage.Or.niceToString());
        }
        else
          return "Unknown";
      }
  }
}
