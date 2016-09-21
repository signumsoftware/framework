﻿import * as React from 'react'
import * as moment from 'moment'
import { Tabs, Tab} from 'react-bootstrap'
import { ExceptionEntity } from '../Signum.Entities.Basics'
import { ValueLine, ValueLineType, EntityLine, TypeContext } from '../Lines'

export default class Exception extends React.Component<{ ctx: TypeContext<ExceptionEntity> }, void> {
    render() {
        const ctx = this.props.ctx;
        const sc = this.props.ctx.subCtx({ labelColumns: { sm: 4 } });
        return (
            <div>
                <div className="row">
                    <div className="col-sm-6">
                        <ValueLine ctx={sc.subCtx(f => f.environment) } />
                        <ValueLine ctx={sc.subCtx(f => f.creationDate) } unitText={moment(sc.value.creationDate!).toUserInterface().fromNow() } />
                        <EntityLine ctx={sc.subCtx(f => f.user) } />
                        <ValueLine ctx={sc.subCtx(f => f.version) } />
                        <ValueLine ctx={sc.subCtx(f => f.threadId) } />
                        <ValueLine ctx={sc.subCtx(f => f.machineName) } />
                        <ValueLine ctx={sc.subCtx(f => f.applicationName) } />
                    </div>
                    <div className="col-sm-6">
                        <ValueLine ctx={sc.subCtx(f => f.actionName) } />
                        <ValueLine ctx={sc.subCtx(f => f.controllerName) } />
                        <ValueLine ctx={sc.subCtx(f => f.userHostAddress) } />
                        <ValueLine ctx={sc.subCtx(f => f.userHostName) } />
                        <ValueLine ctx={sc.subCtx(f => f.userAgent) } valueLineType={ValueLineType.TextArea} />
                    </div>
                </div>
                <ValueLine ctx={ctx.subCtx(f => f.requestUrl) } />
                <ValueLine ctx={ctx.subCtx(f => f.urlReferer) } />
                <h3 style={ { color: "rgb(139, 0, 0)" } }>{ctx.value.exceptionType}</h3>
                <pre><code>{ctx.value.exceptionMessage}</code></pre>
                <Tabs id="exceptionTabs">
                    { this.codeTab(0, a => a.stackTrace) }
                    { this.codeTab(1, a => a.data) }
                    { this.codeTab(2, a => a.queryString) }
                    { this.codeTab(3, a => a.form) }
                    { this.codeTab(4, a => a.session) }
                </Tabs>
            </div>
        );
    }

    codeTab(eventKey: number, property: (ex: ExceptionEntity) => any) {
        const tc = this.props.ctx.subCtx(property);

        if (!tc.value || tc.value == "")
            return undefined;

        return <Tab title={tc.propertyRoute.member!.niceName} eventKey={eventKey}>
            <pre>
                <code>{tc.value}</code>
            </pre>
        </Tab>;
    }
}

