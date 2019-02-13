import * as React from 'react'
import * as moment from 'moment'
import { RestLogEntity } from '../Signum.Entities.Rest'
import { TypeContext, ValueLine, EntityLine, EntityRepeater } from "@framework/Lines";
import { } from "@framework/ConfigureReactWidgets";
import { RestLogDiff, API } from '../RestClient'
import { DiffDocument } from '../../DiffLog/Templates/DiffDocument';
import * as Navigator from '@framework/Navigator'
import { Tab, UncontrolledTabs } from '@framework/Components/Tabs'
import { Button } from '@framework/Components';

export interface RestLogState {
    diff?: RestLogDiff,
    newURL: string
}

export default class RestLog extends React.Component<{ ctx: TypeContext<RestLogEntity> }, RestLogState> {

    constructor(props: { ctx: TypeContext<RestLogEntity> }) {
        super(props);
        const prefix = Navigator.toAbsoluteUrl("~/api");
        const suffix = props.ctx.subCtx(f => f.url).value.after("/api");
        const queryParams = props.ctx.value.queryString.map(mle =>`${mle.element.key}=${mle.element.value}`).join("&");
        this.state = {
            newURL: `${location.protocol}//${location.hostname}:${location.port}${prefix}${suffix}?${queryParams}`
        }
    }

    render() {
        const ctx = this.props.ctx;
        const ctx4 = ctx.subCtx({ labelColumns: 4 });

        return (
            <div>
                <ValueLine ctx={ctx.subCtx(f => f.startDate)} unitText={moment(ctx.value.startDate).toUserInterface().fromNow()} />
                <ValueLine ctx={ctx.subCtx(f => f.endDate)} />
                <EntityLine ctx={ctx.subCtx(f => f.user)} />
                <ValueLine ctx={ctx.subCtx(f => f.url)} unitText={ctx.value.httpMethod!} />
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
                        <Button color="info" onClick={() => { API.replayRestLog(ctx.value.id!, encodeURIComponent(this.state.newURL)).then(d => this.setState({ diff: d })).done() }}>Replay</Button>
                        <input type="text" className="form-control" value={this.state.newURL} onChange={e => this.setState({ newURL: e.currentTarget.value })} />
                    </div>
                }
                {this.renderCode(ctx.subCtx(f => f.requestBody))}

                <fieldset>
                    <legend>{ctx.subCtx(f => f.responseBody).niceName()}</legend>
                    <UncontrolledTabs defaultEventKey="prev">
                        <Tab title="prev" eventKey="prev" className="linkTab">{this.renderPre(ctx.subCtx(f => f.responseBody).value!)}</Tab>
                        {this.state.diff && <Tab title="diff" eventKey="diff" className="linkTab">{this.state.diff.diff && <DiffDocument diff={this.state.diff.diff} />}</Tab>}
                        {this.state.diff && <Tab title="curr" eventKey="curr" className="linkTab">{this.renderPre(this.state.diff.current)}</Tab>}
                    </UncontrolledTabs>
                </fieldset>
            </div>
        );
    }

    renderCode(ctx: TypeContext<string | null>) {
        return (
            <fieldset>
                <legend>{ctx.niceName()}</legend>
                {this.renderPre(ctx.value!)}
            </fieldset>
        );

    }

    renderPre(text: string) {
        return <pre><code>{text}</code></pre>
    }
}

