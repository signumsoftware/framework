import * as React from 'react'
import { Dic, areEqual, classes } from '../Globals'
import { tryGetTypeInfos, TypeReference, TypeInfo, tryGetTypeInfo, getTypeName, Binding, getTypeInfos, IsByAll, getTypeInfo, MemberInfo, OperationInfo, isNumberType } from '../Reflection'
import { ModifiableEntity, SearchMessage, JavascriptMessage, Lite, Entity, OperationMessage } from '../Signum.Entities'
import { Navigator } from '../Navigator'
import { ViewReplacer } from '../Frames/ReactVisitor'
import { EntityLine, EntityCombo, EntityDetail, EntityStrip, TypeContext, EntityCheckboxList, EnumCheckboxList, EntityTable, PropertyRoute, StyleContext } from '../Lines'
import { Type } from '../Reflection';
import { EntityRepeater } from '../Lines/EntityRepeater';
import { MultiValueLine } from '../Lines/MultiValueLine';
import { Operations } from '../Operations';
import { useForceUpdate } from '../Hooks'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { DropdownList } from 'react-widgets-up'
import { QueryTokenMessage } from '../Signum.DynamicQuery.Tokens'
import { Finder } from '../Finder'
import { openModal, IModalProps } from '../Modals'
import { Modal } from 'react-bootstrap'
import { ErrorBoundary } from '../Components'
import './MultiPropertySetter.css';
import { FilterOperation, filterOperations, getFilterType } from '../FindOptions'
import { PropertyOperation } from '../Signum.Operations'
import { CollectionMessage } from '../Signum.External'
import { EnumLine } from '../Lines/EnumLine'
import { AutoLine } from '../Lines/AutoLine'
import SelectorModal from '../SelectorModal'


interface MultiPropertySetterModalProps extends IModalProps<boolean | undefined> {
  typeInfo: TypeInfo;
  lites: Lite<Entity>[];
  operationInfo: OperationInfo;
  setters: Operations.API.PropertySetter[];
  mandatory: boolean;
}

export function MultiPropertySetterModal(p: MultiPropertySetterModalProps): React.ReactElement {

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
        <button type="button" className="btn-close" data-dismiss="modal" aria-label="Close" onClick={handleCancelClicked}/>
      </div>
      <div className="modal-body">
        <ErrorBoundary>
          <MultiPropertySetter setters={p.setters} root={PropertyRoute.root(p.typeInfo)} isPredicate={false} onChange={forceUpdate} />
        </ErrorBoundary>
      </div>
      <div className="modal-footer">
          <p>
            {OperationMessage.PleaseConfirmThatYouWouldLikeToApplyTheAboveChangesAndExecute0Over12.niceToString().formatHtml(
              <strong>{p.operationInfo.niceName}</strong>,
              <strong>{p.lites.length}</strong>,
              <strong>{p.lites.length == 1 ? p.typeInfo.niceName : p.typeInfo.nicePluralName}</strong>
            )}
        </p>
        <br />
        <button className="btn btn-primary sf-entity-button sf-ok-button" disabled={p.setters.some(s => !isValid(s)) || p.mandatory && p.setters.length == 0} onClick={handleOkClicked}>
          {JavascriptMessage.ok.niceToString()}
        </button>
        <button className="btn btn-tertiary sf-entity-button sf-close-button" onClick={handleCancelClicked}>
          {JavascriptMessage.cancel.niceToString()}
        </button>
      </div>
    </Modal>
  );

  function isValid(setter: Operations.API.PropertySetter) {
    return setter.property != null;
  }
}

export namespace MultiPropertySetterModal {
  export function show(typeInfo: TypeInfo, lites: Lite<Entity>[], operationInfo: OperationInfo, mandatory: boolean, setters?: Operations.API.PropertySetter[]): Promise<Operations.API.PropertySetter[] | undefined> {
    var settersOrDefault = setters ?? [{ property: null!, operation: null! } as Operations.API.PropertySetter];
    return openModal<boolean | undefined>(<MultiPropertySetterModal typeInfo={typeInfo} lites={lites} operationInfo={operationInfo} mandatory={mandatory} setters={settersOrDefault} />).then(a => a ? settersOrDefault : undefined);
  };
}

