import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Combobox } from 'react-widgets-up'
import { classes, Dic } from '@framework/Globals'
import { Binding, EntityDataValues, EntityKindValues, EntityKind, PropertyRoute, Type } from '@framework/Reflection'
import { Navigator } from '@framework/Navigator'
import { SearchControl } from '@framework/Search'
import { StyleContext } from '@framework/TypeContext'
import { EntityControlMessage, toLite } from '@framework/Signum.Entities'
import SelectorModal from '@framework/SelectorModal';
import MessageModal from '@framework/Modals/MessageModal'
import { DynamicTypeClient } from '../DynamicTypeClient';
import { EvalClient } from '../../Signum.Eval/EvalClient'
import { TypeHelpClient } from '../../Signum.Eval/TypeHelp/TypeHelpClient';
import ValueComponent from './ValueComponent';
import TypeHelpComponent from '../../Signum.Eval/TypeHelp/TypeHelpComponent'
import CSharpCodeMirror from '../../Signum.CodeMirror/CSharpCodeMirror';
import AutoLineModal from '@framework/AutoLineModal'
import "./DynamicType.css"
import { Tabs, Tab } from 'react-bootstrap';
import CollapsableCard from '@framework/Components/CollapsableCard';
import { Typeahead } from '@framework/Components';
import { useForceUpdate, useAPI } from '@framework/Hooks'
import { DynamicTypeEntity, DynamicTypeMessage } from '../Signum.Dynamic.Types'
import { DynamicMixinConnectionEntity } from '../Signum.Dynamic.Mixins'
import { TextAreaLine } from '@framework/Lines'

import Validators = DynamicTypeClient.Validators;
import DynamicProperty = DynamicTypeClient.DynamicProperty;
import DynamicTypeDefinition = DynamicTypeClient.DynamicTypeDefinition;
import { JSX } from 'react'

export interface DynamicTypeDesignContext {
  refreshView: () => void;
}

interface DynamicTypeDefinitionComponentProps {
  dynamicType: DynamicTypeEntity;
  definition: DynamicTypeDefinition;
  dc: DynamicTypeDesignContext;
  showDatabaseMapping: boolean;
}


const requiresSaveKinds: EntityKind[] = ["Main", "Shared", "String", "Relational"];

