import * as React from 'react';
import * as AppContext from '@framework/AppContext'
import { DashboardClient, PanelPartContentProps } from '../DashboardClient';
import { SeparatorPartEntity } from '../Signum.Dashboard';


export default function SeparatorPart(p: PanelPartContentProps<SeparatorPartEntity>): React.JSX.Element {
  return (
    <div>
      <h1>{p.content.title}</h1>
    </div>
  );
}
