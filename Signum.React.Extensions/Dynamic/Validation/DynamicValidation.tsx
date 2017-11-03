import * as React from 'react'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import { MemberInfo, getTypeInfo, PropertyRoute, Binding, TypeInfo } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { DynamicValidationEntity, DynamicViewMessage } from '../Signum.Entities.Dynamic'
import { ValueLine, EntityLine, RenderEntity, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater, EntityCheckboxList, EntityTabRepeater, TypeContext, ValueLineType, FormGroup } from '../../../../Framework/Signum.React/Scripts/Lines'
import { Entity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { API, DynamicValidationTestResponse } from '../DynamicValidationClient'
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror'
import TypeHelpComponent from '../../TypeHelp/TypeHelpComponent'
import TypeHelpButtonBarComponent from '../../TypeHelp/TypeHelpButtonBarComponent'
import ValueLineModal from '../../../../Framework/Signum.React/Scripts/ValueLineModal'
import { ContextMenuPosition } from '../../../../Framework/Signum.React/Scripts/SearchControl/ContextMenu'
import PropertyRouteCombo from "../../Basics/Templates/PropertyRouteCombo";
import { ModifiableEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities';
import { Lite } from '../../../../Framework/Signum.React/Scripts/Signum.Entities';

interface DynamicValidationProps {
    ctx: TypeContext<DynamicValidationEntity>;
}

interface DynamicValidationState {
    exampleEntity?: Entity;
    response?: DynamicValidationTestResponse;
    parentType?: string;
}

export default class DynamicValidation extends React.Component<DynamicValidationProps, DynamicValidationState> {


    constructor(props: DynamicValidationProps) {
        super(props);

        this.state = {};
    }

    updateParentType() {
        if (!this.props.ctx.value.propertyRoute)
            this.setState({ parentType: undefined });
        else
            API.parentType(this.props.ctx.value.propertyRoute)
                .then(parentType => this.setState({ parentType }))
                .done();
    }

    componentWillMount() {
        this.updateParentType();
    }

    handleEntityTypeChange = () => {
        this.props.ctx.value.propertyRoute = null;
        this.setState({
            exampleEntity: undefined,
            response: undefined,
            parentType: undefined,
        });
    }

    handleCodeChange = (newScript: string) => {
        const evalEntity = this.props.ctx.value.eval;
        evalEntity.modified = true;
        evalEntity.script = newScript;
        this.forceUpdate();
    }

    handlePropertyRouteChange = () => {
        this.updateParentType();
    }

    render() {
        var ctx = this.props.ctx;

        return (
            <div>
                <EntityLine ctx={ctx.subCtx(d => d.entityType)} onChange={this.handleEntityTypeChange} />
                <FormGroup ctx={ctx.subCtx(d => d.propertyRoute)}>
                    {ctx.value.entityType && <PropertyRouteCombo ctx={ctx.subCtx(d => d.propertyRoute)} type={ctx.value.entityType} onChange={this.handlePropertyRouteChange} />}
                </FormGroup>
                <ValueLine ctx={ctx.subCtx(d => d.name)} />
                {ctx.value.propertyRoute &&
                    <div>
                        <br />
                        <div className="row">
                            <div className="col-sm-7">
                                {this.state.exampleEntity && <button className="btn btn-success" onClick={this.handleEvaluate}><i className="fa fa-play" aria-hidden="true"></i> Evaluate</button>}
                                <div className="code-container">
                                    <TypeHelpButtonBarComponent typeName={ctx.value.entityType!.cleanName} mode="CSharp" ctx={ctx} />
                                    <pre style={{ border: "0px", margin: "0px" }}>{"string PropertyValidate(" + (this.state.parentType || "ModifiableEntity") + " e, PropertyInfo pi)\n{"}</pre>
                                    <CSharpCodeMirror script={ctx.value.eval.script || ""} onChange={this.handleCodeChange} />
                                    <pre style={{ border: "0px", margin: "0px" }}>{"}"}</pre>
                                </div>
                                {this.renderTest()}
                            </div>
                            <div className="col-sm-5">
                                <TypeHelpComponent initialType={ctx.value.entityType ? ctx.value.entityType.cleanName : undefined} mode="CSharp" onMemberClick={this.handleTypeHelpClick} />
                            </div>
                        </div>
                    </div>}
            </div>
        );
    }

    getParentProperty(): PropertyRoute {

        const pre = this.props.ctx.value.propertyRoute!;
        return PropertyRoute.parse(pre.rootType.cleanName, pre.path);
    }

    castToTop(pr: PropertyRoute): string {
        if (pr.propertyRouteType == "Root")
            return "e";
        else if (pr.propertyRouteType == "Mixin")
            return `((${pr.parent!.typeReference().name}Entity)${this.castToTop(pr.parent!)}.MainEntity)`;
        else 
            return `((${pr.parent!.typeReference().name}Entity)${this.castToTop(pr.parent!)}.GetParentEntity())`;
    }

    handleTypeHelpClick = (pr: PropertyRoute | undefined) => {
        if (!pr)
            return;

        const ppr = this.getParentProperty().parent!;
        const prefix = this.castToTop(ppr);

        ValueLineModal.show({
            type: { name: "string" },
            initialValue: TypeHelpComponent.getExpression(prefix, pr, "CSharp"),
            valueLineType: "TextArea",
            title: "Mixin Template",
            message: "Copy to clipboard: Ctrl+C, ESC",
            initiallyFocused: true,
        }).done();
    }

  
    handleEvaluate = () => {

        if (this.state.exampleEntity == undefined)
            this.setState({ response: undefined });
        else {
            API.validationTest({
                dynamicValidation: this.props.ctx.value,
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
                type={{ name: typeName }} labelText={DynamicViewMessage.ExampleEntity.niceToString()} />
        );
    }

    handleOnView = (exampleEntity: Lite<Entity> | ModifiableEntity) => {
        return Navigator.view(exampleEntity, { requiresSaveOperation: false });
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
