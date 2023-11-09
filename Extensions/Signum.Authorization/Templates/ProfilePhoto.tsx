import React, { useEffect, useState } from "react";
import { UserEntity } from "../Signum.Authorization";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import './ProfilePhoto.css'
import { getToString, Lite, liteKey, toLite } from "@framework/Signum.Entities";
import UserCircle from "./UserCircle";
import * as UserCircles from "./UserCircle";
import { Dic, classes } from "@framework/Globals";

export var urlProviders: ((u: UserEntity | Lite<UserEntity>, size: number) => Promise<string | null>)[] = [];

export function clearCache() {
  Dic.clear(urlCache);
  urlCache = {};
}

export default function ProfilePhoto(p: { user: UserEntity, size: number }) {
  const [imageError, setImageError] = useState(false);
  const url = useCachedUrl(p.user, p.size!);

  useEffect(() => {
    setImageError(false);
  }, [url]);

  var color = p.user.isNew ? "gray" : UserCircles.Options.getUserColor(toLite(p.user));

  var iconSize = p.size >= 250 ? "10x" : `${Math.ceil(p.size / 25)}x`;
  return (
    <div className="user-profile-photo align-items-center d-flex justify-content-center" style={{ width: `${p.size}px`, height: `${p.size}px`, borderColor: !url ? color : undefined }}>
      {!url ? <FontAwesomeIcon icon="user" size={iconSize as any} color={color} /> :
        <img src={url} style={{ maxWidth: `${p.size - 3}px`, maxHeight: `${p.size - 3}px` }} onError={() => setImageError(true)} title={getToString(p.user)} />}
    </div>
  );
}

export function SmallProfilePhoto(p: { user: Lite<UserEntity>, size?: number, className?: string, fallback?: React.ReactNode }) {
  const [imageError, setImageError] = useState(false);
  const size = p.size ?? 22;

  const url = useCachedUrl(p.user, size!);

  useEffect(() => {
    setImageError(false);
  }, [url]);

  return (
    <div className={classes("small-user-profile-photo", p.className)}>
      {url && !imageError ? <img src={url} style={{ maxWidth: `${size}px`, maxHeight: `${size}px` }} onError={(e) => setImageError(true)} title={getToString(p.user)} /> :
        p.fallback ?? <UserCircle user = {p.user } />}
    </div>
  );
}

var urlCache: { [userKey: string]: Promise<string | null> | null } = { };

function useCachedUrl(user: UserEntity | Lite<UserEntity>, size: number) {
  const [url, setUrl] = useState<string | null>(null);

  useEffect(() => {

    if (!user.id)
      return;

    const userLite = UserEntity.isLite(user) ? user : toLite(user);
    const userLiteKey = liteKey(userLite);

    const promise = (urlCache[userLiteKey] ??= urlProviders.map(f => f(user, size)).notNull().firstOrNull());

    promise ? promise.then(u => setUrl(u)) : setUrl(null);

  }, [user.id]);

  return url
}

