import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { ValueSearchControlLine } from '@framework/Search'
import { toLite } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import { TypeContext } from '@framework/TypeContext'
import { ProcessEntity, ProcessExceptionLineEntity } from '../Signum.Entities.Processes'
import ProgressBar from '../../MachineLearning/Templates/ProgressBar';
import { BsColor } from '@framework/Components';

export default class Process extends React.Component<{ ctx: TypeContext<ProcessEntity> }> {
  handler: number | undefined;
  componentWillMount() {
    this.reloadIfNecessary(this.props.ctx.value);
  }

  componentWillReceiveProps(newProps: { ctx: TypeContext<ProcessEntity> }) {
    this.reloadIfNecessary(newProps.ctx.value);
  }

  reloadIfNecessary(e: ProcessEntity) {
    if ((e.state == "Executing" || e.state == "Queued") && this.handler == undefined) {
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

  processExceptionsCounter!: ValueSearchControlLine;

  render() {

    const ctx4 = this.props.ctx.subCtx({ labelColumns: { sm: 4 } });
    const ctx5 = this.props.ctx.subCtx({ labelColumns: { sm: 5 } });
    const ctx3 = this.props.ctx.subCtx({ labelColumns: { sm: 3 } });

    return (
      <div>
        <div className="row">
          <div className="col-sm-6">
            <ValueLine ctx={ctx4.subCtx(f => f.state)} readOnly={true} />
            <EntityLine ctx={ctx4.subCtx(f => f.algorithm)} />
            <EntityLine ctx={ctx4.subCtx(f => f.user)} />
            <EntityLine ctx={ctx4.subCtx(f => f.data)} readOnly={true} />
            <ValueLine ctx={ctx4.subCtx(f => f.machineName)} />
            <ValueLine ctx={ctx4.subCtx(f => f.applicationName)} />
          </div>
          <div className="col-sm-6">
            <ValueLine ctx={ctx5.subCtx(f => f.creationDate)} />
            <ValueLine ctx={ctx5.subCtx(f => f.plannedDate)} hideIfNull={true} readOnly={true} />
            <ValueLine ctx={ctx5.subCtx(f => f.cancelationDate)} hideIfNull={true} readOnly={true} />
            <ValueLine ctx={ctx5.subCtx(f => f.queuedDate)} hideIfNull={true} readOnly={true} />
            <ValueLine ctx={ctx5.subCtx(f => f.executionStart)} hideIfNull={true} readOnly={true} />
            <ValueLine ctx={ctx5.subCtx(f => f.executionEnd)} hideIfNull={true} readOnly={true} />
            <ValueLine ctx={ctx5.subCtx(f => f.suspendDate)} hideIfNull={true} readOnly={true} />
            <ValueLine ctx={ctx5.subCtx(f => f.exceptionDate)} hideIfNull={true} readOnly={true} />
          </div>
        </div>

        <EntityLine ctx={ctx3.subCtx(f => f.exception)} hideIfNull={true} readOnly={true} labelColumns={2} />

        <h4>{this.props.ctx.niceName(a => a.progress)}</h4>

        {this.renderProgress()}

        <ValueSearchControlLine ctx={ctx3}
          ref={(vsc: ValueSearchControlLine) => this.processExceptionsCounter = vsc}
          findOptions={{
            queryName: ProcessExceptionLineEntity,
            parentToken: ProcessExceptionLineEntity.token(e => e.process),
            parentValue: ctx3.value
          }} />
      </div>
    );
  }

  renderProgress() {

    const p = this.props.ctx.value;

    const color: BsColor | undefined =
      p.state == "Queued" ? "info" :
        p.state == "Executing" ? undefined :
          p.state == "Finished" ? "success" :
            p.state == "Suspending" || p.state == "Suspended" ? "warning" :
              p.state == "Error" ? "danger" :
                undefined;

    return (
      <ProgressBar
        message={p.state == "Finished" ? null : p.status}
        value={p.state == "Created" ? 0 : (p.progress == 0 || p.progress == 1) ? null : p.progress}
        color={color}
        showPercentageInMessage={p.state != "Created" && p.state != "Finished"}
        active={p.state == "Finished" ? false : undefined}
        striped={p.state == "Finished" ? false : undefined}
      />
    );
  }
}

