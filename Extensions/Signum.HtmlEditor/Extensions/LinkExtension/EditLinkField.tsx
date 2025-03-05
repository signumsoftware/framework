import { FontAwesomeIcon } from "@framework/Lines";
import { AutoLineProps } from "@framework/Lines/AutoLine";
import React, { ReactNode, useState } from "react";

export default function EditLinkField(p: AutoLineProps): ReactNode {
  const { binding } = p.ctx;
  const [url, setUrl] = useState<string>(() => binding.getValue())

  const handleUpdateURL = (value: string) => {
    setUrl(value);
    binding.setValue(value);
  }

  const removeURL = () => {
    setUrl("");
    binding.setValue("");
  }
  
  return (
    <div className="d-flex flex-row align-items-center gap-2">
      <input 
        value={url} 
        onChange={event => handleUpdateURL(event.target.value)} 
        aria-label="Insert hyperlink" 
        placeholder="Insert hyperlink" 
        className="flex-grow-1 form-control" 
      />
      <button aria-label="Remove hyperlink" onClick={removeURL} className="btn btn-light sf-remove">
        <FontAwesomeIcon icon="trash" />
      </button>
    </div>
  )
}
