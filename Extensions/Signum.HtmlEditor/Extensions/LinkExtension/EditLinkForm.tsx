import { FontAwesomeIcon } from "@framework/Lines";
import { AutoLineProps } from "@framework/Lines/AutoLine";
import React, { useState, ReactNode } from "react";

export default function EditLinkForm(p: AutoLineProps): ReactNode {
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
    <div style={{display: "flex", gap: "8px" }}>
      <input aria-label="Insert hyperlink URL" value={url} onChange={event => handleUpdateURL(event.target.value)} style={{ padding: "8px", outline: 0, flex: 1}} />
      <button aria-label="Remove hyperlink" onClick={removeURL} style={{borderRadius: "8px", outline: 0, border: "0 none", aspectRatio: 1}}>
        <FontAwesomeIcon icon="xmark"/>
      </button>
    </div>
  )
}
