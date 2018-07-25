import * as React from 'react'
import { ValueLine, EntityLine, TypeContext, FormGroup, EntityStrip, EntityDetail } from '@framework/Lines'
import { PropertyRoute, Binding } from '@framework/Reflection'
import * as Navigator from '@framework/Navigator'
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror'
import { WorkflowLaneModel, WorkflowLaneActorsEval, ICaseMainEntity, WorkflowMessage } from '../Signum.Entities.Workflow'
import { WorkflowConditionTestResponse, API, DecisionResultValues } from '../WorkflowClient'
import TypeHelpComponent from '../../TypeHelp/TypeHelpComponent'


interface WorkflowLaneModelComponentProps {
    ctx: TypeContext<WorkflowLaneModel>;
}



export default class WorkflowLaneModelComponent extends React.Component<WorkflowLaneModelComponentProps> {


    render() {
        var ctx = this.props.ctx;

        return (
            <div>
                <ValueLine ctx={ctx.subCtx(wc => wc.name)} />
                <EntityStrip ctx={ctx.subCtx(wc => wc.actors)} />
                {ctx.value.mainEntityType ?
                    <EntityDetail ctx={ctx.subCtx(wc => wc.actorsEval)} getComponent={this.renderActorEval} onCreate={() => Promise.resolve(WorkflowLaneActorsEval.New({ script: "new List<Lite<Entity>>{ e.YourProperty }" }))} />
                    :
                    <div className="alert alert-warning">{WorkflowMessage.ToUse0YouSouldSetTheWorkflow1.niceToString(ctx.niceName(e => e.actorsEval), ctx.niceName(e => e.mainEntityType))}</div>}            
            </div>
        );
    }
    
    handleCodeChange = (newScript: string) => {
        const actorsEval = this.props.ctx.value.actorsEval!;
        actorsEval.script = newScript;
        actorsEval.modified = true;
        this.forceUpdate();
    }

    renderActorEval = (ectx: TypeContext<WorkflowLaneActorsEval>) => {
        var mainEntityName = this.props.ctx.value.mainEntityType.cleanName;
        return (
            <div className="row">
                <div className="col-sm-7">
                    <div className="code-container">
                        <pre style={{ border: "0px", margin: "0px" }}>{"IEnumerable<Lite<Entity>> GetActors(" + mainEntityName + "Entity e, WorkflowTransitionContext ctx)\n{"}</pre>
                        <CSharpCodeMirror script={ectx.value.script || ""} onChange={this.handleCodeChange} />
                        <pre style={{ border: "0px", margin: "0px" }}>{"}"}</pre>
                    </div>
                </div>
                <div className="col-sm-5">
                    <TypeHelpComponent initialType={mainEntityName} mode="CSharp" />
                </div>
            </div>
        );
    }

}

