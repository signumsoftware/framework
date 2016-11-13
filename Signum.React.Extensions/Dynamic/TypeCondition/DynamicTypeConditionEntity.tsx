import * as React from 'react'
import { ValueLine, EntityLine, TypeContext, FormGroup } from '../../../../Framework/Signum.React/Scripts/Lines'
import { PropertyRoute, Binding } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import CSharpCodeMirror from '../../../../Extensions/Signum.React.Extensions/Codemirror/CSharpCodeMirror'
import { Entity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { DynamicTypeConditionEntity } from '../Signum.Entities.Dynamic'
import { DynamicTypeConditionTestResponse, API } from '../DynamicTypeConditionClient'


interface DynamicTypeConditionComponentProps {
    ctx: TypeContext<DynamicTypeConditionEntity>;
}

interface DynamicTypeConditionComponentState {
    exampleEntity?: Entity;
    response?: DynamicTypeConditionTestResponse;
}

export default class DynamicTypeConditionComponent extends React.Component<DynamicTypeConditionComponentProps, DynamicTypeConditionComponentState> {

    constructor(props: DynamicTypeConditionComponentProps) {
        super(props);
        this.state = {};
    }

    handleEntityTypeChange = () => {
        this.props.ctx.value.eval!.script = "";
        this.changeState(s => {
            s.exampleEntity = undefined;
            s.response = undefined
        });
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
                <EntityLine ctx={ctx.subCtx(dt => dt.symbolName)} />
                <EntityLine ctx={ctx.subCtx(dt => dt.entityType)} onChange={this.handleEntityTypeChange} />
                {ctx.value.entityType &&
                    <div>
                        <br />
                        {this.state.exampleEntity && <button className="btn btn-success" onClick={this.handleEvaluate}><i className="fa fa-play" aria-hidden="true"></i> Evaluate</button>}
                        <div className="code-container">
                            <pre style={{ border: "0px", margin: "0px" }}>{"boolean Evaluate(" + ctx.value.entityType.cleanName + " e) =>"}</pre>
                            <CSharpCodeMirror script={ctx.value.eval!.script || ""} onChange={this.handleCodeChange} />
                        </div>
                        {this.renderTest()}
                    </div>}
            </div>
        );
    }

    handleEvaluate = () => {

        if (this.state.exampleEntity == undefined)
            this.changeState(s => { s.response = undefined; });
        else {
            API.typeConditionTest({
                dynamicTypeCondition: this.props.ctx.value,
                exampleEntity: this.state.exampleEntity,
            })
                .then(r => this.changeState(s => s.response = r))
                .done();
        }
    }

    renderTest() {
        const ctx = this.props.ctx;
        const res = this.state.response;
        return (
            <fieldset>
                <legend>TEST</legend>
                {this.renderExampleEntity(ctx.value.entityType!.cleanName)}
                {res && this.renderMessage(res)}
            </fieldset>
        );
    }

    renderExampleEntity(typeName: string) {
        const exampleCtx = new TypeContext<Entity | undefined>(undefined, undefined, PropertyRoute.root(typeName), Binding.create(this.state, s => s.exampleEntity));

        return (
            <EntityLine ctx={exampleCtx} create={true} find={true} remove={true} view={true} onView={this.handleOnView} onChange={this.handleEvaluate}
                type={{ name: typeName }} labelText="Example Entity" />
        );
    }

    handleOnView = (exampleEntity: Entity) => {
        return Navigator.view(exampleEntity, { requiresSaveOperation: false, showOperations: false });
    }

    renderMessage(res: DynamicTypeConditionTestResponse) {
        if (res.compileError)
            return <div className="alert alert-danger">COMPILE ERROR: {res.compileError}</div >;

        if (res.validationException)
            return <div className="alert alert-danger">EXCEPTION: {res.validationException}</div>;

        return (
            <div>
                {
                    res.validationResult == true ?
                        <div className="alert alert-success">True</div> :
                        <div className="alert alert-warning">False</div>
                        
                }
            </div>
        );
    }
}

