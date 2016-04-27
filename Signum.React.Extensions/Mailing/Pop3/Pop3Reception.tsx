import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, EntityComponent, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityFrame, EntityTabRepeater, EntityDetail} from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl, CountSearchControl }  from '../../../../Framework/Signum.React/Scripts/Search'
import { getToString }  from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { ExceptionEntity }  from '../../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { Pop3ReceptionEntity, EmailMessageEntity } from '../Signum.Entities.Mailing'

export default class Pop3Reception extends EntityComponent<Pop3ReceptionEntity> {

    renderEntity() {

         var sc = this.props.ctx;

        return (
            <div>
                <EntityLine ctx={sc.subCtx(s => s.pop3Configuration)}  />
                <ValueLine ctx={sc.subCtx(s => s.startDate)}  />
                <ValueLine ctx={sc.subCtx(s => s.endDate)}  />
                <ValueLine ctx={sc.subCtx(s => s.newEmails)}  />
                <EntityLine ctx={sc.subCtx(s => s.exception)}  />
                <CountSearchControl ctx={sc}  findOptions={{queryName: EmailMessageEntity, parentColumn: "Entity.ReceptionInfo.Reception", parentValue: sc.value}}/>
                <CountSearchControl ctx={sc}  findOptions={{queryName: ExceptionEntity, parentColumn: "Entity.Pop3Reception", parentValue: sc.value}}/>}
            </div>
        );
    }
}

