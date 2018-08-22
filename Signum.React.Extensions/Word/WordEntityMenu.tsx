import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { RouteComponentProps } from 'react-router'
import { Dic, classes } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '@framework/FindOptions'
import { SearchMessage, JavascriptMessage, parseLite, is, Lite, toLite, Entity, EntityPack } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import SearchControlLoaded from '@framework/SearchControl/SearchControlLoaded'
import { WordTemplateEntity, WordTemplateMessage } from './Signum.Entities.Word'
import * as WordClient from './WordClient'
import { saveFile } from "@framework/Services";
import { UncontrolledDropdown, DropdownToggle, DropdownItem, DropdownMenu } from '@framework/Components';

export interface WordEntityMenuProps {
    entityPack: EntityPack<Entity>;
}

export default class WordEntityMenu extends React.Component<WordEntityMenuProps> {

    handleOnClick = (wt: Lite<WordTemplateEntity>) => {

        Navigator.API.fetchAndForget(wt)
            .then<string | undefined>(wordTemplate => wordTemplate.systemWordTemplate ? WordClient.API.getConstructorType(wordTemplate.systemWordTemplate!) : undefined)
            .then(ct => {

                if (!ct || ct == this.props.entityPack.entity.Type)
                    return WordClient.API.createAndDownloadReport({ template: wt, lite: toLite(this.props.entityPack.entity) });

                var s = WordClient.settings[ct];
                if (!s)
                    throw new Error("No 'WordModelSettings' defined for '" + ct + "'");

                if (!s.createFromEntities)
                    throw new Error("No 'createFromEntities' defined in the WordModelSettings of '" + ct + "'");

                return s.createFromEntities(wt, [toLite(this.props.entityPack.entity)])
                    .then<Response | undefined>(m => m && WordClient.API.createAndDownloadReport({ template: wt, entity: m }));
            })
            .then(response => response && saveFile(response))
            .done();
    }

    render() {

        const label = <span><FontAwesomeIcon icon={["far", "file-word"]} />&nbsp;{WordTemplateMessage.WordReport.niceToString()}</span>;

        return (
            <UncontrolledDropdown id="wordMenu" className="sf-word-dropdown">
                <DropdownToggle caret>{label as any}</DropdownToggle>
                <DropdownMenu>
                {
                    this.props.entityPack.wordTemplates!.map((wt, i) =>
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



