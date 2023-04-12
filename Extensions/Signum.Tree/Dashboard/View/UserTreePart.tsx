
import * as React from 'react'
import * as Finder from '@framework/Finder'
import { getTypeInfos } from '@framework/Reflection'
import { JavascriptMessage } from '@framework/Signum.Entities'
import * as UserQueryClient from '../../../Signum.UserQueries/UserQueryClient'
import { useAPI } from '@framework/Hooks'
import { PanelPartContentProps } from '../../../Signum.Dashboard/DashboardClient'
import { TreeViewer } from '../../TreeViewer'
import { TreeOperation, UserTreePartEntity } from '../../Signum.Tree'
import * as Operations from '@framework/Operations'


export default function UserTreePart(p: PanelPartContentProps<UserTreePartEntity>) {

  const treeViewRef = React.useRef<TreeViewer>(null);
  const fo = useAPI(signal => UserQueryClient.Converter.toFindOptions(p.content.userQuery, p.entity), [p.content.userQuery, p.entity, ...p.deps ?? []]);
  const qd = useAPI(() => Finder.getQueryDescription(p.content.userQuery.query.key), [p.content.userQuery.query.key]);


  if (!fo || !qd)
    return <span>{JavascriptMessage.loading.niceToString()}</span>;

  const ti = getTypeInfos(qd.columns["Entity"].type).single();

  return (
    <TreeViewer ref={treeViewRef}
      initialShowFilters={false}
      typeName={ti.name}
      allowMove={Operations.tryGetOperationInfo(TreeOperation.Move, ti) !== null}
      filterOptions={fo.filterOptions}
      key={ti.name}
    />
  );
}




