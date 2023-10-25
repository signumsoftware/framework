import * as React from 'react'
import { AutoLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { FileLine } from '../../Signum.Files/Files'
import { ImageAttachmentEntity } from '../Signum.Mailing.Templates';

export default function ImageAttachment(p: { ctx: TypeContext<ImageAttachmentEntity> }) {
  const sc = p.ctx.subCtx({ formGroupStyle: "Basic" });

  return (
    <div className="row">
      <div className="col-sm-6">
        <AutoLine ctx={sc.subCtx(c => c.type)} />
        <AutoLine ctx={sc.subCtx(c => c.contentId)} />
      </div>
      <div className="col-sm-6">
        <FileLine ctx={sc.subCtx(c => c.file)} />
        <AutoLine ctx={sc.subCtx(c => c.fileName)} />
      </div>
    </div>
  );
}

