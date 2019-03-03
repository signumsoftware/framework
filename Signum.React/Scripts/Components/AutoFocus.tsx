import * as React from 'react';
import { BsSize, BsColor } from './Basic';
import { classes } from '../Globals';


export function AutoFocus(p: React.Props<any>) {
  var ref = React.useRef<HTMLDivElement | null>(null);
  React.useEffect(() => {
    debugger;
    var input = ref.current!.querySelector("button, [href], input, select, textarea, [tabindex]:not([tabindex='-1'])");
    if (input)
      (input as HTMLInputElement).focus();
  }, []);

  return (
    <div ref={ref}>
      {p.children}
    </div>
  );
}
