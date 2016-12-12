import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Constructor from '../../../../Framework/Signum.React/Scripts/Constructor'
import { DynamicViewOverrideEntity, DynamicViewMessage } from '../Signum.Entities.Dynamic'
import { EntityLine, TypeContext } from '../../../../Framework/Signum.React/Scripts/Lines'
import { Entity, JavascriptMessage, is } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { getTypeInfo, Binding, PropertyRoute, ReadonlyBinding } from '../../../../Framework/Signum.React/Scripts/Reflection'
import JavascriptCodeMirror from '../../Codemirror/JavascriptCodeMirror'
import * as DynamicViewClient from '../DynamicViewClient'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { ViewReplacer } from '../../../../Framework/Signum.React/Scripts/Frames/ReactVisitor';
import TypeHelpComponent from '../Help/TypeHelpComponent'
import { AuthInfo } from './AuthInfo'


interface DynamicViewOverrideEntityComponentProps {
    ctx: TypeContext<DynamicViewOverrideEntity>;
}

interface DynamicViewOverrideEntityComponentState {
    exampleEntity?: Entity;
    componentClass?: React.ComponentClass<{ ctx: TypeContext<Entity> }> | null;
    syntaxError?: string;
    viewOverride?: (e: ViewReplacer<Entity>, authInfo: AuthInfo) => void;
    scriptChanged?: boolean;
    viewNames?: string[];
}

export default class DynamicViewOverrideEntityComponent extends React.Component<DynamicViewOverrideEntityComponentProps, DynamicViewOverrideEntityComponentState> {

    constructor(props: DynamicViewOverrideEntityComponentProps) {
        super(props);

        this.state = {};
    }

    componentWillMount() {
        this.updateViewNames(this.props);
    }

    componentWillReceiveProps(newProps: DynamicViewOverrideEntityComponentProps) {
        if (!is(this.props.ctx.value.entityType, newProps.ctx.value.entityType))
            this.updateViewNames(newProps);
    }

    updateViewNames(props: DynamicViewOverrideEntityComponentProps) {
        this.setState({ viewNames: undefined });
        if (props.ctx.value.entityType)
            DynamicViewClient.API.getDynamicViewNames(props.ctx.value.entityType!.cleanName)
                .then(viewNames => this.setState({ viewNames: viewNames }))
                .done();
    }

    handleTypeChange = () => {
        this.updateViewNames(this.props);
    }

    handleTypeRemove = () => {
        if (this.state.scriptChanged == true)
            return Promise.resolve(confirm(JavascriptMessage.loseCurrentChanges.niceToString()));

        return Promise.resolve(true);
    }

    render() {
        const ctx = this.props.ctx;

        return (
            <div>
                <EntityLine ctx={ctx.subCtx(a => a.entityType)} onChange={this.handleTypeChange} onRemove={this.handleTypeRemove} />

                {ctx.value.entityType &&
                    <div>
                        <br />
                        <div className="row">
                            <div className="col-sm-7">
                                {this.renderExampleEntity(ctx.value.entityType!.cleanName)}
                                {this.renderEditor()}
                            </div>
                            <div className="col-sm-5">
                                <TypeHelpComponent initialType={ctx.value.entityType.cleanName} mode="Typescript" />
                                <br />
                            </div>
                        </div>
                        <hr />
                        {this.renderTest()}
                    </div>
                }
            </div>
        );
    }

    renderTest() {
        const ctx = this.props.ctx;
        return (
            <div>
                {this.state.exampleEntity && this.state.componentClass &&
                    <RenderWithReplacements entity={this.state.exampleEntity}
                        componentClass={this.state.componentClass}
                        viewOverride={this.state.viewOverride} />}
            </div>
        );
    }

