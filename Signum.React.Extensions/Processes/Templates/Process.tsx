import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, EntityComponent, EntityComponentProps, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityFrame} from '../../../../Framework/Signum.React/Scripts/Lines'
import { CountSearchControl }  from '../../../../Framework/Signum.React/Scripts/Search'
import { toLite }  from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator  from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { ProcessEntity, ProcessState, ProcessExceptionLineEntity } from '../Signum.Entities.Processes'

export default class UserQuery extends EntityComponent<ProcessEntity> {

    handler: number;
    componentWillMount(){
        this.reloadIfNecessary(this.props.ctx.value);
    }

    componentWillReceiveProps(newProps : EntityComponentProps<ProcessEntity>){
        this.reloadIfNecessary(newProps.ctx.value);
    }

    reloadIfNecessary(e : ProcessEntity){
        if((this.entity.state == "Executing" || this.entity.state == "Queued") && this.handler == null) {
            this.handler = setTimeout(()=> {
                this.handler = null;
                var lite = toLite(this.entity);
                Navigator.API.fetchEntityPack(lite)
                    .then(pack=> this.props.frame.onReload(pack))
                    .done(); 
            });
        }
    }


    renderEntity() {

        var ctx4 = this.props.ctx.subCtx({ labelColumns: { sm: 4 } });
        var ctx5 = this.props.ctx.subCtx({ labelColumns: { sm: 5 } });
        var ctx3 = this.props.ctx.subCtx({ labelColumns: { sm: 3 } });

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
                        <ValueLine ctx={ctx5.subCtx(f => f.plannedDate) }  hideIfNull={true} readOnly={true} />
                        <ValueLine ctx={ctx5.subCtx(f => f.cancelationDate) }  hideIfNull={true} readOnly={true} />
                        <ValueLine ctx={ctx5.subCtx(f => f.queuedDate) }  hideIfNull={true} readOnly={true} />
                        <ValueLine ctx={ctx5.subCtx(f => f.executionStart) }  hideIfNull={true} readOnly={true} />
                        <ValueLine ctx={ctx5.subCtx(f => f.executionEnd) }  hideIfNull={true} readOnly={true} />
                        <ValueLine ctx={ctx5.subCtx(f => f.suspendDate) }  hideIfNull={true} readOnly={true} />
                        <ValueLine ctx={ctx5.subCtx(f => f.exceptionDate) }  hideIfNull={true} readOnly={true} />
                    </div>
                </div>

                <EntityLine ctx={ctx3.subCtx(f => f.exception) }  hideIfNull={true} readOnly={true} />


                <h4> { this.props.ctx.niceName(a=>a.progress) }  </h4>           

                {this.renderProgress()}

                <CountSearchControl ctx={ctx3} findOptions={{ queryName: ProcessExceptionLineEntity, parentColumn: "Process", parentValue: ctx3.value }} />
            </div>
        );
    }

    renderProgress() {

        var p = this.entity;

        var val = p.progress != null ? p.progress * 100 :
            ((p.state == "Queued" || p.state == "Suspended" || p.state == "Finished" || p.state == "Error") ? 100 : 0);

        var progressContainerClass =
            p.state == "Executing" || p.state == "Queued" || p.state == "Suspending" ? "progress-striped active" : "";

        var progressClass =
            p.state == "Queued" ? "progress-bar-info" :
                p.state == "Executing" ? "" :
                    p.state == "Finished" ? "progress-bar-success" :
                        p.state == "Suspending" || p.state == "Suspended" ? "progress-bar-warning" :
                            p.state == "Error" ? "progress-bar-danger" :
                                "";

        return (
            <div className={classes("progress",  progressContainerClass)}>
                <div className={classes("progress-bar", progressClass)}  role="progressbar" id="progressBar"  aria-valuenow="@val" aria-valuemin="0" aria-valuemax="100" style={{ width: val + "%"}}>
                    <span className="sr-only">{val}% Complete</span>
                </div>
            </div>
        );
    }
}

