import * as React from 'react'
import { Dic } from '@framework/Globals'
import { MemberInfo, getTypeInfo, PropertyRoute, Binding, TypeInfo } from '@framework/Reflection'
import { ValueLine, EntityLine, RenderEntity, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater, EntityCheckboxList, EntityTabRepeater, TypeContext, ValueLineType, FormGroup } from '@framework/Lines'
import { Entity } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror'
import TypeHelpComponent from '../../TypeHelp/TypeHelpComponent'
import TypeHelpButtonBarComponent from '../../TypeHelp/TypeHelpButtonBarComponent'
import ValueLineModal from '@framework/ValueLineModal'
import { ContextMenuPosition } from '@framework/SearchControl/ContextMenu'
import PropertyRouteCombo from "../../Basics/Templates/PropertyRouteCombo";
import { TemplateApplicableEval } from "../Signum.Entities.Templating";
import { QueryEntity } from "@framework/Signum.Entities.Basics";

interface TemplateApplicableProps {
    ctx: TypeContext<TemplateApplicableEval>;
    query: QueryEntity;
}

interface TemplateApplicableState {
    typeName?: string;
}

export default class TemplateApplicable extends React.Component<TemplateApplicableProps, TemplateApplicableState> {

    constructor(props: TemplateApplicableProps) {
        super(props);
        this.state = {};
    }

    componentWillMount() {
        this.loadData(this.props);
    }

    loadData(props: TemplateApplicableProps) {
        Finder.getQueryDescription(this.props.query.key)
            .then(qd => this.setState({ typeName: qd.columns["Entity"].type.name.split(",")[0] || "Entity" }))
            .done();
    }


    handleCodeChange = (newScript: string) => {
        const evalEntity = this.props.ctx.value;
        evalEntity.modified = true;
        evalEntity.script = newScript;
        this.forceUpdate();
    }

    render() {
        var ctx = this.props.ctx;
        if (!this.state.typeName)
            return null;

        return (
            <div>

                <div>
                    <br />
                    <div className="row">
                        <div className="col-sm-7">
                            <div className="code-container">
                                <TypeHelpButtonBarComponent typeName={this.state.typeName} mode="CSharp" ctx={this.props.ctx} />
                                <pre style={{ border: "0px", margin: "0px" }}>{"bool IsApplicable(" + this.state.typeName + "Entity e)\n{"}</pre>
                                <CSharpCodeMirror script={ctx.value.script || ""} onChange={this.handleCodeChange} />
                                <pre style={{ border: "0px", margin: "0px" }}>{"}"}</pre>
                            </div>
                        </div>
                        <div className="col-sm-5">
                            <TypeHelpComponent initialType={this.state.typeName} mode="CSharp" onMemberClick={this.handleTypeHelpClick} />
                        </div>
                    </div>
                </div>}
            </div>
        );
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
}
