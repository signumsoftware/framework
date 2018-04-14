
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
