import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { FileLine } from '../../Signum.Files/Files'
import { ImageAttachmentEntity } from '../Signum.Mailing.Templates';

export default function ImageAttachment(p: { ctx: TypeContext<ImageAttachmentEntity> }) {
  const sc = p.ctx.subCtx({ formGroupStyle: "Basic" });

  return (
    <div className="row">
      <div className="col-sm-6">
        <ValueLine ctx={sc.subCtx(c => c.type)} />
        <ValueLine ctx={sc.subCtx(c => c.contentId)} />
      </div>
      <div className="col-sm-6">
        <FileLine ctx={sc.subCtx(c => c.file)} />
        <ValueLine ctx={sc.subCtx(c => c.fileName)} />
      </div>
    </div>
  );
}

