import * as React from 'react'
import { PseudoType } from './Reflection'
import { Entity, Lite } from './Signum.Entities'
import { Symbol } from './Signum.Basics'

/**
 * Framework-level extension point for tour buttons.
 *
 * Modules can place a `<TourButton trigger={...} />` next to any page/section and declare a
 * tour trigger, without taking a dependency on the optional Signum.Tour extension.
 *
 * Signum.Tour registers the actual implementation in `TourClient.start` (see TourClient.tsx).
 * If the application does not start Signum.Tour, the button simply renders nothing.
 */
export const TourButtonOptions = {
  renderer: null as ((trigger: PseudoType | Symbol | Lite<Entity>) => React.ReactNode) | null
};

export function TourButton(p: { trigger: PseudoType | Symbol | Lite<Entity> }): React.ReactNode {
  return TourButtonOptions.renderer ? TourButtonOptions.renderer(p.trigger) : null;
}
