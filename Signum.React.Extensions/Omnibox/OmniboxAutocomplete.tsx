
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
    handleOnSelect = (item: OmniboxClient.OmniboxResult) => {
        
        return "";
    }

    render() {
        return (
            <Typeahead getItems={OmniboxClient.getResults} 
                renderItem={OmniboxClient.renderItem}
                onSelect={this.handleOnSelect}
                divAttrs={this.props.divAttrs}
                inputAttrs={this.props.inputAttrs}
                menuAttrs={this.props.menuAttrs}
                />  
         );
    }
}






