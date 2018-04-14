import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTabRepeater, EntityDetail} from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl, ValueSearchControlLine }  from '../../../../Framework/Signum.React/Scripts/Search'
import { getToString }  from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { ExceptionEntity }  from '../../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { Pop3ReceptionEntity, EmailMessageEntity } from '../Signum.Entities.Mailing'

export default class Pop3Reception extends React.Component<{ ctx: TypeContext<Pop3ReceptionEntity> }> {

    render() {

         const sc = this.props.ctx;

        return (
            <div>
                <EntityLine ctx={sc.subCtx(s => s.pop3Configuration)}  />
                <ValueLine ctx={sc.subCtx(s => s.startDate)}  />
                <ValueLine ctx={sc.subCtx(s => s.endDate)}  />
                <ValueLine ctx={sc.subCtx(s => s.newEmails)}  />
                <EntityLine ctx={sc.subCtx(s => s.exception)}  />
                <ValueSearchControlLine ctx={sc} findOptions={{ queryName: EmailMessageEntity, parentColumn: "Entity.ReceptionInfo.Reception", parentValue: sc.value }} />
                <ValueSearchControlLine ctx={sc} findOptions={{ queryName: ExceptionEntity, parentColumn: "Entity.Pop3Reception", parentValue: sc.value }} />}
            </div>
        );
    }
}