export function DynamicTypeDefinitionComponent(p: DynamicTypeDefinitionComponentProps): React.JSX.Element {
  const forceUpdate = useForceUpdate();

  const [expressionNames, setExpressionNames] = React.useState<string[] | undefined>(undefined);

  const typeEntity = useAPI(() =>
    p.dynamicType.baseType != "Entity" ? Promise.resolve(undefined) :
      p.dynamicType.isNew ? Promise.resolve(undefined) :
        Navigator.API.getType(p.dynamicType.typeName!)
          .then(te => te ? toLite(te) : false),
    [p.dynamicType.isNew, p.dynamicType.baseType, p.dynamicType.typeName]);


  React.useEffect(() => {
    if (p.dynamicType.isNew)
      fixSaveOperation();

  });

  function handleTabSelect(eventKey: any /*string*/) {
    var dt = p.dynamicType;
    if (!dt.isNew && dt.typeName && eventKey == "query")
      DynamicTypeClient.API.expressionNames(dt.typeName + "Entity")
        .then(exprNames => setExpressionNames(exprNames));
  }

  function handlePropertyRemoved(dp: DynamicProperty) {
    var qfs = p.definition.queryFields;
    if (qfs?.contains(dp.name))
      qfs.remove(dp.name);

    p.dc.refreshView();
  }

  function handleEntityKindChange() {
    fixSaveOperation();
    forceUpdate();
  }

  function handleHasTickChanged() {
    var ticks = p.definition.ticks;

    if (ticks)
      p.definition.ticks = {
        hasTicks: ticks.hasTicks,
        name: ticks.hasTicks ? "Ticks" : undefined,
        type: ticks.hasTicks ? "int" : undefined,
      };
  }

  function fixSaveOperation() {
    const def = p.definition;
    var requiresSave = p.dynamicType.baseType == "Entity" &&
      def.entityKind != undefined && requiresSaveKinds.contains(def.entityKind);

    if (requiresSave && !def.operationSave) {
      def.operationSave = { execute: "" };
    } else if (!requiresSave && def.operationSave) {
      if (isEmpty(def.operationSave))
        def.operationSave = undefined;
      else {
        MessageModal.show({
          title: EntityControlMessage.Remove.niceToString(),
          message: DynamicTypeMessage.RemoveSaveOperation.niceToString(),
          buttons: "yes_no",
          icon: "question"
        }).then(result => {
          if (result == "yes")
            def.operationSave = undefined;
        });
      }
    }
  }

  function isEmpty(operation: DynamicTypeClient.OperationExecute) {
    return !operation.execute && !operation.canExecute;
  }


  function handleTypeHelpClick(pr: PropertyRoute | undefined) {
    if (!pr)
      return;

    AutoLineModal.show({
      type: { name: "string" },
      initialValue: TypeHelpComponent.getExpression("e", pr, "CSharp"),
      customComponent:  p => <TextAreaLine {...p}/>,
      title: "Property Template",
      message: "Copy to clipboard: Ctrl+C, ESC",
    });
  }


  function renderOthers() {
    var ctx = new StyleContext(undefined, { labelColumns: 3 });
    return React.createElement("div", {}, ...EvalClient.Options.onGetDynamicLineForType.map(f => f(ctx, p.dynamicType.typeName!)));
  }
  const def = p.definition;
    const primaryKey = def.primaryKey!;
    const ticks = def.ticks!;

    var propNames = def.properties.map(p => "e." + p.name);

  var expressionNamesStr = (expressionNames ?? []).map(exp => exp + "= e." + exp + "()");

  var dt = p.dynamicType;

    return (
      <div>
        {dt.baseType == "Entity" &&
          <div>
          {p.showDatabaseMapping &&
            <ValueComponent dc={p.dc} labelColumns={2} binding={Binding.create(def, d => d.tableName)} type="string" defaultValue={null} labelClass="database-mapping" />
            }

            < div className="row">
              <div className="col-sm-6">
              <ValueComponent dc={p.dc} labelColumns={4} binding={Binding.create(def, d => d.entityKind)} type="string" defaultValue={null} options={EntityKindValues} onChange={handleEntityKindChange} />
              </div>
              <div className="col-sm-6">
              <ValueComponent dc={p.dc} labelColumns={4} binding={Binding.create(def, d => d.entityData)} type="string" defaultValue={null} options={EntityDataValues} />
              </div>
            </div>

          {p.showDatabaseMapping &&
              <div className="row database-mapping">
                <div className="col-sm-6">
                  <PrimaryKeyFieldsetComponent
                    binding={Binding.create(def, d => d.primaryKey)}
                    title="Primary Key"
                    onCreate={() => ({ name: "Id", type: "int", identity: true })}
                    renderContent={item =>
                      <div>
                      <ValueComponent dc={p.dc} labelColumns={4} binding={Binding.create(item, i => i.name)} type="string" defaultValue={null} />
                      <ValueComponent dc={p.dc} labelColumns={4} binding={Binding.create(item, i => i.type)} type="string" defaultValue={null} options={["int", "long", "short", "string", "Guid"]} />
                      <ValueComponent dc={p.dc} labelColumns={4} binding={Binding.create(item, i => i.identity)} type="boolean" defaultValue={null} />
                      </div>
                    }
                  />
                </div>
                <div className="col-sm-6">
                  <TicksFieldsetComponent
                    binding={Binding.create(def, d => d.ticks)}
                    onCreate={() => ({ hasTicks: false })}
                    renderContent={item =>
                      <div>
                      <ValueComponent dc={p.dc} labelColumns={4} binding={Binding.create(item, i => i.hasTicks)} type="boolean" defaultValue={null} onChange={handleHasTickChanged} />
                      <ValueComponent dc={p.dc} labelColumns={4} binding={Binding.create(item, i => i.name)} type="string" defaultValue={null} />
                      <ValueComponent dc={p.dc} labelColumns={4} binding={Binding.create(item, i => i.type)} type="string" defaultValue={null} options={["int", "Guid", "DateTime"]} />
                      </div>
                    }
                  />
                </div>
              </div>
            }
          </div>
        }

        <Tabs defaultActiveKey="properties" id="DynamicTypeTabs" onSelect={handleTabSelect} mountOnEnter={true}>
          <Tab eventKey="properties" title="Properties">
          <PropertyRepeaterComponent dc={p.dc} properties={def.properties} onRemove={handlePropertyRemoved} showDatabaseMapping={p.showDatabaseMapping} />
            <br />
            {dt.baseType == "Entity" &&
              <MultiColumnUniqueIndexFieldsetComponent
                binding={Binding.create(def, d => d.multiColumnUniqueIndex)}
                title="Multi-Column Unique Index"
                onCreate={() => ({ fields: [""] })}
                renderContent={item =>
                  <div className="row">
                    <div className="col-sm-6">
                      <ComboBoxRepeaterComponent options={def.properties.filter(p => p.isMList == null).map(p => "e." + p.name)} list={item.fields} />
                    </div>
                    <div className="col-sm-6">
                      <CSharpExpressionCodeMirror binding={Binding.create(item, i => i.where)} title="Where" signature={"(" + (dt.typeName ?? "") + "Entity e) =>"} />
                    </div>
                  </div>
                }
              />
            }

            <fieldset>
              <legend>ToString expression</legend>
              <CSharpExpressionCodeMirror binding={Binding.create(def, d => d.toStringExpression)} signature={"(" + (dt.typeName ?? "") + dt.baseType + " e) =>"} />
            </fieldset>
          </Tab>

          {dt.baseType == "Entity" &&
            <Tab eventKey="query" title="Query">
            <ComboBoxRepeaterComponent options={["e.Id"].concat(propNames).concat(expressionNamesStr)} list={def.queryFields} />
            </Tab>
          }

          {dt.baseType == "Entity" &&
            <Tab eventKey="operations" title="Operations">
              <div className="row">
                <div className="col-sm-7">
                  <CreateOperationFieldsetComponent
                    binding={Binding.create(def, d => d.operationCreate)}
                    title="Create"
                    onCreate={() => ({ construct: getConstructor(dt.typeName, def) })}
                    renderContent={oc => <CSharpExpressionCodeMirror binding={Binding.create(oc, d => d.construct)} signature={"(object[] args) =>"} />}
                  />

                  <SaveOperationFieldsetComponent
                    binding={Binding.create(def, d => d.operationSave)}
                    title="Save"
                    onCreate={() => ({ execute: "" })}
                    renderContent={oe =>
                      <div>
                        <CSharpExpressionCodeMirror binding={Binding.create(oe, d => d.canExecute)} title="CanSave" signature={"string (" + dt.typeName + "Entity e) =>"} />
                        <CSharpExpressionCodeMirror binding={Binding.create(oe, d => d.execute)} title="OperationSave" signature={"(" + dt.typeName + "Entity e, object[] args) =>"} />
                      </div>}
                  />

                  <DeleteOperationFieldsetComponent
                    binding={Binding.create(def, d => d.operationDelete)}
                    title="Delete"
                    onCreate={() => ({ delete: "" })}
                    renderContent={od =>
                      <div>
                        <CSharpExpressionCodeMirror binding={Binding.create(od, d => d.canDelete)} title="CanDelete" signature={"string (" + dt.typeName + "Entity e) =>"} />
                        <CSharpExpressionCodeMirror binding={Binding.create(od, d => d.delete)} title="OperationDelete" signature={"(" + dt.typeName + "Entity e, object[] args) =>"} />
                      </div>}
                  />

                  <CloneOperationFieldsetComponent
                    binding={Binding.create(def, d => d.operationClone)}
                    title="Clone"
                    onCreate={() => ({
                      construct:
                        "// NOTE: This sample code is only for simple properties\n" +
                        "// MList/Embedded/Mixin properties were ignored if exists\n" +
                        "return new " + dt.typeName + "Entity\n{\n" +
                        def.properties
                          .filter(p => !p.isMList && !isEmbedded(p.type))
                          .map(p => "    " + p.name + " = e." + p.name).join(", \n") +
                        "\n};"
                    })}
                    renderContent={oc =>
                      <div>
                        <CSharpExpressionCodeMirror binding={Binding.create(oc, d => d.canConstruct)} title="CanConstruct" signature={"string (" + dt.typeName + "Entity e) =>"} />
                        <CSharpExpressionCodeMirror binding={Binding.create(oc, d => d.construct)} title="OperationClone" signature={`${dt.typeName}Entity (${dt.typeName}Entity e, object[] args) =>`} />
                      </div>}
                  />
                </div>
                <div className="col-sm-5">
                  {!dt.isNew &&
                  <TypeHelpComponent initialType={dt.typeName!} mode="CSharp" onMemberClick={handleTypeHelpClick} />
                  }
                </div>
              </div>
            </Tab>
          }

          <Tab eventKey="customCode" title="Custom Code">
            <CustomCodeTab definition={def} dynamicType={dt} />
          </Tab>

          {!dt.isNew && dt.baseType == "MixinEntity" &&
            <Tab eventKey="connections" title="Apply To">
              <SearchControl findOptions={{
                queryName: DynamicMixinConnectionEntity,
                filterOptions: [{ token: DynamicMixinConnectionEntity.token(e => e.mixinName), value: dt.typeName}]
              }} />
            </Tab>
          }

        {!dt.isNew && dt.baseType == "Entity" && typeEntity != null &&
            <Tab eventKey="connections" title="Mixins">
            {typeEntity == false ? <p className="alert alert-warning">{DynamicTypeMessage.TheEntityShouldBeSynchronizedToApplyMixins.niceToString()}</p> :
                <SearchControl findOptions={{
                  queryName: DynamicMixinConnectionEntity,
                  filterOptions: [{ token: DynamicMixinConnectionEntity.token(e => e.entityType), value: typeEntity}]
                }} />
              }
            </Tab>
          }

          {!dt.isNew && dt.baseType == "Entity" &&
            <Tab eventKey="other" title="Other">
            {renderOthers()}
            </Tab>
          }
      </Tabs>
      </div>
    );
  }


