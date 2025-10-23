import * as React from "react";

export function LinkButton(p: { title: string | undefined } & React.AnchorHTMLAttributes<HTMLAnchorElement>): React.ReactElement {

  return (
   <a role="button" aria-label={p.title} {...p}
      onClick={e => { e.preventDefault(); p.onClick?.(e) }}>
      {p.children}
    </a>
  );
}
