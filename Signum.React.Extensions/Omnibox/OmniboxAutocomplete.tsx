
import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import Typeahead from '../../../Framework/Signum.React/Scripts/Lines/Typeahead'
import * as OmniboxClient from './OmniboxClient'

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

        var url = OmniboxClient.navigateTo(result);
        if (url)
            Navigator.currentHistory.push(url);

        return "";
    }

    render() {

        var result  = (
            <Typeahead getItems={OmniboxClient.getResults} 
                renderItem={OmniboxClient.renderItem}
                onSelect={this.handleOnSelect}
                divAttrs={this.props.divAttrs}
                inputAttrs={this.props.inputAttrs}
                menuAttrs={this.props.menuAttrs}
                >Hola<span>Juas</span></Typeahead>  
        );

        return result;
    }
}






