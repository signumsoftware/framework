import * as React from 'react'
import { UserQueryEntity, UserQueryEntity_Type, UserQueryMessage, QueryFilterEntity, QueryOrderEntity, QueryColumnEntity } from '../../UserQueries/Signum.Entities.UserQueries'
import ChartBuilder from '../Templates/ChartBuilder'
import { ChartScriptEntity, ChartScriptEntity_Type, ChartScriptColumnEntity, ChartScriptParameterEntity } from '../Signum.Entities.Chart'
import { FormGroup, FormControlStatic, EntityComponent, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityFrame} from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'

require("!style!css!../Chart.css");

export default class UserChart extends EntityComponent<ChartScriptEntity> {

    renderEntity() {

        return (
            <div>

                <div className="row">
                    <div className="col-sm-11">
                        <ValueLine ctx={this.subCtx(c => c.name) }  />
                        {/* @Html.FileLineLite(cc, c => c.Icon, fl =>
                        {
                            fl.LabelText = Html.PropertyNiceName(() => cc.Value.Icon);
                        fl.AttachFunction = ChartClient.ModuleScript["refreshIcon"](fl, imageRoute);
                        }) */}
                        <ValueLine ctx={this.subCtx(c => c.groupBy) }  />
                    </div>
                    <div className="col-sm-1">
                            <div className="col-sm-6">
                                <img src="@Url.Action((FileController fc) => fc.Download(new RuntimeInfo(cc.Value.Icon).ToString()))" />
                            </div>
                    </div>
                </div>

                <div className="sf-chartscript-columns">
                    <EntityRepeater ctx={this.subCtx(c => c.columns) } getComponent={this.renderColumn}  />
                </div>
                <EntityRepeater ctx={this.subCtx(c => c.parameters) } getComponent={this.renderParameter}  />
                @Html.Partial(Signum.Web.Chart.ChartClient.ChartScriptCodeView, cc)
            </div>
        );
    }

    renderColumn = (ctx: TypeContext<ChartScriptColumnEntity>) => {
        return (
            <div>
                <ValueLine ctx={ctx.subCtx(c => c.displayName) } valueColumns={{ sm: 8 }} />
                <div className="row">
                    <div className="col-sm-6">
                        <ValueLine ctx={ctx.subCtx(c => c.columnType) } labelColumns={{ sm: 4 }} />
                    </div>
                    <div className="col-sm-3">
                        <ValueLine ctx={ctx.subCtx(c => c.isGroupKey) } labelColumns={{ sm: 6 }} />
                    </div>
                    <div className="col-sm-3">
                        <ValueLine ctx={ctx.subCtx(c => c.isOptional) } labelColumns={{ sm: 6 }} />
                    </div>
                </div>
            </div>
        );
    }

    renderParameter = (ctx: TypeContext<ChartScriptParameterEntity>) => {
        var cc = ctx.subCtx({ formGroupStyle: FormGroupStyle.Basic });
        return (
            <div>
                <div className="form-vertical">
                    <div className="col-sm-2">
                        <ValueLine ctx={cc.subCtx(c => c.name) }  />
                    </div>

                    <div className="col-sm-2">
                        <ValueLine ctx={cc.subCtx(c => c.type) }  />
                    </div>

                    <div className="col-sm-6">
                        <ValueLine ctx={cc.subCtx(c => c.valueDefinition) }  />
                    </div>
                    <div className="col-sm-2">
                        <ValueLine ctx={cc.subCtx(c => c.columnIndex) }  />
                    </div>
                </div>
            </div>
        );
    }
}

