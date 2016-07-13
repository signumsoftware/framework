import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTabRepeater, EntityDetail} from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl }  from '../../../../Framework/Signum.React/Scripts/Search'
import { getToString }  from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { SmtpConfigurationEntity, SmtpNetworkDeliveryEntity, ClientCertificationFileEntity} from '../Signum.Entities.Mailing'

export default class SmtpConfiguration extends React.Component<{ ctx: TypeContext<SmtpConfigurationEntity> }, void> {

    render() {

        const sc = this.props.ctx;

        return (
            <div>
                <ValueLine ctx={sc.subCtx(s => s.name)}  />
                <ValueLine ctx={sc.subCtx(s => s.deliveryFormat)}  />
                <ValueLine ctx={sc.subCtx(s => s.deliveryMethod)}  />
                <EntityDetail ctx={sc.subCtx(s => s.network)} getComponent={this.renderNetwork} />
	            <ValueLine ctx={sc.subCtx(s => s.pickupDirectoryLocation)}  />
	            <EntityDetail ctx={sc.subCtx(s => s.defaultFrom)}  />
	            <EntityRepeater ctx={sc.subCtx(s => s.additionalRecipients)}  />
            </div>
        );
    }

    renderNetwork = (sc: TypeContext<SmtpNetworkDeliveryEntity>) => {
        return (
            <div>
                <ValueLine ctx={sc.subCtx(s => s.port)}  />
                <ValueLine ctx={sc.subCtx(s => s.host)}  />
                <ValueLine ctx={sc.subCtx(s => s.useDefaultCredentials)}  />
                <ValueLine ctx={sc.subCtx(s => s.username)}  />
                <ValueLine ctx={sc.subCtx(s => s.password)}  valueHtmlProps={{type: "password"}} />
                <ValueLine ctx={sc.subCtx(s => s.enableSSL)}  />
                <EntityRepeater ctx={sc.subCtx(s => s.clientCertificationFiles)} getComponent={this.renderClientCertification} />
            </div>
        );  
    };

    renderClientCertification = (sc: TypeContext<ClientCertificationFileEntity>) => {
        return (
            <div>
                <ValueLine ctx={sc.subCtx(s => s.certFileType)}  />
                <ValueLine ctx={sc.subCtx(s => s.fullFilePath)}  />
            </div>
        );  
    };
}