export function CustomCodeTab(p: { definition: DynamicTypeDefinition, dynamicType: DynamicTypeEntity }): React.JSX.Element {

  function getDynamicTypeFullName() {
    var suffix = p.dynamicType.baseType == "MixinEntity" ? "Mixin" : "Entity";

    return `${p.dynamicType.typeName}${suffix}`;
            }

  function handleWorkflowCustomInheritanceClick() {
    popupCodeSnippet("ICaseMainEntity");
            }

  function handleTreeCustomInheritanceClick() {
    popupCodeSnippet("TreeEntity");
            }

  function handleSMSInheritanceClick() {
    popupCodeSnippet("ISMSOwnerEntity");
            }

  function handleEmailInheritanceClick() {
    popupCodeSnippet("IEmailOwnerEntity");
          }


  function handlePreSavingClick() {
    popupCodeSnippet(`protected override void PreSaving(PreSavingContext ctx)
{
    base.PreSaving(ctx);
    //Your code here
}`);
  }

  function handlePostRetrievingClick() {
    popupCodeSnippet(`protected override void PostRetrieving()
{
    //Your code here
}`);
  }

  function handlePropertyValidatorClick() {
    popupCodeSnippet(`protected override string? PropertyValidation(PropertyInfo pi)
{
    if (pi.Name == nameof(YourProperty))
    {
        if(YourProperty == "AAA")
            return "AAA is not a valid value";
    }

    return base.PropertyValidation(pi);
}`);
  }

  function handleSMSOwnerDataClick() {
    let entityName = p.dynamicType.typeName!;
    popupCodeSnippet(`public static Expression<Func<${entityName}Entity, SMSOwnerData>> SMSOwnerDataExpression =
@this =>  new SMSOwnerData
{
    CultureInfo = null,
    Owner = @ToLite(),
    TelephoneNumber = @[property that points to telephone number],
};
[ExpressionField("SMSOwnerDataExpression")]
public SMSOwnerData SMSOwnerData
{
    get { return SMSOwnerDataExpression.Evaluate(this); }
}`);
  }

  function handleEmailOwnerDataClick() {
    let entityName = p.dynamicType.typeName!;
    popupCodeSnippet(`public static Expression<Func<${entityName}Entity, EmailOwnerData>> EmailOwnerDataExpression = @this => new EmailOwnerData
{
    CultureInfo = null,
    DisplayName = null,
    Owner = @ToLite(),
    Email = @[property that points to email],
};
[ExpressionField("EmailOwnerDataExpression")]
public EmailOwnerData EmailOwnerData
{
    get { return EmailOwnerDataExpression.Evaluate(this); }
}`) 
  }

  function handleWithWorkflowClick() {
    let entityName = p.dynamicType.typeName!;
    const def = p.definition;
    var os = def.operationSave;
    var oc = def.operationCreate;

    popupCodeSnippet(`fi.WithWorkflow(
  constructor: () => { ${oc ? `return OperationLogic.Construct(${entityName}Operation.Create);` : getConstructor(entityName, def)} },
save: e => ${os ? `e.Execute(${entityName}Operation.Save)` : "e.Save()"}
);`);
  }

  function handleWithTreeClick() {
    popupCodeSnippet(`fi.WithTree();`);
  }

  function handleOverrideCreateRoot() {
    let entityName = p.dynamicType.typeName!;
    popupCodeSnippet(`Graph<${entityName}Entity>.Construct.Untyped(TreeOperation.CreateRoot).Do(a =>
{
    c.Construct = (args) => new ${entityName}Entity
    {
        ParentOrSibling = null,
        Level = 1,
        IsSibling = false,
        // Write your code here ...
    };
    c.Register(replace: true);
});`);
    }

  function handleOverrideCreateChild() {
    let entityName = p.dynamicType.typeName!;
    popupCodeSnippet(`Graph<${entityName}Entity>.ConstructFrom<${entityName}Entity>.Untyped(TreeOperation.CreateChild).Do(c =>
{
    c.Construct = (t, _) => new ${entityName}Entity
    {
        ParentOrSibling = t.ToLite(),
        Level = (short)(t.Level + 1),
        IsSibling = false,
        // Write your code here ...
    };
    c.Register(replace: true);
});`);
    }

  function handleOverrideNextSibling () {
    let entityName = p.dynamicType.typeName!;
    popupCodeSnippet(`Graph<${entityName}Entity>.ConstructFrom<${entityName}Entity>.Untyped(TreeOperation.CreateNextSibling).Do(c =>
{
    c.Construct = (t, _) => new ${entityName}Entity
    {
        ParentOrSibling = t.ToLite(),
        Level = t.Level,
        IsSibling = true,
        // Write your code here ...
    };
    c.Register(replace: true);
});`);
    }

  function handleRegisterOperationsClick() {

    let entityName = p.dynamicType.typeName!;

    popupCodeSnippet(
      `// Sample for Delete symbol operations
new Graph<${entityName}Entity>.Delete(${entityName}Operation.Delete)
{
    FromStates = { },
    CanDelete = e => return null or a message string,
    Delete = (e) => { e.Delete(); },
}.Register();

// Sample for Execute symbol operations
new Graph<${entityName}Entity>.Execute(${entityName}Operation.Save)
{
    FromStates = { },
    ToStates = { },
    CanExecute = e => return null or a message string,
    Execute = (e, args) => { e.Save(); },
}.Register();`);
  }

  function handleRegisterExpressionsClick() {
    let entityName = p.dynamicType.typeName!;
    popupCodeSnippet(`QueryLogic.Expressions.Register((${entityName}Entity e) => e.[Expression Name]());`);
  }

  function handleQueryExpressionClick() {
    let entityName = p.dynamicType.typeName!;
    popupCodeSnippet(
      `static Expression<Func<[Your Entity], IQueryable<${entityName}Entity>>> QueryExpression =
    e => Database.Query<${entityName}Entity>().Where(a => [Your conditions here]);
[ExpressionField("QueryExpression")]
public static IQueryable<${entityName}Entity> Queries(this [Your Entity] e)
{
    return QueryExpression.Evaluate(e);
}`);
  }

  function handleScalarExpressionClick() {
    let entityName = p.dynamicType.typeName!;
    popupCodeSnippet(`static Expression<Func<${entityName}Entity, bool>> IsDisabledExpression =
    e => [Your conditions here] ;
[ExpressionField]
public static bool IsDisabled(this ${entityName}Entity entity)
{
    return IsDisabledExpression.Evaluate(entity);
}`);
  }

  function handleCreateSMSModelClick() {
    let entityName = p.dynamicType.typeName!;
    popupCodeSnippet(`public class ${entityName}SMS : SMSModel<${entityName}Entity>
{
    public ${entityName}SMS(${entityName}Entity entity) : base(entity)
    { }
}`);
  }

  function handleSMSProviderClick() {
    popupCodeSnippet(`public class SMSProvider : ISMSProvider
{
    public List<string> SMSMultipleSendAction(MultipleSMSModel template, List<string> phones)
    {
        return phones.Select(p => Guid.NewGuid().ToString()).ToList();
    }

    public string SMSSendAndGetTicket(SMSMessageEntity message)
    {
        return Guid.NewGuid().ToString();
    }

    public SMSMessageState SMSUpdateStatusAction(SMSMessageEntity message)
    {
        return SMSMessageState.Delivered;
    }
}`);
  }

  function handleCreateEmailModelClassClick() {
    let entityName = p.dynamicType.typeName!;
    popupCodeSnippet(`public class ${entityName}Email : EmailModel<${entityName}Entity>
{
    public ${entityName}Email(${entityName}Entity entity) : base(entity)
    { }
}`);
  }

  function handleRegisterSMSOwnerDataClick() {
    let entityName = p.dynamicType.typeName!;
    popupCodeSnippet(`SMSProcessLogic.RegisterSMSOwnerData((${entityName}Entity e) => e.SMSOwnerData);`);
  }

  function handleRegisterSMSModelClick() {
    let entityName = p.dynamicType.typeName!;
    popupCodeSnippet(`SMSModelLogic.RegisterSMSModel<${entityName}SMS>(() => null!);`);
  }

  function handleOverrideSMSProviderClick() {
    popupCodeSnippet(`SMSLogic.Provider = new SMSProvider();`);
  }

  function handleRegisterEmailOwnerDataClick() {
  }

  function handleRegisterEmailModelClick() {
    let entityName = p.dynamicType.typeName!;
    popupCodeSnippet(`EmailModelLogic.RegisterEmailModel<${entityName}Email>(() => null!);`);
  }

  function handleAddUnitClick() {
    let entityName = p.dynamicType.typeName!;
    popupCodeSnippet(`UnitAttribute.UnitTranslations.Add("$", () => ${entityName}Message.Dollar.NiceToString());`);
  }

  function handleEnumClick() {
    popupCodeSnippet(`public enum EnumName
{
    Item1,
    Item2,
    ....
};`);
  }

  function handleOperationClick() {
    let entityName = p.dynamicType.typeName!;

    popupCodeSnippet(`[AutoInit]
public static class ${entityName}Operation2
{
    public static readonly ConstructSymbol<${entityName}Entity>.From<Your Entity> CreateFrom;
    public static readonly ExecuteSymbol<${entityName}Entity> DoSomething;
    public static readonly DeleteSymbol<${entityName}Entity> DeleteSomething;
}`);
  }

  function handleOverrideClick() {
    popupCodeSnippet(`sb.Schema.Settings.FieldAttributes((StaticType ua) => ua.Property).Replace(new ImplementedByAttribute(typeof(YourDynamicTypeEntity)));`);
  }

  function popupCodeSnippet(snippet: string) {
    AutoLineModal.show({
      type: { name: "string" },
      initialValue: snippet,
      customComponent: p => <TextAreaLine {...p}/>,
      title: `Code Snippet`,
      message: "Copy to clipboard: Ctrl+C, ESC",
      valueHtmlAttributes: { style: { height: 150 } },
    });
  }
  const def = p.definition;
  const dt = p.dynamicType;
  const entityName = getDynamicTypeFullName();
  return (
    <div className="row">
      <div className="col-sm-7">
        <CustomCodeFieldsetComponent
          binding={Binding.create(def, d => d.customInheritance)}
          title="Custom Inheritance"
          onCreate={() => ({ code: dt.baseType })}
          renderContent={e =>
            <div>
              <div className="btn-group" style={{ marginBottom: "3px" }}>
                {dt.baseType == "Entity" && CustomCodeTab.suggestWorkflow &&
                  <input type="button" className="btn btn-success btn-xs sf-button" value="Workflow" onClick={handleWorkflowCustomInheritanceClick} />}

                {dt.baseType == "Entity" && CustomCodeTab.suggestTree &&
                  <input type="button" className="btn btn-warning btn-xs sf-button" value="Tree" onClick={handleTreeCustomInheritanceClick} />}
                {dt.baseType == "Entity" &&
                  <input type="button" className="btn btn-danger btn-xs sf-button" value="SMS" onClick={handleSMSInheritanceClick} />}
              </div>
              <div className="code-container">
                <pre style={{ border: "0px", margin: "0px" }}>{`public class ${entityName}:`}</pre>
                <CSharpExpressionCodeMirror binding={Binding.create(e, d => d.code)} />
              </div>
            </div>
          }
        />

        <CustomCodeFieldsetComponent
          binding={Binding.create(def, d => d.customEntityMembers)}
          title="Entity Members"
          onCreate={() => ({ code: "" })}
          renderContent={e =>
            <div>
              <div className="btn-group" style={{ marginBottom: "3px" }}>
                <input type="button" className="btn btn-warning btn-xs sf-button" value="Pre Saving" onClick={handlePreSavingClick} />
                <input type="button" className="btn btn-success btn-xs sf-button" value="Post Retrieving" onClick={handlePostRetrievingClick} />
                <input type="button" className="btn btn-info btn-xs sf-button" value="Property Validator" onClick={handlePropertyValidatorClick} />
                {dt.baseType == "Entity" &&
                  <input type="button" className="btn btn-danger btn-xs sf-button" value="SMS Owner Data" onClick={handleSMSOwnerDataClick} />}
              </div>
              <div className="code-container">
                <pre style={{ border: "0px", margin: "0px" }}>{`public class ${entityName}
{`}</pre>
                <CSharpExpressionCodeMirror binding={Binding.create(e, d => d.code)} />
                <pre style={{ border: "0px", margin: "0px" }}>{`}`}</pre>
              </div>
            </div>
}
        />

        <CustomCodeFieldsetComponent
          binding={Binding.create(def, d => d.customStartCode)}
          title="Start Code"
          onCreate={() => ({ code: "" })}
          renderContent={e =>
            <div>
              {dt.baseType == "Entity" &&
                <div>
                  <div className="btn-group" style={{ marginBottom: "3px" }}>
                    {CustomCodeTab.suggestWorkflow && <input type="button" className="btn btn-success btn-xs sf-button" value="Workflow" onClick={handleWithWorkflowClick} />}
                    {CustomCodeTab.suggestTree && <input type="button" className="btn btn-info btn-xs sf-button" value="Tree" onClick={handleWithTreeClick} />}
                    {CustomCodeTab.suggestTree && <input type="button" className="btn btn-info btn-xs sf-button" value="CreateRoot" onClick={handleOverrideCreateRoot} />}
                    {CustomCodeTab.suggestTree && <input type="button" className="btn btn-info btn-xs sf-button" value="CreateChild" onClick={handleOverrideCreateChild} />}
                    {CustomCodeTab.suggestTree && <input type="button" className="btn btn-info btn-xs sf-button" value="NextSibling" onClick={handleOverrideNextSibling} />}
                    <input type="button" className="btn btn-warning btn-xs sf-button" value="Register Operations" onClick={handleRegisterOperationsClick} />
                    <input type="button" className="btn btn-danger btn-xs sf-button" value="Register Expressions" onClick={handleRegisterExpressionsClick} />
                    <input type="button" className="btn btn-info btn-xs sf-button" value="Add Unit" onClick={handleAddUnitClick} />
                  </div>
                </div>}
              <div className="btn-group" style={{ marginBottom: "3px" }}>
                {dt.baseType == "Entity" &&
                  <input type="button" className="btn btn-success btn-xs sf-button" value="Register SMS Owner Data" onClick={handleRegisterSMSOwnerDataClick} />}
                <input type="button" className="btn btn-danger btn-xs sf-button" value="Register SMS Model" onClick={handleRegisterSMSModelClick} />
                <input type="button" className="btn btn-info btn-xs sf-button" value="Override SMS Provider" onClick={handleOverrideSMSProviderClick} />
              </div>
              <div className="code-container">
                <pre style={{ border: "0px", margin: "0px" }}>{`SchemaBuilder sb, FluentInclude<${entityName}> fi`}</pre>
                <CSharpExpressionCodeMirror binding={Binding.create(e, d => d.code)} />
              </div>
            </div>
          } />

        <CustomCodeFieldsetComponent
          binding={Binding.create(def, d => d.customLogicMembers)}
          title="Logic Members"
          onCreate={() => ({ code: "" })}
          renderContent={e =>
            <div>
              <div className="btn-group" style={{ marginBottom: "3px" }}>
                {dt.baseType == "Entity" && <input type="button" className="btn btn-success btn-xs sf-button" value="Query Expression" onClick={handleQueryExpressionClick} />}
                {dt.baseType == "Entity" || dt.baseType == "EmbeddedEntity" && <input type="button" className="btn btn-warning btn-xs sf-button" value="Scalar Expression" onClick={handleScalarExpressionClick} />}
                <input type="button" className="btn btn-danger btn-xs sf-button" value="SMS Model" onClick={handleCreateSMSModelClick} />
                <input type="button" className="btn btn-info btn-xs sf-button" value="SMS Provider" onClick={handleSMSProviderClick} />
              </div>
              <div className="code-container">
                <pre style={{ border: "0px", margin: "0px" }}>{`public static class ${dt.typeName}Logic
{`}</pre>
                <CSharpExpressionCodeMirror binding={Binding.create(e, d => d.code)} />
                <pre style={{ border: "0px", margin: "0px" }}>{`}`}</pre>
              </div>
            </div>
          }
        />

        <CustomCodeFieldsetComponent
          binding={Binding.create(def, d => d.customTypes)}
          title="Types"
          onCreate={() => ({ code: "" })}
          renderContent={e =>
            <div>
              <div className="btn-group" style={{ marginBottom: "3px" }}>
                <input type="button" className="btn btn-success btn-xs sf-button" value="Enum" onClick={handleEnumClick} />
                {dt.baseType == "Entity" && <input type="button" className="btn btn-warning btn-xs sf-button" value="Operation" onClick={handleOperationClick} />}
              </div>
              <div className="code-container">
                <pre style={{ border: "0px", margin: "0px" }}>{`public namespace Signum.Entities.CodeGen
  {`}</pre>
                <CSharpExpressionCodeMirror binding={Binding.create(e, d => d.code)} />
                <pre style={{ border: "0px", margin: "0px" }}>{`}`}</pre>
              </div>
            </div>
          }
        />


        <CustomCodeFieldsetComponent
          binding={Binding.create(def, d => d.customBeforeSchema)}
          title="Before Schema"
          onCreate={() => ({ code: "" })}
          renderContent={e =>
            <div>
              <div className="btn-group" style={{ marginBottom: "3px" }}>
                <input type="button" className="btn btn-success btn-xs sf-button" value="Override" onClick={handleOverrideClick} />
              </div>
              <div className="code-container">
                <pre style={{ border: "0px", margin: "0px" }}>{`public void OverrideSchema(SchemaBuilder sb)
{`}</pre>
                <CSharpExpressionCodeMirror binding={Binding.create(e, d => d.code)} />
                <pre style={{ border: "0px", margin: "0px" }}>{`}`}</pre>
              </div>
            </div>
          }
        />
      </div>
      <div className="col-sm-5">
        {!dt.isNew &&
          <TypeHelpComponent initialType={dt.typeName!} mode="CSharp" />
        }
      </div>
    </div>
  );
}

