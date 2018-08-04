import * as React from 'react'
import { WorkflowEventTaskConditionEval } from '../Signum.Entities.Workflow'
import { TypeContext, EntityDetail, ValueLine, PropertyRoute, ValueLineType } from '@framework/Lines'
import { TypeEntity } from '@framework/Signum.Entities.Basics'
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror';

export interface WorkflowEventTaskConditionComponentProps {
    ctx: TypeContext<WorkflowEventTaskConditionEval | undefined | null>;
}

export default class WorkflowEventTaskConditionComponent extends React.Component<WorkflowEventTaskConditionComponentProps> {

    render() {
        var ctx = this.props.ctx;

        return (
            <EntityDetail ctx={ctx} onChange={() => this.forceUpdate()} remove={false} getComponent={(ctx: TypeContext<WorkflowEventTaskConditionEval>) =>
                <div className="code-container">
                    <pre style={{ border: "0px", margin: "0px" }}>{"public bool CustomCondition() \n{"}</pre>
                    <CSharpCodeMirror script={ctx.value.script || ""} onChange={this.handleCodeChange} />
                    <pre style={{ border: "0px", margin: "0px" }}>{"}"}</pre>
                </div>} />
        );
    }

    handleCodeChange = (newScript: string) => {
        const evalEntity = this.props.ctx.value!;
        evalEntity.script = newScript;
        evalEntity.modified = true;
        this.forceUpdate();
    }
}
