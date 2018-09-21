import * as React from 'react'
import { getTypeInfo } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { TreeViewer } from './TreeViewer'
import { RouteComponentProps } from "react-router";
import { FilterOption } from "../../../Framework/Signum.React/Scripts/FindOptions";
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
                        <span className="fa fa-external-link"></span>
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



