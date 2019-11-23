
import * as React from 'react'
import * as Navigator from '@framework/Navigator'
import { LinkListPartEntity, LinkElementEmbedded } from '../Signum.Entities.Dashboard'
import { PanelPartContentProps } from '../DashboardClient';

export default function LinkListPart(p: PanelPartContentProps<LinkListPartEntity >){
  return (
    <ul className="sf-cp-link-list">
      {
        p.part.links.map(mle => mle.element)
          .map((le, i) =>
            <li key={i} >
              <a href={Navigator.toAbsoluteUrl(le.link!)}
                onClick={le.link!.startsWith("~") ? (e => { e.preventDefault(); Navigator.history.push(le.link!) }) : undefined}
                title={le.label!}>
                {le.label}
              </a>
            </li>)
      }
    </ul>
  );
}
