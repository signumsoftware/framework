import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, EntityComponent, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityFrame} from '../../../../Framework/Signum.React/Scripts/Lines'
import {SearchControl }  from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { HolidayCalendarEntity, HolidayEntity } from '../Signum.Entities.Scheduler'

export default class HolidayCalendar extends EntityComponent<HolidayCalendarEntity> {

    renderEntity() {
        
        var e = this.props.ctx;

        return (
            <div>    
                <ValueLine ctx={e.subCtx(f => f.name)}  />
                <div>
                    <EntityRepeater ctx={e.subCtx(f => f.holidays) } getComponent={(ctx: TypeContext<HolidayEntity>) => {
                        var ct4 = ctx.subCtx({ labelColumns: { sm: 4 } });
                        return (
                            <div className="row">
                                <div className="col-sm-4">
                                    <ValueLine ctx={ct4.subCtx(f => f.date) }  />
                                </div>
                                <div className="col-sm-4">
                                    <ValueLine ctx={ctx.subCtx(f => f.name) }  />
                                </div>
                            </div>);
                    }}  />
                </div>
            </div>
        );
    }
}

