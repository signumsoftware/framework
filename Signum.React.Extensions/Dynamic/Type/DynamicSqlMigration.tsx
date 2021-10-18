import * as React from 'react'
import { Dic, classes } from '@framework/Globals'
import * as Constructor from '@framework/Constructor'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import { DynamicSqlMigrationEntity, DynamicSqlMigrationMessage } from '../Signum.Entities.Dynamic'
import { ValueLine, EntityLine, TypeContext } from '@framework/Lines'
import { ModifiableEntity, Entity, Lite, JavascriptMessage } from '@framework/Signum.Entities'
import { getTypeInfo, Binding, PropertyRoute } from '@framework/Reflection'
import SqlCodeMirror from '../../Codemirror/SqlCodeMirror'


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
        <SqlCodeMirror script={ctx.value.script ?? ""} onChange={handleScriptChange} isReadOnly={executed} />
      </div>
    </div>
  );
}
