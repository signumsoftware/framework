import * as React from 'react'
import { classes } from '@framework/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater} from '@framework/Lines'
import {SearchControl }  from '@framework/Search'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import { ScheduledTaskEntity } from '../Signum.Entities.Scheduler'

export default class ScheduledTask extends React.Component<{ ctx: TypeContext<ScheduledTaskEntity> }> {

    render() {
        
        const e = this.props.ctx;

        return (
            <div>    
                <ValueLine ctx={e.subCtx(f => f.suspended)}  />
                <EntityLine ctx={e.subCtx(f => f.task)} create={false} />
                <EntityDetail ctx={e.subCtx(f => f.rule)}  />
                <ValueLine ctx={e.subCtx(f => f.machineName)}  />
                <ValueLine ctx={e.subCtx(f => f.applicationName)}  />
                <EntityLine ctx={e.subCtx(f => f.user)}  />
            </div>
        );
    }
}