export function MultiPropertySetter({ root, setters, onChange, isPredicate }: { root: PropertyRoute, setters: Operations.API.PropertySetter[], isPredicate: boolean, onChange: () => void }): React.ReactElement {

  function handleNewPropertySetter(e: React.MouseEvent) {
    e.preventDefault();
    setters.push({ property: null!, operation: null! });
    onChange();
  }

  function handleDeletePropertySetter(ps: Operations.API.PropertySetter) {
    setters.remove(ps);
    onChange();
  }

  var addElement = isPredicate ?
    SearchMessage.AddFilter.niceToString() :
    OperationMessage.AddSetter.niceToString()

  return (
    <table className="table-sm">
      <thead>
        <tr>
          <th style={{ minWidth: "24px" }}></th>
          <th>{SearchMessage.Field.niceToString()}</th>
          <th>{OperationMessage.Operation.niceToString()}</th>
          <th style={{ paddingRight: "20px" }}>{SearchMessage.Value.niceToString()}</th>
        </tr>
      </thead>
      <tbody>
        {setters.map((ps, i) =>
          <PropertySetterComponent
            key={i} setter={ps} onDeleteSetter={handleDeletePropertySetter}
            root={root}
            isPredicate={isPredicate}
            onSetterChanged={() => onChange()} />
        )}
        {
          <tr className="sf-property-create">
            <td colSpan={4}>
              <a href="#"
                title={StyleContext.default.titleLabels ? addElement : undefined}
                className="sf-line-button sf-create sf-create-condition"
                role="button"
                tabIndex={0}
                onClick={e => handleNewPropertySetter(e)}>
                <FontAwesomeIcon aria-hidden={true} icon="plus" className="sf-create me-1" />{addElement}
              </a>
            </td>
          </tr>
        }
      </tbody>
    </table>
  );
}


function isPart(typeName: string) {
  var tis = tryGetTypeInfos(typeName);
  return tis != null && tis.length == 1 && (tis[0]?.entityKind == "Part" || tis[0]?.entityKind == "SharedPart");
}

export function getPropertyOperations(type: TypeReference): PropertyOperation[] {

  if (type.isCollection && (type.isEmbedded || isPart(type.name)))
    return ["AddNewElement", "ChangeElements", "RemoveElementsWhere"];

  if (type.isCollection)
    return ["AddElement", "RemoveElement"];

  if (type.isEmbedded)
    return ["Set", "CreateNewEntity", "ModifyEntity"]

  if (type.name == IsByAll)
    return ["Set"];

  var typeInfos = tryGetTypeInfos(type.name);
  if (typeInfos.length == 0)
    return [];

  if (typeInfos[0] == null)
    return ["Set"];

  if (!typeInfos.some(a => a?.entityKind == "Part" || a?.entityKind == "SharedPart"))
    return ["Set"];

  return ["Set", "CreateNewEntity", "ModifyEntity"];
}

export interface PropertySetterComponentProps {
  root: PropertyRoute;
  setter: Operations.API.PropertySetter;
  onDeleteSetter: (pi: Operations.API.PropertySetter) => void;
  isPredicate: boolean;
  onSetterChanged: () => void;
}


