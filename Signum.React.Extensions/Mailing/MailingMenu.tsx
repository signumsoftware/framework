
import * as React from 'react'
import { RouteComponentProps } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { SearchMessage, JavascriptMessage, parseLite, is, Lite, toLite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import SearchControlLoaded from '../../../Framework/Signum.React/Scripts/SearchControl/SearchControlLoaded'
import { EmailTemplateEntity, EmailMessageEntity } from './Signum.Entities.Mailing'
import * as MailingClient from './MailingClient'
import { saveFile } from "../../../Framework/Signum.React/Scripts/Services";
import { UncontrolledDropdown, DropdownMenu, DropdownItem, DropdownToggle } from '../../../Framework/Signum.React/Scripts/Components';

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

        const label = <span><i className="fa fa-envelope-o"></i> &nbsp; {EmailMessageEntity.nicePluralName()}</span>;

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



