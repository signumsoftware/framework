import * as React from 'react'
import { Tab, Tabs } from 'react-bootstrap'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SubTokensOptions, QueryToken, QueryTokenType, hasAnyOrAll } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { getToString, getMixin } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { MoveTreeModel } from '../Signum.Entities.Tree'
import * as TreeClient from '../TreeClient'
import { TypeReference } from "../../../../Framework/Signum.React/Scripts/Reflection";

export default class MoveTreeModelComponent extends React.Component<{ ctx: TypeContext<MoveTreeModel>, typeName: string }, void> {

    render() {
        const ctx = this.props.ctx;
        const type = { name: this.props.typeName, isLite: true } as TypeReference; 
        return (
            <div>
                <EntityLine ctx={ctx.subCtx(a => a.newParent)} type={type} onChange={() => this.forceUpdate()} onFind={() => TreeClient.openTree(this.props.typeName)} />
                <ValueLine ctx={ctx.subCtx(a => a.insertPlace)} onChange={() => this.forceUpdate()} />
                {(ctx.value.insertPlace == "Before" || ctx.value.insertPlace == "After") &&
                    <EntityLine ctx={ctx.subCtx(a => a.sibling)} type={type}
                        findOptions={{ queryName: this.props.typeName, parentColumn: "Entity.Parent", parentValue: ctx.value.newParent }}
                        onFind={() => TreeClient.openTree(this.props.typeName, [{ columnName: "Entity.Parent", value: ctx.value.newParent }])} />}
            </div>
        );
    }

    
}
                