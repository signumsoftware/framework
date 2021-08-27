import { EntitySettings } from '@framework/Navigator'
import * as Constructor from '@framework/Constructor'
import * as Navigator from '@framework/Navigator'
import { TimeSpanEmbedded, DateSpanEmbedded } from './Signum.Entities.Basics'

export function start(options: { routes: JSX.Element[] }) {
  Navigator.addSettings(new EntitySettings(TimeSpanEmbedded, e => import('./Templates/TimeSpan')));
  Navigator.addSettings(new EntitySettings(DateSpanEmbedded, e => import('./Templates/DateSpan')));
  Constructor.registerConstructor(TimeSpanEmbedded, () => TimeSpanEmbedded.New({ days: 0, hours: 0, minutes: 0, seconds: 0 }));
  Constructor.registerConstructor(DateSpanEmbedded, () => DateSpanEmbedded.New({ years: 0, months: 0, days: 0 }));
}