    renderExampleEntity(typeName: string) {
        const exampleCtx = new TypeContext<Entity | undefined>(undefined, undefined, PropertyRoute.root(typeName), Binding.create(this.state, s => s.exampleEntity));

        return (
            <div className="form-vertical code-container">
                <EntityLine ctx={exampleCtx} create={true} find={true} remove={true} view={true} onView={this.handleOnView} onChange={this.handleEntityChange} formGroupStyle="Basic"
                    type={{ name: typeName }} labelText={DynamicViewMessage.ExampleEntity.niceToString()} />
            </div>
        );
    }

    handleOnView = (exampleEntity: Entity) => {
        return Navigator.view(exampleEntity, { requiresSaveOperation: false, showOperations: false });
    }

    handleCodeChange = (newCode: string) => {
        var dvo = this.props.ctx.value;

        if (dvo.script != newCode) {
            dvo.script = newCode;
            dvo.modified = true;
            this.setState({ scriptChanged: true });
            this.compileFunction();
        };
    }

    handleEntityChange = () => {

        if (!this.state.exampleEntity)
            this.setState({ componentClass: undefined });
        else {

            const entity = this.state.exampleEntity;
            const settings = Navigator.getSettings(entity.Type);

            if (!settings || !settings.getViewPromise)
                this.setState({ componentClass: null });

            else
                settings.getViewPromise(entity).applyViewOverrides(settings).promise.then(func => {
                    var tempCtx = new TypeContext(undefined, undefined, PropertyRoute.root(entity.Type), new ReadonlyBinding(entity, "example"));
                    var re = func(tempCtx);
                    this.setState({ componentClass: re.type as React.ComponentClass<{ ctx: TypeContext<Entity> }> });
                });
        }

    }

    compileFunction() {

        this.setState({
            syntaxError: undefined,
            viewOverride: undefined,
        });

        const dvo = this.props.ctx.value;
        let func: (rep: ViewReplacer<Entity>, auth: AuthInfo) => void;
        try {
            func = DynamicViewClient.asOverrideFunction(dvo);
            this.setState({
                viewOverride : func
            });
        } catch (e) {
            this.setState({
                syntaxError : (e as Error).message
            });
            return;
        }
    }

    allViewNames() {
        return this.state.viewNames || [];
    }

    renderEditor() {

        const ctx = this.props.ctx;
        return (
            <div className="code-container">
                <pre style={{ border: "0px", margin: "0px" }}>{`(vr: ViewReplacer<${ctx.value.entityType!.className}>, 
auth: AuthInfo) =>`}</pre>
                <JavascriptCodeMirror code={ctx.value.script || ""} onChange={this.handleCodeChange} />
                {this.state.syntaxError && <div className="alert alert-danger">{this.state.syntaxError}</div>}
            </div>
        );
    }
}


interface RenderWithReplacementsProps {
    entity: Entity;
    componentClass: React.ComponentClass<{ ctx: TypeContext<Entity> }>;
    viewOverride?: (e: ViewReplacer<Entity>, authInfo: AuthInfo) => void;
}

export class RenderWithReplacements extends React.Component<RenderWithReplacementsProps, void> {


    originalRender: any;
    componentWillMount() {

        this.originalRender = this.props.componentClass.prototype.render;

        DynamicViewClient.unPatchComponent(this.props.componentClass);

        if (this.props.viewOverride)
            DynamicViewClient.patchComponent(this.props.componentClass, this.props.viewOverride);
    }

    componentWillReceiveProps(newProps: RenderWithReplacementsProps) {
        if (newProps.componentClass != this.props.componentClass)
            throw new Error("not implemented");

        if (newProps.viewOverride != this.props.viewOverride) {
            DynamicViewClient.unPatchComponent(this.props.componentClass);
            if (newProps.viewOverride)
                DynamicViewClient.patchComponent(this.props.componentClass, newProps.viewOverride);
        }
    }

    componentWillUnmount() {
        this.props.componentClass.prototype.render = this.originalRender;
    }

    render() {

        var ctx = new TypeContext(undefined, undefined, PropertyRoute.root(this.props.entity.Type), new ReadonlyBinding(this.props.entity, "example"));

        return React.createElement(this.props.componentClass, { ctx: ctx });
    }
}

