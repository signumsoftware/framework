
import * as React from 'react'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, RenderEntity} from '@framework/Lines'
import * as Finder from '@framework/Finder'
import { QueryDescription, SubTokensOptions } from '@framework/FindOptions'
import { getQueryNiceName } from '@framework/Reflection'
import { ModifiableEntity, EntityControlMessage } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import FileLine, {FileTypeSymbol} from '../../Files/FileLine'
import { LinkListPartEntity, LinkElementEmbedded } from '../Signum.Entities.Dashboard'


export default class ValueSearchControlPart extends React.Component<{ ctx: TypeContext<LinkListPartEntity> }> {

    render() {
        const ctx = this.props.ctx.subCtx({ formGroupStyle: "SrOnly", placeholderLabels: true });
        
        return (
            <div className="form-inline">
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
