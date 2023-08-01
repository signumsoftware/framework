import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { FileLine } from '../../Signum.Files/Files'
import { EmailTemplateEntity, FileTokenAttachmentEntity } from '../Signum.Mailing.Templates';
import QueryTokenEmbeddedBuilder from '../../Signum.UserAssets/Templates/QueryTokenEmbeddedBuilder';
import { SubTokensOptions } from '@framework/FindOptions';

export default function FileTokenAttachment(p: { ctx: TypeContext<FileTokenAttachmentEntity> }) {
  const sc = p.ctx.subCtx({ formGroupStyle: "Basic" });
  var et = p.ctx.findParent(EmailTemplateEntity);

  return (
    <div className="row">
      <div className="col-sm-6">
        <ValueLine ctx={sc.subCtx(c => c.type)} />
        <ValueLine ctx={sc.subCtx(c => c.contentId)} />
      </div>
      <div className="col-sm-6">
        <QueryTokenEmbeddedBuilder
          ctx={sc.subCtx(a => a.fileToken)}
          queryKey={et.query.key}
          subTokenOptions={SubTokensOptions.CanElement}
          helpText="Expression pointing to an File" />
        <ValueLine ctx={sc.subCtx(c => c.fileName)} />
      </div>
    </div>
  );
}

