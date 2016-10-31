import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Constructor from '../../../../Framework/Signum.React/Scripts/Constructor'
import { DynamicViewSelectorEntity, DynamicViewMessage } from '../Signum.Entities.Dynamic'
import { EntityLine, TypeContext } from '../../../../Framework/Signum.React/Scripts/Lines'
import { Entity, JavascriptMessage, is } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { getTypeInfo, Binding, PropertyRoute } from '../../../../Framework/Signum.React/Scripts/Reflection'
import JavascriptCodeMirror from '../../Codemirror/JavascriptCodeMirror'
import * as DynamicViewClient from '../DynamicViewClient'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { AuthInfo } from './AuthInfo'


interface DynamicViewSelectorEntityComponentProps {
    ctx: TypeContext<DynamicViewSelectorEntity>;
}

interface DynamicViewSelectorEntityComponentState {
    exampleEntity?: Entity;
    syntaxError?: string;
    testResult?: { type: "ERROR", error: string } | { type: "RESULT", result: string | undefined } | undefined;
    scriptChanged?: boolean;
    viewNames?: string[];
}

export default class DynamicViewSelectorEntityComponent extends React.Component<DynamicViewSelectorEntityComponentProps, DynamicViewSelectorEntityComponentState> {

    constructor(props: DynamicViewSelectorEntityComponentProps) {
        super(props);

        this.state = {};
    }

    componentWillMount() {
        this.updateViewNames(this.props);
    }

    componentWillReceiveProps(newProps: DynamicViewSelectorEntityComponentProps) {
        if (!is(this.props.ctx.value.entityType, newProps.ctx.value.entityType))
            this.updateViewNames(newProps);
    }

    updateViewNames(props: DynamicViewSelectorEntityComponentProps) {
        this.changeState(s => s.viewNames = undefined);
        if (props.ctx.value.entityType)
            DynamicViewClient.API.getDynamicViewNames(props.ctx.value.entityType!.cleanName)
                .then(viewNames => this.changeState(s => s.viewNames = viewNames))
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
                        {this.renderEditor()}
                        {this.renderTest()}
                    </div>
                }
            </div>
        );
    }

    renderTest() {
        const ctx = this.props.ctx;
        const res = this.state.testResult;
        return (
            <fieldset>
                <legend>TEST</legend>
                {this.renderExampleEntity(ctx.value.entityType!.cleanName)}
                {res && res.type == "ERROR" && <div className="alert alert-danger">ERROR: {res.error}</div>}
                {res && res.type == "RESULT" && <div className={classes("alert", this.getTestAlertType(res.result))}>RESULT: {res.result === undefined ? "undefined" : JSON.stringify(res.result)}</div>}
            </fieldset>
        );
    }

    getTestAlertType(result: string | undefined) {

        if (!result)
            return "alert-danger";

        if (this.allViewNames().contains(result))
            return "alert-success";

        return "alert-danger";
    }

    renderExampleEntity(typeName: string) {
        const exampleCtx = new TypeContext<Entity | undefined>(undefined, undefined, PropertyRoute.root(typeName), Binding.create(this.state, s => s.exampleEntity));

        return (
            <EntityLine ctx={exampleCtx} create={true} find={true} remove={true} view={true} onView={this.handleOnView} onChange={() => this.evaluateTest()}
                type={{ name: typeName }} labelText={DynamicViewMessage.ExampleEntity.niceToString()} />
        );
    }

    handleOnView = (exampleEntity: Entity) => {
        return Navigator.view(exampleEntity, { requiresSaveOperation: false, showOperations: false });
    }

    handleCodeChange = (newCode: string) => {
        var dvs = this.props.ctx.value;

        if (dvs.script != newCode) {
            dvs.script = newCode;
            dvs.modified = true;
            this.changeState(s => s.scriptChanged = true);
            this.evaluateTest();
        };
    }

    evaluateTest() {

        this.changeState(s => {
            s.syntaxError = undefined;
            s.testResult = undefined;
        });

        const dvs = this.props.ctx.value;
        let func: (e: Entity, auth: AuthInfo) => any;
        try {
            func = DynamicViewClient.asFunction(dvs);
        } catch (e) {
            this.changeState(s => {
                s.syntaxError = (e as Error).message;
            });
            return;
        }


        if (this.state.exampleEntity) {
            try {
                this.changeState(s => {
                    s.testResult = {
                        type: "RESULT",
                        result: func(this.state.exampleEntity!, new AuthInfo())
                    }
                });
            } catch (e) {
                this.changeState(s => {
                    s.testResult = {
                        type: "ERROR",
                        error: (e as Error).message
                    }
                });
            }
        }
    }

    allViewNames() {
        return ["NEW", "STATIC", "CHOOSE"].concat(this.state.viewNames || []);
    }

    renderEditor() {
        const ctx = this.props.ctx;
        return (
            <div className="code-container">
                <pre style={{ border: "0px", margin: "0px", color: "Green" }}>Return {this.allViewNames().map(vn => '"' + vn + '"').joinComma(" or ")}</pre>
                <pre style={{ border: "0px", margin: "0px" }}>{"(e: " + ctx.value.entityType!.className + ", auth) =>"}</pre>
                <JavascriptCodeMirror code={ctx.value.script || ""} onChange={this.handleCodeChange} />
                {this.state.syntaxError && <div className="alert alert-danger">{this.state.syntaxError}</div>}
            </div>
        );
    }
}