export function PropertySetterComponent(p: PropertySetterComponentProps): React.ReactElement {

  const forceUpdate = useForceUpdate();

  function handleDeleteSetter(e: React.MouseEvent<any>) {
    e.preventDefault();
    p.onDeleteSetter(p.setter);
  }

  function handlePropertyChanged(newProperty: PropertyRoute | null | undefined) {
    const s = p.setter;
    s.property = newProperty == null ? undefined! :
      p.root.propertyRouteType == "Root" ? newProperty.propertyPath() :
        removeInitialPoin(newProperty.propertyPath().after(p.root.propertyPath()));

    s.operation = newProperty == null || p.isPredicate ? null! : getPropertyOperations(newProperty.typeReference()).firstOrNull()!;


    const filterType = newProperty && getFilterType(newProperty.typeReference());
    s.filterOperation = newProperty == null || !p.isPredicate || filterType == null ? null! : filterOperations[filterType].firstOrNull()!;

    s.value = undefined;
    fixOperation(s, newProperty).then(() => {
      p.onSetterChanged();
      forceUpdate();
    });
  }

  function removeInitialPoin(str: string) {
    if (str.startsWith("."))
      return str.after(".");

    return str;
  }

  function handleChangeOperation(event: React.FormEvent<HTMLSelectElement>) {
    const operation = (event.currentTarget as HTMLSelectElement).value as PropertyOperation;
    const s = p.setter;
    s.operation = operation;
    fixOperation(s, pr!).then(() => {
      p.onSetterChanged();
      forceUpdate();
    });
  }

  function handleChangeFilterOperation(event: React.FormEvent<HTMLSelectElement>) {
    const fOperation = (event.currentTarget as HTMLSelectElement).value as FilterOperation;
    const s = p.setter;
    s.filterOperation = fOperation;
  }

  function fixOperation(p: Operations.API.PropertySetter, pr: PropertyRoute | null | undefined): Promise<void> {

    p.value = undefined;
    p.predicate = p.operation && showPredicate(p.operation) ? [] : undefined;
    p.setters = p.operation && showSetters(p.operation) ? [] : undefined;
    if (pr && (p.setters || p.predicate)) {
      var infos = tryGetTypeInfos(pr.typeReference());
      var promise = SelectorModal.chooseType(infos.notNull().filter(tr => tr!.entityKind == "Part" || tr!.entityKind == "SharedPart"));

      return promise.then(type => { p.entityType = type?.name; });
    }

    p.entityType = undefined;
    return Promise.resolve(undefined);
  }

  const pr = React.useMemo(() => p.setter.property == null ? null : p.root.addMembers(p.setter.property),
    [p.root, p.setter.property]);

  var operations = pr == null || p.isPredicate ? undefined : getPropertyOperations(pr.typeReference());

  var filterType = p.isPredicate && pr ? getFilterType(pr.typeReference()) : null;
  var fOperations = filterType ? filterOperations[filterType] : null;

  var subRoot = pr &&
    (pr.typeReference().isCollection ? (pr.typeReference().isEmbedded ? pr.addMember("Indexer", "Item", true) : tryGetTypeInfo(pr.typeReference().name) && PropertyRoute.root(getTypeInfo(pr.typeReference().name))) :
      (pr.typeReference().isEmbedded ? pr : (p.setter.entityType != null ? PropertyRoute.root(getTypeInfo(p.setter.entityType)) : null)));

  return (
    <>
      <tr className="sf-property-setter">
        <td>
          {<a href="#"
            title={StyleContext.default.titleLabels ? SearchMessage.DeleteFilter.niceToString() : undefined}
            className="sf-line-button sf-remove"
            role="button"
            tabIndex={0}
            onClick={handleDeleteSetter}>
            <FontAwesomeIcon aria-hidden={true} icon="xmark" />
          </a>}
        </td>
        <td>
          <div className="rw-widget-xs">
            <PropertySelector
              property={pr}
              root={p.root}
              onPropertyChanged={handlePropertyChanged} />
          </div>
        </td>
        <td>
          {
            operations &&
            <select className="form-select form-select-xs" value={p.setter.operation} disabled={operations.length == 1} onChange={handleChangeOperation}>
                {operations.map((op, i) => <option key={i} value={op}>{PropertyOperation.niceToString(op)}</option>)}
              </select>
          }

          {
            fOperations &&
            <select className="form-select form-select-xs" value={p.setter.filterOperation} disabled={fOperations.length == 1} onChange={handleChangeFilterOperation}>
              {fOperations.map((op, i) => <option key={i} value={op}>{FilterOperation.niceToString(op)}</option>)}
            </select>
          }
        </td>
        <td className="sf-filter-value">
          {p.isPredicate ?
            <>
              {p.setter.property && renderValue()}
            </> :
            <>
              {p.setter.property && p.setter.operation && showValue(p.setter.operation) && renderValue()}
              {subRoot && p.setter.operation && showPredicate(p.setter.operation) && pr && <div>
                <h5>{OperationMessage.Condition.niceToString()}</h5>
                <MultiPropertySetter onChange={p.onSetterChanged} setters={p.setter.predicate!} isPredicate={true} root={subRoot} />
              </div>
              }
              {subRoot && p.setter.operation && showSetters(p.setter.operation) && pr && <div>
                <h5>{OperationMessage.Setters.niceToString()}</h5>
                <MultiPropertySetter onChange={p.onSetterChanged} setters={p.setter.setters!} isPredicate={false} root={subRoot} />
              </div>
              }
            </>
          }
        </td>
      </tr>
    </>
  );

  function renderValue() {

    const ctx = new TypeContext<any>(undefined, { formGroupStyle: "None", formSize: "xs" }, pr!, Binding.create(p.setter, a => a.value));

    return createSetterValueControl(ctx, handleValueChange);
  }

  function handleValueChange() {
    p.onSetterChanged();
  }
}


