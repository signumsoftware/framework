import * as moment from 'moment'
import * as numbro from 'numbro'
import * as React from 'react'
import { openModal, IModalProps } from '../../../../Framework/Signum.React/Scripts/Modals';
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder';
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator';
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals';
import { SelectorMessage, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeInfo, TypeReference, Binding } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { FormGroupStyle, TypeContext } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { ValueLineType, ValueLine } from '../../../../Framework/Signum.React/Scripts/Lines/ValueLine'
import { CaseActivityStats } from "../WorkflowClient";
import { FormGroup, StyleContext } from "../../../../Framework/Signum.React/Scripts/Lines";
import { CaseActivityEntity, WorkflowActivityEntity, WorkflowActivityMessage, DoneType, CaseNotificationEntity, CaseActivityMessage, WorkflowActivityType, CaseEntity } from "../Signum.Entities.Workflow";
import { EntityLink, SearchControl } from "../../../../Framework/Signum.React/Scripts/Search";
import { OperationLogEntity } from "../../../../Framework/Signum.React/Scripts/Signum.Entities.Basics";
import { Tab, Tabs, UncontrolledTabs } from '../../../../Framework/Signum.React/Scripts/Components/Tabs';
import { Modal } from '../../../../Framework/Signum.React/Scripts/Components';
import * as SelectorModal from "../../../../Framework/Signum.React/Scripts/SelectorModal";


interface CaseActivityStatsModalProps extends React.Props<CaseActivityStatsModal>, IModalProps {
    case: CaseEntity;
    caseActivityStats: CaseActivityStats[];
}

export default class CaseActivityStatsModal extends React.Component<CaseActivityStatsModalProps, { show: boolean }>  {

    constructor(props: CaseActivityStatsModalProps) {
        super(props);

        this.state = {
            show: true,
        };
    }

    handleCloseClicked = () => {
        this.setState({ show: false });
    }

    handleOnExited = () => {
        this.props.onExited!(undefined);
    }

    render() {

        var caseActivityStats = this.props.caseActivityStats;
        return (
            <Modal size="lg" onHide={this.handleCloseClicked} show={this.state.show} onExited={this.handleOnExited}>
                <div className="modal-header">
                    <h5 className="modal-title">{caseActivityStats.first().WorkflowActivity.toStr} ({caseActivityStats.length} {caseActivityStats.length == 1 ? CaseActivityEntity.niceName() : CaseActivityEntity.nicePluralName()})</h5>
                    <button type="button" className="close" data-dismiss="modal" aria-label="Close" onClick={this.handleCloseClicked}>
                        <span aria-hidden="true">&times;</span>
                    </button>
                </div>
                <div className="modal-body">
                    {
                        <div>
                            {caseActivityStats.length == 1 ? <CaseActivityStatsComponent stats={caseActivityStats.first()} caseEntity={this.props.case} /> :
                                <UncontrolledTabs id="statsTabs">
                                    {
                                        caseActivityStats.map(a =>
                                            <Tab key={a.CaseActivity.id!.toString()} eventKey={a.CaseActivity.id!}
                                                title={a.DoneDate == null ? CaseActivityMessage.Pending.niceToString() : <span>{a.DoneBy.toStr} {DoneType.niceToString(a.DoneType!)} <mark>({moment(a.DoneDate).fromNow()})</mark></span> as any}>
                                                <CaseActivityStatsComponent stats={a} caseEntity={this.props.case} />
                                            </Tab>)
                                    }
                                </UncontrolledTabs>
                            }
                        </div>
                    }
                </div>
                <div className="modal-footer">
                    <button className="btn btn-primary sf-entity-button sf-ok-button" onClick={this.handleCloseClicked}>
                        {JavascriptMessage.ok.niceToString()}
                    </button>
                </div>
            </Modal>
        );
    }

    static show(caseEntity: CaseEntity, caseActivityStats: CaseActivityStats[]): Promise<any> {
        return openModal<any>(<CaseActivityStatsModal case={caseEntity} caseActivityStats={caseActivityStats} />);
    }
}

interface CaseActivityStatsComponentProps {
    caseEntity: CaseEntity;
    stats: CaseActivityStats;
}

