import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater} from '../../../../Framework/Signum.React/Scripts/Lines'
import { ValueSearchControlLine }  from '../../../../Framework/Signum.React/Scripts/Search'
import { toLite }  from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator  from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { ProcessEntity, ProcessState, ProcessExceptionLineEntity } from '../Signum.Entities.Processes'

export default class Process extends React.Component<{ ctx: TypeContext<ProcessEntity> }> {

    handler: number | undefined;
    componentWillMount(){
        this.reloadIfNecessary(this.props.ctx.value);
    }

    componentWillReceiveProps(newProps: { ctx: TypeContext<ProcessEntity> }){
        this.reloadIfNecessary(newProps.ctx.value);
    }

    reloadIfNecessary(e : ProcessEntity){
        if((e.state == "Executing" || e.state == "Queued") && this.handler == undefined) {
            this.handler = setTimeout(() => {
                this.handler = undefined;
                const lite = toLite(e);
                this.processExceptionsCounter && this.processExceptionsCounter.refreshValue();
                Navigator.API.fetchEntityPack(lite)
                    .then(pack => this.props.ctx.frame!.onReload(pack))
                    .done();
            }, 500);
        }
    }

    processExceptionsCounter: ValueSearchControlLine;

    render() {

        const ctx4 = this.props.ctx.subCtx({ labelColumns: { sm: 4 } });
        const ctx5 = this.props.ctx.subCtx({ labelColumns: { sm: 5 } });
        const ctx3 = this.props.ctx.subCtx({ labelColumns: { sm: 3 } });

        return (
            <div>
                <div className="row">
                    <div className="col-sm-6">
                        <ValueLine ctx={ctx4.subCtx(f => f.state) } readOnly={true} />
                        <EntityLine ctx={ctx4.subCtx(f => f.algorithm) }  />
                        <EntityLine ctx={ctx4.subCtx(f => f.user) }  />
                        <EntityLine ctx={ctx4.subCtx(f => f.data) } readOnly={true} />
                        <ValueLine ctx={ctx4.subCtx(f => f.machineName) }  />
                        <ValueLine ctx={ctx4.subCtx(f => f.applicationName) }  />
                    </div>
                    <div className="col-sm-6">
                        <ValueLine ctx={ctx5.subCtx(f => f.creationDate) }  />
                        <ValueLine ctx={ctx5.subCtx(f => f.plannedDate) } hideIfNull={true} readOnly={true} />
                        <ValueLine ctx={ctx5.subCtx(f => f.cancelationDate) }  hideIfNull={true} readOnly={true} />
                        <ValueLine ctx={ctx5.subCtx(f => f.queuedDate) }  hideIfNull={true} readOnly={true} />
                        <ValueLine ctx={ctx5.subCtx(f => f.executionStart) }  hideIfNull={true} readOnly={true} />
                        <ValueLine ctx={ctx5.subCtx(f => f.executionEnd) }  hideIfNull={true} readOnly={true} />
                        <ValueLine ctx={ctx5.subCtx(f => f.suspendDate) }  hideIfNull={true} readOnly={true} />
                        <ValueLine ctx={ctx5.subCtx(f => f.exceptionDate) }  hideIfNull={true} readOnly={true} />
                    </div>
                </div>

                <EntityLine ctx={ctx3.subCtx(f => f.exception)} hideIfNull={true} readOnly={true} labelColumns={2} />

                <h4>{ this.props.ctx.niceName(a=>a.progress) }</h4>           

                {this.renderProgress()}

                <ValueSearchControlLine ctx={ctx3}
                    ref={(vsc: ValueSearchControlLine) => this.processExceptionsCounter = vsc}
                    findOptions={{
                        queryName: ProcessExceptionLineEntity,
                        parentColumn: "Process",
                        parentValue: ctx3.value
                    }} />
            </div>
        );
    }

    renderProgress() {

        const p = this.props.ctx.value;

        const val = p.progress != undefined ? (p.progress == 0 && p.status != null ? 100: p.progress * 100) :
            ((p.state == "Queued" || p.state == "Suspended" || p.state == "Finished" || p.state == "Error") ? 100 : 0);

        const progressContainerClass =
            p.state == "Executing" || p.state == "Queued" || p.state == "Suspending" ? "progress-striped active" : "";

        const progressClass =
            p.state == "Queued" ? "progress-bar-info" :
                p.state == "Executing" ? "" :
                    p.state == "Finished" ? "progress-bar-success" :
                        p.state == "Suspending" || p.state == "Suspended" ? "progress-bar-warning" :
                            p.state == "Error" ? "progress-bar-danger" :
                                "";

        const message =
            p.state != "Executing" && p.state != "Suspending" ? "" :
                val == 100 && p.status ? p.status :
                    p.status ? `${val}% - ${p.status}` :
                        `${val}%`;

        return (
            <div className={classes("progress",  progressContainerClass)}>
                <div className={classes("progress-bar", progressClass)} role="progressbar" id="progressBar" aria-valuenow="@val" aria-valuemin="0" aria-valuemax="100" style={{ width: val + "%" }}>
                    <span>{message}</span>
                </div>
            </div>
        );
    }
}

