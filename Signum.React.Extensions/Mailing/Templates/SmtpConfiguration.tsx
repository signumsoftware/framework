import * as React from 'react'
import { classes } from '@framework/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTabRepeater, EntityDetail} from '@framework/Lines'
import { SearchControl }  from '@framework/Search'
import { getToString }  from '@framework/Signum.Entities'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import { SmtpConfigurationEntity, SmtpNetworkDeliveryEmbedded, ClientCertificationFileEmbedded} from '../Signum.Entities.Mailing'

export default class SmtpConfiguration extends React.Component<{ ctx: TypeContext<SmtpConfigurationEntity> }> {

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

    renderNetwork = (sc: TypeContext<SmtpNetworkDeliveryEmbedded>) => {
        return (
            <div>
                <ValueLine ctx={sc.subCtx(s => s.port)}  />
                <ValueLine ctx={sc.subCtx(s => s.host)}  />
                <ValueLine ctx={sc.subCtx(s => s.useDefaultCredentials)}  />
                <ValueLine ctx={sc.subCtx(s => s.username)}  />
                <ValueLine ctx={sc.subCtx(s => s.password)}  valueHtmlAttributes={{type: "password"}} />
                <ValueLine ctx={sc.subCtx(s => s.enableSSL)}  />
                <EntityRepeater ctx={sc.subCtx(s => s.clientCertificationFiles)} getComponent={this.renderClientCertification} />
            </div>
        );  
    };

    renderClientCertification = (sc: TypeContext<ClientCertificationFileEmbedded>) => {
        return (
            <div>
                <ValueLine ctx={sc.subCtx(s => s.certFileType)}  />
                <ValueLine ctx={sc.subCtx(s => s.fullFilePath)}  />
            </div>
        );  
    };
}

