import * as React from 'react'
import { RouteObject } from 'react-router'
import * as Navigator from '@framework/Navigator'
import * as Constructor from '@framework/Constructor'
import * as Finder from '@framework/Finder'
import { EntitySettings } from '@framework/Navigator'
import { EmailMessageEntity } from '../Signum.Mailing/Signum.Mailing'
import { EntityLine } from '@framework/Lines'
import { EmailMessagePackageMixin, EmailPackageEntity, SendEmailTaskEntity } from './Signum.Mailing.Package'

export function start(options: {
  routes: RouteObject[],
  sendEmailTask: boolean,
}) {

  Navigator.addSettings(new EntitySettings(EmailPackageEntity, e => import('./Templates/EmailPackage')));

  if (options.sendEmailTask) {
    Navigator.addSettings(new EntitySettings(SendEmailTaskEntity, e => import('./Templates/SendEmailTask')));
  }

  Navigator.getSettings(EmailMessageEntity)!.overrideView((rep) => {
    rep.insertAfterLine(e => e.template, ctx => [
      <EntityLine ctx={ctx.subCtx(EmailMessagePackageMixin).subCtx(e => e.package)} hideIfNull />
    ]);
  });
}
