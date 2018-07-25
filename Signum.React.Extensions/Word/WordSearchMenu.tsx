import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { RouteComponentProps } from 'react-router'
import { Dic, classes } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '@framework/FindOptions'
import { SearchMessage, JavascriptMessage, parseLite, is, Lite, toLite } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import SearchControlLoaded from '@framework/SearchControl/SearchControlLoaded'
import { WordTemplateEntity, WordTemplateMessage } from './Signum.Entities.Word'
import * as WordClient from './WordClient'
import { saveFile } from "@framework/Services";
import { UncontrolledDropdown, DropdownToggle, DropdownItem, DropdownMenu } from '@framework/Components';

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

        const label = <span><FontAwesomeIcon icon={["far", "file-word"]} />&nbsp;{this.props.searchControl.props.largeToolbarButtons == true ? " " + WordTemplateMessage.WordReport.niceToString() : undefined}</span>;

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

declare module '@framework/SearchControl/SearchControlLoaded' {

    export interface ShowBarExtensionOption {
        showWordReport?: boolean;
    }
}



