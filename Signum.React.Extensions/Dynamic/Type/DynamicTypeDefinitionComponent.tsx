import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import * as Combobox from 'react-widgets/lib/Combobox'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater } from '@framework/Lines'
import { classes, Dic } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import { QueryDescription, SubTokensOptions, QueryToken, filterOperations, OrderType, ColumnOptionsMode } from '@framework/FindOptions'
import { getQueryNiceName, Binding, EntityDataValues, EntityKindValues, EntityKind, PropertyRoute } from '@framework/Reflection'
import * as Navigator from '@framework/Navigator'
import { SearchControl } from '@framework/Search'
import { StyleContext, FormGroupStyle } from '@framework/TypeContext'
import QueryTokenBuilder from '@framework/SearchControl/QueryTokenBuilder'
import { ModifiableEntity, JavascriptMessage, EntityControlMessage, is, Lite, Entity, toLite } from '@framework/Signum.Entities'
import { QueryEntity, TypeEntity } from '@framework/Signum.Entities.Basics'
import { FilterOperation, PaginationMode } from '@framework/Signum.Entities.DynamicQuery'
import SelectorModal from '@framework/SelectorModal';
import MessageModal from '@framework/Modals/MessageModal'
import * as DynamicTypeClient from '../DynamicTypeClient';
import * as DynamicClientOptions from '../DynamicClientOptions';
import * as TypeHelpClient from '../../TypeHelp/TypeHelpClient';
import { DynamicTypeMessage, DynamicTypeEntity, DynamicMixinConnectionEntity } from '../Signum.Entities.Dynamic';
import { Validators, DynamicTypeDefinition, DynamicProperty } from '../DynamicTypeClient';
import ValueComponent from './ValueComponent';
import TypeHelpComponent from '../../TypeHelp/TypeHelpComponent'
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror';
import ContextMenu from '@framework/SearchControl/ContextMenu'
import { ContextMenuPosition } from '@framework/SearchControl/ContextMenu'
import ValueLineModal from '@framework/ValueLineModal'

import "./DynamicType.css"
import { Tabs, Tab, UncontrolledTabs } from '@framework/Components/Tabs';
import CollapsableCard from '../../Basics/Templates/CollapsableCard';
import { Typeahead } from '@framework/Components';

export interface DynamicTypeDesignContext {
  refreshView: () => void;
}

interface DynamicTypeDefinitionComponentProps {
  dynamicType: DynamicTypeEntity;
  definition: DynamicTypeDefinition;
  dc: DynamicTypeDesignContext;
  showDatabaseMapping: boolean;
}

interface DynamicTypeDefinitionComponentState {
  expressionsNames?: string[];
  typeEntity?: Lite<TypeEntity> | false;
}

export class DynamicTypeDefinitionComponent extends React.Component<DynamicTypeDefinitionComponentProps, DynamicTypeDefinitionComponentState> {

  constructor(props: DynamicTypeDefinitionComponentProps) {
    super(props);
    this.state = {};

    this.initialize(props);
  }

  componentWillReceiveProps(nextProps: DynamicTypeDefinitionComponentProps) {
    this.initialize(nextProps);
  }

  initialize(props: DynamicTypeDefinitionComponentProps) {
    if (props.dynamicType.isNew)
      this.fixSaveOperation();

    if (props.dynamicType.baseType == "Entity") {
      if (props.dynamicType.isNew)
        this.setState({ typeEntity: undefined });
      else
        Navigator.API.getType(props.dynamicType.typeName!)
          .then(te => this.setState({ typeEntity: te ? toLite(te) : false }))
          .done();
    } else {
      this.setState({ typeEntity: undefined });
    }
  }

  handleTabSelect = (eventKey: any /*string*/) => {
    var dt = this.props.dynamicType;
    if (!dt.isNew && dt.typeName && eventKey == "query")
      DynamicTypeClient.API.expressionNames(dt.typeName + "Entity")
        .then(exprNames => this.setState({ expressionsNames: exprNames }))
        .done();
  }

  handlePropertyRemoved = (dp: DynamicProperty) => {
    var qfs = this.props.definition.queryFields;
    if (qfs && qfs.contains(dp.name))
      qfs.remove(dp.name);

    this.props.dc.refreshView();
  }

  handleEntityKindChange = () => {
    this.fixSaveOperation();
    this.forceUpdate();
  }

  handleHasTickChanged = () => {
    var ticks = this.props.definition.ticks;

    if (ticks)
      this.props.definition.ticks = {
        hasTicks: ticks.hasTicks,
        name: ticks.hasTicks ? "Ticks" : undefined,
        type: ticks.hasTicks ? "int" : undefined,
      };
  }