export class CaseActivityStatsComponent extends React.Component<CaseActivityStatsComponentProps> {
    render() {

        var ctx = new StyleContext(undefined, { labelColumns: 3 });
        var stats = this.props.stats;

        return (
            <div>
                <FormGroup ctx={ctx} labelText={CaseActivityEntity.niceName()}> <EntityLink lite={stats.CaseActivity} /></FormGroup>
                <FormGroup ctx={ctx} labelText={CaseActivityEntity.nicePropertyName(a => a.doneBy)}>{stats.DoneBy && <EntityLink lite={stats.DoneBy} />}</FormGroup>
                <FormGroup ctx={ctx} labelText={CaseActivityEntity.nicePropertyName(a => a.startDate)}>{formatDate(stats.StartDate)}</FormGroup>
                <FormGroup ctx={ctx} labelText={CaseActivityEntity.nicePropertyName(a => a.doneDate)}>{formatDate(stats.DoneDate)}</FormGroup>
                <FormGroup ctx={ctx} labelText={CaseActivityEntity.nicePropertyName(a => a.doneType)}>{stats.DoneType && DoneType.niceToString(stats.DoneType)}</FormGroup>
                <FormGroup ctx={ctx} labelText={WorkflowActivityEntity.nicePropertyName(a => a.estimatedDuration)}>{formatDuration(stats.EstimatedDuration)}</FormGroup>
                <FormGroup ctx={ctx} labelText={WorkflowActivityMessage.AverageDuration.niceToString()}>{formatDuration(stats.AverageDuration)}</FormGroup>
                <FormGroup ctx={ctx} labelText={CaseActivityEntity.nicePropertyName(a => a.duration)}>{formatDuration(stats.Duration)}</FormGroup>
                <FormGroup ctx={ctx} labelText={WorkflowActivityType.niceName()}>{WorkflowActivityType.niceToString(stats.WorkflowActivityType)}</FormGroup>
                {
                    stats.WorkflowActivityType == "Task" || stats.WorkflowActivityType == "Decision" ? this.renderTaskExtra() :
                        stats.WorkflowActivityType == "Script" ? this.renderScriptTaskExtra() :
                            stats.WorkflowActivityType == "CallWorkflow" || stats.WorkflowActivityType == "DecompositionWorkflow" ? this.renderSubWorkflowExtra(ctx) :
                                undefined

                }
            </div>
        );
    }

    renderTaskExtra() {
        var stats = this.props.stats;

        return (
            <div>
                <h3>{CaseNotificationEntity.nicePluralName()}</h3>
                <SearchControl findOptions={{ queryName: CaseNotificationEntity, parentColumn: "CaseActivity", parentValue: stats.CaseActivity }} />
            </div>
        );
    }

    renderScriptTaskExtra() {
        var stats = this.props.stats;

        return (
            <div>
                <h3>{OperationLogEntity.nicePluralName()}</h3>
                <SearchControl findOptions={{ queryName: OperationLogEntity, parentColumn: "Target", parentValue: stats.CaseActivity }} />
            </div>
        );
    }

    handleClick = (e: React.MouseEvent<HTMLButtonElement>) => {
        e.preventDefault();

        Finder.find<CaseEntity>({
            queryName: CaseEntity,
            filterOptions: [{ columnName: "Entity.DecompositionSurrogateActivity", value: this.props.stats.CaseActivity, frozen: true }]
        }, { autoSelectIfOne: true })
            .then(c => c && Navigator.navigate(c))
            .done();
    }

    renderSubWorkflowExtra(ctx: StyleContext) {
        var stats = this.props.stats;

        return (
            <FormGroup ctx={ctx}>
                <button className="btn btn-light" onClick={this.handleClick}>
                    <i className="fa fa-random" style={{ color: "green" }} /> {WorkflowActivityMessage.CaseFlow.niceToString()}
                </button>
            </FormGroup>
        );
    }
}

function formatDate(date: string | undefined) {
    if (date == undefined)
        return undefined;

    return <span>{moment(date).format("L LT")} <mark>({moment(date).fromNow()})</mark></span>
}

function formatDuration(duration: number | undefined) {
    if (duration == undefined)
        return undefined;

    var unit = CaseActivityEntity.memberInfo(a => a.duration).unit;

    return <span>{numbro(duration).format("0.00")} {unit} <mark>({moment.duration(duration, "minutes").format("d[d] h[h] m[m] s[s]")})</mark></span>
}
