import * as React from 'react'
import { classes } from '@framework/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTabRepeater, EntityDetail} from '@framework/Lines'
import { SearchControl, ValueSearchControl }  from '@framework/Search'
import { getToString }  from '@framework/Signum.Entities'
import { ExceptionEntity }  from '@framework/Signum.Entities.Basics'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import { NewsletterDeliveryEntity } from '../Signum.Entities.Mailing'
import { ProcessExceptionLineEntity } from '../../Processes/Signum.Entities.Processes'

export default class NewsletterDelivery extends React.Component<{ ctx: TypeContext<NewsletterDeliveryEntity> }> {

    render() {

         const nc = this.props.ctx;

        return (
            <div>
            	<ValueLine ctx={nc.subCtx(n => n.sent)}  />
	            <ValueLine ctx={nc.subCtx(n => n.sendDate)}  />
	            <EntityLine ctx={nc.subCtx(n => n.recipient)}  />
	            <EntityLine ctx={nc.subCtx(n => n.newsletter)}  />
                <SearchControl findOptions={{queryName: ProcessExceptionLineEntity, parentToken: "Line", parentValue: nc.value}}/>
            </div>
        );
    }
}