function showValue(o: PropertyOperation) {
  return o == "Set" || o == "AddElement" || o == "RemoveElement";
}

function showPredicate(o: PropertyOperation) {
  return o == "ChangeElements" || o == "RemoveElementsWhere";
}

function showSetters(o: PropertyOperation) {
  return o == "AddNewElement" || o == "ChangeElements" || o == "CreateNewEntity" || o == "ModifyEntity";
}

export function createSetterValueControl(ctx: TypeContext<any>, handleValueChange: () => void): React.ReactElement {
  var tr = ctx.propertyRoute!.typeReference();

  if (tr.isEmbedded)
    return <EntityLine ctx={ctx} autocomplete={null} onChange={handleValueChange} create={false} />;

  if (tr.isLite)
    return <EntityLine ctx={ctx} onChange={handleValueChange} create={false}/>;

  var tis = tryGetTypeInfos(tr.name);

  if (tis[0]) {

    if (tis[0].kind == "Enum") {
      const ti = tis.single()!;
      const members = Dic.getValues(ti.members).filter(a => !a.isIgnoredEnum);
      return <EnumLine ctx={ctx} optionItems={members} onChange={handleValueChange} />;
    }

    if (tr.name == IsByAll || tis.some(ti => !ti!.isLowPopulation))
      return <EntityLine ctx={ctx} onChange={handleValueChange} />;
    else
      return <EntityCombo ctx={ctx} onChange={handleValueChange} />
  }
  return <AutoLine ctx={ctx} onChange={handleValueChange} />;
}

interface PropertySelectorProps {
  root: PropertyRoute;
  property: PropertyRoute | undefined | null;
  onPropertyChanged: (newProperty: PropertyRoute | undefined) => void;
}

export default function PropertySelector(p: PropertySelectorProps): React.ReactElement {
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

export function PropertyPart(p: PropertyPartProps): React.ReactElement | null {

  if (p.parentRoute.propertyRouteType != "Mixin") {
    var tr = p.parentRoute.typeReference();
    if (tr.name.contains(",") || tr.name == IsByAll)
      return null;

    var ti = tryGetTypeInfo(tr.name);
    if (p.parentRoute.propertyRouteType != "Root" && ti != null && (ti.entityKind == "Part" || ti?.entityKind != "SharedPart"))
      return null;
  }

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
        dataKey="name"
        textField="niceName"
        renderValue={a => <PropertyItem item={a.item} />}
        renderListItem={a => <PropertyItemOptional item={a.item} />}
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

export function PropertyItem(p: { item: MemberInfo | null }): React.ReactElement | null {

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

export function PropertyItemOptional(p: { item: MemberInfo | null }): React.ReactElement {

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


export function getTypeColor(type: TypeReference): string {

  if (type.isCollection)
    return "#CE6700";

  if (isNumberType(type.name))
    return "#000000";

  switch (type.name) {
    case "string":
    case "Guid":
    case "boolean": return "#000000";
    case "DateOnly": return "#5100A1";
    case "DateTime": return "#5100A1";
    default:
      {
        if (type.isEmbedded)
          return "#156F8A";

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

export function getNiceTypeName(tr: TypeReference): string {
  if (tr.isCollection)
    return QueryTokenMessage.ListOf0.niceToString(Finder.getTypeNiceName({ ...tr, isCollection: false }));

  switch (tr.name) {
    case "number": return QueryTokenMessage.Number.niceToString();
    case "string": return QueryTokenMessage.Text.niceToString();
    case "Guid": return QueryTokenMessage.GlobalUniqueIdentifier.niceToString();
    case "boolean": return QueryTokenMessage.Check.niceToString();
    case "DateOnly": return QueryTokenMessage.Date.niceToString();
    case "DateTime": return QueryTokenMessage.DateTime.niceToString();
    default:
      {
        if (tr.isEmbedded)
          return QueryTokenMessage.Embedded0.niceToString().formatWith(tr.typeNiceName);


        if (tr.name == IsByAll)
          return QueryTokenMessage.AnyEntity.niceToString();

        var tis = tryGetTypeInfos(tr.name);
        if (tis[0]) {
          return tis.map(a => a!.niceName).joinComma(CollectionMessage.Or.niceToString());
        }
        else
          return "Unknown";
      }
  }
}
