
import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import Typeahead from '../../../Framework/Signum.React/Scripts/Lines/Typeahead'
import * as OmniboxClient from './OmniboxClient'
import {  OmniboxMessage } from './Signum.Entities.Omnibox'

export interface OmniboxAutocompleteProps {
    divAttrs?: React.HTMLAttributes;
    inputAttrs?: React.HTMLAttributes;
    menuAttrs?: React.HTMLAttributes;
}

export default class OmniboxAutocomplete extends React.Component<OmniboxAutocompleteProps, void>
{
    handleOnSelect = (result: OmniboxClient.OmniboxResult, e: React.SyntheticEvent) => {

        var ke = e as React.KeyboardEvent;

        if (ke.keyCode && ke.keyCode == 9) {
            return OmniboxClient.toString(result);
        }

        OmniboxClient.navigateTo(result).then(url => {
            if (url)
                Navigator.currentHistory.push(url);

        }).done();

        return "";
    }

    render() {

        var inputAttr = this.props.inputAttrs;

        if (inputAttr == null)
            inputAttr = {};

        if (inputAttr.placeholder == null)
            inputAttr.placeholder = OmniboxMessage.Search.niceToString();


        var result  = (
            <Typeahead getItems={OmniboxClient.getResults} 
                renderItem={OmniboxClient.renderItem}
                onSelect={this.handleOnSelect}
                divAttrs={this.props.divAttrs}
                inputAttrs={this.props.inputAttrs}
                menuAttrs={this.props.menuAttrs}
                ></Typeahead>  
        );

        return result;
    }
}






