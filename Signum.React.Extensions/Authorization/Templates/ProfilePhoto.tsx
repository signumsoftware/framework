import React, { useState } from "react";
import { TypeContext } from "../../../Signum.React/Scripts/Lines";
import { UserEntity } from "../Signum.Entities.Authorization";
import * as AppContext from "@framework/AppContext"
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

export default function ProfilePhoto(p: { user: UserEntity }) {


 const [immageError, setImageError] = useState(false);

  function addDefaultSrc(ev: any) {
    setImageError(true);
}

return (
  <div style={{ display: "flex", flexDirection: "row", justifyContent: "center" }}>
    {immageError ? <FontAwesomeIcon icon="user" size="6x" /> :
      <img src={AppContext.toAbsoluteUrl("~/api/thumbnailphoto/" + p.user.userName)} style={{ width: "150px", height: "150px", borderRadius: "50%", objectFit: "cover", border: "3px solid white", boxShadow: "1px 1px 10px grey" }} onError={addDefaultSrc} />}
  </div>
  );
}

