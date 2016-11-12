import * as React from 'react'
import { ValueLine, EntityLine, TypeContext, FormGroup } from '../../../../Framework/Signum.React/Scripts/Lines'
import { PropertyRoute, Binding, getTypeInfo } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import CSharpCodeMirror from '../../../../Extensions/Signum.React.Extensions/Codemirror/CSharpCodeMirror'
import { Entity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import Typeahead from '../../../../Framework/Signum.React/Scripts/Lines/Typeahead'
import { DynamicExpressionEntity } from '../Signum.Entities.Dynamic'
import { DynamicExpressionTestResponse, API } from '../DynamicExpressionClient'
import * as DynamicClient from '../DynamicClient';

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

    render() {
        var ctx = this.props.ctx;

        return (
            <div>
                {this.state.exampleEntity && <button className="btn btn-success" onClick={this.handleEvaluate}><i className="fa fa-play" aria-hidden="true"></i> Evaluate</button>}
                <div className="code-container">
                    <pre style={{ border: "0px", margin: "0px", overflow: "visible" }}>
                        {this.renderTypeAutocomplete(ctx.subCtx(dt => dt.returnType))} {this.renderInput(ctx.subCtx(dt => dt.name))}({this.renderTypeAutocomplete(ctx.subCtx(dt => dt.fromType))} e) =>
                    </pre>
                    <CSharpCodeMirror script={ctx.value.body || ""} onChange={this.handleCodeChange} />
                </div>
                {this.renderTest()}
            </div>
        );
    }

    handleGetItems = (query: string) => {
        return DynamicClient.API.autocompleteType({ query: query, limit: 5, includeBasicTypes: true, includeEntities: true, includeQueriable: true });
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
                        getItems={this.handleGetItems}
                        value={ctx.value || undefined}
                        onChange={txt => { ctx.value = txt; this.forceUpdate(); } } />
              
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
                } } />
        );
    }

    handleEvaluate = () => {

        if (this.state.exampleEntity == undefined)
            this.changeState(s => { s.response = undefined; });
        else {
            API.expressionTest({
                dynamicExpression: this.props.ctx.value,
                exampleEntity: this.state.exampleEntity,
            })
                .then(r => this.changeState(s => s.response = r))
                .done();
        }
    }

    renderTest() {
        const ctx = this.props.ctx;
        const res = this.state.response;
        if (!ctx.value.body || !ctx.value.fromType)
            return null;

        var fromType = ctx.value.fromType;

        if (fromType.endsWith("Entity"))
            fromType = fromType.beforeLast("Entity");

        if (getTypeInfo(fromType) == null)
            return null;

        return (
            <fieldset>
                <legend>TEST</legend>
                {this.renderExampleEntity(fromType)}
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

    renderMessage(res: DynamicExpressionTestResponse) {
        if (res.compileError)
            return <div className="alert alert-danger">COMPILE ERROR: {res.compileError}</div >;

        if (res.validationException)
            return <div className="alert alert-danger">EXCEPTION: {res.validationException}</div>;

        return <div className="alert alert-success">VALUE: {res.validationResult}</div>;
    }
}

