import * as React from 'react'
import { ValueLine, EntityLine, TypeContext, FormGroup, ValueLineType, LiteAutocompleteConfig } from '../../../../Framework/Signum.React/Scripts/Lines'
import { PropertyRoute, Binding } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror'
import { WorkflowActionEntity } from '../Signum.Entities.Workflow'
import { WorkflowConditionTestResponse, API, DecisionResultValues, showWorkflowTransitionContextCodeHelp } from '../WorkflowClient'
import TypeHelpComponent from "../../TypeHelp/TypeHelpComponent";
import ValueLineModal from '../../../../Framework/Signum.React/Scripts/ValueLineModal'

interface WorkflowConditionComponentProps {
    ctx: TypeContext<WorkflowActionEntity>;
}

export default class WorkflowConditionComponent extends React.Component<WorkflowConditionComponentProps> {

    handleMainEntityTypeChange = () => {
        this.props.ctx.value.eval!.script = "";
        this.forceUpdate();
    }

    handleCodeChange = (newScript: string) => {
        const evalEntity = this.props.ctx.value.eval!;
        evalEntity.script = newScript;
        evalEntity.modified = true;
        this.forceUpdate();
    }

    render() {
        var ctx = this.props.ctx;

        return (
            <div>
                <ValueLine ctx={ctx.subCtx(wc => wc.name)} />
                <EntityLine ctx={ctx.subCtx(wc => wc.mainEntityType)}
                    onChange={this.handleMainEntityTypeChange}
                    autoComplete={new LiteAutocompleteConfig((ac, str) => API.findMainEntityType({ subString: str, count: 5 }, ac), false)}
                    find={false} />
                {ctx.value.mainEntityType &&
                    <div>
                        <br />
                        <div className="row">
                            <div className="col-sm-7">
                                <div className="code-container">
                                    <div className="btn-group" style={{ marginBottom: "3px" }}>
                                        <input type="button" className="btn btn-success btn-xs sf-button" value="ctx" onClick={() => showWorkflowTransitionContextCodeHelp()} />
                                    </div>
                                    <pre style={{ border: "0px", margin: "0px" }}>{"void Action(" + ctx.value.mainEntityType.cleanName + "Entity e, WorkflowTransitionContext ctx)\n{"}</pre>
                                    <CSharpCodeMirror script={ctx.value.eval!.script || ""} onChange={this.handleCodeChange} />
                                    <pre style={{ border: "0px", margin: "0px" }}>{"}"}</pre>
                                </div>
                            </div>
                            <div className="col-sm-5">
                                <TypeHelpComponent initialType={ctx.value.mainEntityType.cleanName} mode="CSharp" onMemberClick={this.handleTypeHelpClick} />
                            </div>
                        </div>
                    </div>}
            </div>
        );
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
}

