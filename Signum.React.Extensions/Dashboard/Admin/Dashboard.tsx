
import * as React from 'react'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, RenderEntity} from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos, basicConstruct } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Constructor from '../../../../Framework/Signum.React/Scripts/Constructor'
import SelectorModal from '../../../../Framework/Signum.React/Scripts/SelectorModal'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import FileLine, {FileTypeSymbol} from '../../Files/FileLine'
import { DashboardEntity, PanelPartEntity, IPartEntity } from '../Signum.Entities.Dashboard'
import { EntityGridRepeater, EntityGridItem } from './EntityGridRepeater'



require("!style!css!../Dashboard.css");

export default class ChartScript extends React.Component<{ ctx: TypeContext<DashboardEntity> }, void> {

    render() {
        const ctx = this.props.ctx;
        const sc = ctx.subCtx({ formGroupStyle: "Basic" });
        return (
            <div>
                <div className="form-vertical">
                    <div className="row">
                        <div className="col-sm-6">
                            <ValueLine ctx={sc.subCtx(cp => cp.displayName) }  />
                        </div>
                        <div className="col-sm-3">
                            <ValueLine ctx={sc.subCtx(cp => cp.dashboardPriority) }  />
                        </div>
                        <div className="col-sm-3">
                            <ValueLine ctx={sc.subCtx(cp => cp.autoRefreshPeriod) }  />
                        </div>
                    </div>
                    <div className="row">
                        <div className="col-sm-4">
                            <EntityLine ctx={sc.subCtx(cp => cp.owner) } create={false} />
                        </div>
                        <div className="col-sm-4">
                            <EntityLine ctx={sc.subCtx(cp => cp.entityType) } onChange={() => this.forceUpdate() }  />
                        </div>
                        {sc.value.entityType && <div className="col-sm-4">
                            <ValueLine ctx={sc.subCtx(f => f.embeddedInEntity) }  />
                        </div>}
                    </div>
                </div>
                <EntityGridRepeater ctx={ctx.subCtx(cp => cp.parts) } getComponent={this.renderPart} onCreate={this.handleOnCreate}/>
            </div>
        );
    }

    handleOnCreate = () => {
        const pr = DashboardEntity.memberInfo(a => a.parts![0].element.content);

        return SelectorModal.chooseType(getTypeInfos(pr.type))
            .then(ti => {
                if (ti == undefined)
                    return undefined;

                return PanelPartEntity.New(p => {
                    p.content = basicConstruct(ti.name) as any as IPartEntity;
                    p.style = "Default";
                });
            });
    }

    renderPart = (tc: TypeContext<PanelPartEntity>) => {

        const title = (
            <div>
                <ValueLine ctx={tc.subCtx(pp => pp.title, { formGroupStyle: "None", placeholderLabels: true }) }  />
                &nbsp;
                <ValueLine ctx={tc.subCtx(pp => pp.style, { formGroupStyle: "None" }) } onChange={() => this.forceUpdate() }  />
            </div>
        );

        return (
            <EntityGridItem title={title} bsStyle={tc.value.style}>
                <RenderEntity ctx={tc.subCtx(a => a.content) }/>
            </EntityGridItem>
        );
    }
}
