import * as React from 'react'
import { DateTime } from 'luxon'
import { RestLogEntity } from '../Signum.Entities.Rest'
import { TypeContext, ValueLine, EntityLine, EntityRepeater } from "@framework/Lines";
import { } from "@framework/ConfigureReactWidgets";
import { API } from '../RestClient'
import { DiffDocument } from '../../DiffLog/Templates/DiffDocument';
import * as AppContext from '@framework/AppContext'
import { Tab, Tabs, Button } from 'react-bootstrap';
import { FormatJson } from '../../../Signum.React/Scripts/Exceptions/Exception';

export interface RestLogState {
  replayResult?: string,
  newURL: string
}

export default class RestLog extends React.Component<{ ctx: TypeContext<RestLogEntity> }, RestLogState> {

  constructor(props: { ctx: TypeContext<RestLogEntity> }) {
    super(props);
    const prefix = AppContext.toAbsoluteUrl("~/api");
    const suffix = props.ctx.subCtx(f => f.url).value.after("/api");
    const queryParams = props.ctx.value.queryString.map(mle => `${mle.element.key}=${mle.element.value}`).join("&");
    this.state = {
      newURL: `${location.protocol}//${location.hostname}:${location.port}${prefix}${suffix}?${queryParams}`
    }
  }

  render() {
    const ctx = this.props.ctx;
    const ctx4 = ctx.subCtx({ labelColumns: 4 });

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

        <EntityRepeater ctx={ctx.subCtx(f => f.queryString)} />
        {
          ctx.value.allowReplay && <div>
            <Button variant="info" onClick={() => { API.replayRestLog(ctx.value.id!, encodeURIComponent(this.state.newURL)).then(d => this.setState({ replayResult: d })) }}>Replay</Button>
            <input type="text" className="form-control" value={this.state.newURL} onChange={e => this.setState({ newURL: e.currentTarget.value })} />
          </div>
        }
        {this.renderCode(ctx.subCtx(f => f.requestBody))}

        <fieldset>
          <legend>{ctx.subCtx(f => f.responseBody).niceName()}</legend>
          <Tabs defaultActiveKey="prev" id="restLogs">
            <Tab title="prev" eventKey="prev" className="linkTab">{this.renderPre(ctx.value.responseBody!)}</Tab>
            {this.state.replayResult && <Tab title="diff" eventKey="diff" className="linkTab">{<DiffDocument first={ctx.value.responseBody ?? ""} second={this.state.replayResult} />}</Tab>}
            {this.state.replayResult && <Tab title="curr" eventKey="curr" className="linkTab">{this.renderPre(this.state.replayResult)}</Tab>}
          </Tabs>
        </fieldset>
      </div>
    );
  }

  renderCode(ctx: TypeContext<string | null>) {
    return (
      <fieldset>
        <legend>{ctx.niceName()}</legend>
        <FormatJson code={ctx.value!} />
      </fieldset>
    );

  }

  renderPre(text: string) {
    return <pre style={{ whiteSpace: "pre-wrap" }}><code>{text}</code></pre>
  }
}

