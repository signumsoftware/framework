import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater} from '../../../../Framework/Signum.React/Scripts/Lines'
import {SearchControl }  from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { ScheduleRuleMinutelyEntity } from '../Signum.Entities.Scheduler'

export default class ScheduleRuleMinutely extends React.Component<{ ctx: TypeContext<ScheduleRuleMinutelyEntity> }> {

    render() {
        const ctx4 = this.props.ctx.subCtx({ labelColumns: {sm: 2}});

        return (
            <div>
                <ValueLine ctx={ctx4.subCtx(f => f.startingOn)} />
                <ValueLine ctx={ctx4.subCtx(f => f.eachMinutes)} />
            </div>
        );
    }
}

