
import * as React from 'react'
import { RouteComponentProps } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { SearchMessage, JavascriptMessage, parseLite, is, Lite, toLite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import SearchControlLoaded from '../../../Framework/Signum.React/Scripts/SearchControl/SearchControlLoaded'
import { WordTemplateEntity, WordTemplateMessage } from './Signum.Entities.Word'
import * as WordClient from './WordClient'
import { saveFile } from "../../../Framework/Signum.React/Scripts/Services";
import { UncontrolledDropdown, DropdownToggle, DropdownItem, DropdownMenu } from '../../../Framework/Signum.React/Scripts/Components';

export interface WordSearchMenuProps {
    searchControl: SearchControlLoaded;
}

export default class WordSearchMenu extends React.Component<WordSearchMenuProps> {

    handleOnClick = (wt: Lite<WordTemplateEntity>) => {

        Navigator.API.fetchAndForget(wt)
            .then(wordTemplate => WordClient.API.getConstructorType(wordTemplate.systemWordTemplate!))
            .then(ct => {

                var s = WordClient.settings[ct];
                if (!s)
                    throw new Error("No 'WordModelSettings' defined for '" + ct + "'");

                if (!s.createFromQuery)
                    throw new Error("No 'createFromQuery' defined in the WordModelSettings of '" + ct + "'");

                return s.createFromQuery(wt, this.props.searchControl.getQueryRequest())
                    .then<Response | undefined>(m => m && WordClient.API.createAndDownloadReport({ template: wt, entity: m }));
            })
            .then(response => response && saveFile(response))
            .done();
    }

    render() {

        var wordReports = this.props.searchControl.props.queryDescription.wordTemplates;

        if (!wordReports || !wordReports.length ||
            (this.props.searchControl.props.showBarExtensionOption && this.props.searchControl.props.showBarExtensionOption.showWordReport == false))
            return null;

        const label = <span><i className="fa fa-file-word-o"></i>&nbsp;{this.props.searchControl.props.largeToolbarButtons == true ? " " + WordTemplateMessage.WordReport.niceToString() : undefined}</span>;

        return (
            <UncontrolledDropdown id="wordTemplateDropDown" className="sf-word-dropdown">
                <DropdownToggle>{label}</DropdownToggle>
                <DropdownMenu>
                    {
                        wordReports.map((wt, i) =>
                            <DropdownItem key={i}
                                onClick={() => this.handleOnClick(wt)}>
                                {wt.toStr}
                            </DropdownItem>)
                    }
                </DropdownMenu>
            </UncontrolledDropdown>
        );
    }

}

declare module '../../../Framework/Signum.React/Scripts/SearchControl/SearchControlLoaded' {

    export interface ShowBarExtensionOption {
        showWordReport?: boolean;
    }
}



