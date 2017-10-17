import * as Services from './Services';
import * as Navigator from './Navigator';

import * as React from 'react'
import { Entity, Lite, is } from './Signum.Entities';

interface RetrieveProps {
    lite?: Lite<Entity> | null;
    children: (entity?: Entity) => React.ReactElement<any> | null | undefined;
}

interface RetrieveState {
    entity?: Entity;
}

export class Retrieve extends React.Component<RetrieveProps, RetrieveState> {

    constructor(props: RetrieveProps) {
        super(props);
        this.state = {
            entity: undefined
        };
    }

    componentWillMount() {
        this.loadData(this.props);
    }

    componentWillReceiveProps(newProps: RetrieveProps) {
        if (!is(newProps.lite, this.props.lite))
            this.loadData(newProps);
    }

    loadData(props: RetrieveProps) {

        if (this.props.lite == null)
            this.setState({ entity: undefined });
        else
            Navigator.API.fetchAndForget(this.props.lite)
                .then(e => this.setState({ entity: e }))
                .done();
    }

    render() {
        return (this.props.children(this.state.entity)) || null;
    }
}


