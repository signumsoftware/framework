import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SubTokensOptions, QueryToken, QueryTokenType, hasAnyOrAll } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { getToString, getMixin, Lite } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { MoveTreeModel, TreeEntity } from '../Signum.Entities.Tree'
import * as TreeClient from '../TreeClient'
import { TypeReference } from "../../../../Framework/Signum.React/Scripts/Reflection";
import { is } from "../../../../Framework/Signum.React/Scripts/Signum.Entities";

export interface MoveTreeModelComponentProps {
    ctx: TypeContext<MoveTreeModel>;
    lite: Lite<TreeEntity>;
}

export default class MoveTreeModelComponent extends React.Component<MoveTreeModelComponentProps> {

    render() {
        const ctx = this.props.ctx;
        const typeName = this.props.lite.EntityType;
        const type = { name: typeName, isLite: true } as TypeReference; 
        return (
            <div>
                <EntityLine ctx={ctx.subCtx(a => a.newParent)} type={type} onChange={() => this.forceUpdate()}
                    findOptions={{
                        queryName: typeName,
                        filterOptions: [{ columnName: "Entity", operation: "DistinctTo", value: this.props.lite, frozen: true }]
                    }}
                    onFind={() => TreeClient.openTree(typeName, [{ columnName: "Entity", operation: "DistinctTo", value: this.props.lite, frozen: true }])} />

                <ValueLine ctx={ctx.subCtx(a => a.insertPlace)} onChange={() => this.forceUpdate()} />

                {(ctx.value.insertPlace == "Before" || ctx.value.insertPlace == "After") &&
                    <EntityLine ctx={ctx.subCtx(a => a.sibling)} type={type}
                        findOptions={{ queryName: typeName, parentColumn: "Entity.Parent", parentValue: ctx.value.newParent }}
                        onFind={() => TreeClient.openTree(typeName, [{ columnName: "Entity.Parent", value: ctx.value.newParent }])} />}
            </div>
        );
    }

    
}
                