
import * as React from 'react'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, RenderEntity } from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos, New } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Constructor from '../../../../Framework/Signum.React/Scripts/Constructor'
import SelectorModal from '../../../../Framework/Signum.React/Scripts/SelectorModal'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import FileLine, { FileTypeSymbol } from '../../Files/FileLine'
import { DashboardEntity, PanelPartEmbedded, IPartEntity } from '../Signum.Entities.Dashboard'
import { EntityGridRepeater, EntityGridItem } from './EntityGridRepeater'
import { IModalProps, openModal } from "../../../../Framework/Signum.React/Scripts/Modals";
import * as DashboardClient from "../DashboardClient";

import "../Dashboard.css"
import { classes } from "../../../../Framework/Signum.React/Scripts/Globals";
import { Color } from "../../Basics/Color";
import { IconTypeaheadLine } from "../../Basics/Templates/IconTypeahead";
import { ColorTypeaheadLine } from "../../Basics/Templates/ColorTypeahead";

export default class Dashboard extends React.Component<{ ctx: TypeContext<DashboardEntity> }> {

    handleEntityTypeChange = () => {
        if (!this.props.ctx.value.entityType)
            this.props.ctx.value.embeddedInEntity = null;

        this.forceUpdate()
    }

    render() {
        const ctx = this.props.ctx;
        const sc = ctx.subCtx({ formGroupStyle: "Basic" });
        return (
            <div>
                <div>
                    <div className="row">
                        <div className="col-sm-6">
                            <ValueLine ctx={sc.subCtx(cp => cp.displayName)} />
                        </div>
                        <div className="col-sm-3">
                            <ValueLine ctx={sc.subCtx(cp => cp.dashboardPriority)} />
                        </div>
                        <div className="col-sm-3">
                            <ValueLine ctx={sc.subCtx(cp => cp.autoRefreshPeriod)} />
                        </div>
                    </div>
                    <div className="row">
                        <div className="col-sm-4">
                            <EntityLine ctx={sc.subCtx(cp => cp.owner)} create={false} />
                        </div>
                        <div className="col-sm-4">
                            <EntityLine ctx={sc.subCtx(cp => cp.entityType)} onChange={this.handleEntityTypeChange} />
                        </div>
                        {sc.value.entityType && <div className="col-sm-4">
                            <ValueLine ctx={sc.subCtx(f => f.embeddedInEntity)} />
                        </div>}
                    </div>
                </div>
                <ValueLine ctx={sc.subCtx(cp => cp.combineSimilarRows)} inlineCheckbox={true} />
                <EntityGridRepeater ctx={ctx.subCtx(cp => cp.parts)} getComponent={this.renderPart} onCreate={this.handleOnCreate} />
            </div>
        );
    }

    handleOnCreate = () => {
        const pr = DashboardEntity.memberInfo(a => a.parts![0].element.content);

        return SelectorModal.chooseType(getTypeInfos(pr.type))
            .then(ti => {
                if (ti == undefined)
                    return undefined;

                const part = New(ti.name) as any as IPartEntity;

                const icon = DashboardClient.defaultIcon(part);

                return PanelPartEmbedded.New({
                    content: part,
                    iconName: icon.iconName,
                    iconColor: icon.iconColor,
                    style: "Default"
                });
            });
    }

    renderPart = (tc: TypeContext<PanelPartEmbedded>) => {

        const tcs = tc.subCtx({ formGroupStyle: "Basic", formSize: "ExtraSmall", placeholderLabels: true });
        
        const title = (
            <div> 
                <div className="row">
                    <div className="col-sm-1">
                        <span className={tc.value.iconName || undefined} style={{ color: tc.value.iconColor || undefined, fontSize: "25px", marginTop: "17px" }} />
                    </div>
                    <div className="col-sm-11">
                        <ValueLine ctx={tcs.subCtx(pp => pp.title)} />
                    </div>
                </div>
                <div className="row">
                    <div className="col-sm-4">
                        <ValueLine ctx={tcs.subCtx(pp => pp.style)} onChange={() => this.forceUpdate()} />
                    </div>
                    <div className="col-sm-4">
                        <IconTypeaheadLine ctx={tcs.subCtx(t => t.iconName)} onChange={() => this.forceUpdate()} />
                    </div>
                    <div className="col-sm-4">
                        <ColorTypeaheadLine ctx={tcs.subCtx(t => t.iconColor)} onChange={() => this.forceUpdate()} />
                    </div>
                </div>

            </div>
        );

        return (
            <EntityGridItem title={title} bsStyle={tc.value.style}>
                <RenderEntity ctx={tc.subCtx(a => a.content)} />
            </EntityGridItem>
        );
    }
}