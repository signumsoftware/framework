import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { ValueSearchControlLine } from '@framework/Search'
import { toLite } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import { TypeContext } from '@framework/TypeContext'
import { ProcessEntity, ProcessExceptionLineEntity } from '../Signum.Entities.Processes'
import ProgressBar from '../../MachineLearning/Templates/ProgressBar';
import { BsColor } from '@framework/Components';

export default function Process(p : { ctx: TypeContext<ProcessEntity> }){
  handler: number | undefined;
  function componentWillMount() {
    reloadIfNecessary(p.ctx.value);
  }

  function componentWillReceiveProps(newProps: { ctx: TypeContext<ProcessEntity> }) {
    reloadIfNecessary(newProps.ctx.value);
  }

  function reloadIfNecessary(e: ProcessEntity) {
    if ((e.state == "Executing" || e.state == "Queued") && handler == undefined) {
      handler = setTimeout(() => {
        handler = undefined;
        const lite = toLite(e);
        processExceptionsCounter && processExceptionsCounter.refreshValue();
        Navigator.API.fetchEntityPack(lite)
          .then(pack => p.ctx.frame!.onReload(pack))
          .done();
      }, 500);
    }
  }

  processExceptionsCounter!: ValueSearchControlLine;


  function renderProgress() {
    const p = p.ctx.value;

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
  const ctx4 = p.ctx.subCtx({ labelColumns: { sm: 4 } });
  const ctx5 = p.ctx.subCtx({ labelColumns: { sm: 5 } });
  const ctx3 = p.ctx.subCtx({ labelColumns: { sm: 3 } });

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

      <h4>{p.ctx.niceName(a => a.progress)}</h4>

      {renderProgress()}

      <ValueSearchControlLine ctx={ctx3}
        ref={(vsc: ValueSearchControlLine) => processExceptionsCounter = vsc}
        findOptions={{
          queryName: ProcessExceptionLineEntity,
          parentToken: ProcessExceptionLineEntity.token(e => e.process),
          parentValue: ctx3.value
        }} />
    </div>
  );
}

