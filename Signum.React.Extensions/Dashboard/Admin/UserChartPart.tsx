
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
import { UserChartPartEntity } from '../Signum.Entities.Dashboard'


export default class UserChartPart extends EntityComponent<UserChartPartEntity> {

    renderEntity() {
        var ctx = this.props.ctx;
        
        return (
            <div >
                <EntityLine ctx={ctx.subCtx(p => p.userChart) } create={false} />
                <ValueLine ctx={ctx.subCtx(p => p.showData) } />
            </div>
        );
    }
}
