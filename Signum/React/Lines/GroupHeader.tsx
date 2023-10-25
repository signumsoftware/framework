import * as React from 'react';
import { classes } from '../Globals';

export type HeaderType = "h1" | "h2" | "h3" | "h4" | "h5" | "h6" | "display-1" | "display-2" | "display-3" | "display-4" | "display-5" | "display-6" | "display-7";   
export function Title(p: { children: React.ReactNode, type: HeaderType }) {

  var ElementType = p.type.contains("display-") ? ("h" + p.type.after("display-")) as "h1" :
    p.type as "h1";

  const className = p.type.contains("display-") ? p.type : undefined;

  return <ElementType className={className}>{p.children}</ElementType>;
}

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

    return (
      <div className={p.className} {...p.htmlAttributes}>
        {p.avoidFieldSet != true && <Title type={p.avoidFieldSet}>{p.label}{p.labelIcon} {p.buttons}</Title>}
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
