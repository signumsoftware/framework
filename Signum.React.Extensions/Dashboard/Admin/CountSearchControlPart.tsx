
import * as React from 'react'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, RenderEntity} from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import FileLine, {FileTypeSymbol} from '../../Files/FileLine'
import { CountSearchControlPartEntity, CountUserQueryElementEntity } from '../Signum.Entities.Dashboard'


export default class CountSearchControlPart extends React.Component<{ ctx: TypeContext<CountSearchControlPartEntity> }, void> {

    render() {
        const ctx = this.props.ctx.subCtx({ formGroupStyle: "SrOnly", placeholderLabels: true });
        
        return (
            <div className="form-inline repeater-inline">
                <EntityRepeater ctx={ctx.subCtx(p => p.userQueries) } getComponent={this.renderUserQuery}/>
            </div>
        );
    }

    renderUserQuery = (tc: TypeContext<CountUserQueryElementEntity>) => {
        return (
            <div className="form-inline repeater-inline">
                <ValueLine ctx={tc.subCtx(cuq => cuq.label) }  />
                &nbsp;
                <EntityLine ctx={tc.subCtx(cuq => cuq.userQuery) }  formGroupHtmlProps={{ style: { maxWidth: "300px" } }} />
                &nbsp;
                <ValueLine ctx={tc.subCtx(cuq => cuq.href) }  />
            </div>
        );

    }
}
