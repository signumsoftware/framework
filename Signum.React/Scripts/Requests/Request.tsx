import * as React from 'react'
import * as moment from 'moment'
import { Tabs, Tab} from 'react-bootstrap'
import { RequestEntity } from '../Signum.Entities.Basics'
import { ValueLine, ValueLineType, EntityLine, TypeContext, EntityRepeater } from '../Lines'

export default class Request extends React.Component<{ ctx: TypeContext<RequestEntity> }, void> {
    render() {
        var ctx = this.props.ctx;
        const sc = this.props.ctx.subCtx({ labelColumns: { sm: 4 } });
        return (
            <div>
                <div className="row">
                    <div className="col-sm-12">
                        <ValueLine ctx={sc.subCtx(f => f.creationDate)} unitText={moment(sc.value.creationDate).toUserInterface().fromNow()} />
                        <ValueLine ctx={sc.subCtx(f => f.request)} />
                        <EntityRepeater ctx={sc.subCtx(f => f.values)}/>
                    </div>

                </div>
                
                <pre><code>{ctx.value.response}</code></pre>
            </div>
        );
    }
}

