import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { DynamicApiEntity, DynamicApiEval } from './Signum.Entities.Dynamic'
import * as Constructor from '@framework/Constructor'

export function start(options: { routes: JSX.Element[] }) {
  Navigator.addSettings(new EntitySettings(DynamicApiEntity, w => import('./Api/DynamicApi')));
  Constructor.registerConstructor(DynamicApiEntity, () => DynamicApiEntity.New({ eval: DynamicApiEval.New() }));
}
