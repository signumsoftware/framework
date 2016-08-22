import * as React from 'react'
import * as moment from 'moment'
import { Tabs, Tab} from 'react-bootstrap'
import { RestRequestEntity } from '../Signum.Entities.RestLogging'
import { TypeContext, ValueLine, ValueLineType, EntityLine, EntityRepeater } from "../../../../Framework/Signum.React/Scripts/Lines";

export default class Request extends React.Component<{ ctx: TypeContext<RestRequestEntity> }, void> {
    render() {
        var ctx = this.props.ctx;
        const sc = this.props.ctx.subCtx({ labelColumns: { sm: 4 } });
        return (
            <div>
                <div className="row">
                    <div className="col-sm-12">
                        <ValueLine ctx={sc.subCtx(f => f.creationDate)} unitText={moment(sc.value.creationDate).toUserInterface().fromNow()} />
                        <ValueLine ctx={sc.subCtx(f => f.uRL)} />
                        <ValueLine ctx={sc.subCtx(f => f.controller)}/>
                        <ValueLine ctx={sc.subCtx(f => f.action)}/>
                        <EntityLine ctx={sc.subCtx(f => f.exception)}/>


                        <EntityRepeater ctx={sc.subCtx(f => f.queryString)}/>
                    </div>

                </div>
                
                <pre><code>{ctx.value.response}</code></pre>
            </div>
        );
    }
}

