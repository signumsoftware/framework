using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Signum.Utilities
{
  

    public class StringDistance
    {
        //Nullable? doesn't work with reference types
        public struct Maybe<T>
        {
            public readonly bool HasValue;
            public readonly T Value;

            public Maybe(T value)
            {
                this.HasValue = true;
                this.Value = value;
            }

            public override string ToString()
            {
                if (!HasValue)
                    return "-";

                if (Value == null)
                    return "";

                return Value.ToString();
            }
        }

        int[,] num;

        public int LevenshteinDistance(string str1, string str2, IEqualityComparer<char> comparer = null, Func<Maybe<char>, Maybe<char>, int> weight = null)
        {
            return LevenshteinDistance<char>(str1.ToCharArray(), str2.ToCharArray(), comparer, weight);
        }

        public int LevenshteinDistance<T>(T[] str1, T[] str2, IEqualityComparer<T> comparer = null, Func<Maybe<T>, Maybe<T>, int> weight = null)
        {
            int M1 = str1.Length + 1;
            int M2 = str2.Length + 1;

            if (comparer == null)
                comparer = EqualityComparer<T>.Default;

            if(weight == null)
                weight = (s1, s2)=>1;

            ResizeArray(M1, M2);

            num[0, 0] = 0;

            for (int i = 1; i < M1; i++)
                num[i, 0] = num[i - 1, 0] + weight(new Maybe<T>(str1[i - 1]), new Maybe<T>());
            for (int j = 1; j < M2; j++)
                num[0, j] = num[0, j - 1] + weight(new Maybe<T>(), new Maybe<T>(str2[j - 1]));

            for (int i = 1; i < M1; i++)
            {
                for (int j = 1; j < M2; j++)
                {
                    if (comparer.Equals(str1[i - 1], str2[j - 1]))
                        num[i, j] = num[i - 1, j - 1];
                    else
                        num[i, j] = Math.Min(Math.Min(
                            num[i - 1, j] + weight(new Maybe<T>(str1[i - 1]), new Maybe<T>()),
                            num[i, j - 1] + weight(new Maybe<T>(), new Maybe<T>(str2[j - 1]))),
                            num[i - 1, j - 1] + weight(new Maybe<T>(str1[i - 1]), new Maybe<T>(str2[j - 1])));
                }
            }

            return num[M1 - 1, M2 - 1];
        }

        public List<LevenshteinChoice<char>> LevenshteinChoices(string str1, string str2, IEqualityComparer<char> comparer = null, Func<Maybe<char>, Maybe<char>, int> weight = null)
        {
            return LevenshteinChoices<char>(str1.ToCharArray(), str2.ToCharArray(), comparer, weight);
        }

        public struct LevenshteinChoice<T>
        {
            public LevenshteinChoice(Maybe<T> maybe1, Maybe<T> maybe2)
            {
                this.Maybe1 = maybe1;
                this.Maybe2 = maybe2;
            }

            public Maybe<T> Maybe1;
            public Maybe<T> Maybe2;

            public override string ToString()
            {
                return "[{0},{1}]".Formato(Maybe1, Maybe2);
            }
        }

        public List<LevenshteinChoice<T>> LevenshteinChoices<T>(T[] str1, T[] str2, IEqualityComparer<T> comparer = null, Func<Maybe<T>, Maybe<T>, int> weight = null)
        {
            if (comparer == null)
                comparer = EqualityComparer<T>.Default;

            if(weight == null)
                weight = (s1, s2)=>1;

            this.LevenshteinDistance<T>(str1, str2, comparer, weight);

            int i = str1.Length;
            int j = str2.Length;

            List<LevenshteinChoice<T>> result = new List<LevenshteinChoice<T>>();

            while (i > 0 && j > 0)
            {
                if (comparer.Equals(str1[i - 1], str2[j - 1]))
                {
                    result.Add(new LevenshteinChoice<T>(new Maybe<T>(str1[i - 1]), new Maybe<T>(str2[j - 1])));
                    i = i - 1;
                    j = j - 1;
                }
                else
                {
                    var add1 = num[i - 1, j] + weight(new Maybe<T>(str1[i - 1]), new Maybe<T>());
                    var remove2 = num[i, j - 1] + weight(new Maybe<T>(), new Maybe<T>(str2[j - 1]));
                    var replace = num[i - 1, j - 1] + weight(new Maybe<T>(str1[i - 1]), new Maybe<T>(str2[j - 1]));

                    var min = Math.Min(add1, Math.Min(remove2, replace));

                    if (replace == min)
                    {
                        result.Add(new LevenshteinChoice<T>(new Maybe<T>(str1[i - 1]), new Maybe<T>(str2[j - 1])));
                        i = i - 1;
                        j = j - 1;
                    }
                    else if (add1 == min)
                    {
                        result.Add(new LevenshteinChoice<T>(new Maybe<T>(str1[i - 1]), new Maybe<T>()));
                        i = i - 1;
                    }
                    else if (remove2 == min)
                    {
                        result.Add(new LevenshteinChoice<T>(new Maybe<T>(), new Maybe<T>(str2[j - 1])));
                        j = j - 1;
                    }
                }
            }

            result.Reverse();

            return result;
        }
      

        public int LongestCommonSubstring(string str1, string str2)
        {
            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
                return 0;

            int pos1;
            int pos2; 
            return LongestCommonSubstring<char>(str1.ToCharArray(), str2.ToCharArray(), out pos1, out pos2);
        }

        public int LongestCommonSubstring(string str1, string str2, out int startPos1, out int startPos2)
        {
            startPos1 = 0;
            startPos2 = 0;

            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
                return 0;

            return LongestCommonSubstring<char>(str1.ToCharArray(), str2.ToCharArray(), out startPos1, out startPos2);
        }

        public int LongestCommonSubstring<T>(T[] str1, T[] str2, out int startPos1, out int startPos2, IEqualityComparer<T> comparer = null)
        {
            if (str1 == null)
                throw new ArgumentNullException("str1");

            if (str2 == null)
                throw new ArgumentNullException("str2");

            return LongestCommonSubstring(new Slice<T>(str1), new Slice<T>(str2), out startPos1, out startPos2, comparer);
        }

        //http://en.wikibooks.org/wiki/Algorithm_Implementation/Strings/Longest_common_substring
        public int LongestCommonSubstring<T>(Slice<T> str1, Slice<T> str2, out int startPos1, out int startPos2, IEqualityComparer<T> comparer = null)
        {
            startPos1 = 0;
            startPos2 = 0;

            if (str1.Length == 0 || str2.Length == 0)
                return 0;

            if (comparer == null)
                comparer = EqualityComparer<T>.Default;

            ResizeArray(str1.Length, str2.Length);

            int maxlen = 0;

            for (int i = 0; i < str1.Length; i++)
            {
                for (int j = 0; j < str2.Length; j++)
                {
                    if (!comparer.Equals(str1[i], str2[j]))
                        num[i, j] = 0;
                    else
                    {
                        if ((i == 0) || (j == 0))
                            num[i, j] = 1;
                        else
                            num[i, j] = 1 + num[i - 1, j - 1];

                        if (num[i, j] > maxlen)
                        {
                            maxlen = num[i, j];
                            startPos1 = i - num[i, j] + 1;
                            startPos2 = j - num[i, j] + 1;
                        }
                    }
                }
            }
            return maxlen;
        }

        public int LongestCommonSubsequence(string str1, string str2)
        {
            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
                return 0;

            return LongestCommonSubsequence<char>(str1.ToCharArray(), str2.ToCharArray());
        }


        /// <summary>
        /// ACE is a subsequence of ABCDE
        /// </summary>
        public int LongestCommonSubsequence<T>(T[] str1, T[] str2, IEqualityComparer<T> comparer = null)
        {
            if (str1 == null)
                throw new ArgumentNullException("str1");

            if (str2 == null)
                throw new ArgumentNullException("str2");

            if(str1.Length == 0 ||  str2.Length == 0)
                return 0;

            if (comparer == null)
                comparer = EqualityComparer<T>.Default;

            int M1 = str1.Length + 1;
            int M2 = str2.Length + 1;

            ResizeArray(M1, M2);

            for (int i = 0; i < M1; i++)
                num[i, 0] = 0;
            for (int j = 0; j < M2; j++)
                num[0, j] = 0;

            for (int i = 1; i < M1; i++)
            {
                for (int j = 1; j < M2; j++)
                {
                    if (!comparer.Equals(str1[i], str2[j]))
                        num[i, j] = num[i - 1, j - 1] + 1;
                    else
                    {
                        if (num[i, j - 1] > num[i - 1, j])
                            num[i, j] = num[i, j - 1];
                        else
                            num[i, j] = num[i - 1, j];
                    }
                }
            }

            return num[str1.Length, str2.Length];
        }

        private void ResizeArray(int M1, int M2)
        {
            if (num == null || M1 > num.GetLength(0) || M2 > num.GetLength(1))
            {
                num = new int[M1, M2];
            }
        }


        public enum DiffAction
        {
            Equal,
            Added,
            Removed
        }

        public struct DiffPair<T>
        {
            public DiffPair(DiffAction action, T value)
            {
                this.Action = action;
                this.Value = value;
            }
            public readonly DiffAction Action;
            public readonly T Value;

            public override string ToString()
            {
                var str = Action == DiffAction.Added ? "+" :
                    Action == DiffAction.Removed ? "-" : "";

                return str + Value;
            }
        }

        
        public List<DiffPair<List<DiffPair<string>>>> DiffText(string textOld, string textNew)
        {
            var linesOld = textOld.Lines();
            var linesNew = textNew.Lines();

            List<DiffPair<string>> diff = this.Diff(linesOld, linesNew);

            List<IGrouping<bool, DiffPair<string>>> groups = diff.GroupWhenChange(a => a.Action == DiffAction.Equal).ToList();

            StringDistance sd = new StringDistance(); 
            return groups.SelectMany(g=>
            {
                if (g.Key)
                    return g.Select(dp => new DiffPair<List<DiffPair<string>>>(DiffAction.Equal, new List<DiffPair<string>> { dp })); 

                var removed = g.Where(a=>a.Action == DiffAction.Removed).Select(a=>a.Value).ToArray();
                var added = g.Where(a=>a.Action == DiffAction.Added).Select(a=>a.Value).ToArray();

                var choices = this.LevenshteinChoices<string>(removed, added, weight: (a, b) =>
                {
                    if (!a.HasValue)
                        return b.Value.Length;

                    if (!b.HasValue)
                        return a.Value.Length;

                    var distance = sd.LevenshteinDistance(a.Value, b.Value);

                    return distance * 2;
                });

                return choices.Select(c =>
                {
                    if (!c.Maybe1.HasValue)
                        return new DiffPair<List<DiffPair<string>>>(DiffAction.Added, new List<DiffPair<string>> { new DiffPair<string>(DiffAction.Added, c.Maybe2.Value) });

                    if (!c.Maybe2.HasValue)
                        return new DiffPair<List<DiffPair<string>>>(DiffAction.Removed, new List<DiffPair<string>> { new DiffPair<string>(DiffAction.Removed, c.Maybe1.Value) });

                    var diffWords = sd.DiffWords(c.Maybe1.Value, c.Maybe2.Value);

                    return new DiffPair<List<DiffPair<string>>>(DiffAction.Equal, diffWords); 
                });
            }).ToList(); 


        }

        public enum DiffTextAction
        {
            Equal,
            Added,
            Removed,
            Changed,
        }

        static readonly Regex WordsRegex = new Regex(@"([\w\d]+|\s+|.)");
        public List<DiffPair<string>> DiffWords(string strOld, string strNew)
        {
            var wordsOld = WordsRegex.Matches(strOld).Cast<Match>().Select(m => m.Value).ToArray();
            var wordsNew = WordsRegex.Matches(strNew).Cast<Match>().Select(m => m.Value).ToArray();

            return Diff(wordsOld, wordsNew);
        }

        public List<DiffPair<T>> Diff<T>(T[] strOld, T[] strNew, IEqualityComparer<T> comparer = null)
        {
            var result = new List<DiffPair<T>>();
            DiffPrivate<T>(new Slice<T>(strOld), new Slice<T>(strNew), comparer, result);
            return result;
        }

        public void DiffPrivate<T>(Slice<T> sliceOld, Slice<T> sliceNew, IEqualityComparer<T> comparer, List<DiffPair<T>> result)
        {
            int posOld;
            int posNew;
            int length = LongestCommonSubstring<T>(sliceOld, sliceNew, out posOld, out posNew, comparer);

            if (length == 0)
            {
                AddResults(result, sliceOld, DiffAction.Removed);
                AddResults(result, sliceNew, DiffAction.Added);
            }
            else
            {
                TryDiff(sliceOld.SubSliceStart(posOld), sliceNew.SubSliceStart(posNew), comparer, result);

                AddResults(result, sliceOld.SubSlice(posOld, length), DiffAction.Equal);

                TryDiff(sliceOld.SubSliceEnd(posOld + length), sliceNew.SubSliceEnd(posNew + length), comparer, result);
            }
        }

        private static void AddResults<T>(List<DiffPair<T>> list, Slice<T> slice, DiffAction action)
        {
            for (int i = 0; i < slice.Length; i++)
                list.Add(new DiffPair<T>(action, slice[i]));
        }

        private void TryDiff<T>(Slice<T> sliceOld, Slice<T> sliceNew, IEqualityComparer<T> comparer, List<DiffPair<T>> result)
        {
            if (sliceOld.Length > 0 && sliceOld.Length > 0)
            {
                DiffPrivate(sliceOld, sliceNew, comparer, result);
            }
            else if (sliceOld.Length > 0)
            {
                AddResults(result, sliceOld, DiffAction.Removed);
            }
            else if (sliceNew.Length > 0)
            {
                AddResults(result, sliceNew, DiffAction.Added);
            }
        }
    }

    public struct Slice<T>
    {
        public Slice(T[] array) : this(array, 0, array.Length) { }

        public Slice(T[] array, int offset, int length)
        {
            if (offset + length > array.Length)
                throw new ArgumentException("Invalid slice");

            this.Array = array;
            this.Offset = offset;
            this.Length = length;
        }

        public readonly T[] Array;
        public readonly int Offset;
        public readonly int Length;

        public T this[int index]
        {
            get
            {
                if (index > Length)
                    throw new IndexOutOfRangeException();

                return Array[Offset + index];
            }
            set
            {
                if (index > Length)
                    throw new IndexOutOfRangeException();

                Array[Offset + index] = value;
            }
        }

        public Slice<T> SubSlice(int relativeOffset, int length)
        {
            return new Slice<T>(this.Array, this.Offset + relativeOffset, length);
        }

        public Slice<T> SubSliceStart(int pos)
        {
            return new Slice<T>(this.Array, 0, pos);
        }

        public Slice<T> SubSliceEnd(int pos)
        {
            return new Slice<T>(this.Array, pos, this.Length - pos);
        }

        public override string ToString()
        {
            return this.Array.Skip(Offset).Take(Length).ToString("");
        }
    }
}
