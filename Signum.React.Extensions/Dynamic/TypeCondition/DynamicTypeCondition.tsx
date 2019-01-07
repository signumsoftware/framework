import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { ValueLine, EntityLine, TypeContext, FormGroup, ValueLineType } from '@framework/Lines'
import { PropertyRoute, Binding } from '@framework/Reflection'
import * as Navigator from '@framework/Navigator'
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror'
import { Entity } from '@framework/Signum.Entities'
import { DynamicTypeConditionEntity } from '../Signum.Entities.Dynamic'
import { DynamicTypeConditionTestResponse, API } from '../DynamicTypeConditionClient'
import TypeHelpComponent from '../../TypeHelp/TypeHelpComponent'
import ValueLineModal from '@framework/ValueLineModal'

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
        this.setState({
            exampleEntity : undefined,
            response : undefined
        });
    }

    handleCodeChange = (newScript: string) => {
        const evalEntity = this.props.ctx.value.eval!;
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

    render() {
        var ctx = this.props.ctx;

        return (
            <div>
                <EntityLine ctx={ctx.subCtx(dt => dt.symbolName)} />
                <EntityLine ctx={ctx.subCtx(dt => dt.entityType)} onChange={this.handleEntityTypeChange} />
                {ctx.value.entityType &&
                    <div>
                        <br />
                        <div className="row">
                            <div className="col-sm-7">

                            {this.state.exampleEntity && <button className="btn btn-success" onClick={this.handleEvaluate}><FontAwesomeIcon icon="play"/> Evaluate</button>}

                                <div className="code-container">
                                    <pre style={{ border: "0px", margin: "0px" }}>{"boolean Evaluate(" + ctx.value.entityType.cleanName + "Entity e) =>"}</pre>
                                    <CSharpCodeMirror script={ctx.value.eval!.script || ""} onChange={this.handleCodeChange} />
                                </div>
                                {this.renderTest()}
                            </div>
                            <div className="col-sm-5">
                                <TypeHelpComponent initialType={ctx.value.entityType.cleanName} mode="CSharp" onMemberClick={this.handleTypeHelpClick} />
                            </div>
                        </div>
                    </div>}
            </div>
        );
    }

    handleEvaluate = () => {

        if (this.state.exampleEntity == undefined)
            this.setState({ response: undefined });
        else {
            API.typeConditionTest({
                dynamicTypeCondition: this.props.ctx.value,
                exampleEntity: this.state.exampleEntity,
            })
                .then(r => this.setState({ response: r }))
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
        return Navigator.view(exampleEntity, { requiresSaveOperation: false, isOperationVisible: eoc => false });
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

