import * as React from 'react'
import { classes } from '@framework/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater } from '@framework/Lines'
import { SubTokensOptions, QueryToken, QueryTokenType, hasAnyOrAll } from '@framework/FindOptions'
import { SearchControl } from '@framework/Search'
import { getToString, getMixin, Lite } from '@framework/Signum.Entities'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import { MoveTreeModel, TreeEntity } from '../Signum.Entities.Tree'
import * as Finder from "@framework/Finder";
import * as TreeClient from '../TreeClient'
import { TypeReference } from "@framework/Reflection";
import { is } from "@framework/Signum.Entities";

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
                <EntityLine ctx={ctx.subCtx(a => a.newParent)} type={type} onChange={this.handleNewParentChange}
                    findOptions={{
                        queryName: typeName,
                        filterOptions: [{ token: "Entity", operation: "DistinctTo", value: this.props.lite, frozen: true }]
                    }}
                    onFind={() => TreeClient.openTree(typeName, [{ token: "Entity", operation: "DistinctTo", value: this.props.lite, frozen: true }])} />

                <ValueLine ctx={ctx.subCtx(a => a.insertPlace)} onChange={() => this.forceUpdate()} />

                {(ctx.value.insertPlace == "Before" || ctx.value.insertPlace == "After") &&
                    <EntityLine ctx={ctx.subCtx(a => a.sibling)} type={type}
                        findOptions={{ queryName: typeName, parentToken: "Entity.Parent", parentValue: ctx.value.newParent }}
                        onFind={() => Finder.find({
                            queryName: typeName,
                            filterOptions: [{ token: "Entity.Parent", value: ctx.value.newParent, frozen: true }]
                        }, { useDefaultBehaviour: true, searchControlProps: { create: false } })}
                    />}
            </div>
        );
    }

    handleNewParentChange = () => {
        var ctx = this.props.ctx;
        ctx.value.sibling = null;
        ctx.value.modified = true;
        this.forceUpdate();
    }
}
                