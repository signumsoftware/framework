import React, { useEffect, useState } from "react";
import { UserEntity } from "../Signum.Entities.Authorization";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import './ProfilePhoto.css'
import { getToString, Lite } from "@framework/Signum.Entities";
import UserCircle from "../../ConcurrentUser/UserCircle";
import { classes } from "../../../Signum.React/Scripts/Globals";

export var urlProviders: ((u: UserEntity | Lite<UserEntity>, size: number) => string | null)[] = []; 

export default function ProfilePhoto(p: { user: UserEntity | Lite<UserEntity>, size: number }) {
  const [immageError, setImageError] = useState(false);

  var url = urlProviders.map(f => f(p.user, p.size)).notNull().firstOrNull(); 

  useEffect(() => {
    setImageError(false);
  }, [url]);

  function addDefaultSrc(ev: any) {
    setImageError(true);
  }

  var iconSize = p.size >= 250 ? "10x" : `${Math.ceil(p.size / 25)}x`;
  return (
    <div className="user-profile-photo align-items-center d-flex justify-content-center" style={{ width: `${p.size}px`, height: `${p.size}px` }}>
      {!url || immageError ? <FontAwesomeIcon icon="user" size={iconSize as any} color="gray" /> :
        <img src={url} style={{ maxWidth: `${p.size - 3}px`, maxHeight: `${p.size - 3}px` }} onError={addDefaultSrc} />}
    </div>
  );
}

export function SmallProfilePhoto(p: { user: Lite<UserEntity>, size?: number, className?: string }) {
  const [immageError, setImageError] = useState(false);
  var size = p.size ?? 22;

  var url = urlProviders.map(f => f(p.user, size!)).notNull().firstOrNull();

  useEffect(() => {
    setImageError(false);
  }, [url]);

  function addDefaultSrc(ev: any) {
    setImageError(true);
  }

  return (
    <div className={classes("d-inline-flex small-user-profile-photo align-items-center justify-content-center", p.className)}>
      {url && !immageError ? <img src={url} style={{ maxWidth: `${size}px`, maxHeight: `${size}px` }} onError={addDefaultSrc} title={getToString(p.user)} /> :
        p.user ? <UserCircle user={p.user} /> : <FontAwesomeIcon icon="user" color="gray" />}
    </div>
  );
}
