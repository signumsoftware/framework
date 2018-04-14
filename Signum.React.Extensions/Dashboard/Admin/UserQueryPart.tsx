
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
import { UserQueryPartEntity } from '../Signum.Entities.Dashboard'


export default class UserQueryPart extends React.Component<{ ctx: TypeContext<UserQueryPartEntity> }> {

    render() {
        const ctx = this.props.ctx;
        
        return (
            <div >
                <EntityLine ctx={ctx.subCtx(p => p.userQuery)} create={false} />
                <ValueLine ctx={ctx.subCtx(p => p.renderMode)} inlineCheckbox={true} />
            </div>
        );
    }
}
