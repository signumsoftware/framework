import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { RouteComponentProps } from 'react-router'
import { Dic, classes } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '@framework/FindOptions'
import { SearchMessage, JavascriptMessage, parseLite, is, Lite, toLite } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import SearchControlLoaded from '@framework/SearchControl/SearchControlLoaded'
import { EmailTemplateEntity, EmailMessageEntity } from './Signum.Entities.Mailing'
import * as MailingClient from './MailingClient'
import { saveFile } from "@framework/Services";
import { UncontrolledDropdown, DropdownMenu, DropdownItem, DropdownToggle } from '@framework/Components';

export interface MailingMenuProps {
    searchControl: SearchControlLoaded;
}

export default class MailingMenu extends React.Component<MailingMenuProps> {
    handleClick = (et: Lite<EmailTemplateEntity>) => {

        Navigator.API.fetchAndForget(et)
            .then(emailTemplate => MailingClient.API.getConstructorType(emailTemplate.systemEmail!))
            .then(ct => {

                var s = MailingClient.settings[ct];
                if (!s)
                    throw new Error("No 'WordModelSettings' defined for '" + ct + "'");

                if (!s.createFromQuery)
                    throw new Error("No 'createFromQuery' defined in the WordModelSettings of '" + ct + "'");

                return s.createFromQuery(et, this.props.searchControl.getQueryRequest())
                    .then(m => m && MailingClient.createAndViewEmail(et, m));
            })
            .done();
    }

    render() {

        const emailTemplates = this.props.searchControl.props.queryDescription.emailTemplates;

        if (!emailTemplates || !emailTemplates.length)
            return null;

        const label = <span><FontAwesomeIcon icon={["far", "envelope"]} /> &nbsp; {EmailMessageEntity.nicePluralName()}</span>;

        return (
            <UncontrolledDropdown id="mailingDropDown" className="sf-mailing-dropdown">
                <DropdownToggle color="light" caret>{label as any}</DropdownToggle>
                <DropdownMenu>
                    {
                        emailTemplates.map((wt, i) =>
                            <DropdownItem key={i}
                                onClick={() => this.handleClick(wt)}>
                                {wt.toStr}
                            </DropdownItem>)
                    }
                </DropdownMenu>
            </UncontrolledDropdown>
        );
    }
}



