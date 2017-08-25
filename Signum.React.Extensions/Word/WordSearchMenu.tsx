
import * as React from 'react'
import { RouteComponentProps } from 'react-router'
import { DropdownButton, MenuItem } from 'react-bootstrap'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { SearchMessage, JavascriptMessage, parseLite, is, Lite, toLite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import SearchControlLoaded from '../../../Framework/Signum.React/Scripts/SearchControl/SearchControlLoaded'
import { WordTemplateEntity, WordTemplateMessage } from './Signum.Entities.Word'
import * as WordClient from './WordClient'
import { saveFile } from "../../../Framework/Signum.React/Scripts/Services";

export interface WordSearchMenuProps {
    searchControl: SearchControlLoaded;
}

export default class WordSearchMenu extends React.Component<WordSearchMenuProps, { wordReports?: Lite<WordTemplateEntity>[] }> {

    constructor(props: WordSearchMenuProps) {
        super(props);
        this.state = { };
    }

    componentWillMount() {
        this.reloadList().done();
    }

    reloadList(): Promise<void> {
        return WordClient.API.getWordTemplates(this.props.searchControl.props.findOptions.queryKey, "Query", null)
            .then(list => this.setState({ wordReports: list }));
    }

    handleSelect = (wt: Lite<WordTemplateEntity>) => {

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

        if (!this.state.wordReports || !this.state.wordReports.length ||
            (this.props.searchControl.props.showBarExtensionOption && this.props.searchControl.props.showBarExtensionOption.showWordReport == false))
            return null;

        const label = <span><i className="fa fa-file-word-o"></i>&nbsp;{this.props.searchControl.props.largeToolbarButtons == true ? " " + WordTemplateMessage.WordReport.niceToString() : undefined}</span>;

        return (
            <DropdownButton title={label as any} id="userQueriesDropDown" className="sf-userquery-dropdown">
                {
                    this.state.wordReports.map((wt, i) =>
                        <MenuItem key={i}
                            onSelect={() => this.handleSelect(wt) }>
                            { wt.toStr }
                        </MenuItem>)
                }
            </DropdownButton>
        );
    }
 
}

declare module '../../../Framework/Signum.React/Scripts/SearchControl/SearchControlLoaded' {

    export interface ShowBarExtensionOption {
        showWordReport?: boolean;
    }
}



