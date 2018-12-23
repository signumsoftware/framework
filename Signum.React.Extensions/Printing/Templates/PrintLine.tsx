import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { SearchControl } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import { PrintLineEntity } from '../Signum.Entities.Printing'
import { ProcessExceptionLineEntity } from '../../Processes/Signum.Entities.Processes'
import FileLine from '../../Files/FileLine'

export default class PrintLine extends React.Component<{ ctx: TypeContext<PrintLineEntity> }> {
  render() {
    const e = this.props.ctx.subCtx({ readOnly: true });

    return (
      <div>
        <ValueLine ctx={e.subCtx(f => f.creationDate)} />
        <EntityLine ctx={e.subCtx(f => f.referred)} />
        <FileLine ctx={e.subCtx(f => f.file)} fileType={e.value.testFileType || undefined} readOnly={this.props.ctx.value.state != "NewTest"} />
        <ValueLine ctx={e.subCtx(f => f.state)} />
        <ValueLine ctx={e.subCtx(f => f.printedOn)} />
        {!e.value.isNew &&
          <fieldset>
            <legend>{ProcessExceptionLineEntity.nicePluralName()}</legend>
            <SearchControl findOptions={{ queryName: ProcessExceptionLineEntity, parentToken: ProcessExceptionLineEntity.token(e => e.line), parentValue: e.value }} />
          </fieldset>
        }
      </div>
    );
  }
}

