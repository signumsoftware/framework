import * as moment from 'moment'
import * as numbro from 'numbro'
import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { openModal, IModalProps } from '@framework/Modals';
import * as Finder from '@framework/Finder';
import * as Navigator from '@framework/Navigator';
import { Dic } from '@framework/Globals';
import { SelectorMessage, JavascriptMessage } from '@framework/Signum.Entities'
import { TypeInfo, TypeReference, Binding } from '@framework/Reflection'
import { FormGroupStyle, TypeContext } from '@framework/TypeContext'
import { ValueLineType, ValueLine } from '@framework/Lines/ValueLine'
import { CaseActivityStats } from "../WorkflowClient";
import { FormGroup, StyleContext } from "@framework/Lines";
import { CaseActivityEntity, WorkflowActivityEntity, WorkflowActivityMessage, DoneType, CaseNotificationEntity, CaseActivityMessage, WorkflowActivityType, CaseEntity } from "../Signum.Entities.Workflow";
import { EntityLink, SearchControl } from "@framework/Search";
import { OperationLogEntity } from "@framework/Signum.Entities.Basics";
import { Tab, Tabs, UncontrolledTabs } from '@framework/Components/Tabs';
import { Modal } from '@framework/Components';
import * as SelectorModal from "@framework/SelectorModal";


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
                <SearchControl findOptions={{ queryName: CaseNotificationEntity, parentToken: "CaseActivity", parentValue: stats.CaseActivity }} />
            </div>
        );
    }

    renderScriptTaskExtra() {
        var stats = this.props.stats;

        return (
            <div>
                <h3>{OperationLogEntity.nicePluralName()}</h3>
                <SearchControl findOptions={{ queryName: OperationLogEntity, parentToken: "Target", parentValue: stats.CaseActivity }} />
            </div>
        );
    }

    handleClick = (e: React.MouseEvent<HTMLButtonElement>) => {
        e.preventDefault();

        Finder.find<CaseEntity>({
            queryName: CaseEntity,
            filterOptions: [{ token: "Entity.DecompositionSurrogateActivity", value: this.props.stats.CaseActivity, frozen: true }]
        }, { autoSelectIfOne: true })
            .then(c => c && Navigator.navigate(c))
            .done();
    }

    renderSubWorkflowExtra(ctx: StyleContext) {
        var stats = this.props.stats;

        return (
            <FormGroup ctx={ctx}>
                <button className="btn btn-light" onClick={this.handleClick}>
                    <FontAwesomeIcon icon="random" color="green"/> {WorkflowActivityMessage.CaseFlow.niceToString()}
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
