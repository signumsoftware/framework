import * as React from 'react';

/** Names the typeahead input that react-widgets' `DropdownList` renders whenever `filter` is set.
 *
 * The widget builds that input itself, and `DropdownListInput` destructures the props it knows and drops
 * the rest, so `inputProps` never reaches it - there is no way in from the call site. The input therefore
 * has no accessible name, which axe reports as `label`, and it is the element that actually takes focus:
 * the `role="combobox"` root above it carries `tabindex="-1"` when there is a filter. Unknown props do
 * reach that root, so this names the root the ordinary way, marks it, and writes the same name onto the
 * input underneath.
 *
 * A DOM write because the library leaves no other way in, but a stable one: React does not manage
 * `aria-label` on that element, `allowSearch` follows the `filter` prop rather than the popup opening, so
 * the input exists from the first render, and the effect runs after every render.
 *
 *   <DropdownList {...useDropdownListSearchLabel(label)} filter={…} … />
 */
export function useDropdownListSearchLabel(label: string | undefined): {
  "aria-label": string | undefined,
  "data-rw-search": string,
} {

  const marker = React.useId();

  React.useLayoutEffect(() => {
    if (!label)
      return;

    const root = document.querySelector(`[data-rw-search="${CSS.escape(marker)}"]`);
    root?.querySelector("input.rw-dropdownlist-search")?.setAttribute("aria-label", label);
  });

  return { "aria-label": label, "data-rw-search": marker };
}
