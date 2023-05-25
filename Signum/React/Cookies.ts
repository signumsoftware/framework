
export namespace Cookies {

  export function set(name: string, value: string, days?: number, path?: string, domain?: string) {
    let cookie: any = { [name]: value, path: path ?? '/' };
    
    if (days) {
      let date: Date = new Date();
      date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
      cookie.expires = date.toUTCString();
    }

    if (domain) {
      cookie.domain = domain
    }
    
    let arr = []
    for (let key in cookie) {
      arr.push(`${key}=${cookie[key]}`);
    }
    document.cookie = arr.join('; ');

    return get(name);
  }

  export function getAll() {
    let cookie: { [name: string]: string } = {};
    document.cookie.split(';').forEach(el => {
      let [k, v] = el.split('=');
      cookie[k.trim()] = v;
    })
    return cookie;
  }

  export function get(name : string) {
    return getAll()[name];
  }

  export function remove(name: string, path?: string, domain?: string) {
    set(name, '', -1, path, domain);
  }
};
