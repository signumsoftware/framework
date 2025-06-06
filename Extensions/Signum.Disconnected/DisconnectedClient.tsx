import { RouteObject } from 'react-router'
import { getColorProviders } from '../Signum.Map/Schema/ClientColorProvider';

export namespace DisconnectedClient {
  
  export function start(options: { routes: RouteObject[] }): void {  
  
    getColorProviders.push(smi => import("./DisconnectedColorProvider").then((c: any) => c.default(smi)));
  
  }
}
