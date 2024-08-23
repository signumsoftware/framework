
import * as React from 'react'
import { Finder } from '@framework/Finder'
import { getTypeInfos, tryGetOperationInfo } from '@framework/Reflection'
import { JavascriptMessage, Lite } from '@framework/Signum.Entities'
import { UserQueryClient } from '../../../Signum.UserQueries/UserQueryClient'
import { useAPI } from '@framework/Hooks'
import { DashboardClient, PanelPartContentProps } from '../../../Signum.Dashboard/DashboardClient'
import { TreeViewer } from '../../TreeViewer'
import { TreeEntity, TreeOperation, UserTreePartEntity } from '../../Signum.Tree'
import { Operations } from '@framework/Operations'
import { TreeOptions } from '../../TreeClient'


export default function UserTreePart(p: PanelPartContentProps<UserTreePartEntity>): React.JSX.Element {

  const treeViewRef = React.useRef<TreeViewer>(null);
  const fo = useAPI(signal => UserQueryClient.Converter.toFindOptions(p.content.userQuery, p.entity), [p.content.userQuery, p.entity, ...p.deps ?? []]);
  const qd = useAPI(() => Finder.getQueryDescription(p.content.userQuery.query.key), [p.content.userQuery.query.key]);


  if (!fo || !qd)
    return <span>{JavascriptMessage.loading.niceToString()}</span>;

  const ti = getTypeInfos(qd.columns["Entity"].type).single();
  const to = {
    typeName: ti.name,
    filterOptions: fo.filterOptions,
    columnOptions: fo.columnOptions,
    columnOptionsMode: fo.columnOptionsMode,
  } as TreeOptions;

  return (
    <TreeViewer ref={treeViewRef}
      treeOptions={to}
      defaultSelectedLite={p.entity as Lite<TreeEntity>}
      initialShowFilters={false}
      allowMove={tryGetOperationInfo(TreeOperation.Move, ti) !== null}
      showExpandCollapseButtons={true}
      key={ti.name}
    />
  );
}




