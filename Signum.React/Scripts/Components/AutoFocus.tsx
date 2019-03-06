import * as React from 'react';
import { BsSize, BsColor } from './Basic';
import { classes } from '../Globals';


export function AutoFocus(p: React.Props<any>) {
  var ref = React.useRef<HTMLDivElement | null>(null);
  React.useEffect(() => {
    var input = Array.from(ref.current!.querySelectorAll("button, [href], input, select, textarea"))
      .firstOrNull(e => { var tabIndex = e.getAttribute("tabindex"); return tabIndex == null || tabIndex >= "0"; });

    if (input)
      (input as HTMLInputElement).focus();
  }, []);

  return (
    <div ref={ref}>
      {p.children}
    </div>
  );
}
