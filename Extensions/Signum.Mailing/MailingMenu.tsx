import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { getToString, Lite } from '@framework/Signum.Entities'
import { Navigator } from '@framework/Navigator'
import SearchControlLoaded from '@framework/SearchControl/SearchControlLoaded'
import { EmailMessageEntity } from './Signum.Mailing'
import { MailingClient } from './MailingClient'
import { DropdownButton, Dropdown } from 'react-bootstrap';
import { EmailTemplateEntity } from './Signum.Mailing.Templates'

export interface MailingMenuProps {
  searchControl: SearchControlLoaded;
}

export default function MailingMenu(p : MailingMenuProps): React.JSX.Element | null {
  function handleClick(et: Lite<EmailTemplateEntity>) {
    Navigator.API.fetch(et)
      .then(emailTemplate => MailingClient.API.getConstructorType(emailTemplate.model!))
      .then(ct => {

        var s = MailingClient.settings[ct];
        if (!s)
          throw new Error("No 'WordModelSettings' defined for '" + ct + "'");

        if (!s.createFromQuery)
          throw new Error("No 'createFromQuery' defined in the WordModelSettings of '" + ct + "'");

        return s.createFromQuery(et, p.searchControl.getQueryRequest())
          .then(m => m && MailingClient.createAndViewEmail(et, m));
      });
  }

  const emailTemplates = p.searchControl.props.queryDescription.emailTemplates;

  if (!emailTemplates || !emailTemplates.length)
    return null;

  const label = <span><FontAwesomeIcon aria-hidden={true} icon="envelope" /> &nbsp; {EmailMessageEntity.nicePluralName()}</span>;

  return (
    <DropdownButton id="mailingDropDown" variant="light" className="sf-mailing-dropdown" title={label}>
      {
        emailTemplates == "error" ? <Dropdown.Item className="text-danger">Error</Dropdown.Item> : 
        emailTemplates.map((wt, i) =>
          <Dropdown.Item key={i}
            onClick={() => handleClick(wt)}>
            {getToString(wt)}
          </Dropdown.Item>)
      }
    </DropdownButton>
  );
}



