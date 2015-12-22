/// <reference path="../typings/react/react.d.ts" />

import * as React from 'react'
import { Basics, getMixin, CorruptMixin_Type } from 'framework/signum.react/Scripts/Signum.Entities'
import { TypeContext } from 'framework/signum.react/Scripts/TypeContext'
import { ValueLine, ValueLineType, EntityComponent, EntityLine } from 'framework/signum.react/Scripts/Lines'

export class Exception extends EntityComponent<Basics.ExceptionEntity> {
    render() {
        var e = this.subContext(a=> a, { labelColumns: { sm: 4 } });
        return (
            <div>
    <div className="row">
        <div className="col-sm-6">
            <ValueLine ctx={e.subCtx(f => f.environment) } />
            <ValueLine ctx={e.subCtx(f => f.creationDate) } /> {/*unitText={this.value.creationDate.toUserInterface().toAgoString() } */}
            <EntityLine ctx={e.subCtx(f => f.user) } />
            <ValueLine ctx={e.subCtx(f => f.version) } />
            <ValueLine ctx={e.subCtx(f => f.threadId) } />
            <ValueLine ctx={e.subCtx(f => f.machineName) } />
            <ValueLine ctx={e.subCtx(f => f.applicationName) } />
            </div>
        <div className="col-sm-6">
            <ValueLine ctx={e.subCtx(f => f.mixins) } />
            <ValueLine ctx={e.subCtx(f => getMixin(f, CorruptMixin_Type).corrupt) } />
            <ValueLine ctx={e.subCtx(f => f.userHostAddress) } />
            <ValueLine ctx={e.subCtx(f => f.userHostName) } />
            <ValueLine ctx={e.subCtx(f => f.userAgent) } valueLineType={ValueLineType.TextArea} />
            </div>
        </div>
    <ValueLine ctx={this.subContext(f => f.requestUrl) } />
    <ValueLine ctx={this.subContext(f => f.urlReferer) } />
                </div>);
    }
}