  fixSaveOperation() {
    const def = this.props.definition;
    var requiresSave = this.props.dynamicType.baseType == "Entity" &&
      def.entityKind != undefined &&
      DynamicTypeDefinitionComponent.requiresSave.contains(def.entityKind);

    if (requiresSave && !def.operationSave) {
      def.operationSave = { execute: "" };
    } else if (!requiresSave && def.operationSave) {
      if (this.isEmpty(def.operationSave))
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
        }).done();
      }
    }
  }

  isEmpty(operation: DynamicTypeClient.OperationExecute) {
    return !operation.execute && !operation.canExecute;
  }

  static requiresSave: EntityKind[] = ["Main", "Shared", "String", "Relational"];

  handleTypeHelpClick = (pr: PropertyRoute | undefined) => {
    if (!pr)
      return;

    ValueLineModal.show({
      type: { name: "string" },
      initialValue: TypeHelpComponent.getExpression("e", pr, "CSharp"),
      valueLineType: "TextArea",
      title: "Property Template",
      message: "Copy to clipboard: Ctrl+C, ESC",
      initiallyFocused: true,
    }).done();
  }

  render() {
    const def = this.props.definition;
    const primaryKey = def.primaryKey!;
    const ticks = def.ticks!;

    var propNames = def.properties.map(p => "e." + p.name);

    var expressionNames = (this.state.expressionsNames || []).map(exp => exp + "= e." + exp + "()");

    var dt = this.props.dynamicType;

    return (
      <div>
        {dt.baseType == "Entity" &&
          <div>
            {this.props.showDatabaseMapping &&
              <ValueComponent dc={this.props.dc} labelColumns={2} binding={Binding.create(def, d => d.tableName)} type="string" defaultValue={null} labelClass="database-mapping" />
            }

            < div className="row">
              <div className="col-sm-6">
                <ValueComponent dc={this.props.dc} labelColumns={4} binding={Binding.create(def, d => d.entityKind)} type="string" defaultValue={null} options={EntityKindValues} onChange={this.handleEntityKindChange} />
              </div>
              <div className="col-sm-6">
                <ValueComponent dc={this.props.dc} labelColumns={4} binding={Binding.create(def, d => d.entityData)} type="string" defaultValue={null} options={EntityDataValues} />
              </div>
            </div>

            {this.props.showDatabaseMapping &&
              <div className="row database-mapping">
                <div className="col-sm-6">
                  <PrimaryKeyFieldsetComponent
                    binding={Binding.create(def, d => d.primaryKey)}
                    title="Primary Key"
                    onCreate={() => ({ name: "Id", type: "int", identity: true })}
                    renderContent={item =>
                      <div>
                        <ValueComponent dc={this.props.dc} labelColumns={4} binding={Binding.create(item, i => i.name)} type="string" defaultValue={null} />
                        <ValueComponent dc={this.props.dc} labelColumns={4} binding={Binding.create(item, i => i.type)} type="string" defaultValue={null} options={["int", "long", "short", "string", "Guid"]} />
                        <ValueComponent dc={this.props.dc} labelColumns={4} binding={Binding.create(item, i => i.identity)} type="boolean" defaultValue={null} />
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
                        <ValueComponent dc={this.props.dc} labelColumns={4} binding={Binding.create(item, i => i.hasTicks)} type="boolean" defaultValue={null} onChange={this.handleHasTickChanged} />
                        <ValueComponent dc={this.props.dc} labelColumns={4} binding={Binding.create(item, i => i.name)} type="string" defaultValue={null} />
                        <ValueComponent dc={this.props.dc} labelColumns={4} binding={Binding.create(item, i => i.type)} type="string" defaultValue={null} options={["int", "Guid", "DateTime"]} />
                      </div>
                    }
                  />
                </div>
              </div>
            }
          </div>
        }

        <UncontrolledTabs defaultEventKey="properties" id="DynamicTypeTabs" onToggled={this.handleTabSelect}>
          <Tab eventKey="properties" title="Properties">
            <PropertyRepeaterComponent dc={this.props.dc} properties={def.properties} onRemove={this.handlePropertyRemoved} showDatabaseMapping={this.props.showDatabaseMapping} />
            <br />
            {dt.baseType == "Entity" &&
              <MultiColumnUniqueIndexFieldsetComponent
                binding={Binding.create(def, d => d.multiColumnUniqueIndex)}
                title="Multi-Column Unique Index"
                onCreate={() => ({ fields: [""] })}
                renderContent={item =>
                  <div className="row">
                    <div className="col-sm-6">
                      <ComboBoxRepeaterComponent options={propNames} list={item.fields} />
                    </div>
                    <div className="col-sm-6">
                      <CSharpExpressionCodeMirror binding={Binding.create(item, i => i.where)} title="Where" signature={"(" + (dt.typeName || "") + "Entity e) =>"} />
                    </div>
                  </div>
                }
              />
            }

            <fieldset>
              <legend>ToString expression</legend>
              <CSharpExpressionCodeMirror binding={Binding.create(def, d => d.toStringExpression)} signature={"(" + (dt.typeName || "") + dt.baseType + " e) =>"} />
            </fieldset>
          </Tab>

          {dt.baseType == "Entity" &&
            <Tab eventKey="query" title="Query">
              <ComboBoxRepeaterComponent options={["e.Id"].concat(propNames).concat(expressionNames)} list={def.queryFields} />
            </Tab>
          }

          {dt.baseType == "Entity" &&
            <Tab eventKey="operations" title="Operations">
              <div className="row">
                <div className="col-sm-7">
                  <CreateOperationFieldsetComponent
                    binding={Binding.create(def, d => d.operationCreate)}
                    title="Create"
                    onCreate={() => ({
                      construct: "return new " + dt.typeName + "Entity\r\n{\r\n" +
                        def.properties.map(p => "    " + p.name + " = null").join(", \r\n") +
                        "\r\n};"
                    })}
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
                </div>
                <div className="col-sm-5">
                  {!dt.isNew &&
                    <TypeHelpComponent initialType={dt.typeName!} mode="CSharp" onMemberClick={this.handleTypeHelpClick} />
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
                parentToken: DynamicMixinConnectionEntity.token(e => e.mixinName),
                parentValue: dt.typeName
              }} />
            </Tab>
          }

          {!dt.isNew && dt.baseType == "Entity" && this.state.typeEntity != null &&
            <Tab eventKey="connections" title="Mixins">
              {this.state.typeEntity == false ? <p className="alert alert-warning">{DynamicTypeMessage.TheEntityShouldBeSynchronizedToApplyMixins.niceToString()}</p> :
                <SearchControl findOptions={{
                  queryName: DynamicMixinConnectionEntity,
                  parentToken: DynamicMixinConnectionEntity.token(e => e.entityType),
                  parentValue: this.state.typeEntity
                }} />
              }
            </Tab>
          }

          {!dt.isNew && dt.baseType == "Entity" &&
            <Tab eventKey="other" title="Other">
              {this.renderOthers()}
            </Tab>
          }
        </UncontrolledTabs>
      </div>
    );
  }

  renderOthers() {
    var ctx = new StyleContext(undefined, { labelColumns: 3 });
    return React.createElement("div", {}, ...DynamicClientOptions.Options.onGetDynamicLineForType.map(f => f(ctx, this.props.dynamicType.typeName!)));
  }
}


