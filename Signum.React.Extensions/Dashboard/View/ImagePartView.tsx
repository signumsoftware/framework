import * as React from 'react';
import * as AppContext from '@framework/AppContext'
import { PanelPartContentProps } from '../DashboardClient';
import { ImagePartEntity } from '../Signum.Entities.Dashboard';


export default function ImagePart(p: PanelPartContentProps<ImagePartEntity>) {
  return (
    <div>
      <a href={p.part.clickActionURL ? AppContext.toAbsoluteUrl(p.part.clickActionURL!) : undefined}
        onClick={p.part.clickActionURL?.startsWith("~") ? (e => { e.preventDefault(); AppContext.history.push(p.part.clickActionURL!) }) : undefined}>
        <img src={p.part.imageSrcContent} style={{ width: "100%" }} />
      </a>
    </div>
  );
}
