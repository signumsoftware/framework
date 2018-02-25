
import * as React from 'react'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, RenderEntity} from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import FileLine, {FileTypeSymbol} from '../../Files/FileLine'
import { LinkListPartEntity, LinkElementEmbedded } from '../Signum.Entities.Dashboard'


export default class ValueSearchControlPart extends React.Component<{ ctx: TypeContext<LinkListPartEntity> }> {

    render() {
        const ctx = this.props.ctx.subCtx({ formGroupStyle: "SrOnly", placeholderLabels: true });
        
        return (
            <div className="form-inline repeater-inline">
                <EntityRepeater ctx={ctx.subCtx(p => p.links) } getComponent={this.renderLink}/>
            </div>
        );
    }

    renderLink = (tc: TypeContext<LinkElementEmbedded>) => {
        return (
            <div>
                <ValueLine ctx={tc.subCtx(cuq => cuq.label) }  />
                &nbsp;
                <ValueLine ctx={tc.subCtx(cuq => cuq.link) }  />
            </div>
        );

    }
}
