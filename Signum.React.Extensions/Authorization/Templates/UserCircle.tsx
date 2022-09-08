import * as React from 'react'
import { getToString, Lite } from '@framework/Signum.Entities'
import { UserEntity } from '../Signum.Entities.Authorization'
import { classes } from '@framework/Globals';
import './UserCircle.css'

export namespace Options {

  export let colors = "#750b1c #a4262c #d13438 #ca5010 #986f0b #498205 #0b6a0b #038387 #005b70 #0078d4 #004e8c #4f6bed #5c2e91 #8764b8 #881798 #c239b3 #e3008c #8e562e #7a7574 #69797e".split(" ");

  export function getUserColor(u: Lite<UserEntity>): string {

    var id = u.id as number;

    return colors[id % colors.length];
  }
}

export function getUserInitials(u: Lite<UserEntity>): string {
  var str = getToString(u);
  if (!str)
    return "";

  return str.split(" ").map(m => m[0]).filter((a, i) => i < 2).join("").toUpperCase() ?? "";
}

export default function UserCircle(p: { user: Lite<UserEntity>, className?: string }) {
  var color = Options.getUserColor(p.user);
  return (
    <span className={classes("user-circle", p.className)} style={{
      color: "white",
      textDecoration: "underline",
      textDecorationColor: color,
      backgroundColor: color
    }} title={getToString(p.user)}>
      {getUserInitials(p.user)}
    </span>
  );
}

