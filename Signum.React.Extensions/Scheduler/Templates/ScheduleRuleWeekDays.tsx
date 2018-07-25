import * as React from 'react'
import { classes } from '@framework/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater} from '@framework/Lines'
import {SearchControl }  from '@framework/Search'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import { ScheduleRuleWeekDaysEntity } from '../Signum.Entities.Scheduler'

export default class ScheduleRuleWeekDays extends React.Component<{ ctx: TypeContext<ScheduleRuleWeekDaysEntity> }> {

    render() {
        
        const ctx4 = this.props.ctx.subCtx({ labelColumns: {sm: 4}});
        const ctx2 = this.props.ctx.subCtx({ labelColumns: {sm: 2}});

        return (
            <div>    
                <div className="row">
                    <div className="col-sm-6">
                        <ValueLine ctx={ctx4.subCtx(f => f.monday)}  />
                        <ValueLine ctx={ctx4.subCtx(f => f.tuesday)}  />
                        <ValueLine ctx={ctx4.subCtx(f => f.wednesday)}  />
                        <ValueLine ctx={ctx4.subCtx(f => f.thursday)}  />
                        <ValueLine ctx={ctx4.subCtx(f => f.friday)}  />
                    </div>
                    <div className="col-sm-6">
                        <ValueLine ctx={ctx4.subCtx(f => f.sunday)}  />
                        <ValueLine ctx={ctx4.subCtx(f => f.saturday)}  />
                        <ValueLine ctx={ctx4.subCtx(f => f.holiday)}  />
                    </div>
                </div>
    
                <EntityLine ctx={ctx2.subCtx(f => f.calendar)}  />
                <ValueLine ctx={ctx2.subCtx(f => f.startingOn)}  />
            </div>
        );
    }
}

