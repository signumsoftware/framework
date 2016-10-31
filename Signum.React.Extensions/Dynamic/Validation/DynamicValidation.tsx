import * as React from 'react'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import { MemberInfo, getTypeInfo, PropertyRoute, Binding } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { DynamicValidationEntity, DynamicViewMessage } from '../Signum.Entities.Dynamic'
import { ValueLine, EntityLine, RenderEntity, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater, EntityCheckboxList, EntityTabRepeater, TypeContext, ValueLineType, FormGroup } from '../../../../Framework/Signum.React/Scripts/Lines'
import { Entity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeEntity, PropertyRouteEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { API, DynamicValidationTestResponse } from '../DynamicValidationClient'
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror'

interface DynamicValidationProps {
    ctx: TypeContext<DynamicValidationEntity>;
}

interface DynamicValidationState {
    exampleEntity?: Entity;
    response?: DynamicValidationTestResponse;

}

export default class DynamicValidation extends React.Component<DynamicValidationProps, DynamicValidationState> {


    constructor(props: DynamicValidationProps) {
        super(props);

        this.state = {};
    }

    handleEntityTypeChange = () => {
        this.props.ctx.value.propertyRoute = null;
        this.changeState(s => {
            s.exampleEntity = undefined;
            s.response = undefined;
        });
    }

    handleCodeChange = (newScript: string) => {
        const evalEntity = this.props.ctx.value.eval;
        evalEntity.modified = true;
        evalEntity.script = newScript;
        this.forceUpdate();
    }

    render() {
        var ctx = this.props.ctx;

        return (
            <div>
                <EntityLine ctx={ctx.subCtx(d => d.entityType)} onChange={this.handleEntityTypeChange} />
                <FormGroup ctx={ctx.subCtx(d => d.propertyRoute)}>
                    {ctx.value.entityType && <PropertyRouteCombo ctx={ctx.subCtx(d => d.propertyRoute)} type={ctx.value.entityType} onChange={() => this.forceUpdate()} />}
                </FormGroup>
                <ValueLine ctx={ctx.subCtx(d => d.name)} />
                <ValueLine ctx={ctx.subCtx(d => d.isGlobalyEnabled)} inlineCheckbox={true} />
                {ctx.value.propertyRoute &&
                    <div>
                        {this.state.exampleEntity && <button className="btn btn-success" onClick={this.handeEvaluate}><i className="fa fa-play" aria-hidden="true"></i> Evaluate</button>}
                        <div className="code-container">
                            <pre style={{ border: "0px", margin: "0px" }}>{"string PropertyValidate(" + (!ctx.value.entityType ? "Entity" : ctx.value.entityType.cleanName) + " e, PropertyInfo pi)\n{"}</pre>
                            <CSharpCodeMirror script={ctx.value.eval.script || ""} onChange={this.handleCodeChange} />
                            <pre style={{ border: "0px", margin: "0px" }}>{"}"}</pre>
                        </div>
                        {this.renderTest()}
                    </div>}
            </div>
        );
    }

    handeEvaluate = () => {

        if (this.state.exampleEntity == undefined)
            this.changeState(s => s.response = undefined);
        else {
            API.validationTest({
                dynamicValidation: this.props.ctx.value,
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
            <EntityLine ctx={exampleCtx} create={true} find={true} remove={true} view={true} onView={this.handleOnView} onChange={this.handeEvaluate}
                type={{ name: typeName }} labelText={DynamicViewMessage.ExampleEntity.niceToString()} />
        );
    }

    handleOnView = (exampleEntity: Entity) => {
        return Navigator.view(exampleEntity, { requiresSaveOperation: false, showOperations: false });
    }

    renderMessage(res: DynamicValidationTestResponse) {
        if (res.compileError)
            return <div className="alert alert-danger">COMPILE ERROR: {res.compileError}</div >;

        if (res.validationException)
            return <div className="alert alert-danger">EXCEPTION: {res.validationException}</div>;

        return (
            <div>
                {
                    res.validationResult!.map(error => error ?
                        <div className="alert alert-warning">INVALID: {res.validationResult}</div> :
                        <div className="alert alert-success">VALID: null</div>)
                }
            </div>
        );
    }
}

interface PropertyRouteComboProps {
    ctx: TypeContext<PropertyRouteEntity | undefined | null>;
    type: TypeEntity;
    onChange?: () => void;
}

class PropertyRouteCombo extends React.Component<PropertyRouteComboProps, void> {

    handleChange = (e: React.FormEvent) => {
        var currentValue = (e.currentTarget as HTMLSelectElement).value;
        this.props.ctx.value = currentValue ? PropertyRouteEntity.New(e => { e.path = currentValue; e.rootType = this.props.type; }) : null;
        this.forceUpdate();
        if (this.props.onChange)
            this.props.onChange();
    }

    render() {
        var ctx = this.props.ctx;

        var members = Dic.getValues(getTypeInfo(this.props.type.cleanName).members).filter(a => a.name != "Id");

        return (
            <select className="form-control" value={ctx.value && ctx.value.path || ""} onChange={this.handleChange} >
                <option value=""> - </option>
                {members.map(m =>
                    <option key={m.name} value={m.name}>{m.name}</option>
                )}
            </select>
        );;
    }
}
