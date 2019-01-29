import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Dic } from '@framework/Globals'
import { MemberInfo, getTypeInfo, PropertyRoute, Binding, TypeInfo } from '@framework/Reflection'
import { DynamicValidationEntity, DynamicValidationMessage, DynamicViewMessage } from '../Signum.Entities.Dynamic'
import { ValueLine, EntityLine, RenderEntity, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater, EntityCheckboxList, EntityTabRepeater, TypeContext, ValueLineType, FormGroup } from '@framework/Lines'
import { Entity } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import { API, DynamicValidationTestResponse } from '../DynamicValidationClient'
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror'
import TypeHelpComponent from '../../TypeHelp/TypeHelpComponent'
import TypeHelpButtonBarComponent from '../../TypeHelp/TypeHelpButtonBarComponent'
import ValueLineModal from '@framework/ValueLineModal'
import { ContextMenuPosition } from '@framework/SearchControl/ContextMenu'
import PropertyRouteCombo from "../../Basics/Templates/PropertyRouteCombo";
import { ModifiableEntity } from '@framework/Signum.Entities';
import { Lite } from '@framework/Signum.Entities';
import { PropertyRouteEntity } from '@framework/Signum.Entities.Basics';
import { UncontrolledDropdown, DropdownToggle, DropdownMenu, DropdownItem } from '@framework/Components';

interface DynamicValidationProps {
    ctx: TypeContext<DynamicValidationEntity>;
}

interface DynamicValidationState {
    exampleEntity?: Entity;
    response?: DynamicValidationTestResponse;
    routeTypeName?: string;
}

export default class DynamicValidation extends React.Component<DynamicValidationProps, DynamicValidationState> {


    constructor(props: DynamicValidationProps) {
        super(props);

        this.state = {};
    }

    updateRouteTypeName() {

        this.setState({ routeTypeName: undefined });

        const dv = this.props.ctx.value;
        if (dv.subEntity) {
            API.routeTypeName(dv.subEntity)
                .then(routeTypeName => this.setState({ routeTypeName }))
                .done();
        } else if (dv.entityType) {
            this.setState({ routeTypeName: dv.entityType.className });
        }
    }

    componentWillMount() {
        this.updateRouteTypeName();
    }

    handleEntityTypeChange = () => {
        this.props.ctx.value.subEntity = null;
        this.setState({
            exampleEntity: undefined,
            response: undefined,
            routeTypeName: undefined,
        }, () => this.updateRouteTypeName());
    }

    handleCodeChange = (newScript: string) => {
        const evalEntity = this.props.ctx.value.eval;
        evalEntity.modified = true;
        evalEntity.script = newScript;
        this.forceUpdate();
    }

    handlePropertyRouteChange = () => {
        this.updateRouteTypeName();
    }

    render() {
        var ctx = this.props.ctx;
        return (
            <div>
                <ValueLine ctx={ctx.subCtx(d => d.name)} />
                <EntityLine ctx={ctx.subCtx(d => d.entityType)} onChange={this.handleEntityTypeChange} />
                <FormGroup ctx={ctx.subCtx(d => d.subEntity)}>
                    {ctx.value.entityType && <PropertyRouteCombo ctx={ctx.subCtx(d => d.subEntity)} type={ctx.value.entityType} onChange={this.handlePropertyRouteChange} routes={PropertyRoute.generateAll(ctx.value.entityType.cleanName).filter(a => a.propertyRouteType == "Mixin" || a.typeReference().isEmbedded && !a.typeReference().isCollection)} />}
                </FormGroup>
                {ctx.value.entityType &&
                    <div>
                        <br />
                        <div className="row">
                            <div className="col-sm-7">
                                {this.state.exampleEntity && <button className="btn btn-success" onClick={this.handleEvaluate}><FontAwesomeIcon icon="play"/> Evaluate</button>}
                                <div className="code-container">
                                    <TypeHelpButtonBarComponent typeName={ctx.value.entityType.cleanName} mode="CSharp" ctx={ctx} extraButtons={
                                        <PropertyIsHelpComponent route={this.getCurrentRoute(ctx.value.entityType.cleanName)} />
                                    } />
                                    <pre style={{ border: "0px", margin: "0px" }}>{"string PropertyValidate(" + (this.state.routeTypeName || "ModifiableEntity") + " e, PropertyInfo pi)\n{"}</pre>
                                    <CSharpCodeMirror script={ctx.value.eval.script || ""} onChange={this.handleCodeChange} />
                                    <pre style={{ border: "0px", margin: "0px" }}>{"}"}</pre>
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

    getCurrentRoute(rootName : string): PropertyRoute {

        const ctx = this.props.ctx;
        return ctx.value.subEntity ?
            PropertyRoute.parse(rootName, ctx.value.subEntity.path) :
            PropertyRoute.root(rootName);
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
        if (!pr || !this.props.ctx.value.entityType)
            return;

        const ppr = this.getCurrentRoute(this.props.ctx.value.entityType.cleanName);
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
                type={{ name: typeName }} labelText={DynamicViewMessage.ExampleEntity.niceToString()} labelColumns={3} />
        );
    }

    handleOnView = (exampleEntity: Lite<Entity> | ModifiableEntity) => {
        return Navigator.view(exampleEntity, { requiresSaveOperation: false, isOperationVisible: eoc => false });
    }

    renderMessage(res: DynamicValidationTestResponse) {
        if (res.compileError)
            return <div className="alert alert-danger">COMPILE ERROR: {res.compileError}</div >;

        if (res.validationException)
            return <div className="alert alert-danger">EXCEPTION: {res.validationException}</div>;

        const errors = res.validationResult!.filter(vr => !!vr.validationResult);

        return (
            <div>
                {
                    (errors.length > 0) ?
                        <ul style={{ listStyleType: "none" }} className="alert alert-danger">
                            {errors.orderBy(e => e.propertyName).map((e, i) => <li key={i}>{e.propertyName} - {e.validationResult}</li>)}
                        </ul> :
                        <div className="alert alert-success">VALID: null</div>
                }
            </div>
        );
    }
}

interface PropertyIsHelpComponentProps {
    route: PropertyRoute;
}

export class PropertyIsHelpComponent extends React.Component<PropertyIsHelpComponentProps> {

    render() {
        return (
            <UncontrolledDropdown>
                <DropdownToggle color="info" caret>{DynamicValidationMessage.PropertyIs.niceToString()}</DropdownToggle>
                <DropdownMenu>
                    {Dic.map(this.props.route.subMembers(), (key, memberInfo) =>
                        <DropdownItem style={{ paddingTop: "0", paddingBottom: "0" }} key={key} onClick={() => this.handlePropertyIsClick(key)}>{key}</DropdownItem>)}
                </DropdownMenu>
            </UncontrolledDropdown>
        );
    }

    handlePropertyIsClick = (key: string) => {

        var text = `if (pi.Name == nameof(e.${key}) && e.${key} == )
{
    return "error";
}

return null;`;

        ValueLineModal.show({
            type: { name: "string" },
            initialValue: text,
            valueLineType: "TextArea",
            title: DynamicValidationMessage.PropertyIs.niceToString(),
            message: "Copy to clipboard: Ctrl+C, ESC",
            initiallyFocused: true,
            valueHtmlAttributes: { style: { height: "200px" } },
        }).done();
    }
}

