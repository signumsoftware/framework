import * as Services from './Services';
import * as Navigator from './Navigator';

import * as React from 'react'
import { Entity, Lite, is } from './Signum.Entities';

interface RetrieveProps {
    lite?: Lite<Entity> | null;
    children: (entity?: Entity | null) => React.ReactElement<any> | null | undefined | false;
}

interface RetrieveState {
    //undefined => for not loaded yet
    //null => lite is null or undefined
    entity?: Entity | null; }

export class Retrieve extends React.Component<RetrieveProps, RetrieveState> {

    static create<T extends Entity>(lite: Lite<T> | null | undefined, render: (entity?: T | null) => React.ReactElement<any> | null | undefined | false): React.ReactElement<any> {
        return (
            <Retrieve lite={lite}>
                {entity => render(entity as T | undefined)}
            </Retrieve>
        );
    }

    constructor(props: RetrieveProps) {
        super(props);
        this.state = {
            entity: props.lite ? undefined : null
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

        if (props.lite == null)
            this.setState({ entity: null });
        else {
            this.setState({ entity: undefined });
            Navigator.API.fetchAndForget(props.lite)
                .then(e => this.setState({ entity: e }))
                .done();
        }
    }

    render() {
        return (this.props.children(this.state.entity)) || null;
    }
}


