import * as React from 'react'
import { AutoLine, EntityLine, EntityDetail } from '@framework/Lines'
import { SearchValueLine, SearchValueLineController } from '@framework/Search'
import { toLite } from '@framework/Signum.Entities'
import { Navigator } from '@framework/Navigator'
import { TypeContext } from '@framework/TypeContext'
import { ProcessEntity, ProcessExceptionLineEntity, ProcessState } from '../Signum.Processes'
import { BsColor } from '@framework/Components';
import ProgressBar from '@framework/Components/ProgressBar';
import { useInterval } from '@framework/Hooks'

export default function Process({ ctx }: { ctx: TypeContext<ProcessEntity> }): React.JSX.Element {
  const isActive = ctx.value.state == "Executing" || ctx.value.state == "Queued" || ctx.value.state == "Suspending";

  const tick = useInterval(isActive ? 500 : null, 0, n => n + 1);
  const vscl = React.useRef<SearchValueLineController>(null);
  React.useEffect(() => {
    if (isActive) {
      const lite = toLite(ctx.value);
      vscl.current && vscl.current.searchValue!.refreshValue();
      Navigator.API.fetchEntityPack(lite)
        .then(pack => ctx.frame!.onReload(pack));
    }
  }, [tick]);

  const ctx4 = ctx.subCtx({ labelColumns: { sm: 4 } });
  const ctx5 = ctx.subCtx({ labelColumns: { sm: 5 } });
  const ctx3 = ctx.subCtx({ labelColumns: { sm: 3 } });

  return (
    <div>
      <div className="row">
        <div className="col-sm-6">
          <AutoLine ctx={ctx4.subCtx(f => f.state)} readOnly={true} />
          <EntityLine ctx={ctx4.subCtx(f => f.algorithm)} />
          <EntityLine ctx={ctx4.subCtx(f => f.user)} />
          <AutoLine ctx={ctx4.subCtx(f => f.machineName)} />
          <AutoLine ctx={ctx4.subCtx(f => f.applicationName)} />
          <EntityLine ctx={ctx4.subCtx(f => f.data)} readOnly={false} />
        </div>
        <div className="col-sm-6">
          <AutoLine ctx={ctx5.subCtx(f => f.creationDate)} />
          <AutoLine ctx={ctx5.subCtx(f => f.plannedDate)} hideIfNull={true} readOnly={true} />
          <AutoLine ctx={ctx5.subCtx(f => f.cancelationDate)} hideIfNull={true} readOnly={true} />
          <AutoLine ctx={ctx5.subCtx(f => f.queuedDate)} hideIfNull={true} readOnly={true} />
          <AutoLine ctx={ctx5.subCtx(f => f.executionStart)} hideIfNull={true} readOnly={true} />
          <AutoLine ctx={ctx5.subCtx(f => f.executionEnd)} hideIfNull={true} readOnly={true} />
          <AutoLine ctx={ctx5.subCtx(f => f.suspendDate)} hideIfNull={true} readOnly={true} />
          <AutoLine ctx={ctx5.subCtx(f => f.exceptionDate)} hideIfNull={true} readOnly={true} />
        </div>
      </div>

      <EntityLine ctx={ctx3.subCtx(f => f.exception)} hideIfNull={true} readOnly={true} labelColumns={2} />

      <h2 className="h4">{ctx.niceName(a => a.progress)}</h2>

      <ProcessProgressBar state={ctx.value.state} status={ctx.value.status} progress={ctx.value.progress} />

      <SearchValueLine ctx={ctx3}
        badgeColor="danger"
        ref={vscl}
        findOptions={{
          queryName: ProcessExceptionLineEntity,
          filterOptions: [{ token: ProcessExceptionLineEntity.token(e => e.process), value: ctx3.value}]
        }} />
    </div>
  );
}

export function ProcessProgressBar({ state, status, progress }: { state: ProcessState, status?: string |null, progress: number | null }): React.JSX.Element {

  const color: BsColor | undefined =
    state == "Queued" ? "info" :
      state == "Executing" ? undefined :
        state == "Finished" ? "success" :
          state == "Suspending" || state == "Suspended" ? "warning" :
            state == "Error" ? "danger" :
              undefined;

  return (
    <ProgressBar
      message={state == "Finished" ? null : status}
      value={state == "Created" ? 0 : progress}
      color={color}
      showPercentageInMessage={state != "Created" && state != "Finished"}
      animated={state == "Finished" ? false : undefined}
      striped={state == "Finished" ? false : undefined}
    />
  );
}

