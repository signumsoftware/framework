
import * as React from 'react'
import { RouteComponentProps } from 'react-router'
import { ButtonDropdown, DropdownItem } from 'reactstrap'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { SearchMessage, JavascriptMessage, parseLite, is, Lite, toLite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import SearchControlLoaded from '../../../Framework/Signum.React/Scripts/SearchControl/SearchControlLoaded'
import { EmailTemplateEntity, EmailMessageEntity } from './Signum.Entities.Mailing'
import * as MailingClient from './MailingClient'
import { saveFile } from "../../../Framework/Signum.React/Scripts/Services";

export interface MailingMenuProps {
    searchControl: SearchControlLoaded;
}

export default class MailingMenu extends React.Component<MailingMenuProps, { wordReports?: Lite<EmailTemplateEntity>[] }> {

    constructor(props: MailingMenuProps) {
        super(props);
        this.state = { };
    }

    componentWillMount() {
        this.reloadList().done();
    }

    reloadList(): Promise<void> {
        return MailingClient.API.getEmailTemplates(this.props.searchControl.props.findOptions.queryKey, "Query")
            .then(list => this.setState({ wordReports: list }));
    }

    handleSelect = (et: Lite<EmailTemplateEntity>) => {

        Navigator.API.fetchAndForget(et)
            .then(emailTemplate => MailingClient.API.getConstructorType(emailTemplate.systemEmail!))
            .then(ct => {

                var s = MailingClient.settings[ct];
                if (!s)
                    throw new Error("No 'WordModelSettings' defined for '" + ct + "'");

                if (!s.createFromQuery)
                    throw new Error("No 'createFromQuery' defined in the WordModelSettings of '" + ct + "'");

                return s.createFromQuery(et, this.props.searchControl.getQueryRequest())
                    .then(m => m && MailingClient.createAndViewEmail(et, m ));
            })
            .done();
    }

    render() {

        if (!this.state.wordReports || !this.state.wordReports.length)
            return null;

        const label = <span><i className="fa fa-file-word-o"></i> &nbsp; {EmailMessageEntity.nicePluralName()}</span>;

        return (
            <ButtonDropdown title={label as any} id="userQueriesDropDown" className="sf-userquery-dropdown">
                {
                    this.state.wordReports.map((wt, i) =>
                        <DropdownItem key={i}
                            onSelect={() => this.handleSelect(wt) }>
                            { wt.toStr }
                        </DropdownItem>)
                }
            </ButtonDropdown>
        );
    }
 
}



