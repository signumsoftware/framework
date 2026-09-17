import * as React from 'react';
import { StyleContext } from '../Lines';
import { classes } from '../Globals';

export type HeaderType = "h1" | "h2" | "h3" | "h4" | "h5" | "h6" | "display-1" | "display-2" | "display-3" | "display-4" | "display-5" | "display-6" | "display-7" | "lead" | "label";

/** The level the next section heading should use. The frames render the page or dialog title as the <h1>,
 * so the sections below it start at 2, and every group that renders a heading moves its own children one
 * level further down. */
export const HeadingLevelContext: React.Context<number> = React.createContext<number>(2);

export function NextHeadingLevel(p: { children: React.ReactNode }): React.ReactElement {
  const level = React.useContext(HeadingLevelContext);
  return <HeadingLevelContext.Provider value={Math.min(level + 1, 6)}>{p.children}</HeadingLevelContext.Provider>;
}

export function Title(p: { children: React.ReactNode, type: HeaderType, ctx?: StyleContext, id?: string }): React.ReactElement {

  //For groups that behave like a single field (a checkbox list, a set of radios) instead of a section.
  if (p.type == "label")
    return <label className={p.ctx?.labelClass} id={p.id}>{p.children}</label>;

  if (p.type == "lead")
    return <p className={classes("mt-3", "lead")} id={p.id}>{p.children}</p>;

  // HeaderType used to pick the element, so a form whose title is an <h1> jumped straight to the <h5> its
  // sections wanted to look like, and a screen reader reading the heading list saw four levels missing
  // (WCAG 1.3.1). The level now comes from how deep the section actually is and the requested type becomes
  // the Bootstrap size class, which is what keeps the size identical: Bootstrap's _type.scss declares
  // .hN as @extend hN, so a stylesheet rule written for the element already applies to the class.
  const level = React.useContext(HeadingLevelContext);
  const ElementType = ("h" + level) as "h1";

  return <ElementType className={classes("mt-3", p.type)} id={p.id}>{p.children}</ElementType>;
}

export function GroupHeader(p: {
  label?: React.ReactNode;
  labelIcon?: React.ReactNode;
  buttons?: React.ReactNode;
  avoidFieldSet?: boolean | HeaderType;
  children: React.ReactNode;
  className?: string;
  htmlAttributes?: React.HTMLAttributes<HTMLDivElement>;
  fieldsetClassName?: string
  fieldsetHtmlAttributes?: React.HTMLAttributes<HTMLFieldSetElement>
  /** Only used by the "label" HeaderType, to get the labelClass of the form size. */
  ctx?: StyleContext;
  /** Pass "group" when the children are a set of form controls that belong together - a checkbox list, a
   * set of radios. The fieldset branch gets this from <fieldset> and <legend> for free, but avoidFieldSet
   * renders a plain div, and then nothing tied the controls to the label a sighted user reads above them
   * (WCAG 1.3.1). Left unset for a section that merely has a heading, so an ordinary group of lines is not
   * announced as something it is not. */
  role?: "group";
  /** Applied beside the role - aria-required, aria-invalid and aria-describedby belong to the set as a
   * whole rather than to each option. */
  ariaAttributes?: React.AriaAttributes;
}): React.ReactElement {

  const titleId = React.useId();

  if (p.avoidFieldSet) {

    // Only a real heading opens a level for what follows it: "label" renders a <label> and the fieldset
    // branch below renders a <legend>, and neither is a heading.
    const rendersHeading = p.avoidFieldSet != true && p.avoidFieldSet != "label" && p.avoidFieldSet != "lead";
    const hasTitle = p.avoidFieldSet != true;

    return (
      <div className={p.className} {...p.htmlAttributes}
        role={p.role}
        {...(p.role ? p.ariaAttributes : undefined)}
        aria-labelledby={p.role && hasTitle ? titleId : undefined}>
        {hasTitle && <Title type={p.avoidFieldSet as HeaderType} ctx={p.ctx} id={titleId}>{p.label}{p.labelIcon} {p.buttons}</Title>}
        {rendersHeading ? <NextHeadingLevel>{p.children}</NextHeadingLevel> : p.children}
      </div>
    );
  }

  return (
    <fieldset className={p.fieldsetClassName} {...p.fieldsetHtmlAttributes}>
      {(p.label || p.labelIcon || p.buttons) && < legend >
        {/* A span, not a div: a legend takes phrasing content only. d-block keeps the full-width box that
            EntityAccordion's float-end buttons need. */}
        <span className="d-block">
          <span>{p.label}{p.labelIcon}</span>
          {p.buttons}
        </span>
      </legend>
      }
      <div className={p.className} {...p.htmlAttributes}>
      {p.children}
      </div>
    </fieldset>
  );
}