export namespace CustomCodeTab {
  export let suggestWorkflow = true;
  export let suggestTree = true;
}


export interface CSharpExpressionCodeMirrorProps {
  binding: Binding<string | undefined>;
  title?: string;
  signature?: string;
}

export function CSharpExpressionCodeMirror(p: CSharpExpressionCodeMirrorProps): React.JSX.Element {
  const forceUpdate = useForceUpdate();
  let val = p.binding.getValue();

    return (
      <div>
      {p.title && <h5> <strong>{p.title}</strong></h5>}
        <div className="code-container">
        {p.signature && <pre style={{ border: "0px", margin: "0px" }}>{p.signature}</pre>}
          <div className="small-codemirror">
            <CSharpCodeMirror
              script={val ?? ""}
            onChange={newScript => { p.binding.setValue(newScript); forceUpdate(); }} />
          </div>
        </div>
      </div>
    );
  }

export interface CustomFieldsetComponentProps<T> {
  binding: Binding<T | undefined>;
  title?: React.ReactElement | string;
  renderContent: (item: T) => React.ReactElement<any>;
  onCreate: () => T;
  onChange?: () => void;
}

export class CustomFieldsetComponent<T> extends React.Component<CustomFieldsetComponentProps<T>> {

  handleChecked = (e: React.FormEvent<any>) : void=> {
    let val = this.props.binding.getValue();
    if (val)
      this.props.binding.deleteValue();
    else
      this.props.binding.setValue(this.props.onCreate());

    this.forceUpdate();

    if (this.props.onChange)
      this.props.onChange();
  }

