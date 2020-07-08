
export namespace QueryString {
  export function parse(queryString: string): { [key: string]: string } {
    var params = new URLSearchParams(queryString);
    var result: { [key: string]: string } = {};
    params.forEach((value, key) => {
      if (!result.hasOwnProperty(key)) {
        result[key] = value;
      }
    });
    return result;
  }

  export function stringify(query: { [key: string]: string | number | boolean | null | undefined }): string {
    var params = new URLSearchParams();
    for (var key in query) {
      var value = query[key];
      if (value != null)
        params.set(key, value.toString());
    }
    return params.toString();
  }
}
