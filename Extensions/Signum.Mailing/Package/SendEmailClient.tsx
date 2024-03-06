import * as React from 'react'
import { RouteObject } from 'react-router'
import { Navigator, EntitySettings } from '@framework/Navigator'
import * as Constructor from '@framework/Constructor'
import * as Finder from '@framework/Finder'
import { EntityLine } from '@framework/Lines'
import { EmailMessagePackageMixin, EmailPackageEntity, SendEmailTaskEntity } from './Signum.Mailing.Package'
import { EmailMessageEntity } from '../Signum.Mailing'

export function start(options: { routes: RouteObject[] }) {

  Navigator.addSettings(new EntitySettings(SendEmailTaskEntity, e => import('./SendEmailTask')));
}
