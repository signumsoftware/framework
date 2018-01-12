import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater} from '../../../../Framework/Signum.React/Scripts/Lines'
import {SearchControl }  from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { ScheduleRuleMonthsEntity } from '../Signum.Entities.Scheduler'

export default class ScheduleRuleMonths extends React.Component<{ ctx: TypeContext<ScheduleRuleMonthsEntity> }> {

    render() {
        
        const ctx4 = this.props.ctx.subCtx({ labelColumns: {sm: 4}});
        const ctx2 = this.props.ctx.subCtx({ labelColumns: {sm: 2}});

        return (
            <div>    
                <div className="row">
                    <div className="col-sm-3">
                        <ValueLine ctx={ctx4.subCtx(f => f.january)}  />
                        <ValueLine ctx={ctx4.subCtx(f => f.february)}  />
                        <ValueLine ctx={ctx4.subCtx(f => f.march)}  />
                    </div>
                    <div className="col-sm-3">
                        <ValueLine ctx={ctx4.subCtx(f => f.april)}  />
                        <ValueLine ctx={ctx4.subCtx(f => f.march)}  />
                        <ValueLine ctx={ctx4.subCtx(f => f.june)}  />
                    </div>
                    <div className="col-sm-3">
                        <ValueLine ctx={ctx4.subCtx(f => f.july)}  />
                        <ValueLine ctx={ctx4.subCtx(f => f.august)}  />
                        <ValueLine ctx={ctx4.subCtx(f => f.september)}  />
                    </div>
                    <div className="col-sm-3">
                        <ValueLine ctx={ctx4.subCtx(f => f.october)}  />
                        <ValueLine ctx={ctx4.subCtx(f => f.november)}  />
                        <ValueLine ctx={ctx4.subCtx(f => f.december)}  />
                    </div>
                </div>
                <ValueLine ctx={ctx2.subCtx(f => f.startingOn)}  />
            </div>
        );
    }
}

