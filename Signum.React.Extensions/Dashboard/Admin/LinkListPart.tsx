
import * as React from 'react'
import { FormGroup, FormControlStatic, EntityComponent, EntityComponentProps, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityFrame, RenderEntity} from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import FileLine, {FileTypeSymbol} from '../../Files/FileLine'
import { LinkListPartEntity, LinkElementEntity} from '../Signum.Entities.Dashboard'


export default class CountSearchControlPart extends EntityComponent<LinkListPartEntity> {

    renderEntity() {
        var ctx = this.props.ctx.subCtx({ formGroupStyle: FormGroupStyle.SrOnly, placeholderLabels: true });
        
        return (
            <div className="form-inline repeater-inline">
                <EntityRepeater ctx={ctx.subCtx(p => p.links) } getComponent={this.renderLink}/>
            </div>
        );
    }

    renderLink = (tc: TypeContext<LinkElementEntity>) => {
        return (
            <div>
                <ValueLine ctx={tc.subCtx(cuq => cuq.label) }  />
                &nbsp;
                <ValueLine ctx={tc.subCtx(cuq => cuq.link) }  />
            </div>
        );

    }
}
