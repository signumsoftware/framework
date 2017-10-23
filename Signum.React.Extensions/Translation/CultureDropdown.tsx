import * as React from 'react'
import { NavDropdown, MenuItem }  from 'react-bootstrap'
import { Route } from 'react-router'
import { Dic } from '../../../Framework/Signum.React/Scripts/Globals';
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import { Entity, Lite, is, toLite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { CultureInfoEntity } from '../Basics/Signum.Entities.Basics'
import * as CultureClient from './CultureClient'

export interface CultureDropdownProps {
}

export interface CultureDropdownState {
    cultures?: { [name: string]: Lite<CultureInfoEntity> };
}

export default class CultureDropdown extends React.Component<CultureDropdownProps, CultureDropdownState> {

    constructor(props: CultureDropdownProps) {
        super(props);
        this.state = {};
    }
    
    componentWillMount() {
        CultureClient.getCultures(false)
            .then(cultures => this.setState({ cultures: cultures }))
            .done();
    }

    handleSelect = (c: Lite<CultureInfoEntity>) => {
        CultureClient.changeCurrentCulture(c);
    }

    render() {
        const cultures = this.state.cultures;

        if (!cultures)
            return null;

        const current = CultureClient.currentCulture;

        const pair = Dic.map(cultures, (name, c) => ({ name, c })).filter(p => is(p.c, current)).singleOrNull();

        return (
            <NavDropdown id="culture-dropdown" title={current.toStr} data-culture={pair && pair.name}>
                {
                    Dic.map(cultures, (name, c, i) =>
                        <MenuItem key={i} data-culture={name} selected={is(c, current) } onSelect={() => this.handleSelect(c)}>
                            {c.toStr}
                        </MenuItem>)
                }
            </NavDropdown>
        );
    }
}