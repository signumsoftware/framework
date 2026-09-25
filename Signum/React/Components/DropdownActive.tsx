import { classes } from '../Globals';

/** Marks a dropdown item as the chosen one of a set of mutually exclusive choices - a theme, a culture, a
 * filter mode.
 *
 * Use this instead of react-bootstrap's own `active`, which emits `aria-selected` on an element whose role
 * is `button`, where that attribute is not allowed and so is ignored: the choice a reader has made was not
 * conveyed at all, and axe reports it as `aria-allowed-attr`. The call site cannot override it, because
 * `DropdownItem` renders `<Component {...props} {...dropdownItemProps} />` and the library's props come
 * last. Leaving `active` out is what drops the attribute - with no `eventKey` either, `useDropdownItem`
 * resolves it to undefined and React omits it - so the class and the state are stated here instead.
 *
 * `aria-current` rather than `aria-checked`: the latter needs `role="menuitemradio"`, which in turn needs a
 * `role="menu"` parent that these menus do not have and cannot easily be given, since some of them also
 * hold a checkbox and descriptive text. `aria-current` is global, so it is allowed whatever the role, and
 * `.dropdown-item.active` with `aria-current="true"` is Bootstrap's own documented markup for this.
 *
 *   <Dropdown.Item {...dropdownActive(mode == theme)} onClick={…}>
 */
export function dropdownActive(active: boolean | undefined, className?: string): {
  className: string | undefined,
  "aria-current": true | undefined,
} {
  return {
    className: classes(className, active ? "active" : undefined),
    "aria-current": active ? true : undefined,
  };
}
