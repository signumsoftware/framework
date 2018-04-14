import * as React from 'react'
import { Dic, classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Constructor from '../../../../Framework/Signum.React/Scripts/Constructor'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { DynamicSqlMigrationEntity, DynamicSqlMigrationMessage } from '../Signum.Entities.Dynamic'
import { ValueLine, EntityLine, TypeContext } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ModifiableEntity, Entity, Lite, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { getTypeInfo, Binding, PropertyRoute } from '../../../../Framework/Signum.React/Scripts/Reflection'
import SqlCodeMirror from '../../Codemirror/SqlCodeMirror'


interface DynamicSqlMigrationComponentProps {
    ctx: TypeContext<DynamicSqlMigrationEntity>;
}

export default class DynamicSqlMigrationComponent extends React.Component<DynamicSqlMigrationComponentProps> {

    handleScriptChange = (newScript: string) => {

        const ctxValue = this.props.ctx.value;
        ctxValue.script = newScript;
        ctxValue.modified = true;
    }

    render() {

        const ctx = this.props.ctx;
        const ctx4 = ctx.subCtx({ labelColumns: { sm: 4 } });
        const executed = ctx.value.executedBy != null;

        return (
            <div>
                <div className="row">
                    <div className="col-sm-6">
                        <ValueLine ctx={ctx4.subCtx(sm => sm.creationDate)} readOnly={true} />
                        <ValueLine ctx={ctx4.subCtx(sm => sm.executionDate)} readOnly={true} />
                    </div>

                    <div className="col-sm-6">
                        <EntityLine ctx={ctx4.subCtx(sm => sm.createdBy)} readOnly={true} />
                        <EntityLine ctx={ctx4.subCtx(sm => sm.executedBy)} readOnly={true} />
                    </div>
                </div>

                <ValueLine ctx={ctx.subCtx(sm => sm.comment)} readOnly={executed} />
                <div className="code-container">
                    <SqlCodeMirror script={ctx.value.script || ""} onChange={this.handleScriptChange} isReadOnly={executed} />
                </div>
            </div>
        );
    }
}
