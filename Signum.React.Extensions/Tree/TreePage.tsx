import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
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

export default class TreePage extends React.Component<TreePageProps, TreePageState> {

    constructor(props: TreePageProps) {
        super(props);
        this.state = this.calculateState(props);
    }

    componentWillReceiveProps(nextProps: TreePageProps) {
        this.setState(this.calculateState(nextProps));
    }

    calculateState(props: TreePageProps): TreePageState {
        var query = QueryString.parse(props.location.search);
        return {
            filterOptions: Finder.Decoder.decodeFilters(query)
        };
    }

    changeUrl() {

        var newPath = this.treeView!.getCurrentUrl();
        
        var currentLocation = Navigator.history.location;

        if (currentLocation.pathname + currentLocation.search != newPath)
            Navigator.history.replace(newPath);
    }

    treeView?: TreeViewer;
    render() {

        var ti = getTypeInfo(this.props.match.params.typeName);

        return (
            <div id="divSearchPage">
                <h2>
                    <span className="sf-entity-title">{ti.nicePluralName}</span>
                    &nbsp;
                    <a className="sf-popup-fullscreen" href="#" onClick={(e) => this.treeView!.handleFullScreenClick(e)}>
                        <FontAwesomeIcon icon="external-link-alt" />
                    </a>
                </h2>
                <TreeViewer ref={tv => this.treeView = tv!}
                    initialShowFilters={true}
                    typeName={ti.name}
                    allowMove={Operations.isOperationAllowed(TreeOperation.Move, ti.name)}
                    filterOptions={this.state.filterOptions}
                    key={ti.name}
                    onSearch={() => this.changeUrl()} />
            </div>
        );
    }
}



