
export type ChoiceType = "Equal" | "Substitute" | "Remove" | "Add" | "Transpose"

export interface Choice<T> {
  type: ChoiceType;
  added?: T;
  removed?: T;
}

export type Char = string;

export default class StringDistance {
  _cachedNum: number[][] = [];

  resizeArray(M1: number, M2: number): number[][] {
    if (this._cachedNum == null || M1 > this._cachedNum.length) {
      {
        for (var i = 0; i < M1; i++) {
          this._cachedNum[i] = [];
        }
      }
    }

    return this._cachedNum;
  }

  levenshteinDistanceString(strOld: string, strNew: string, weight?: (c: Choice<Char>) => number, allowTransposition: boolean = false): number {
    return this.levenshteinDistance<Char>(Array.from(strOld), Array.from(strNew), weight, allowTransposition);
  }

  levenshteinDistance<T>(strOld: T[], strNew: T[], weight?: (c: Choice<T>) => number, allowTransposition: boolean = false): number {
    var M1 = strOld.length + 1;
    var M2 = strNew.length + 1;

    if (weight == null)
      weight = c => 1;

    function equals(a: T, b: T) {
      return a == b || weight!({ type: "Equal", removed: a, added: b }) == 0;
    }

    var num = this.resizeArray(M1, M2);

    num[0][0] = 0;

    for (let i = 1; i < M1; i++)
      num[i][0] = num[i - 1][0] + weight({ type: "Remove", removed: strOld[i - 1] });
    for (let j = 1; j < M2; j++)
      num[0][j] = num[0][j - 1] + weight({ type: "Add", added: strNew[j - 1] });

    for (let i = 1; i < M1; i++) {
      for (let j = 1; j < M2; j++) {
        if (equals(strOld[i - 1], strNew[j - 1]))
          num[i][j] = num[i - 1][j - 1];
        else {
          num[i][j] = Math.min(
            num[i - 1][j] + weight({ type: "Remove", removed: strOld[i - 1] }),
            num[i][j - 1] + weight({ type: "Add", added: strNew[j - 1] }),
            num[i - 1][j - 1] + weight({ type: "Substitute", removed: strOld[i - 1], added: strNew[j - 1] }));

          if (allowTransposition && i > 1 && j > 1 && equals(strOld[i - 1], strNew[j - 2]) && equals(strOld[i - 2], strNew[j - 1]))
            num[i][j] = Math.min(num[i][j], num[i - 2][j - 2] + weight({ type: "Transpose", removed: strOld[i - 1], added: strOld[i - 2] }));
        }
      }
    }

    return num[M1 - 1][M2 - 1];
  }


  levenshteinChoicesString(strOld: string, strNew: string, weight?: (c: Choice<Char>) => number, allowTransposition: boolean = false): Choice<Char>[] {

    return this.levenshteinChoices<Char>(Array.from(strOld), Array.from(strNew), weight, allowTransposition);
  }

  levenshteinChoices<T>(strOld: T[], strNew: T[], weight?: (c: Choice<T>) => number, allowTransposition: boolean = false): Choice<T>[] {

    if (weight == null)
      weight = (c) => 1;

    this.levenshteinDistance<T>(strOld, strNew, weight);

    let i = strOld.length;
    let j = strNew.length;

    function equals(a: T, b: T) {
      return a == b || weight!({ type: "Equal", removed: a, added: b }) == 0;
    }

    const result: Choice<T>[] = [];

    while (i > 0 && j > 0) {
      if (equals(strOld[i - 1], strNew[j - 1])) {
        result.push({ type: "Equal", removed: strOld[i - 1], added: strNew[j - 1] });
        i = i - 1;
        j = j - 1;
      }
      else {
        var cRemove: Choice<T> = { type: "Remove", removed: strOld[i - 1] };
        var cAdd: Choice<T> = { type: "Add", added: strNew[j - 1] };
        var cSubstitute: Choice<T> = { type: "Substitute", removed: strOld[i - 1], added: strNew[j - 1] };

        var num = this._cachedNum!;

        var remove = num[i - 1][j] + weight(cRemove);
        var add = num[i][j - 1] + weight(cAdd);
        var substitute = num[i - 1][j - 1] + weight(cSubstitute);

        var min = Math.min(remove, add, substitute);

        if (substitute == min) {
          result.push(cSubstitute);
          i = i - 1;
          j = j - 1;
        }
        else if (remove == min) {
          result.push(cRemove);
          i = i - 1;
        }
        else if (add == min) {
          result.push(cAdd);
          j = j - 1;
        }
      }
    }

    while (i > 0) {
      result.push({ type: "Remove", removed: strOld[i - 1] });
      i = i - 1;
    }

    while (j > 0) {
      result.push({ type: "Add", added: strNew[j - 1] });
      j = j - 1;
    }

    result.reverse();

    return result;
  }
}
