import * as React from 'react';
import { classes } from '../Globals';

export type HeaderType = "h1" | "h2" | "h3" | "h4" | "h5" | "h6" | "display-1" | "display-2" | "display-3" | "display-4" | "display-5" | "display-6" | "display-7";   

export function GroupHeader(p: {
  label: React.ReactNode;
  labelIcon: React.ReactNode;
  buttons: React.ReactNode;
  avoidFieldSet?: boolean | HeaderType;
  children: React.ReactNode;
  className?: string
  htmlAttributes?: React.HTMLAttributes<HTMLDivElement>
}) {

  if (p.avoidFieldSet) {

    var HeaderType = typeof p.avoidFieldSet == "boolean" ? undefined :
      p.avoidFieldSet.contains("display-") ? ("h" + p.avoidFieldSet.after("display-")) as "h1" :
        p.avoidFieldSet as "h1";


    const className = typeof p.avoidFieldSet == "boolean" ? undefined :
      p.avoidFieldSet.contains("display-") ? p.avoidFieldSet : undefined;

    return (
      <div className={p.className} {...p.htmlAttributes}>
        {HeaderType && <HeaderType className={className}>{p.label}{p.labelIcon} {p.buttons}</HeaderType>}
        {p.children}
      </div>
    );
  }

  return (
    <fieldset >
      <legend>
        <div>
          <span>{p.label}{p.labelIcon}</span>
          {p.buttons}
        </div>
      </legend>
      {p.children}
    </fieldset>
  );
}
