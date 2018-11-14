
import * as React from 'react'
import * as Navigator from '@framework/Navigator'
import { LinkListPartEntity, LinkElementEmbedded } from '../Signum.Entities.Dashboard'

export default class LinkListPart extends React.Component<{ part: LinkListPartEntity }> {
  render() {
    const entity = this.props.part;

    return (
      <ul className="sf-cp-link-list">
        {
          entity.links!.map(mle => mle.element)
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
}
