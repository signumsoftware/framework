import * as React from 'react'
import { AutoLine, EntityLine } from '@framework/Lines'
import { SearchControl } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import { PrintLineEntity } from '../Signum.Printing'
import { ProcessExceptionLineEntity } from '../../Signum.Processes/Signum.Processes'
import { FileLine } from '../../Signum.Files/Components/FileLine'

export default function PrintLine(p : { ctx: TypeContext<PrintLineEntity> }): React.JSX.Element {
  const e = p.ctx.subCtx({ readOnly: true });

  return (
    <div>
      <AutoLine ctx={e.subCtx(f => f.creationDate)} />
      <EntityLine ctx={e.subCtx(f => f.referred)} />
      <FileLine ctx={e.subCtx(f => f.file)} fileType={e.value.testFileType ?? undefined} readOnly={p.ctx.value.state != "NewTest"} />
      <AutoLine ctx={e.subCtx(f => f.state)} />
      <AutoLine ctx={e.subCtx(f => f.printedOn)} />
      {!e.value.isNew &&
        <fieldset>
          <legend>{ProcessExceptionLineEntity.nicePluralName()}</legend>
          <SearchControl findOptions={{ queryName: ProcessExceptionLineEntity, filterOptions: [{ token: ProcessExceptionLineEntity.token(e => e.line), value: e.value }]}} />
        </fieldset>
      }
    </div>
  );
}

