
export async function* jsonObjectStream<T>(reader: ReadableStreamDefaultReader<Uint8Array>): AsyncGenerator<T> {

  const decoder = new TextDecoder();
  //let totalStr = ""; 
  let str = "";
  let isStart = true;

  while (true) {
    debugger;
    var pair = await reader.read();
    if (pair.done)
      return;

    var newPart = decoder.decode(pair.value);
    //totalStr += newPart;
    str += newPart;

    if (isStart) {
      const index = consumeSpaces(str, 0);

      if (index == null)
        continue;

      if (str[index] != "[")
        throw new Error("Start of array not found");

      str = str.substring(index + 1);

      isStart = false;
    }



    while (true) { //Invariatn \s*{object}\s),
      const index = consumeSpaces(str, 0);

      if (index == null)
        break;

      if (str[index] == ']')
        return;

      const index2 = consumeObject(str, index);

      if (index2 == null)
        break;

      var objStr = str.substring(index, index2);

      const index3 = consumeSpaces(str, index2);
      if (index3 == null)
        break;

      var terminator = str[index3];
      if (terminator != "]" && terminator != ",")
        throw new Error("List separator not found");

      var obj = JSON.parse(objStr) as T;
      yield obj;

      if (terminator == "]")
        return;
      else //if (terminator == ",")
        str = str.substring(index3 + 1);
    }
  }
};

function consumeSpaces(text: string, startIndex: number): number | null {

  for (var i = startIndex; i < text.length; i++) {
    var c = text[i];
    if (!(c == " " || c == "\n" || c == "\r" || c == "\t"))
      return i;
  }

  return null;
}

function consumeStringLiteral(text: string, startIndex: number): number | null {
  var lastIsSlash = false;

  for (var i = startIndex + 1; i < text.length; i++) {
    var c = text[i];

    if (c == "\"" && !lastIsSlash)
      return i + 1;

    lastIsSlash = c == "\\";
  }

  return null;
}

function consumeObject(str: string, startIndex: number): number | null {
  var level = 0;

  if (str[startIndex] != "{")
    throw new Error("Start of object not found");

  for (var i = startIndex; i < str.length; i++) {
    var c = str[i];

    switch (c) {
      case "\"": {
        var newIndex = consumeStringLiteral(str, i);
        if (newIndex == null)
          return null;

        i = newIndex
        break;
      }
      case "{": {
        level++;
        break;
      }
      case "}": {
        level--;
        if (level == 0)
          return i + 1;
        break;
      }
    }
  }

  return null;
}
