import * as React from 'react'
import { Dic, classes } from '@framework/Globals'
import * as Constructor from '@framework/Constructor'
import { Finder } from '@framework/Finder'
import { Navigator } from '@framework/Navigator'
import { AutoLine, EntityLine, TypeContext } from '@framework/Lines'
import { ModifiableEntity, Entity, Lite, JavascriptMessage } from '@framework/Signum.Entities'
import { getTypeInfo, Binding, PropertyRoute } from '@framework/Reflection'
import SqlCodeMirror from '../../Signum.CodeMirror/SqlCodeMirror'
import { DynamicSqlMigrationEntity } from '../Signum.Dynamic.SqlMigrations'


interface DynamicSqlMigrationComponentProps {
  ctx: TypeContext<DynamicSqlMigrationEntity>;
}

export default function DynamicSqlMigrationComponent(p : DynamicSqlMigrationComponentProps){
  function handleScriptChange(newScript: string) {
    const ctxValue = p.ctx.value;
    ctxValue.script = newScript;
    ctxValue.modified = true;
  }

  const ctx = p.ctx;
  const ctx4 = ctx.subCtx({ labelColumns: { sm: 4 } });
  const executed = ctx.value.executedBy != null;

  return (
    <div>
      <div className="row">
        <div className="col-sm-6">
          <AutoLine ctx={ctx4.subCtx(sm => sm.creationDate)} readOnly={true} />
          <AutoLine ctx={ctx4.subCtx(sm => sm.executionDate)} readOnly={true} />
        </div>

        <div className="col-sm-6">
          <EntityLine ctx={ctx4.subCtx(sm => sm.createdBy)} readOnly={true} />
          <EntityLine ctx={ctx4.subCtx(sm => sm.executedBy)} readOnly={true} />
        </div>
      </div>

      <AutoLine ctx={ctx.subCtx(sm => sm.comment)} readOnly={executed} />
      <div className="code-container">
        <SqlCodeMirror script={ctx.value.script ?? ""} onChange={handleScriptChange} isReadOnly={executed} />
      </div>
    </div>
  );
}
