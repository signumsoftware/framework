
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
import { UserChartPartEntity } from '../Signum.Entities.Dashboard'


export default class UserChartPart extends React.Component<{ ctx: TypeContext<UserChartPartEntity> }> {

    render() {
        const ctx = this.props.ctx;
        
        return (
            <div >
                <EntityLine ctx={ctx.subCtx(p => p.userChart)} create={false} />
                <ValueLine ctx={ctx.subCtx(p => p.showData)} inlineCheckbox={true} formGroupHtmlAttributes={{ style: { display: "block" } }} />
                <ValueLine ctx={ctx.subCtx(p => p.allowChangeShowData)} inlineCheckbox={true} formGroupHtmlAttributes={{ style: { display: "block" } }} />
            </div>
        );
    }
}
