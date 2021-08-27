import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { JoyrideEntity, JoyrideStepEntity, JoyrideStepStyleEntity } from './Signum.Entities.Joyride'
import * as UserAssetClient from '../UserAssets/UserAssetClient'

export function start(options: { routes: JSX.Element[] }) {

  Navigator.addSettings(new EntitySettings(JoyrideEntity, a => import('./Templates/Joyride')));
  Navigator.addSettings(new EntitySettings(JoyrideStepEntity, a => import('./Templates/JoyrideStep')));
  Navigator.addSettings(new EntitySettings(JoyrideStepStyleEntity, a => import('./Templates/JoyrideStepStyle')));

  UserAssetClient.registerExportAssertLink(JoyrideEntity);
  UserAssetClient.registerExportAssertLink(JoyrideStepEntity);
  UserAssetClient.registerExportAssertLink(JoyrideStepStyleEntity);
}

