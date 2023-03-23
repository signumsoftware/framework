import React, { useEffect, useState } from "react";
import { UserEntity } from "../Signum.Authorization";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import './ProfilePhoto.css'
import { getToString, Lite, toLite } from "@framework/Signum.Entities";
import UserCircle from "./UserCircle";
import * as UserCircles from "./UserCircle";
import { classes } from "@framework/Globals";

export var urlProviders: ((u: UserEntity | Lite<UserEntity>, size: number) => string | null)[] = [];

export default function ProfilePhoto(p: { user: UserEntity, size: number }) {
  const [imageError, setImageError] = useState(false);

  var url = urlProviders.map(f => f(p.user, p.size)).notNull().firstOrNull();

  useEffect(() => {
    setImageError(false);
  }, [url]);

  if (imageError)
    url = null;

  function addDefaultSrc(ev: any) {
    setImageError(true);
  }

  var color = p.user.isNew ? "gray" : UserCircles.Options.getUserColor(toLite(p.user));

  var iconSize = p.size >= 250 ? "10x" : `${Math.ceil(p.size / 25)}x`;
  return (
    <div className="user-profile-photo align-items-center d-flex justify-content-center" style={{ width: `${p.size}px`, height: `${p.size}px`, borderColor: !url ? color : undefined }}>
      {!url ? <FontAwesomeIcon icon="user" size={iconSize as any} color={color} /> :
        <img src={url} style={{ maxWidth: `${p.size - 3}px`, maxHeight: `${p.size - 3}px` }} onError={addDefaultSrc} title={getToString(p.user)} />}
    </div>
  );
}

export function SmallProfilePhoto(p: { user: Lite<UserEntity>, size?: number, className?: string }) {
  const [imageError, setImageError] = useState(false);
  var size = p.size ?? 22;

  var url = urlProviders.map(f => f(p.user, size!)).notNull().firstOrNull();

  useEffect(() => {
    setImageError(false);
  }, [url]);

  function addDefaultSrc(ev: any) {
    setImageError(true);
  }

  return (
    <div className={classes("small-user-profile-photo", p.className)}>
      {url && !imageError ? <img src={url} style={{ maxWidth: `${size}px`, maxHeight: `${size}px` }} onError={addDefaultSrc} title={getToString(p.user)} /> :
        <UserCircle user={p.user} />}
    </div>
  );
}
