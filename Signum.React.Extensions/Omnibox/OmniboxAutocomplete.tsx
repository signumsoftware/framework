
import * as React from 'react'
import { Route } from 'react-router'
import { Dic } from '../../../Framework/Signum.React/Scripts/Globals';
import { ajaxPost, ajaxGet, AbortableRequest } from '../../../Framework/Signum.React/Scripts/Services';
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { Typeahead } from '../../../Framework/Signum.React/Scripts/Components'
import * as OmniboxClient from './OmniboxClient'
import { OmniboxMessage } from './Signum.Entities.Omnibox'
import '../../../Framework/Signum.React/Scripts/Frames/MenuIcons.css'

export interface OmniboxAutocompleteProps {
    inputAttrs?: React.HTMLAttributes<HTMLInputElement>;
}

export default class OmniboxAutocomplete extends React.Component<OmniboxAutocompleteProps>
{
    handleOnSelect = (result: OmniboxClient.OmniboxResult, e: React.KeyboardEvent<any> | React.MouseEvent<any>) => {

        this.abortRequest.abort();

        const ke = e as React.KeyboardEvent<any>;
        if (ke.keyCode && ke.keyCode == 9) {
            return OmniboxClient.toString(result);
        }
        e.persist();

        const promise = OmniboxClient.navigateTo(result);
        if (promise) {
            promise
                .then(url => {
                    if (url)
                        Navigator.pushOrOpenInTab(url, e);
                }).done();
        }
        this.typeahead.blur();

        return null;
    }

    abortRequest = new AbortableRequest((ac, query: string) => OmniboxClient.API.getResults(query, ac));

    typeahead!: Typeahead;

    render() {

        let inputAttr = { tabIndex: -1, placeholder: OmniboxMessage.Search.niceToString(), ...this.props.inputAttrs };
        
        const result = (
            <Typeahead ref={ta => this.typeahead = ta!} getItems={str => this.abortRequest.getData(str)} 
                renderItem={OmniboxClient.renderItem}
                onSelect={this.handleOnSelect}
                inputAttrs={inputAttr}
                minLength={0}
                ></Typeahead>  
        );

        return result;
    }
}






