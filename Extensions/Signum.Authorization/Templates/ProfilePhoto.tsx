import React, { useEffect, useState } from "react";
import { UserEntity } from "../Signum.Authorization";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import './ProfilePhoto.css'
import { getToString, Lite, liteKey, toLite } from "@framework/Signum.Entities";
import UserCircle from "./UserCircle";
import * as UserCircles from "./UserCircle";
import { Dic, classes, isPromise } from "@framework/Globals";
import { useAPI } from "@framework/Hooks";

export var urlProviders: ((u: UserEntity | Lite<UserEntity>, size: number) => string | Promise<string | null> | null)[] = [];

export function clearCache(): void {
  Dic.clear(urlCache);
  urlCache = {};
}

export default function ProfilePhoto(p: { user: UserEntity, size: number }): React.JSX.Element {
  const [imageError, setImageError] = useState(false);
  let url = useCachedUrl(p.user, p.size!);

  useEffect(() => {
    setImageError(false);
  }, [url]);

  if (imageError)
    url = null;

  var color = p.user.isNew ? "gray" : UserCircles.Options.getUserColor(toLite(p.user));

  var iconSize = p.size >= 250 ? "10x" : `${Math.ceil(p.size / 25)}x`;
  return (
    <div className="user-profile-photo align-items-center d-flex justify-content-center" style={{ width: `${p.size}px`, height: `${p.size}px`, borderColor: !url ? color : undefined }}>
      {!url ? <FontAwesomeIcon role="img" icon="user" size={iconSize as any} color={color} /> :
        <img src={url} style={{ maxWidth: `${p.size - 3}px`, maxHeight: `${p.size - 3}px` }} onError={() => setImageError(true)} title={getToString(p.user)} />}
    </div>
  );
}

export function SmallProfilePhoto(p: { user: Lite<UserEntity>, size?: number, className?: string, fallback?: React.ReactNode }): React.JSX.Element {
  const [imageError, setImageError] = useState(false);
  const size = p.size ?? 22;

  const url = useCachedUrl(p.user, size!);

  useEffect(() => {
    setImageError(false);
  }, [url]);

  return (
    <div className={classes("small-user-profile-photo", p.className)}>
      {url && !imageError ? <img src={url} style={{ maxWidth: `${size}px`, maxHeight: `${size}px` }} alt={getToString(p.user)} onError={(e) => setImageError(true)} title={getToString(p.user)} /> :
        p.fallback ?? <UserCircle user={p.user } />}
    </div>
  );
}

function useCachedUrl(user: UserEntity | Lite<UserEntity>, size: number): string | null | undefined {

  var url = useAPI(() => {

    const val = !user.id ? getFirstUrl(user, size) : getCachedFirstUrl(user, size);

    return val;
  }, [user.id]);

  return url
}

var urlCache: { [userKeyPlusSize: string]: Promise<string | null> | string | null } = { };
function getCachedFirstUrl(user: Lite<UserEntity> | UserEntity, size: number) {
  return urlCache[liteKey(UserEntity.isLite(user) ? user : toLite(user)) + ":" + size] ??= getFirstUrl(user, size);
}

function getFirstUrl(user: Lite<UserEntity> | UserEntity, size: number): Promise<string | null> | string | null {
  return urlProviders.map(f => f(user, size)).notNull().firstOrNull();
}