  render(): JSX.Element {
    let value = this.props.binding.getValue();
    return (
      <fieldset style={{ marginTop: "-5px" }}>
        <legend><input type="checkbox" className="form-check-input" checked={!!value} onChange={this.handleChecked} /> {this.props.title ?? this.props.binding.member.toString().firstUpper()}</legend>
        {value && this.props.renderContent(value)}
      </fieldset>
    );
  }
}

type PrimaryKeyFieldsetComponent = new () => CustomFieldsetComponent<DynamicTypeClient.DynamicTypePrimaryKeyDefinition>;
const PrimaryKeyFieldsetComponent = CustomFieldsetComponent as PrimaryKeyFieldsetComponent;

type TicksFieldsetComponent = new () => CustomFieldsetComponent<DynamicTypeClient.DynamicTypeTicksDefinition>;
const TicksFieldsetComponent = CustomFieldsetComponent as TicksFieldsetComponent;

type MultiColumnUniqueIndexFieldsetComponent = new () => CustomFieldsetComponent<DynamicTypeClient.MultiColumnUniqueIndex>;
const MultiColumnUniqueIndexFieldsetComponent = CustomFieldsetComponent as MultiColumnUniqueIndexFieldsetComponent;

type CreateOperationFieldsetComponent = new () => CustomFieldsetComponent<DynamicTypeClient.OperationConstruct>;
const CreateOperationFieldsetComponent = CustomFieldsetComponent as CreateOperationFieldsetComponent;

type DeleteOperationFieldsetComponent = new () => CustomFieldsetComponent<DynamicTypeClient.OperationDelete>;
const DeleteOperationFieldsetComponent = CustomFieldsetComponent as DeleteOperationFieldsetComponent;

type SaveOperationFieldsetComponent = new () => CustomFieldsetComponent<DynamicTypeClient.OperationExecute>;
const SaveOperationFieldsetComponent = CustomFieldsetComponent as SaveOperationFieldsetComponent;

type CloneOperationFieldsetComponent = new () => CustomFieldsetComponent<DynamicTypeClient.OperationConstructFrom>;
const CloneOperationFieldsetComponent = CustomFieldsetComponent as CloneOperationFieldsetComponent;

type IsMListFieldsetComponent = new () => CustomFieldsetComponent<DynamicTypeClient.DynamicTypeBackMListDefinition>;
const IsMListFieldsetComponent = CustomFieldsetComponent as IsMListFieldsetComponent;

type CustomCodeFieldsetComponent = new () => CustomFieldsetComponent<DynamicTypeClient.DynamicTypeCustomCode>;
const CustomCodeFieldsetComponent = CustomFieldsetComponent as CustomCodeFieldsetComponent;

export interface PropertyRepeaterComponentProps {
  properties: DynamicProperty[];
  dc: DynamicTypeDesignContext;
  onRemove?: (dp: DynamicProperty) => void;
  showDatabaseMapping: boolean;
}

