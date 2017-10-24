import * as React from 'react'
import { WorkflowEventTaskActionEval } from '../Signum.Entities.Workflow'
import { TypeContext, ValueLine, PropertyRoute, ValueLineType } from '../../../../Framework/Signum.React/Scripts/Lines'
import { TypeEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import TypeHelpComponent from '../../TypeHelp/TypeHelpComponent'
import ValueLineModal from '../../../../Framework/Signum.React/Scripts/ValueLineModal'
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror';

export interface WorkflowEventTaskActionComponentProps {
    ctx: TypeContext<WorkflowEventTaskActionEval>;
    mainEntityType: TypeEntity;
}

export default class WorkflowEventTaskActionComponent extends React.Component<WorkflowEventTaskActionComponentProps> {

    render() {
        var ctx = this.props.ctx;

        return (
            <fieldset>
                <legend>{"Action"}</legend>
                <div className="row">
                    <div className="col-sm-7">
                        <div className="code-container">
                            <div className="btn-group" style={{ marginBottom: "3px" }}>
                                <input type="button" className="btn btn-success btn-xs sf-button" value="Create case" onClick={this.handleCreateCaseClick} />
                            </div>
                            <pre style={{ border: "0px", margin: "0px" }}>{"public void CustomAction() \n{"}</pre>
                            <CSharpCodeMirror script={ctx.value.script || ""} onChange={this.handleCodeChange} />
                            <pre style={{ border: "0px", margin: "0px" }}>{"}"}</pre>
                        </div>
                    </div>
                    <div className="col-sm-5">
                        <TypeHelpComponent initialType={this.props.mainEntityType.cleanName} mode="CSharp" onMemberClick={this.handleTypeHelpClick} />
                    </div>
                </div>
            </fieldset>
        );
    }
    
    handleCodeChange = (newScript: string) => {
        const evalEntity = this.props.ctx.value;
        evalEntity.script = newScript;
        evalEntity.modified = true;
        this.forceUpdate();
    }

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

    handleCreateCaseClick = () => {
        ValueLineModal.show({
            type: { name: "string" },
            initialValue: `CreateCase(new ${this.props.mainEntityType.cleanName}Entity(){ initial properties here... });`,
            valueLineType: "TextArea",
            title: "Create case Template",
            message: "Copy to clipboard: Ctrl+C, ESC",
            initiallyFocused: true,
            valueHtmlAttributes: { style: { height: "115px" } },
        }).done();
    }
}