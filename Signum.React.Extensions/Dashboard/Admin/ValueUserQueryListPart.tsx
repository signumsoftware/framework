
import * as React from 'react'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, RenderEntity} from '@framework/Lines'
import * as Finder from '@framework/Finder'
import { QueryDescription, SubTokensOptions } from '@framework/FindOptions'
import { getQueryNiceName } from '@framework/Reflection'
import { ModifiableEntity, EntityControlMessage } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import FileLine, { FileTypeSymbol } from '../../Files/FileLine'
import { ValueUserQueryListPartEntity, ValueUserQueryElementEmbedded } from '../Signum.Entities.Dashboard'

export default class ValueUserQueryListPart extends React.Component<{ ctx: TypeContext<ValueUserQueryListPartEntity> }> {

    render() {
        const ctx = this.props.ctx.subCtx({ formGroupStyle: "SrOnly", placeholderLabels: true });
        
        return (
            <div className="form-inline">
                <EntityRepeater ctx={ctx.subCtx(p => p.userQueries)} getComponent={ctx => this.renderUserQuery(ctx as TypeContext<ValueUserQueryElementEmbedded>)} />
            </div>
        );
    }

    renderUserQuery = (tc: TypeContext<ValueUserQueryElementEmbedded>) => {
        return (
            <div className="form-inline">
                <ValueLine ctx={tc.subCtx(cuq => cuq.label) }  />
                &nbsp;
                <EntityLine ctx={tc.subCtx(cuq => cuq.userQuery) }  formGroupHtmlAttributes={{ style: { maxWidth: "300px" } }} />
                &nbsp;
                <ValueLine ctx={tc.subCtx(cuq => cuq.href) }  />
            </div>
        );

    }
}
