import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, EntityComponent, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityFrame} from '../../../../Framework/Signum.React/Scripts/Lines'
import {SearchControl }  from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { HolidayCalendarEntity } from '../Signum.Entities.Scheduler'

export default class HolidayCalendar extends EntityComponent<HolidayCalendarEntity> {

    renderEntity() {
        
        var e = this.props.ctx;

        return (
            <div>    
                <ValueLine ctx={e.subCtx(f => f.name)}  />
                <div>
                    <EntityRepeater ctx={e.subCtx(f => f.holidays)}  />
                </div>
            </div>
        );
    }
}

