import * as React from 'react'
import * as moment from 'moment'
import { RestLogEntity } from '../Signum.Entities.Rest'
import { TypeContext, ValueLine, ValueLineType, EntityLine, EntityRepeater } from "../../../../Framework/Signum.React/Scripts/Lines";
import { } from "../../../../Framework/Signum.React/Scripts/ConfigureReactWidgets";
import { RestLogDiff, API } from '../RestClient'
import { DiffDocument } from '../../DiffLog/Templates/DiffDocument';
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { Tab, UncontrolledTabs } from '../../../../Framework/Signum.React/Scripts/Components/Tabs'
import { Button } from '../../../../Framework/Signum.React/Scripts/Components';

export interface RestLogState {
    diff?: RestLogDiff,
    newURL: string
}

export default class RestLog extends React.Component<{ ctx: TypeContext<RestLogEntity> }, RestLogState> {

    constructor(props: { ctx: TypeContext<RestLogEntity> }) {
        super(props);
        var prefix = Navigator.toAbsoluteUrl("~/api");
        var suffix = props.ctx.subCtx(f => f.url).value.after("/api");
        this.state = {
            newURL: location.protocol + "//" + location.hostname + prefix + suffix
        }
    }

    render() {
        const ctx = this.props.ctx;
        return (
            <div>
                <ValueLine ctx={ctx.subCtx(f => f.startDate)} unitText={moment(ctx.value.startDate).toUserInterface().fromNow()} />
                <ValueLine ctx={ctx.subCtx(f => f.endDate)} />

                <EntityLine ctx={ctx.subCtx(f => f.user)} />
                <ValueLine ctx={ctx.subCtx(f => f.url)} unitText={ctx.value.httpMethod!} />
                <ValueLine ctx={ctx.subCtx(f => f.controller)} />
                <ValueLine ctx={ctx.subCtx(f => f.action)} />

                <ValueLine ctx={ctx.subCtx(f => f.userHostAddress)} />
                <ValueLine ctx={ctx.subCtx(f => f.userHostName)} />
                <ValueLine ctx={ctx.subCtx(f => f.referrer)} />

                <EntityLine ctx={ctx.subCtx(f => f.exception)} />

                <ValueLine ctx={ctx.subCtx(f => f.replayDate)} />
                <ValueLine ctx={ctx.subCtx(f => f.changedPercentage)} />

                <EntityRepeater ctx={ctx.subCtx(f => f.queryString)} />
                {
                    ctx.value.allowReplay && <div>
                        <Button color="info" onClick={() => { API.replayRestLog(ctx.value.id, encodeURIComponent(this.state.newURL)).then(d => this.setState({ diff: d })).done() }}>Replay</Button>
                        <input type="text" className="form-control" value={this.state.newURL} onChange={e => this.setState({ newURL: e.currentTarget.value })} />
                    </div>
                }
                {this.renderCode(ctx.subCtx(f => f.requestBody))}

                <fieldset>
                    <legend>{ctx.subCtx(f => f.responseBody).niceName()}</legend>
                    <UncontrolledTabs defaultEventKey="prev">
                        <Tab title="prev" eventKey="prev" className="linkTab">{this.renderPre(ctx.subCtx(f => f.responseBody).value!)}</Tab>
                        {this.state.diff && <Tab title="diff" eventKey="diff" className="linkTab">{this.renderDiff()}</Tab>}
                        {this.state.diff && this.state.diff.current && <Tab title="curr" eventKey="curr" className="linkTab">{this.renderPre(this.state.diff.current)}</Tab>}
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
    renderDiff(): any {
        return <DiffDocument diff={this.state.diff!.diff} />
    }
}

