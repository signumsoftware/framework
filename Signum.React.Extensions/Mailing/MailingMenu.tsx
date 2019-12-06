import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Lite } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import SearchControlLoaded from '@framework/SearchControl/SearchControlLoaded'
import { EmailTemplateEntity, EmailMessageEntity } from './Signum.Entities.Mailing'
import * as MailingClient from './MailingClient'
import { DropdownButton, Dropdown } from 'react-bootstrap';

export interface MailingMenuProps {
  searchControl: SearchControlLoaded;
}

export default function MailingMenu(p : MailingMenuProps){
  function handleClick(et: Lite<EmailTemplateEntity>) {
    Navigator.API.fetchAndForget(et)
      .then(emailTemplate => MailingClient.API.getConstructorType(emailTemplate.model!))
      .then(ct => {

        var s = MailingClient.settings[ct];
        if (!s)
          throw new Error("No 'WordModelSettings' defined for '" + ct + "'");

        if (!s.createFromQuery)
          throw new Error("No 'createFromQuery' defined in the WordModelSettings of '" + ct + "'");

        return s.createFromQuery(et, p.searchControl.getQueryRequest())
          .then(m => m && MailingClient.createAndViewEmail(et, m));
      })
      .done();
  }

  const emailTemplates = p.searchControl.props.queryDescription.emailTemplates;

  if (!emailTemplates || !emailTemplates.length)
    return null;

  const label = <span><FontAwesomeIcon icon={["far", "envelope"]} /> &nbsp; {EmailMessageEntity.nicePluralName()}</span>;

  return (
    <DropdownButton id="mailingDropDown" variant="light" className="sf-mailing-dropdown" title={label}>
      {
        emailTemplates.map((wt, i) =>
          <Dropdown.Item key={i}
            onClick={() => handleClick(wt)}>
            {wt.toStr}
          </Dropdown.Item>)
      }
    </DropdownButton>
  );
}



