import * as React from 'react'
import { getTypeInfo } from '../../../Framework/Signum.React/Scripts/Reflection'
import { TreeViewer } from './TreeViewer'
import { RouteComponentProps } from "react-router";


interface TreePageProps extends RouteComponentProps<{ typeName: string }> {

}

export default class TreePage extends React.Component<TreePageProps, { }> {

    constructor(props: any) {
        super(props);
        this.state = {};
    }
    
    render() {

        var ti = getTypeInfo(this.props.match.params.typeName);

        return (
            <div id="divSearchPage">
                <h2>
                    <span className="sf-entity-title">{ ti.nicePluralName }</span>
                </h2>
                <TreeViewer typeName={ti.name} key={ti.name} />
            </div>
        );
    }
}