export class CustomCodeTab extends React.Component<{ definition: DynamicTypeDefinition, dynamicType: DynamicTypeEntity }>{

  static suggestWorkflow = true;
  static suggestTree = true;

  render() {
    const def = this.props.definition;
    const dt = this.props.dynamicType;
    const entityName = this.getDynamicTypeFullName();
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
                    <input type="button" className="btn btn-success btn-sm sf-button" value="Workflow" onClick={this.handleWorkflowCustomInheritanceClick} />}

                  {dt.baseType == "Entity" && CustomCodeTab.suggestTree &&
                    <input type="button" className="btn btn-warning btn-sm sf-button" value="Tree" onClick={this.handleTreeCustomInheritanceClick} />}
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
                  <input type="button" className="btn btn-warning btn-sm sf-button" value="PreSaving" onClick={this.handlePreSavingClick} />
                  <input type="button" className="btn btn-success btn-sm sf-button" value="PostRetrieved" onClick={this.handlePostRetrievedClick} />
                  <input type="button" className="btn btn-danger btn-sm sf-button" value="Property Validator" onClick={this.handlePropertyValidatorClick} />
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
                                    <div className="btn-group" style={{ marginBottom: "3px" }}>
                                        {CustomCodeTab.suggestWorkflow && <input type="button" className="btn btn-success btn-sm sf-button" value="Workflow" onClick={this.handleWithWorkflowClick} />}
                                        {CustomCodeTab.suggestTree && <input type="button" className="btn btn-info btn-sm sf-button" value="Tree" onClick={this.handleWithTreeClick} />}
                                        {CustomCodeTab.suggestTree && <input type="button" className="btn btn-info btn-sm sf-button" value="CreateRoot" onClick={this.handleOverrideCreateRoot} />}
                                        {CustomCodeTab.suggestTree && <input type="button" className="btn btn-info btn-sm sf-button" value="CreateChild" onClick={this.handleOverrideCreateChild} />}
                                        {CustomCodeTab.suggestTree && <input type="button" className="btn btn-info btn-sm sf-button" value="NextSibling" onClick={this.handleOverrideNextSibling} />}
                                        <input type="button" className="btn btn-warning btn-sm sf-button" value="Register Operations" onClick={this.handleRegisterOperationsClick} />
                                        <input type="button" className="btn btn-danger btn-sm sf-button" value="Register Expressions" onClick={this.handleRegisterExpressionsClick} />
                                    </div>}
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
                  {dt.baseType == "Entity" && <input type="button" className="btn btn-success btn-sm sf-button" value="Query Expression" onClick={this.handleQueryExpressionClick} />}
                  {dt.baseType == "Entity" || dt.baseType == "EmbeddedEntity" && <input type="button" className="btn btn-warning btn-sm sf-button" value="Scalar Expression" onClick={this.handleScalarExpressionClick} />}
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
                  <input type="button" className="btn btn-success btn-sm sf-button" value="Enum" onClick={this.handleEnumClick} />
                  {dt.baseType == "Entity" && <input type="button" className="btn btn-warning btn-sm sf-button" value="Operation" onClick={this.handleOperationClick} />}
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
                  <input type="button" className="btn btn-success btn-sm sf-button" value="Override" onClick={this.handleOverrideClick} />
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

  getDynamicTypeFullName() {

    var suffix = this.props.dynamicType.baseType == "MixinEntity" ? "Mixin" : "Entity";

    return `${this.props.dynamicType.typeName}${suffix}`;
  }

  handleWorkflowCustomInheritanceClick = () => {
    this.popupCodeSnippet("ICaseMainEntity");
  }

  handleTreeCustomInheritanceClick = () => {
    this.popupCodeSnippet("TreeEntity");
  }

  handlePreSavingClick = () => {
    this.popupCodeSnippet(`protected internal override void PreSaving(ref bool graphModified)
{
    //Your code here
    base.PreSaving(ref graphModified);
}`);
  }

  handlePostRetrievedClick = () => {
    this.popupCodeSnippet(`protected internal override void PostRetrieving()
{
    base.PostRetriving();
}`);
  }

  handlePropertyValidatorClick = () => {
    this.popupCodeSnippet(`protected override string PropertyValidation(PropertyInfo pi)
{
    if (pi.Name == nameof(YourProperty))
    {
        if(this.YourProperty == "AAA")
            return "AAA is not a valid value";
    }

    return base.PropertyValidation(pi);
}`);
  }

  handleWithWorkflowClick = () => {
    let entityName = this.props.dynamicType.typeName!;
    var os = this.props.definition.operationSave;
    var oc = this.props.definition.operationCreate;

    this.popupCodeSnippet(`fi.WithWorkflow(
constructor: () => ${oc ? `{ ${oc.construct} }` : `new ${entityName}Entity()`},
save: e => ${os ? `e.Execute(${entityName}Operation.Save)` : "e.Save()"}
);`);
  }

  handleWithTreeClick = () => {
    this.popupCodeSnippet(`fi.WithTree();`);
  }

    handleOverrideCreateRoot = () => {
      let entityName = this.props.dynamicType.typeName!;
      this.popupCodeSnippet(`Graph<${entityName}Entity>.Construct.Untyped(TreeOperation.CreateRoot).Do(a =>
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

    handleOverrideCreateChild = () => {
      let entityName = this.props.dynamicType.typeName!;
      this.popupCodeSnippet(`Graph<${entityName}Entity>.ConstructFrom<${entityName}Entity>.Untyped(TreeOperation.CreateChild).Do(c =>
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

    handleOverrideNextSibling = () => {
      let entityName = this.props.dynamicType.typeName!;
      this.popupCodeSnippet(`Graph<${entityName}Entity>.ConstructFrom<${entityName}Entity>.Untyped(TreeOperation.CreateNextSibling).Do(c =>
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

    handleRegisterOperationsClick = () => {

    let entityName = this.props.dynamicType.typeName!;

    this.popupCodeSnippet(
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

  handleRegisterExpressionsClick = () => {

    let entityName = this.props.dynamicType.typeName!;
    this.popupCodeSnippet(`QueryLogic.Expressions.Register((${entityName}Entity e) => e.[Expression Name]());`);
  }

  handleQueryExpressionClick = () => {
    let entityName = this.props.dynamicType.typeName!;
    this.popupCodeSnippet(
      `static Expression<Func<[Your Entity], IQueryable<${entityName}Entity>>> QueryExpression =
    e => Database.Query<${entityName}Entity>().Where(a => [Your conditions here]);
[ExpressionField]
public static IQueryable<${entityName}Entity> Queries(this [Your Entity] e)
{
    return QueryExpression.Evaluate(e);
}`);
  }

  handleScalarExpressionClick = () => {
    let entityName = this.props.dynamicType.typeName!;
    this.popupCodeSnippet(`static Expression<Func<${entityName}Entity, bool>> IsDisabledExpression =
    e => [Your conditions here] ;
[ExpressionField]
public static bool IsDisabled(this ${entityName}Entity entity)
{
    return IsDisabledExpression.Evaluate(entity);
}`);
  }

  handleEnumClick = () => {

    this.popupCodeSnippet(`public enum EnumName
{
    Item1,
    Item2,
    ....
};`);
  }

  handleOperationClick = () => {
    let entityName = this.props.dynamicType.typeName!;

    this.popupCodeSnippet(`[AutoInit]
public static class ${entityName}Operation2
{
    public static readonly ConstructSymbol<${entityName}Entity>.From<Your Entity> CreateFrom;
    public static readonly ExecuteSymbol<${entityName}Entity> DoSomething;
    public static readonly DeleteSymbol<${entityName}Entity> DeleteSomething;
}`);
  }

  handleOverrideClick = () => {
    this.popupCodeSnippet(`sb.Schema.Settings.FieldAttributes((StaticType ua) => ua.Property).Replace(new ImplementedByAttribute(typeof(YourDynamicTypeEntity)));`);
  }

  popupCodeSnippet(snippet: string) {
    ValueLineModal.show({
      type: { name: "string" },
      initialValue: snippet,
      valueLineType: "TextArea",
      title: `Code Snippet`,
      message: "Copy to clipboard: Ctrl+C, ESC",
      initiallyFocused: true,
      valueHtmlAttributes: { style: { height: 150 } },
    }).done();
  }
}

export interface CSharpExpressionCodeMirrorProps {
  binding: Binding<string | undefined>;
  title?: string;
  signature?: string;
}

export class CSharpExpressionCodeMirror extends React.Component<CSharpExpressionCodeMirrorProps>{

  render() {
    let val = this.props.binding.getValue();

    return (
      <div>
        {this.props.title && <h5> <strong>{this.props.title}</strong></h5>}
        <div className="code-container">
          {this.props.signature && <pre style={{ border: "0px", margin: "0px" }}>{this.props.signature}</pre>}
          <div className="small-codemirror">
            <CSharpCodeMirror
              script={val || ""}
              onChange={newScript => { this.props.binding.setValue(newScript); this.forceUpdate(); }} />
          </div>
        </div>
      </div>
    );
  }
}

export interface CustomFieldsetComponentProps<T> {
  binding: Binding<T | undefined>;
  title?: React.ReactChild;
  renderContent: (item: T) => React.ReactElement<any>;
  onCreate: () => T;
  onChange?: () => void;
}

export class CustomFieldsetComponent<T> extends React.Component<CustomFieldsetComponentProps<T>> {

  handleChecked = (e: React.FormEvent<any>) => {
    let val = this.props.binding.getValue();
    if (val)
      this.props.binding.deleteValue();
    else
      this.props.binding.setValue(this.props.onCreate());

    this.forceUpdate();

    if (this.props.onChange)
      this.props.onChange();
  }

  render() {
    let value = this.props.binding.getValue();
    return (
      <fieldset style={{ marginTop: "-5px" }}>
        <legend><input type="checkbox" checked={!!value} onChange={this.handleChecked} /> {this.props.title || this.props.binding.member.toString().firstUpper()}</legend>
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

export interface PropertyRepeaterComponentState {
  currentEventKey?: number;
}

export class PropertyRepeaterComponent extends React.Component<PropertyRepeaterComponentProps, PropertyRepeaterComponentState> {

  constructor(props: PropertyRepeaterComponentProps) {
    super(props);
    this.state = { currentEventKey: 0 };
  }

  componentWillMount() {
    this.props.properties.forEach(p => fetchPropertyType(p, this.props.dc));
  }

  componentWillReceiveProps(newProps: PropertyRepeaterComponentProps) {
    newProps.properties.filter(a => a._propertyType_ == undefined).forEach(p => fetchPropertyType(p, this.props.dc));
  }

  handleSelect = (eventKey: number) => {
    this.setState({
      currentEventKey: eventKey == this.state.currentEventKey ? undefined : eventKey
    });
  }

  handleOnRemove = (event: React.MouseEvent<any>, index: number) => {
    event.preventDefault();
    event.stopPropagation();
    var old = this.props.properties[index];
    this.props.properties.removeAt(index);

    if (this.state.currentEventKey == index)
      this.setState({ currentEventKey: undefined });

    this.props.dc.refreshView();

    if (this.props.onRemove)
      this.props.onRemove(old);
  }

  handleOnMoveUp = (event: React.MouseEvent<any>, index: number) => {
    event.preventDefault();
    event.stopPropagation();
    const newIndex = this.props.properties.moveUp(index);
    if (newIndex != index) {
      if (index == this.state.currentEventKey)
        this.setState({ currentEventKey: this.state.currentEventKey - 1 });
      else if (newIndex == this.state.currentEventKey)
        this.setState({ currentEventKey: this.state.currentEventKey + 1 });
    }

    this.props.dc.refreshView();
  }

  handleOnMoveDown = (event: React.MouseEvent<any>, index: number) => {
    event.preventDefault();
    event.stopPropagation();
    const newIndex = this.props.properties.moveDown(index);

    if (newIndex != index) {
      if (index == this.state.currentEventKey)
        this.setState({ currentEventKey: this.state.currentEventKey + 1 });
      else if (newIndex == this.state.currentEventKey)
        this.setState({ currentEventKey: this.state.currentEventKey - 1 });
    }

    this.props.dc.refreshView();
  }

  handleCreateClick = (event: React.SyntheticEvent<any>) => {

    event.preventDefault();

    var p = {
      uid: this.createGuid(),
      name: "Name",
      type: "string",
      isNullable: "No",
    } as DynamicProperty;
    autoFix(p);
    this.props.properties.push(p);
    this.setState({ currentEventKey: this.props.properties.length - 1 });
    this.props.dc.refreshView();

    fetchPropertyType(p, this.props.dc);
  }

  createGuid() {
    let d = new Date().getTime();
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
      let r = (d + Math.random() * 16) % 16 | 0;
      d = Math.floor(d / 16);
      return (c == 'x' ? r : (r & 0x3 | 0x8)).toString(16);
    });
  }

  render() {
    return (
      <div className="properties">
        <div>
          {
            this.props.properties.map((p, i) =>
              <CollapsableCard
                key={i}
                header={this.renderPropertyHeader(p, i)}
                cardStyle={{ background: "light" }}
                headerStyle={{ text: "secondary" }}
                isOpen={this.state.currentEventKey == i}
                toggle={() => this.handleSelect(i)} >
                <PropertyComponent property={p} dc={this.props.dc} showDatabaseMapping={this.props.showDatabaseMapping} />
              </CollapsableCard>)
          }
        </div>
        <a href="#" title="Create Property"
          className="sf-line-button sf-create"
          onClick={this.handleCreateClick}>
          <FontAwesomeIcon icon="plus" className="sf-create" />&nbsp;Create Property
                </a>
      </div>
    );
  }

  renderPropertyHeader(p: DynamicProperty, i: number) {
    return (
      <div>

        <span className="item-group">
          <a href="#" className={classes("sf-line-button", "sf-remove")}
            onClick={e => this.handleOnRemove(e, i)}
            title={EntityControlMessage.Remove.niceToString()}>
            <FontAwesomeIcon icon="times" />
          </a>

          <a href="#" className={classes("sf-line-button", "move-up")}
            onClick={e => this.handleOnMoveUp(e, i)}
            title={EntityControlMessage.MoveUp.niceToString()}>
            <FontAwesomeIcon icon="chevron-up" />
          </a>

          <a href="#" className={classes("sf-line-button", "move-down")}
            onClick={e => this.handleOnMoveDown(e, i)}
            title={EntityControlMessage.MoveDown.niceToString()}>
            <FontAwesomeIcon icon="chevron-down" />
          </a>
        </span>
        {" " + (p._propertyType_ || "") + " " + p.name}
      </div>
    );
  }
}

function fetchPropertyType(p: DynamicProperty, dc: DynamicTypeDesignContext) {
  p._propertyType_ = "";
  DynamicTypeClient.API.getPropertyType(p).then(s => {
    p._propertyType_ = s;
    dc.refreshView();
  }).done()
}


export interface PropertyComponentProps {
  property: DynamicProperty;
  showDatabaseMapping: boolean;
  dc: DynamicTypeDesignContext;
}

export class PropertyComponent extends React.Component<PropertyComponentProps>{

  handleAutoFix = () => {

    const p = this.props.property;

    autoFix(p);

    this.props.dc.refreshView();

    fetchPropertyType(p, this.props.dc);
  }

  render() {
    var p = this.props.property
    return (
      <div>
        <div className="row">
          <div className="col-sm-6">
            <ValueComponent dc={this.props.dc} labelColumns={4} binding={Binding.create(p, d => d.name)} type="string" defaultValue={null} onBlur={this.handleAutoFix} />
            {this.props.showDatabaseMapping &&
              <ValueComponent dc={this.props.dc} labelColumns={4} binding={Binding.create(p, d => d.columnName)} type="string" defaultValue={null} labelClass="database-mapping" />
            }
            <TypeCombo dc={this.props.dc} labelColumns={4} binding={Binding.create(p, d => d.type)} onBlur={this.handleAutoFix} />
            {this.props.showDatabaseMapping &&
              <ValueComponent dc={this.props.dc} labelColumns={4} binding={Binding.create(p, d => d.columnType)} type="string" defaultValue={null} labelClass="database-mapping" />
            }
            <ValueComponent dc={this.props.dc} labelColumns={4} binding={Binding.create(p, d => d.isNullable)} type="string" defaultValue={null} options={DynamicTypeClient.IsNullableValues} onChange={this.handleAutoFix} />
            {allowUnit(p.type) &&
              <ValueComponent dc={this.props.dc} labelColumns={4} binding={Binding.create(p, d => d.unit)} type="string" defaultValue={null} />
            }
            {allowFormat(p.type) &&
              <ValueComponent dc={this.props.dc} labelColumns={4} binding={Binding.create(p, d => d.format)} type="string" defaultValue={null} />
            }
            {(p.isMList || isEmbedded(p.type)) &&
              <ValueComponent dc={this.props.dc} labelColumns={4} binding={Binding.create(p, d => d.notifyChanges)} type="boolean" defaultValue={null} />
            }
          </div>
          <div className="col-sm-6">
            <IsMListFieldsetComponent
              binding={Binding.create(p, d => d.isMList)}
              title="Is MList"
              onCreate={() => ({ preserveOrder: true })}
              renderContent={mle =>
                <div className="database-mapping">
                  <ValueComponent dc={this.props.dc} labelColumns={4} binding={Binding.create(mle, d => d.preserveOrder)} type="boolean" defaultValue={null} />
                  <ValueComponent dc={this.props.dc} labelColumns={4} binding={Binding.create(mle, d => d.orderName)} type="string" defaultValue={null} />
                  <ValueComponent dc={this.props.dc} labelColumns={4} binding={Binding.create(mle, d => d.tableName)} type="string" defaultValue={null} />
                  <ValueComponent dc={this.props.dc} labelColumns={4} binding={Binding.create(mle, d => d.backReferenceName)} type="string" defaultValue={null} />
                </div>
              }
              onChange={() => {
                fetchPropertyType(p, this.props.dc);
                this.handleAutoFix();
              }}
            />

            {p.type && <div>
              {isEntity(p.type) && <ValueComponent dc={this.props.dc} labelColumns={5} binding={Binding.create(p, d => d.isLite)} type="boolean" defaultValue={null} onChange={this.handleAutoFix} />}

              {allowsSize(p.type) &&
                <ValueComponent dc={this.props.dc} labelColumns={5} binding={Binding.create(p, d => d.size)} type="number" defaultValue={null} onBlur={this.handleAutoFix} />}

              {(isDecimal(p.type)) &&
                <ValueComponent dc={this.props.dc} labelColumns={5} binding={Binding.create(p, d => d.scale)} type="number" defaultValue={null} onBlur={this.handleAutoFix} />}

              <ValueComponent dc={this.props.dc} labelColumns={5} binding={Binding.create(p, d => d.uniqueIndex)} type="string" defaultValue={null} options={DynamicTypeClient.UniqueIndexValues} />
            </div>}
          </div>
        </div >
        <br />
        <ValueComponent dc={this.props.dc} labelColumns={3} binding={Binding.create(p, d => d.customFieldAttributes)} type="string" defaultValue={null} onBlur={this.handleAutoFix} />
        <ValueComponent dc={this.props.dc} labelColumns={3} binding={Binding.create(p, d => d.customPropertyAttributes)} type="string" defaultValue={null} onBlur={this.handleAutoFix} />
        <ValidatorRepeaterComponent dc={this.props.dc} property={this.props.property} />
      </div>
    );
  }
}

export class TypeCombo extends React.Component<{ dc: DynamicTypeDesignContext; binding: Binding<string>; labelColumns: number; onBlur: () => void }, { suggestions: string[] }>{

  constructor(props: any) {
    super(props);
    this.state = { suggestions: [] };
  }

  handleGetItems = (query: string) => {
    return TypeHelpClient.API.autocompleteType({
      query: query,
      limit: 5,
      includeBasicTypes: true,
      includeEntities: true,
      includeEmbeddedEntities: true,
      includeMList: true
    });
  }

  handleOnChange = (newValue: string) => {
    this.props.binding.setValue(newValue);
    this.props.dc.refreshView();
  }

  render() {
    let lc = this.props.labelColumns;
    return (
      <div className="form-group form-group-sm row" >
        <label className={classes("col-form-label col-form-label-sm", "col-sm-" + (lc == null ? 2 : lc))}>
          {this.props.binding.member}
        </label>
        <div className={"col-sm-" + (lc == null ? 10 : 12 - lc)}>
          <div style={{ position: "relative" }}>
            <Typeahead
              inputAttrs={{ className: "form-control form-control-sm sf-entity-autocomplete" }}
              onBlur={this.props.onBlur}
              getItems={this.handleGetItems}
              value={this.props.binding.getValue()}
              onChange={this.handleOnChange} />
          </div>
        </div>
      </div>);
  }
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

  if (registeredValidators["NotNull"].allowed(p)) {
    if (!p.validators.some(a => a.type == "NotNull"))
      p.validators.push({ type: "NotNull" });
  }

  if (registeredValidators["StringLength"].allowed(p)) {

    var c = p.validators.filter(a => a.type == "StringLength").firstOrNull() as Validators.StringLength | undefined;
    if (!c) {
      p.validators.push(c = { type: "StringLength" } as any as Validators.StringLength);
    }

    if (c.min == null)
      c.min == 3;

    c.max = p.size;
    c.allowNulls = p.isNullable == "Yes";
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

export class ComboBoxRepeaterComponent extends React.Component<ComboBoxRepeaterComponentProps> {

  handleChange = (val: string, index: number) => {
    var list = this.props.list;
    list[index] = val;
    this.forceUpdate();
  }

  handleCreateClick = (event: React.SyntheticEvent<any>) => {
    event.preventDefault();
    this.props.list.push("");
    this.forceUpdate();
  }

  handleOnRemove = (event: React.MouseEvent<any>, index: number) => {
    event.preventDefault();
    event.stopPropagation();
    this.props.list.removeAt(index);
    this.forceUpdate();
  }

  handleOnMoveUp = (event: React.MouseEvent<any>, index: number) => {
    event.preventDefault();
    event.stopPropagation();
    this.props.list.moveUp(index);
    this.forceUpdate();
  }

  handleOnMoveDown = (event: React.MouseEvent<any>, index: number) => {
    event.preventDefault();
    event.stopPropagation();
    this.props.list.moveDown(index);
    this.forceUpdate();
  }

  render() {
    return (
      <div>
        <table className="table table-sm">
          <tbody>
            {
              this.props.list.map((value, i) => this.renderHeader(value, i))
            }
            <tr>
              <td colSpan={2}>
                <a href="#" title="Create Query Column"
                  className="sf-line-button sf-create"
                  onClick={this.handleCreateClick}>
                  <FontAwesomeIcon icon="plus" className="sf-create" />&nbsp;Create Query Column
                                </a>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    );
  }

  renderHeader(value: string, i: number) {
    return (
      <tr key={i}>
        <td>
          <span className="item-group">
            <a href="#" className={classes("sf-line-button", "sf-remove")}
              onClick={e => this.handleOnRemove(e, i)}
              title={EntityControlMessage.Remove.niceToString()}>
              <FontAwesomeIcon icon="times" />
            </a>

            <a href="#" className={classes("sf-line-button", "move-up")}
              onClick={e => this.handleOnMoveUp(e, i)}
              title={EntityControlMessage.MoveUp.niceToString()}>
              <FontAwesomeIcon icon="chevron-up" />
            </a>

            <a href="#" className={classes("sf-line-button", "move-down")}
              onClick={e => this.handleOnMoveDown(e, i)}
              title={EntityControlMessage.MoveDown.niceToString()}>
              <FontAwesomeIcon icon="chevron-down" />
            </a>
          </span>
        </td>
        <td className="rw-widget-sm">
          <Combobox value={value} key={i}
            data={this.props.options.filter(o => o == value || !this.props.list.contains(o))}
            onChange={val => this.handleChange(val, i)} />
        </td>
      </tr>
    );
  }
}

export interface ValidatorRepeaterComponentProps {
  property: DynamicProperty;
  dc: DynamicTypeDesignContext;
}

export class ValidatorRepeaterComponent extends React.Component<ValidatorRepeaterComponentProps> {


  handleOnRemove = (event: React.MouseEvent<any>, index: number) => {
    event.preventDefault();
    var list = this.props.property.validators!;
    list.removeAt(index);
    if (list.length == 0)
      delete this.props.property.validators;
    this.props.dc.refreshView();
  }

  handleCreateClick = (event: React.SyntheticEvent<any>) => {
    event.preventDefault();
    let val = this.props.property.validators!;
    if (val == null)
      this.props.property.validators = val = [];

    SelectorModal.chooseElement(Dic.getValues(registeredValidators).filter(a => a.allowed(this.props.property)), {
      title: "New Validator",
      message: "Please select a validator type",
      buttonDisplay: vo => vo.name
    }).then(vo => {
      if (vo == undefined)
        return;

      val.push({ type: vo.name });
      this.props.dc.refreshView();
    });
  }

  render() {
    return (
      <div className="validators">
        <h4>Validators</h4>
        <div className="panel-group">
          {
            (this.props.property.validators || []).map((val, i) =>
              <CollapsableCard
                key={i}
                header={this.renderHeader(val, i)}
                cardStyle={{ background: "light" }}
                defaultOpen={true}>
                {registeredValidators[val.type].render && registeredValidators[val.type].render!(val, this.props.dc)}
              </CollapsableCard>
            )
          }
        </div>
        <a href="#" title="Create Validator"
          className="sf-line-button sf-create"
          onClick={this.handleCreateClick}>
          <FontAwesomeIcon icon="plus" className="sf-create" />&nbsp;Create Validator
                </a>
      </div>
    );
  }

  renderHeader(val: Validators.DynamicValidator, i: number) {
    return (
      <div>
        <span className="item-group">
          <a href="#" className={classes("sf-line-button", "sf-remove")}
            onClick={e => this.handleOnRemove(e, i)}
            title={EntityControlMessage.Remove.niceToString()}>
            <FontAwesomeIcon icon="times" />
          </a>
        </span>
        {" "}
        {val.type}
      </div>
    );
  }
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

export function registerValidator<T extends Validators.DynamicValidator>(options: ValidatorOptions<T>) {
  registeredValidators[options.name] = options as ValidatorOptions<Validators.DynamicValidator>;
}

registerValidator<Validators.DynamicValidator>({ name: "NotNull", allowed: p => p.isMList != null || !isString(p.type) && (p.isNullable == "No" && isReferenceType(p.type) || p.isNullable == "OnlyInMemory") });

registerValidator<Validators.StringLength>({
  name: "StringLength",
  allowed: p => !p.isMList && isString(p.type),
  render: (val, dc) =>
    <div className="row">
      <div className="col-sm-4">
        <ValueComponent dc={dc} labelColumns={6} binding={Binding.create(val, v => v.allowNulls)} type="boolean" defaultValue={false} />
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

registerValidator<Validators.DynamicValidator>({ name: "DateInPast", allowed: p => !p.isMList && isDateTime(p.type) });
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


