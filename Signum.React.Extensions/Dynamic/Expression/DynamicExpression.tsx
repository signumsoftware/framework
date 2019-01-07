import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { ValueLine, EntityLine, TypeContext, FormGroup, ValueLineType } from '@framework/Lines'
import { PropertyRoute, Binding, isTypeEntity } from '@framework/Reflection'
import * as Navigator from '@framework/Navigator'
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror'
import { Entity } from '@framework/Signum.Entities'
import { DynamicExpressionEntity } from '../Signum.Entities.Dynamic'
import { DynamicExpressionTestResponse, API } from '../DynamicExpressionClient'
import * as TypeHelpClient from '../../TypeHelp/TypeHelpClient';
import TypeHelpComponent from '../../TypeHelp/TypeHelpComponent'
import ValueLineModal from '@framework/ValueLineModal'
import { ModifiableEntity } from '@framework/Signum.Entities';
import { Lite } from '@framework/Signum.Entities';
import { Typeahead } from '@framework/Components';

interface DynamicExpressionComponentProps {
    ctx: TypeContext<DynamicExpressionEntity>;
}

interface DynamicExpressionComponentState {
    exampleEntity?: Entity;
    response?: DynamicExpressionTestResponse;
}

export default class DynamicExpressionComponent extends React.Component<DynamicExpressionComponentProps, DynamicExpressionComponentState> {

    constructor(props: DynamicExpressionComponentProps) {
        super(props);
        this.state = {};
    }

    handleCodeChange = (newScript: string) => {
        const entity = this.props.ctx.value;
        entity.body = newScript;
        entity.modified = true;
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

        let cleanFromType = ctx.value.fromType || undefined;

        if (cleanFromType && cleanFromType.endsWith("Entity"))
            cleanFromType = cleanFromType.beforeLast("Entity");

        if (cleanFromType && !isTypeEntity(cleanFromType))
            cleanFromType = undefined;

        return (
            <div>
                <ValueLine ctx={ctx.subCtx(dt => dt.translation)} />
                <div className="row">
                    <div className="col-sm-6">
                        <ValueLine ctx={ctx.subCtx(dt => dt.format)} labelColumns={4}
                            helpText={<span>See <a href="https://docs.microsoft.com/en-us/dotnet/standard/base-types/formatting-types" target="_blank">formatting types</a></span>} />
                    </div>
                    <div className="col-sm-6">
                        <ValueLine ctx={ctx.subCtx(dt => dt.unit)} labelColumns={4} />
                    </div>
                </div>
                <br />
                <div className="row">
                    <div className="col-sm-7">
                        {this.state.exampleEntity && <button className="btn btn-success" onClick={this.handleEvaluate}><FontAwesomeIcon icon="play"></FontAwesomeIcon> Evaluate</button>}
                        <div className="code-container">
                            <pre style={{ border: "0px", margin: "0px", overflow: "visible" }}>
                                {this.renderTypeAutocomplete(ctx.subCtx(dt => dt.returnType))} {this.renderInput(ctx.subCtx(dt => dt.name))}({this.renderTypeAutocomplete(ctx.subCtx(dt => dt.fromType))}e) =>
                            </pre>
                            <CSharpCodeMirror script={ctx.value.body || ""} onChange={this.handleCodeChange} />
                        </div>
                        {ctx.value.body && cleanFromType && this.renderTest(cleanFromType)}
                    </div>
                    <div className="col-sm-5">
                        <TypeHelpComponent initialType={cleanFromType} mode="CSharp" onMemberClick={this.handleTypeHelpClick} />
                    </div>
                </div>
            </div>
        );
    }

    handleGetItems = (query: string, type: "ReturnType" | "FromType") => {
        return TypeHelpClient.API.autocompleteType({ query: query, limit: 5, includeBasicTypes: true, includeEntities: true, includeModelEntities: type == "ReturnType", includeQueriable: type == "ReturnType" });
    }

    renderTypeAutocomplete(ctx: TypeContext<string | null | undefined>) {
        return (
            <span style={{ position: "relative" }}>
                <Typeahead
                    inputAttrs={{
                        className: "input-code",
                        placeholder: ctx.niceName(),
                        size: ctx.value ? ctx.value.length : ctx.niceName().length
                    }}
                    getItems={query => this.handleGetItems(query, ctx.propertyRoute.member!.name == "ReturnType" ? "ReturnType" : "FromType")}
                    value={ctx.value || undefined}
                    onChange={txt => { ctx.value = txt; this.forceUpdate(); }} />
            </span>
        );
    }

    renderInput(ctx: TypeContext<string | null | undefined>) {
        return (
            <input type="text"
                className="input-code"
                placeholder={ctx.niceName()}
                size={ctx.value ? ctx.value.length : ctx.niceName().length}
                value={ctx.value || undefined}
                onChange={e => {
                    ctx.value = (e.currentTarget as HTMLInputElement).value;
                    this.forceUpdate();
                }} />
        );
    }

    handleEvaluate = () => {

        if (this.state.exampleEntity == undefined)
            this.setState({ response: undefined });
        else {
            API.expressionTest({
                dynamicExpression: this.props.ctx.value,
                exampleEntity: this.state.exampleEntity,
            })
                .then(r => this.setState({ response: r }))
                .done();
        }
    }

    renderTest(cleanFromType: string) {
        const res = this.state.response;

        return (
            <fieldset>
                <legend>TEST</legend>
                {this.renderExampleEntity(cleanFromType)}
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

    handleOnView = (exampleEntity: ModifiableEntity | Lite<Entity>) => {
        return Navigator.view(exampleEntity, { requiresSaveOperation: false, isOperationVisible: eoc => false });
    }

    renderMessage(res: DynamicExpressionTestResponse) {
        if (res.compileError)
            return <div className="alert alert-danger">COMPILE ERROR: {res.compileError}</div >;

        if (res.validationException)
            return <div className="alert alert-danger">EXCEPTION: {res.validationException}</div>;

        return <div className="alert alert-success">VALUE: {res.validationResult}</div>;
    }
}