export function PropertyRepeaterComponent(p: PropertyRepeaterComponentProps): React.JSX.Element {

  const [currentEventKey, setCurrentEventKey] = React.useState<number | undefined>(0);

  React.useEffect(() => {
    p.properties.forEach(a => fetchPropertyType(a, p.dc));
  }, []);

  React.useEffect(() => {
    p.properties.filter(a => a._propertyType_ == undefined).forEach(a => fetchPropertyType(a, p.dc));
  });

  function handleSelect(eventKey: number) {
    setCurrentEventKey(eventKey == currentEventKey ? undefined : eventKey);
  }

  function handleOnRemove(event: React.MouseEvent<any>, index: number) {
    event.preventDefault();
    event.stopPropagation();
    var old = p.properties[index];
    p.properties.removeAt(index);

    if (currentEventKey == index)
      setCurrentEventKey(undefined);

    p.dc.refreshView();

    if (p.onRemove)
      p.onRemove(old);
  }

  function handleOnMoveUp(event: React.MouseEvent<any>, index: number) {
    event.preventDefault();
    event.stopPropagation();
    const newIndex = p.properties.moveUp(index);
    if (newIndex != index) {
      if (index == currentEventKey)
        setCurrentEventKey(currentEventKey - 1);
      else if (newIndex == currentEventKey)
        setCurrentEventKey(currentEventKey + 1);
    }

    p.dc.refreshView();
  }

  function handleOnMoveDown(event: React.MouseEvent<any>, index: number) {
    event.preventDefault();
    event.stopPropagation();
    const newIndex = p.properties.moveDown(index);

    if (newIndex != index) {
      if (index == currentEventKey)
        setCurrentEventKey(currentEventKey + 1);
      else if (newIndex == currentEventKey)
        setCurrentEventKey(currentEventKey - 1);
    }

    p.dc.refreshView();
  }

  function handleCreateClick(event: React.SyntheticEvent<any>) {
    event.preventDefault();

    var dp = {
      uid: createGuid(),
      name: "Name",
      type: "string",
      isNullable: "No",
    } as DynamicProperty;
    autoFix(dp);
    p.properties.push(dp);
    setCurrentEventKey(p.properties.length - 1);
    p.dc.refreshView();

    fetchPropertyType(dp, p.dc);
  }

  function createGuid() {
    let d = new Date().getTime();
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
      let r = (d + Math.random() * 16) % 16 | 0;
      d = Math.floor(d / 16);
      return (c == 'x' ? r : (r & 0x3 | 0x8)).toString(16);
    });
  }


  function renderPropertyHeader(p: DynamicProperty, i: number) {
    return (
      <div>

        <span className="item-group">
          <a href="#" className={classes("sf-line-button", "sf-remove")}
            onClick={e => handleOnRemove(e, i)}
            title={EntityControlMessage.Remove.niceToString()}>
            <FontAwesomeIcon icon="xmark" />
          </a>

          <a href="#" className={classes("sf-line-button", "move-up")}
            onClick={e => handleOnMoveUp(e, i)}
            title={EntityControlMessage.MoveUp.niceToString()}>
            <FontAwesomeIcon icon="chevron-up" />
          </a>

          <a href="#" className={classes("sf-line-button", "move-down")}
            onClick={e => handleOnMoveDown(e, i)}
            title={EntityControlMessage.MoveDown.niceToString()}>
            <FontAwesomeIcon icon="chevron-down" />
          </a>
        </span>
        {" " + (p._propertyType_ ?? "") + " " + p.name}
      </div>
    );
  }
  return (
    <div className="properties">
      <div>
        {
          p.properties.map((dp, i) =>
            <CollapsableCard
              key={i}
              header={renderPropertyHeader(dp, i)}
              cardStyle={{ background: "light" }}
              headerStyle={{ text: "secondary" }}
              isOpen={currentEventKey == i}
              toggle={() => handleSelect(i)} >
              <PropertyComponent property={dp} dc={p.dc} showDatabaseMapping={p.showDatabaseMapping} />
            </CollapsableCard>)
        }
      </div>
      <a href="#" title="Create Property"
        className="sf-line-button sf-create"
        onClick={handleCreateClick}>
        <FontAwesomeIcon icon="plus" className="sf-create" />&nbsp;Create Property
              </a>
    </div>
  );
}

function fetchPropertyType(p: DynamicProperty, dc: DynamicTypeDesignContext) {
  p._propertyType_ = "";
  DynamicTypeClient.API.getPropertyType(p).then(s => {
    p._propertyType_ = s;
    dc.refreshView();
  })
}

function getConstructor(typeName: string, definition: DynamicTypeDefinition) {
    return "return new " + typeName + "Entity()\n{\n" +
      definition.properties.map(p => "    " + p.name + " = null").join(", \n") +
      "\n};"
}

export interface PropertyComponentProps {
  property: DynamicProperty;
  showDatabaseMapping: boolean;
  dc: DynamicTypeDesignContext;
}

export function PropertyComponent(p: PropertyComponentProps): React.JSX.Element {
  function handleAutoFix() {
    const dp = p.property;

    autoFix(dp);

    p.dc.refreshView();

    fetchPropertyType(dp, p.dc);
  }

  const dp = p.property
  const dc = p.dc;
    return (
      <div>
        <div className="row">
          <div className="col-sm-6">
          <ValueComponent dc={dc} labelColumns={4} binding={Binding.create(dp, d => d.name)} type="string" defaultValue={null} onBlur={handleAutoFix} />
          {p.showDatabaseMapping &&
            <ValueComponent dc={dc} labelColumns={4} binding={Binding.create(dp, d => d.columnName)} type="string" defaultValue={null} labelClass="database-mapping" />
            }
          <TypeCombo dc={dc} labelColumns={4} binding={Binding.create(dp, d => d.type)} onBlur={handleAutoFix} />
          {p.showDatabaseMapping &&
            <ValueComponent dc={dc} labelColumns={4} binding={Binding.create(dp, d => d.columnType)} type="string" defaultValue={null} labelClass="database-mapping" />
            }
          <ValueComponent dc={dc} labelColumns={4} binding={Binding.create(dp, d => d.isNullable)} type="string" defaultValue={null} options={DynamicTypeClient.IsNullableValues} onChange={handleAutoFix} />
          {allowUnit(dp.type) &&
            <ValueComponent dc={dc} labelColumns={4} binding={Binding.create(dp, d => d.unit)} type="string" defaultValue={null} />
            }
          {allowFormat(dp.type) &&
            <ValueComponent dc={dc} labelColumns={4} binding={Binding.create(dp, d => d.format)} type="string" defaultValue={null} />
            }
          {(dp.isMList || isEmbedded(dp.type)) &&
            <ValueComponent dc={dc} labelColumns={4} binding={Binding.create(dp, d => d.notifyChanges)} type="boolean" defaultValue={null} />
            }
          </div>
          <div className="col-sm-6">
            <IsMListFieldsetComponent
            binding={Binding.create(dp, d => d.isMList)}
              title="Is MList"
              onCreate={() => ({ preserveOrder: true })}
              renderContent={mle =>
                <div className="database-mapping">
                <ValueComponent dc={dc} labelColumns={4} binding={Binding.create(mle, d => d.preserveOrder)} type="boolean" defaultValue={null} />
                <ValueComponent dc={dc} labelColumns={4} binding={Binding.create(mle, d => d.orderName)} type="string" defaultValue={null} />
                <ValueComponent dc={dc} labelColumns={4} binding={Binding.create(mle, d => d.tableName)} type="string" defaultValue={null} />
                <ValueComponent dc={dc} labelColumns={4} binding={Binding.create(mle, d => d.backReferenceName)} type="string" defaultValue={null} />
                </div>
              }
              onChange={() => {
              fetchPropertyType(dp, dc);
              handleAutoFix();
              }}
            />

          {dp.type && <div>
            {isEntity(dp.type) && <ValueComponent dc={dc} labelColumns={5} binding={Binding.create(dp, d => d.isLite)} type="boolean" defaultValue={null} onChange={handleAutoFix} />}

            {allowsSize(dp.type) &&
              <ValueComponent dc={dc} labelColumns={5} binding={Binding.create(dp, d => d.size)} type="number" defaultValue={null} onBlur={handleAutoFix} />}

            {(isDecimal(dp.type)) &&
              <ValueComponent dc={dc} labelColumns={5} binding={Binding.create(dp, d => d.scale)} type="number" defaultValue={null} onBlur={handleAutoFix} />}

            <ValueComponent dc={dc} labelColumns={5} binding={Binding.create(dp, d => d.uniqueIndex)} type="string" defaultValue={null} options={DynamicTypeClient.UniqueIndexValues} />
            </div>}
          </div>
        </div >
        <br />
      <ValueComponent dc={dc} labelColumns={3} binding={Binding.create(dp, d => d.customFieldAttributes)} type="string" defaultValue={null} onBlur={handleAutoFix} />
      <ValueComponent dc={dc} labelColumns={3} binding={Binding.create(dp, d => d.customPropertyAttributes)} type="string" defaultValue={null} onBlur={handleAutoFix} />
      <ValidatorRepeaterComponent dc={dc} property={dp} />
      </div>
    );
  }

