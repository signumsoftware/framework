import * as React from 'react'
import { classes } from '@framework/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTabRepeater, EntityDetail} from '@framework/Lines'
import { SearchControl, ValueSearchControlLine }  from '@framework/Search'
import { getToString }  from '@framework/Signum.Entities'
import { ExceptionEntity }  from '@framework/Signum.Entities.Basics'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import { Pop3ConfigurationEntity, EmailMessageEntity } from '../Signum.Entities.Mailing'

export default class Pop3Configuration extends React.Component<{ ctx: TypeContext<Pop3ConfigurationEntity> }> {

    render() {

         const sc = this.props.ctx;

        return (
            <div>
                <ValueLine ctx={sc.subCtx(s => s.active)}  />
                <ValueLine ctx={sc.subCtx(s => s.port)}  />
                <ValueLine ctx={sc.subCtx(s => s.host)}  />
                <ValueLine ctx={sc.subCtx(s => s.username)}  />
                <ValueLine ctx={sc.subCtx(s => s.password)} valueHtmlAttributes={{type: "password"}}  />
                <ValueLine ctx={sc.subCtx(s => s.enableSSL)}  />
                <ValueLine ctx={sc.subCtx(s => s.readTimeout)}  />
                <ValueLine ctx={sc.subCtx(s => s.deleteMessagesAfter)}  />
                <EntityRepeater ctx={sc.subCtx(s => s.clientCertificationFiles)}  />
                {sc.value.isNew && <ValueSearchControlLine ctx={sc} findOptions={{queryName: Pop3ConfigurationEntity, parentToken: "Pop3Configuration", parentValue: sc.value}}/> }
            </div>
        );
    }
}

