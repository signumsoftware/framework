import * as React from 'react'
import { DateTime } from 'luxon'
import { RestLogEntity } from '../Signum.Rest'
import { TypeContext, ValueLine, EntityLine, EntityRepeater, EntityTable } from "@framework/Lines";
import { } from "@framework/ConfigureReactWidgets";
import { API } from '../RestClient'
import { DiffDocument } from '../../Signum.DiffLog/Templates/DiffDocument';
import * as AppContext from '@framework/AppContext'
import { Tab, Tabs, Button } from 'react-bootstrap';
import { FormatJson } from '@framework/Exceptions/Exception';

export interface RestLogState {
  replayResult?: string,
  newURL: string
}

function newUrl(rl: RestLogEntity) {
  const prefix = AppContext.toAbsoluteUrl("");
  const suffix = rl.url;
  const queryParams = rl.queryString.map(mle => `${mle.element.key}=${mle.element.value}`).join("&");
  return `${location.protocol}//${location.hostname}:${location.port}${prefix}${rl.url}?${queryParams}`;
}

export default function RestLog(p: { ctx: TypeContext<RestLogEntity> }) {

  const [replayResult, setReplayResult] = React.useState<string | undefined>(undefined);

  const ctx = p.ctx;
  const ctx4 = ctx.subCtx({ labelColumns: 4 });

  const newURL = newUrl(p.ctx.value);

  return (
    <div>
      <ValueLine ctx={ctx.subCtx(f => f.startDate)} unit={DateTime.fromISO(ctx.value.startDate).toRelative() ?? undefined} />
      <ValueLine ctx={ctx.subCtx(f => f.endDate)} />
      <EntityLine ctx={ctx.subCtx(f => f.user)} />
      <ValueLine ctx={ctx.subCtx(f => f.url)} unit={ctx.value.httpMethod!} />
      <div className="row">
        <div className="col-sm-6">
          <ValueLine ctx={ctx4.subCtx(f => f.controller)} />
        </div>
        <div className="col-sm-6">
          <ValueLine ctx={ctx4.subCtx(f => f.action)} />
        </div>
      </div>
      <div className="row">
        <div className="col-sm-6">
          <ValueLine ctx={ctx4.subCtx(f => f.machineName)} />
        </div>
        <div className="col-sm-6">
          <ValueLine ctx={ctx4.subCtx(f => f.applicationName)} />
        </div>
      </div>
      <div className="row">
        <div className="col-sm-6">
          <ValueLine ctx={ctx4.subCtx(f => f.userHostAddress)} />
        </div>
        <div className="col-sm-6">
          <ValueLine ctx={ctx4.subCtx(f => f.userHostName)} />
        </div>
      </div>

      <ValueLine ctx={ctx.subCtx(f => f.referrer)} />

      <EntityLine ctx={ctx.subCtx(f => f.exception)} />

      <ValueLine ctx={ctx.subCtx(f => f.replayDate)} />
      <ValueLine ctx={ctx.subCtx(f => f.changedPercentage)} />

      <EntityTable ctx={ctx.subCtx(f => f.queryString)} avoidFieldSet />
      {
        ctx.value.allowReplay &&
        <div className="row mt-2">
          <div className="col-sm-10">
            <input type="text" className="form-control" value={newURL} onChange={e => setReplayResult(e.currentTarget.value)} />
          </div>
          <div className="col-sm-2">
            <Button variant="info" onClick={() => { API.replayRestLog(ctx.value.id!, encodeURIComponent(newURL)).then(d => setReplayResult(d)) }}>Replay</Button>
          </div>
        </div>
      }

      {renderCode(ctx.subCtx(f => f.requestBody))}

      <fieldset>
        <legend>{ctx.subCtx(f => f.responseBody).niceName()}</legend>
        <Tabs defaultActiveKey="prev" id="restLogs">
          <Tab title="prev" eventKey="prev" className="linkTab"><FormatJson code={ctx.value.responseBody} /></Tab>
          {replayResult && <Tab title="diff" eventKey="diff" className="linkTab">{<DiffDocument first={ctx.value.responseBody ?? ""} second={replayResult} />}</Tab>}
          {replayResult && <Tab title="curr" eventKey="curr" className="linkTab"><FormatJson code={replayResult} /></Tab>}
        </Tabs>
      </fieldset>
    </div>
  );

  function renderCode(ctx: TypeContext<string | null>) {
    return (
      <fieldset>
        <legend>{ctx.niceName()}</legend>
        <FormatJson code={ctx.value!} />
      </fieldset>
    );

  }
}

