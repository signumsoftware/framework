import * as React from 'react'
import { WorkflowActivityModel, WorkflowActivityValidationEntity, WorkflowActivityMessage } from '../Signum.Entities.Workflow'
import * as WorkflowClient from '../WorkflowClient'
import * as DynamicViewClient from '../../../../Extensions/Signum.React.Extensions/Dynamic/DynamicViewClient'
import { TypeContext, ValueLine, ValueLineType, EntityLine, EntityTable, FormGroup } from '../../../../Framework/Signum.React/Scripts/Lines'
import { is, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { DynamicValidationEntity } from '../../../../Extensions/Signum.React.Extensions/Dynamic/Signum.Entities.Dynamic'
import * as Entities from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals';

interface WorkflowActivityModelComponentProps {
    ctx: TypeContext<WorkflowActivityModel>;
}

interface WorkflowActivityModelComponentState {
    viewInfo: { [name: string] : "Static" | "Dynamic" };
}

export default class WorkflowActivityModelComponent extends React.Component<WorkflowActivityModelComponentProps, WorkflowActivityModelComponentState> {

    constructor(props: WorkflowActivityModelComponentProps) {
        super(props);

        this.state = { viewInfo: {} };
    }

    componentWillMount() {
        
        const typeName = this.props.ctx.value.mainEntityType.cleanName;

        const registeredViews = WorkflowClient.getViewNames(typeName).toObject(k => k, v => "Static") as { [name: string]: "Static" | "Dynamic" };

        DynamicViewClient.API.getDynamicViewNames(typeName)
            .then(dynamicViews => {
                dynamicViews.forEach(dv => {
                    if (registeredViews[dv])
                        throw Error(WorkflowActivityMessage.DuplicateViewNameFound0.niceToString(`"${dv}"`));
                    else
                        registeredViews[dv] = "Dynamic";
                });
                
                this.setState({ viewInfo: registeredViews });
            }).done();
    }

    getViewNameColor(viewName: string) {

        if (this.state.viewInfo[viewName] == "Dynamic")
            return { color: "blue" };

        return { color: "black" };
    }

    handleViewNameChange = (e: React.SyntheticEvent) => {
        this.props.ctx.value.viewName = (e.currentTarget as HTMLSelectElement).value;
        this.props.ctx.value.modified = true;
        this.forceUpdate();
    };

    render() {
        var ctx = this.props.ctx;

        const mainEntityType = this.props.ctx.value.mainEntityType;

        return (
            <div>
                <ValueLine ctx={ctx.subCtx(d => d.name)} />
                <ValueLine ctx={ctx.subCtx(d => d.type)} onChange={() => this.forceUpdate()} />

                <FormGroup ctx={ctx.subCtx(d => d.viewName)} labelText={ctx.niceName(d => d.viewName)}>
                    {
                        <select value={ctx.value.viewName ? ctx.value.viewName : ""} className="form-control" onChange={this.handleViewNameChange} style={this.getViewNameColor(ctx.value.viewName || "")} >
                            {!ctx.value.viewName && <option value="">{" - "}</option> }
                            {Dic.getKeys(this.state.viewInfo).map((v, i) => <option key={i} value={v} style={this.getViewNameColor(v)}>{v}</option>)}
                        </select>
                    }
                </FormGroup>

                <EntityTable ctx={ctx.subCtx(d => d.validationRules)} columns={EntityTable.typedColumns<WorkflowActivityValidationEntity>([
                    {
                        property: wav => wav.rule,
                        headerProps: { style: { width: "100%" } },
                        template: ctx => <EntityLine ctx={ctx.subCtx(wav => wav.rule)} findOptions={{
                            queryName: DynamicValidationEntity,
                            filterOptions: [
                                { columnName: "Entity.EntityType", value: mainEntityType },
                                { columnName: "Entity.IsGlobalyEnabled", value: false },
                            ]
                        }} />
                    },
                    ctx.value.type == "DecisionTask" ? {
                        property: wav => wav.onAccept,
                        cellProps: ctx => ({ style: { verticalAlign: "middle" } }),                  
                        template: ctx => <ValueLine ctx={ctx.subCtx(wav => wav.onAccept)} formGroupStyle="None" valueHtmlProps={{ style: { margin: "0 auto" } }} />,
                    } : null,
                    ctx.value.type == "DecisionTask" ? {
                        property: wav => wav.onDecline,
                        cellProps: ctx => ({ style: { verticalAlign: "middle" } }),
                        template: ctx => <ValueLine ctx={ctx.subCtx(wav => wav.onDecline)} formGroupStyle="None" valueHtmlProps={{ style: { margin: "0 auto" } }} />,
                    } : null,
                ])}/>
                <ValueLine ctx={ctx.subCtx(d => d.description)} />
            </div>
        );
    }
}
