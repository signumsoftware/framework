import * as React from 'react'
import { classes } from '@framework/Globals'
import { DynamicViewSelectorEntity, DynamicViewMessage } from '../Signum.Entities.Dynamic'
import { EntityLine, TypeContext } from '@framework/Lines'
import { Entity, JavascriptMessage, NormalWindowMessage, is } from '@framework/Signum.Entities'
import { Binding, PropertyRoute } from '@framework/Reflection'
import JavascriptCodeMirror from '../../Codemirror/JavascriptCodeMirror'
import * as DynamicViewClient from '../DynamicViewClient'
import * as Navigator from '@framework/Navigator'
import TypeHelpComponent from '../../TypeHelp/TypeHelpComponent'
import ValueLineModal from '@framework/ValueLineModal'
import MessageModal from '@framework/Modals/MessageModal'
import { DropdownItem, DropdownMenu, DropdownToggle, UncontrolledDropdown } from '@framework/Components';

interface DynamicViewSelectorComponentProps {
  ctx: TypeContext<DynamicViewSelectorEntity>;
}

interface DynamicViewSelectorComponentState {
  exampleEntity?: Entity;
  syntaxError?: string;
  testResult?: { type: "ERROR", error: string } | { type: "RESULT", result: string | undefined } | undefined;
  scriptChanged?: boolean;
  viewNames?: string[];
}

export default class DynamicViewSelectorComponent extends React.Component<DynamicViewSelectorComponentProps, DynamicViewSelectorComponentState> {

  constructor(props: DynamicViewSelectorComponentProps) {
    super(props);

    this.state = {};
  }

  componentWillMount() {
    this.updateViewNames(this.props);
  }

  componentWillReceiveProps(newProps: DynamicViewSelectorComponentProps) {
    if (!is(this.props.ctx.value.entityType, newProps.ctx.value.entityType))
      this.updateViewNames(newProps);
  }

  updateViewNames(props: DynamicViewSelectorComponentProps) {
    this.setState({ viewNames: undefined });
    if (props.ctx.value.entityType)
      DynamicViewClient.API.getDynamicViewNames(props.ctx.value.entityType!.cleanName)
        .then(viewNames => this.setState({ viewNames: viewNames }))
        .done();
  }

  handleTypeChange = () => {
    this.updateViewNames(this.props);
  }

  handleTypeRemove = () => {
    if (this.state.scriptChanged == true)
      return MessageModal.show({
        title: NormalWindowMessage.ThereAreChanges.niceToString(),
        message: JavascriptMessage.loseCurrentChanges.niceToString(),
        buttons: "yes_no",
        icon: "warning",
        style: "warning"
      }).then(result => { return result == "yes" });

    return Promise.resolve(true);
  }

  handleTypeHelpClick = (pr: PropertyRoute | undefined) => {
    if (!pr)
      return;

    ValueLineModal.show({
      type: { name: "string" },
      initialValue: TypeHelpComponent.getExpression("e", pr, "TypeScript"),
      valueLineType: "TextArea",
      title: "Property Template",
      message: "Copy to clipboard: Ctrl+C, ESC",
      initiallyFocused: true,
    }).done();
  }

  render() {
    const ctx = this.props.ctx;

    return (
      <div>
        <EntityLine ctx={ctx.subCtx(a => a.entityType)} onChange={this.handleTypeChange} onRemove={this.handleTypeRemove} />

        {ctx.value.entityType &&
          <div>
            <br />
            <div className="row">
              <div className="col-sm-7">
                {this.renderEditor()}
                {this.renderTest()}
              </div>
              <div className="col-sm-5">
                <TypeHelpComponent initialType={ctx.value.entityType.cleanName} mode="TypeScript" onMemberClick={this.handleTypeHelpClick} />
              </div>
            </div>
          </div>
        }
      </div>
    );
  }

  renderTest() {
    const ctx = this.props.ctx;
    const res = this.state.testResult;
    return (
      <fieldset>
        <legend>TEST</legend>
        {this.renderExampleEntity(ctx.value.entityType!.cleanName)}
        {res && res.type == "ERROR" && <div className="alert alert-danger">ERROR: {res.error}</div>}
        {res && res.type == "RESULT" && <div className={classes("alert", this.getTestAlertType(res.result))}>RESULT: {res.result === undefined ? "undefined" : JSON.stringify(res.result)}</div>}
      </fieldset>
    );
  }

  getTestAlertType(result: string | undefined) {

    if (!result)
      return "alert-danger";

    if (this.allViewNames().contains(result))
      return "alert-success";

    return "alert-danger";
  }

  renderExampleEntity(typeName: string) {
    const exampleCtx = new TypeContext<Entity | undefined>(undefined, undefined, PropertyRoute.root(typeName), Binding.create(this.state, s => s.exampleEntity));

    return (
      <EntityLine ctx={exampleCtx} create={true} find={true} remove={true} view={true} onView={this.handleOnView} onChange={() => this.evaluateTest()}
        type={{ name: typeName }} labelText={DynamicViewMessage.ExampleEntity.niceToString()} />
    );
  }

  handleOnView = (exampleEntity: Entity) => {
    return Navigator.view(exampleEntity, { requiresSaveOperation: false, isOperationVisible: eoc => false });
  }

  handleCodeChange = (newCode: string) => {
    var dvs = this.props.ctx.value;

    if (dvs.script != newCode) {
      dvs.script = newCode;
      dvs.modified = true;
      this.setState({ scriptChanged: true });
      this.evaluateTest();
    };
  }

  evaluateTest() {

    this.setState({
      syntaxError: undefined,
      testResult: undefined
    });

    const dvs = this.props.ctx.value;
    let func: (e: Entity) => any;
    try {
      func = DynamicViewClient.asSelectorFunction(dvs);
    } catch (e) {
      this.setState({
        syntaxError: (e as Error).message
      });
      return;
    }


    if (this.state.exampleEntity) {
      try {
        this.setState({
          testResult: {
            type: "RESULT",
            result: func(this.state.exampleEntity!)
          }
        });
      } catch (e) {
        this.setState({
          testResult: {
            type: "ERROR",
            error: (e as Error).message
          }
        });
      }
    }
  }

  allViewNames() {
    return ["NEW", "STATIC", "CHOOSE"].concat(this.state.viewNames || []);
  }

  handleViewNameClick = (viewName: string) => {
    ValueLineModal.show({
      type: { name: "string" },
      initialValue: `"${viewName}"`,
      valueLineType: "TextArea",
      title: "View Name",
      message: "Copy to clipboard: Ctrl+C, ESC",
      initiallyFocused: true,
    }).done();
  }

  renderViewNameButtons() {
    return (
      <UncontrolledDropdown>
        <DropdownToggle color="success" caret>View Names</DropdownToggle>
        <DropdownMenu>
          {this.allViewNames().map((vn, i) =>
            <DropdownItem key={i} onClick={e => this.handleViewNameClick(vn)}>{vn}</DropdownItem>)}
        </DropdownMenu>
      </UncontrolledDropdown>
    );
  }

  renderEditor() {
    const ctx = this.props.ctx;
    return (
      <div className="code-container">
        <div className="btn-toolbar btn-toolbar-small">
          {this.renderViewNameButtons()}
        </div>
        <pre style={{ border: "0px", margin: "0px" }}>{"(e: " + ctx.value.entityType!.className + ", modules) =>"}</pre>
        <JavascriptCodeMirror code={ctx.value.script || ""} onChange={this.handleCodeChange} />
        {this.state.syntaxError && <div className="alert alert-danger">{this.state.syntaxError}</div>}
      </div>
    );
  }
}

