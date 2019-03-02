import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { DynamicApiEntity } from './Signum.Entities.Dynamic'

export function start(options: { routes: JSX.Element[] }) {
  Navigator.addSettings(new EntitySettings(DynamicApiEntity, w => import('./Api/DynamicApi')));
}
