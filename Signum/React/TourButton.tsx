import * as React from 'react'
import { PseudoType } from './Reflection'
import { Entity, Lite } from './Signum.Entities'
import { Symbol, TourTriggerSymbol } from './Signum.Basics'


export function TourButton(p: { trigger: PseudoType | Symbol | Lite<Entity>; className?: string }): React.ReactNode {

  if (!TourButtonOptions.renderer)
    return null;

  if (TourTriggerSymbol.isInstance(p.trigger) && p.trigger.id == null)
    return null;

  return  TourButtonOptions.renderer(p.trigger, p.className);
}

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
  renderer: null as ((trigger: PseudoType | Symbol | Lite<Entity>, className?: string) => React.ReactNode) | null
};
