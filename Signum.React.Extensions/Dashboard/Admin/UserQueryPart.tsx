
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