export function TypeCombo(p: { dc: DynamicTypeDesignContext; binding: Binding<string>; labelColumns: number; onBlur: () => void }): JSX.Element {

  function handleGetItems(query: string) {
    return TypeHelpClient.API.autocompleteType({
      query: query,
      limit: 5,
      includeBasicTypes: true,
      includeEntities: true,
      includeEmbeddedEntities: true,
      includeMList: true
    });
  }

  function handleOnChange(newValue: string) {
    p.binding.setValue(newValue);
    p.dc.refreshView();
  }

  let lc = p.labelColumns;
    return (
      <div className="form-group form-group-sm row" >
        <label className={classes("col-form-label col-form-label-sm", "col-sm-" + (lc == null ? 2 : lc))}>
        {p.binding.member}
        </label>
        <div className={"col-sm-" + (lc == null ? 10 : 12 - lc)}>
            <Typeahead
              inputAttrs={{ className: "form-control form-control-sm sf-entity-autocomplete" }}
          onBlur={p.onBlur}
          getItems={handleGetItems}
          value={p.binding.getValue()}
          onChange={handleOnChange} />
          </div>
        </div>
  );
}

function autoFix(p: DynamicProperty) {

  if (p.name && p.name != p.name.firstUpper())
    p.name = p.name.firstUpper();

  if (!p.type)
    return;

  if (p.scale != undefined && !isDecimal(p.type))
    p.scale = undefined;

  if (p.isLite != undefined && !isEntity(p.type))
    p.isLite = undefined;

  if (p.size != undefined && !allowsSize(p.type))
    p.size = undefined;

  if (p.size === undefined && isString(p.type))
    p.size = 200;

  if (!p.validators)
    p.validators = [];

  p.validators = p.validators.filter(dv => registeredValidators[dv.type].allowed(p));

  if (registeredValidators["StringLength"].allowed(p)) {

    var c = p.validators.filter(a => a.type == "StringLength").firstOrNull() as Validators.StringLength | undefined;
    if (!c) {
      p.validators.push(c = { type: "StringLength" } as any as Validators.StringLength);
    }

    if (c.min == null)
      c.min == 3;

    c.max = p.size;
  }

  if (p.validators.length == 0)
    delete p.validators;

  if (p.unit != undefined && !allowUnit(p.type))
    p.unit = undefined;

  if (p.format != undefined && !allowFormat(p.type))
    p.format = undefined;

  if (p.isMList || isEmbedded(p.type))
    p.notifyChanges = true;
  else
    p.notifyChanges = undefined;
}

function allowsSize(type: string) {
  return isString(type) || isReal(type);
}


export interface ComboBoxRepeaterComponentProps {
  options: string[];
  list: string[];
}

export function ComboBoxRepeaterComponent(p: ComboBoxRepeaterComponentProps): React.JSX.Element {
  const forceUpdate = useForceUpdate();
  function handleChange(val: string, index: number) {
    var list = p.list;
    list[index] = val;
    forceUpdate();
  }

  function handleCreateClick(event: React.SyntheticEvent<any>) {
    event.preventDefault();
    p.list.push("");
    forceUpdate();
  }

  function handleOnRemove(event: React.MouseEvent<any>, index: number) {
    event.preventDefault();
    event.stopPropagation();
    p.list.removeAt(index);
    forceUpdate();
  }

  function handleOnMoveUp(event: React.MouseEvent<any>, index: number) {
    event.preventDefault();
    event.stopPropagation();
    p.list.moveUp(index);
    forceUpdate();
  }

  function handleOnMoveDown(event: React.MouseEvent<any>, index: number) {
    event.preventDefault();
    event.stopPropagation();
    p.list.moveDown(index);
    forceUpdate();
  }


  function renderHeader(value: string, i: number) {
    return (
      <tr key={i}>
        <td>
          <span className="item-group">
            <a href="#" className={classes("sf-line-button", "sf-remove")}
              onClick={e => handleOnRemove(e, i)}
              title={EntityControlMessage.Remove.niceToString()}>
              <FontAwesomeIcon icon="xmark" />
            </a>

            <a href="#" className={classes("sf-line-button", "move-up")}
              onClick={e => handleOnMoveUp(e, i)}
              title={EntityControlMessage.MoveUp.niceToString()}>
              <FontAwesomeIcon icon="chevron-up" />
            </a>

            <a href="#" className={classes("sf-line-button", "move-down")}
              onClick={e => handleOnMoveDown(e, i)}
              title={EntityControlMessage.MoveDown.niceToString()}>
              <FontAwesomeIcon icon="chevron-down" />
            </a>
          </span>
        </td>
        <td className="rw-widget-sm">
          <Combobox value={value} key={i}
            data={p.options.filter(o => o == value || !p.list.contains(o))}
            onChange={val => handleChange(val, i)} />
        </td>
      </tr>
    );
  }
  return (
    <div>
      <table className="table table-sm">
        <tbody>
          {
            p.list.map((value, i) => renderHeader(value, i))
          }
          <tr>
            <td colSpan={2}>
              <a href="#" title="Create Query Column"
                className="sf-line-button sf-create"
                onClick={handleCreateClick}>
                <FontAwesomeIcon icon="plus" className="sf-create" />&nbsp;Create Query Column
                              </a>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  );
}

export interface ValidatorRepeaterComponentProps {
  property: DynamicProperty;
  dc: DynamicTypeDesignContext;
}

export function ValidatorRepeaterComponent(p: ValidatorRepeaterComponentProps): React.JSX.Element {
  function handleOnRemove(event: React.MouseEvent<any>, index: number) {
    event.preventDefault();
    var list = p.property.validators!;
    list.removeAt(index);
    if (list.length == 0)
      delete p.property.validators;
    p.dc.refreshView();
  }

  function handleCreateClick(event: React.SyntheticEvent<any>) {
    event.preventDefault();
    let val = p.property.validators!;
    if (val == null)
      p.property.validators = val = [];

    SelectorModal.chooseElement(Dic.getValues(registeredValidators).filter(a => a.allowed(p.property)), {
      title: "New Validator",
      message: "Please select a validator type",
      buttonDisplay: vo => vo.name
    }).then(vo => {
      if (vo == undefined)
        return;

      val.push({ type: vo.name });
      p.dc.refreshView();
    });
  }


  function renderHeader(val: Validators.DynamicValidator, i: number) {
    return (
      <div>
        <span className="item-group">
          <a href="#" className={classes("sf-line-button", "sf-remove")}
            onClick={e => handleOnRemove(e, i)}
            title={EntityControlMessage.Remove.niceToString()}>
            <FontAwesomeIcon icon="xmark" />
          </a>
        </span>
        {" "}
        {val.type}
      </div>
    );
  }
    return (
      <div className="validators">
        <h4>Validators</h4>
        <div className="panel-group">
          {
          (p.property.validators ?? []).map((val, i) =>
              <CollapsableCard
                key={i}
              header={renderHeader(val, i)}
                cardStyle={{ background: "light" }}
                defaultOpen={true}>
              {registeredValidators[val.type].render && registeredValidators[val.type].render!(val, p.dc)}
              </CollapsableCard>
            )
          }
        </div>
        <a href="#" title="Create Validator"
          className="sf-line-button sf-create"
        onClick={handleCreateClick}>
          <FontAwesomeIcon icon="plus" className="sf-create" />&nbsp;Create Validator
                </a>
      </div>
    );
  }

function isReferenceType(type: string) {
  return isEntity(type) || isString(type);
}

function isString(type: string) {
  return type == "string";
}

function isDateTime(type: string) {
  return type == "DateTime";
}

function isDateOnly(type: string) {
  return type == "DateOnly";
}

