import { RouteObject } from 'react-router'
import { getColorProviders } from '../Signum.Map/Schema/ClientColorProvider';

export function start(options: { routes: RouteObject[] }) {  

  getColorProviders.push(smi => import("./DisconnectedColorProvider").then((c: any) => c.default(smi)));

}
