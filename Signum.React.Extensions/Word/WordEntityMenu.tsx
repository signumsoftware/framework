import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Lite, toLite, Entity, EntityPack } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import { WordTemplateEntity, WordTemplateMessage } from './Signum.Entities.Word'
import * as WordClient from './WordClient'
import { saveFile } from "@framework/Services";
import { DropdownButton, Dropdown } from 'react-bootstrap';

export interface WordEntityMenuProps {
  entityPack: EntityPack<Entity>;
}

export default function WordEntityMenu(p : WordEntityMenuProps){
  function handleOnClick(wt: Lite<WordTemplateEntity>) {
    Navigator.API.fetchAndForget(wt)
      .then<string | undefined>(wordTemplate => wordTemplate.model ? WordClient.API.getConstructorType(wordTemplate.model) : undefined)
      .then(ct => {

        if (!ct || ct == p.entityPack.entity.Type)
          return WordClient.API.createAndDownloadReport({ template: wt, lite: toLite(p.entityPack.entity) });

        var s = WordClient.settings[ct];
        if (!s)
          throw new Error("No 'WordModelSettings' defined for '" + ct + "'");

        if (!s.createFromEntities)
          throw new Error("No 'createFromEntities' defined in the WordModelSettings of '" + ct + "'");

        return s.createFromEntities(wt, [toLite(p.entityPack.entity)])
          .then<Response | undefined>(m => m && WordClient.API.createAndDownloadReport({ template: wt, entity: m }));
      })
      .then(response => response && saveFile(response))
      .done();
  }

  const label = <span><FontAwesomeIcon icon={["far", "file-word"]} />&nbsp;{WordTemplateMessage.WordReport.niceToString()}</span>;

  return (
    <DropdownButton id="wordMenu" className="sf-word-dropdown" title={label}> 
        {
          p.entityPack.wordTemplates!.map((wt, i) =>
            <Dropdown.Item key={i}
              onClick={() => handleOnClick(wt)}>
              {wt.toStr}
            </Dropdown.Item>)
        }
    </DropdownButton>
  )
}



