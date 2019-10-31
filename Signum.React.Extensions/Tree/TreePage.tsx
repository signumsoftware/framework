import * as React from 'react'
import { getTypeInfo } from '@framework/Reflection'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import * as Operations from '@framework/Operations'
import { TreeViewer } from './TreeViewer'
import { RouteComponentProps } from "react-router";
import { FilterOption } from "@framework/FindOptions";
import * as QueryString from 'query-string'
import { TreeOperation } from "./Signum.Entities.Tree";

interface TreePageProps extends RouteComponentProps<{ typeName: string }> {

}

interface TreePageState {
  filterOptions: FilterOption[];
}

export default function TreePage(p : TreePageProps, TreePageState){
  function constructor(props: TreePageProps) {
    super(props);
    state = calculateState(props);
  }

  function componentWillReceiveProps(nextProps: TreePageProps) {
    setState(calculateState(nextProps));
  }

  calculateState(props: TreePageProps): TreePageState {
    var query = QueryString.parse(props.location.search);
    return {
      filterOptions: Finder.Decoder.decodeFilters(query)
    };
  }

  function changeUrl() {
    var newPath = treeView!.getCurrentUrl();

    var currentLocation = Navigator.history.location;

    if (currentLocation.pathname + currentLocation.search != newPath)
      Navigator.history.replace(newPath);
  }

  treeView?: TreeViewer;
  var ti = getTypeInfo(p.match.params.typeName);

  return (
    <div id="divSearchPage">
      <h2>
        <span className="sf-entity-title">{ti.nicePluralName}</span>
        &nbsp;
                  <a className="sf-popup-fullscreen" href="#" onClick={(e) => treeView!.handleFullScreenClick(e)}>
          <span className="fa fa-external-link"></span>
        </a>
      </h2>
      <TreeViewer ref={tv => treeView = tv!}
        initialShowFilters={true}
        typeName={ti.name}
        allowMove={Operations.isOperationAllowed(TreeOperation.Move, ti.name)}
        filterOptions={filterOptions}
        key={ti.name}
        onSearch={() => changeUrl()} />
    </div>
  );
}



