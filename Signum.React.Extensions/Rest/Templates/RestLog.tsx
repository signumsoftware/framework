import * as React from 'react'
import * as moment from 'moment'
import { Tabs, Tab } from 'react-bootstrap'
import { RestLogEntity } from '../Signum.Entities.Rest'
import { TypeContext, ValueLine, ValueLineType, EntityLine, EntityRepeater } from "../../../../Framework/Signum.React/Scripts/Lines";
import { } from "../../../../Framework/Signum.React/Scripts/ConfigureReactWidgets";

export default class RestLog extends React.Component<{ ctx: TypeContext<RestLogEntity> }> {
    render() {
        const ctx = this.props.ctx;
        return (
            <div>
                <ValueLine ctx={ctx.subCtx(f => f.startDate)} unitText={moment(ctx.value.startDate).toUserInterface().fromNow()} />
                <ValueLine ctx={ctx.subCtx(f => f.endDate)} />

                <EntityLine ctx={ctx.subCtx(f => f.user)}/>
                <ValueLine ctx={ctx.subCtx(f => f.url)} />
                <ValueLine ctx={ctx.subCtx(f => f.controller)} />
                <ValueLine ctx={ctx.subCtx(f => f.userHostAddress)} />
                <ValueLine ctx={ctx.subCtx(f => f.userHostName)} />
                <ValueLine ctx={ctx.subCtx(f => f.referrer)} />
                <ValueLine ctx={ctx.subCtx(f => f.action)}/>


                <EntityLine ctx={ctx.subCtx(f => f.exception)}/>

                <EntityRepeater ctx={ctx.subCtx(f => f.queryString)}/>
                {this.renderCode(ctx.subCtx(f => f.requestBody))}
                {this.renderCode(ctx.subCtx(f => f.responseBody))}
                
            </div>
        );
    }
    renderCode(ctx: TypeContext<string | null>) {
        return (
            <fieldset>
                <legend>{ctx.niceName()}</legend>
                <pre><code>{ctx.value}</code></pre>
            </fieldset>
        );

    }
}

