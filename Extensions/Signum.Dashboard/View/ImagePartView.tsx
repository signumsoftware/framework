import * as React from 'react';
import * as AppContext from '@framework/AppContext'
import { DashboardClient, PanelPartContentProps } from '../DashboardClient';
import { ImagePartEntity } from '../Signum.Dashboard';


export default function ImagePart(p: PanelPartContentProps<ImagePartEntity>): React.JSX.Element {
  return (
    <div>
      <a href={p.content.clickActionURL ? AppContext.toAbsoluteUrl(p.content.clickActionURL!) : undefined}
        onClick={p.content.clickActionURL?.startsWith("~") ? (e => { e.preventDefault(); AppContext.navigate(p.content.clickActionURL!) }) : undefined}>
        <img src={p.content.imageSrcContent} style={{ width: "100%" }} alt={p.content.altText ?? "Image part"} />
      </a>
    </div>
  );
}