function isTimeSpan(type: string) {
  return type == "TimeSpan";
}

function isInteger(type: string) {
  return (
    type == "byte" || type == "System.Byte" ||
    type == "sbyte" || type == "System.SByte" ||
    type == "short" || type == "System.Int16" ||
    type == "ushort" || type == "System.UInt16" ||
    type == "int" || type == "System.Int32" ||
    type == "uint" || type == "System.UInt32" ||
    type == "long" || type == "System.Int64" ||
    type == "ulong" || type == "System.UInt64"
  );
}

function isReal(type: string) {
  return (
    type == "float" || type == "System.Single" ||
    type == "double" || type == "System.Double" ||
    type == "decimal" || type == "System.Decimal"
  );
}

function isDecimal(type: string) {
  return (
    type == "decimal" || type == "System.Decimal"
  );
}

function allowUnit(type: string) {
  return isInteger(type) ||
    isDecimal(type) ||
    isReal(type);
}

function allowFormat(type: string) {
  return isInteger(type) ||
    isDecimal(type) ||
    isReal(type) ||
    isDateTime(type) ||
    isDateOnly(type) ||
    isTimeSpan(type);
}

function isEntity(type: string) {
  return type.endsWith("Entity");
}

function isEmbedded(type: string) {
  return type.endsWith("Embedded");
}




export interface ValidatorOptions<T extends Validators.DynamicValidator> {
  name: string;
  allowed: (p: DynamicProperty) => boolean
  render?: (val: T, dc: DynamicTypeDesignContext) => React.ReactElement<any>;
}

export const registeredValidators: { [name: string]: ValidatorOptions<Validators.DynamicValidator> } = {};

export function registerValidator<T extends Validators.DynamicValidator>(options: ValidatorOptions<T>) : void {
  registeredValidators[options.name] = options as ValidatorOptions<Validators.DynamicValidator>;
}

registerValidator<Validators.NotNull>({
  name: "NotNull",
  allowed: p => !p.isMList,
  render: (val, dc) =>
    <div className="row">
      <div className="col-sm-4">
        <ValueComponent dc={dc} labelColumns={6} binding={Binding.create(val, v => v.disabled)} type="boolean" defaultValue={false} />
      </div>
    </div>
});

registerValidator<Validators.StringLength>({
  name: "StringLength",
  allowed: p => !p.isMList && isString(p.type),
  render: (val, dc) =>
    <div className="row">
      <div className="col-sm-4">
        <ValueComponent dc={dc} labelColumns={6} binding={Binding.create(val, v => v.multiLine)} type="boolean" defaultValue={false} />
      </div>
      <div className="col-sm-4">
        <ValueComponent dc={dc} labelColumns={6} binding={Binding.create(val, v => v.min)} type="number" defaultValue={null} />
        <ValueComponent dc={dc} labelColumns={6} binding={Binding.create(val, v => v.max)} type="number" defaultValue={null} />
      </div>
      <div className="col-sm-4">
        <ValueComponent dc={dc} labelColumns={8} binding={Binding.create(val, v => v.allowLeadingSpaces)} type="boolean" defaultValue={val.multiLine} autoOpacity={true} />
        <ValueComponent dc={dc} labelColumns={8} binding={Binding.create(val, v => v.allowTrailingSpaces)} type="boolean" defaultValue={val.multiLine} autoOpacity={true} />
      </div>
    </div>
});

registerValidator<Validators.StringCase>({
  name: "StringCase",
  allowed: p => !p.isMList && isString(p.type),
  render: (val, dc) =>
    <div>
      <ValueComponent dc={dc} binding={Binding.create(val, v => v.textCase)} type="string" options={Validators.StringCaseTypeValues} defaultValue={null} />
    </div>
});

registerValidator<Validators.DynamicValidator>({ name: "EMail", allowed: p => !p.isMList && isString(p.type) });
registerValidator<Validators.DynamicValidator>({ name: "Telephone", allowed: p => !p.isMList && isString(p.type) });
registerValidator<Validators.DynamicValidator>({ name: "MultipleTelephone", allowed: p => !p.isMList && isString(p.type) });
registerValidator<Validators.DynamicValidator>({ name: "NumericText", allowed: p => !p.isMList && isString(p.type) });
registerValidator<Validators.DynamicValidator>({ name: "URL", allowed: p => !p.isMList && isString(p.type) });
registerValidator<Validators.DynamicValidator>({ name: "FileName", allowed: p => !p.isMList && isString(p.type) });
registerValidator<Validators.DynamicValidator>({ name: "Ip", allowed: p => !p.isMList && isString(p.type) });

registerValidator<Validators.DynamicValidator>({ name: "NoRepeat", allowed: p => p.isMList != null });
registerValidator<Validators.CountIs>({
  name: "CountIs",
  allowed: p => p.isMList != null,
  render: (val, dc) =>
    <div className="row">
      <div className="col-sm-6">
        <ValueComponent dc={dc} labelColumns={6} binding={Binding.create(val, v => v.comparisonType)} type="string" options={Validators.ComparisonTypeValues} defaultValue={null} />
      </div>
      <div className="col-sm-6">
        <ValueComponent dc={dc} labelColumns={6} binding={Binding.create(val, v => v.number)} type="number" defaultValue={null} />
      </div>
    </div>
});

registerValidator<Validators.DynamicValidator>({ name: "DateInPast", allowed: p => !p.isMList && isDateTime(p.type) || isDateOnly(p.type) });
registerValidator<Validators.DateTimePrecision>({
  name: "DateTimePrecision",
  allowed: p => !p.isMList && isDateTime(p.type),
  render: (val, dc) =>
    <div>
      <ValueComponent dc={dc} binding={Binding.create(val, v => v.precision)} type="string" options={Validators.DateTimePrecisionTypeValues} defaultValue={null} />
    </div>
});

registerValidator<Validators.TimeSpanPrecision>({
  name: "TimeSpanPrecision",
  allowed: p => !p.isMList && isTimeSpan(p.type),
  render: (val, dc) =>
    <div>
      <ValueComponent dc={dc} binding={Binding.create(val, v => v.precision)} type="string" options={Validators.DateTimePrecisionTypeValues} defaultValue={null} />
    </div>
});

registerValidator<Validators.Decimals>({
  name: "Decimals",
  allowed: p => !p.isMList && isReal(p.type),
  render: (val, dc) =>
    <div>
      <ValueComponent dc={dc} binding={Binding.create(val, v => v.decimalPlaces)} type="number" defaultValue={null} />
    </div>
});

registerValidator<Validators.NumberBetween>({
  name: "NumberBetween",
  allowed: p => !p.isMList && isInteger(p.type),
  render: (val, dc) =>
    <div className="row">
      <div className="col-sm-6">
        <ValueComponent dc={dc} labelColumns={6} binding={Binding.create(val, v => v.min)} type="number" defaultValue={null} />
      </div>
      <div className="col-sm-6">
        <ValueComponent dc={dc} labelColumns={6} binding={Binding.create(val, v => v.max)} type="number" defaultValue={null} />
      </div>
    </div>
});

registerValidator<Validators.NumberIs>({
  name: "NumberIs",
  allowed: p => !p.isMList && isInteger(p.type),
  render: (val, dc) =>
    <div className="row">
      <div className="col-sm-6">
        <ValueComponent dc={dc} labelColumns={6} binding={Binding.create(val, v => v.comparisonType)} type="string" options={Validators.ComparisonTypeValues} defaultValue={null} />
      </div>
      <div className="col-sm-6">
        <ValueComponent dc={dc} labelColumns={6} binding={Binding.create(val, v => v.number)} type="number" defaultValue={null} />
      </div>
    </div>
});
