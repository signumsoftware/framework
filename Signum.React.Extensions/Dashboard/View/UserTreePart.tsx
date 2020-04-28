
import * as React from 'react'
import { FindOptions } from '@framework/FindOptions'
import { getQueryNiceName } from '@framework/Reflection'
import { Entity, Lite, is, JavascriptMessage } from '@framework/Signum.Entities'
import { SearchControl, ValueSearchControl } from '@framework/Search'
import * as UserQueryClient from '../../UserQueries/UserQueryClient'
import {  PanelStyle, UserTreePartEntity } from '../Signum.Entities.Dashboard'
import { classes } from '@framework/Globals';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { parseIcon } from '../Admin/Dashboard';
import { useAPI } from '../../../../Framework/Signum.React/Scripts/Hooks'
import { PanelPartContentProps } from '../DashboardClient'
import { TreeViewer } from '../../Tree/TreeViewer'
import { TreeOperation } from '../../Tree/Signum.Entities.Tree'
import * as Operations from '@framework/Operations'
import * as Navigator from '@framework/Navigator'
import { getTypeInfo } from '@framework/Reflection'


export default function UserTreePart(p: PanelPartContentProps<UserTreePartEntity>) {

  const treeViewRef = React.useRef<TreeViewer>(null);
  const fo = useAPI(signal => UserQueryClient.Converter.toFindOptions(p.part.userQuery, p.entity), [p.part.userQuery, p.entity]);

  if (!fo)
    return <span>{JavascriptMessage.loading.niceToString()}</span>;

  const ti = getTypeInfo(p.part.userQuery.entityType?.toStr!);
  const filterOptions = fo.filterOptions!;

  return (
    <TreeViewer ref={treeViewRef}
      initialShowFilters={false}
      typeName={ti.name}
      allowMove={Operations.tryGetOperationInfo(TreeOperation.Move, ti.name) !== null}
      filterOptions={filterOptions}
      key={ti.name}
      //onSearch={() => changeUrl()}
    />
  );
}




