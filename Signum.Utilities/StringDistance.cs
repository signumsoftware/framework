using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Signum.Utilities
{
    public class StringDistance
    {
        int[,] num;

        public int LevenshteinDistance(string strOld, string strNew, IEqualityComparer<char> comparer = null, Func<Choice<char>, int> weight = null, bool allowTransposition = false)
        {
            return LevenshteinDistance<char>(strOld.ToCharArray(), strNew.ToCharArray(), comparer, weight, allowTransposition);
        }

        public int LevenshteinDistance<T>(T[] strOld, T[] strNew, IEqualityComparer<T> comparer = null, Func<Choice<T>, int> weight = null, bool allowTransposition = false)
        {
            int M1 = strOld.Length + 1;
            int M2 = strNew.Length + 1;

            if (comparer == null)
                comparer = EqualityComparer<T>.Default;

            if (weight == null)
                weight = c => 1;

            ResizeArray(M1, M2);

            num[0, 0] = 0;

            for (int i = 1; i < M1; i++)
                num[i, 0] = num[i - 1, 0] + weight(Choice<T>.Remove(strOld[i - 1]));
            for (int j = 1; j < M2; j++)
                num[0, j] = num[0, j - 1] + weight(Choice<T>.Add(strNew[j - 1]));

            for (int i = 1; i < M1; i++)
            {
                for (int j = 1; j < M2; j++)
                {
                    if (comparer.Equals(strOld[i - 1], strNew[j - 1]))
                        num[i, j] = num[i - 1, j - 1];
                    else
                    {
                        num[i, j] = Math.Min(Math.Min(
                            num[i - 1, j] + weight(Choice<T>.Remove(strOld[i - 1])),
                            num[i, j - 1] + weight(Choice<T>.Add(strNew[j - 1]))),
                            num[i - 1, j - 1] + weight(Choice<T>.Substitute(strOld[i - 1], strNew[j - 1])));

                        if (allowTransposition && i > 1 && j > 1 && comparer.Equals(strOld[i - 1], strNew[j - 2]) && comparer.Equals(strOld[i - 2], strNew[j - 1]))
                            num[i, j] = Math.Min(num[i, j], num[i - 2, j - 2] + weight(Choice<T>.Transpose(strOld[i - 1], strOld[i - 2])));
                    }
                }
            }

            return num[M1 - 1, M2 - 1];
        }

    

        public enum ChoiceType
        {
            Equal,
            Substitute,
            Remove,
            Add,
            Transpose,
        }

        public struct Choice<T>
        {
            public readonly ChoiceType Type;
            public readonly T Removed;
            public readonly T Added;

            public bool HasRemoved { get { return Type != ChoiceType.Add; } }
            public bool HasAdded { get { return Type != ChoiceType.Remove; } }

            internal Choice( ChoiceType type, T removed, T added)
            {
                this.Type = type;
                this.Removed = removed;
                this.Added = added;
            }

            public static Choice<T> Add(T value)
            {
                return new Choice<T>(ChoiceType.Add, default(T), value);
            }

            public static Choice<T> Remove(T value)
            {
                return new Choice<T>(ChoiceType.Remove, value, default(T));
            }

            public static Choice<T> Equal(T value)
            {
                return new Choice<T>(ChoiceType.Equal, value, value);
            }

            public static Choice<T> Substitute(T remove, T add)
            {
                return new Choice<T>(ChoiceType.Substitute, remove, add);
            }

            internal static Choice<T> Transpose(T remove, T add)
            {
                return new Choice<T>(ChoiceType.Transpose, remove, add);
            }


            public override string ToString()
            {
                switch (Type)
                {
                    case ChoiceType.Equal: return "{0}".FormatWith(Added);
                    case ChoiceType.Substitute: return "[-{0}+{1}]".FormatWith(Removed, Added);
                    case ChoiceType.Remove: return "-{0}".FormatWith(Removed);
                    case ChoiceType.Add: return "+{0}".FormatWith(Added);
                    default: return null;
                }
            }

            
        }

        public List<Choice<char>> LevenshteinChoices(string strOld, string strNew, IEqualityComparer<char> comparer = null, Func<Choice<char>, int> weight = null)
        {
            return LevenshteinChoices<char>(strOld.ToCharArray(), strNew.ToCharArray(), comparer, weight);
        }

        public List<Choice<T>> LevenshteinChoices<T>(T[] strOld, T[] strNew, IEqualityComparer<T> comparer = null, Func<Choice<T>, int> weight = null)
        {
            if (comparer == null)
                comparer = EqualityComparer<T>.Default;

            if (weight == null)
                weight = (c) => 1;

            this.LevenshteinDistance<T>(strOld, strNew, comparer, weight);

            int i = strOld.Length;
            int j = strNew.Length;

            List<Choice<T>> result = new List<Choice<T>>();

            while (i > 0 && j > 0)
            {
                if (comparer.Equals(strOld[i - 1], strNew[j - 1]))
                {
                    result.Add(Choice<T>.Equal(strOld[i - 1]));
                    i = i - 1;
                    j = j - 1;
                }
                else
                {
                    var cRemove = Choice<T>.Remove(strOld[i - 1]);
                    var cAdd = Choice<T>.Add(strNew[j - 1]);
                    var cSubstitute = Choice<T>.Substitute(strOld[i - 1], strNew[j - 1]);

                    var remove = num[i - 1, j] + weight(cRemove);
                    var add = num[i, j - 1] + weight(cAdd);
                    var substitute = num[i - 1, j - 1] + weight(cSubstitute);

                    var min = Math.Min(remove, Math.Min(add, substitute));

                    if (substitute == min)
                    {
                        result.Add(cSubstitute);
                        i = i - 1;
                        j = j - 1;
                    }
                    else if (remove == min)
                    {
                        result.Add(cRemove);
                        i = i - 1;
                    }
                    else if (add == min)
                    {
                        result.Add(cAdd);
                        j = j - 1;
                    }
                }
            }

            while (i > 0)
            {
                result.Add(Choice<T>.Remove(strOld[i - 1]));
                i = i - 1;
            }

            while (j > 0)
            {
                result.Add(Choice<T>.Add(strNew[j - 1]));
                j = j - 1;
            }

            result.Reverse();

            return result;
        }
      

        public int LongestCommonSubstring(string str1, string str2)
        {
            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
                return 0;

            return LongestCommonSubstring<char>(str1.ToCharArray(), str2.ToCharArray(), out int pos1, out int pos2);
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

        string DebugTable()
        {
            return num.SelectArray(a => a.ToString()).FormatTable();
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
                    if (comparer.Equals(str1[i - 1], str2[j - 1]))
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

        
        public List<DiffPair<List<DiffPair<string>>>> DiffText(string textOld, string textNew, bool lineEndingDifferences = true)
        {
            textOld = textOld ?? "";
            textNew = textNew ?? "";

            var linesOld = lineEndingDifferences ? textOld.Split('\n') : textOld.Lines();
            var linesNew = lineEndingDifferences ? textNew.Split('\n') : textNew.Lines();

            List<DiffPair<string>> diff = this.Diff(linesOld, linesNew);

            List<IGrouping<bool, DiffPair<string>>> groups = diff.GroupWhenChange(a => a.Action == DiffAction.Equal).ToList();

            StringDistance sd = new StringDistance(); 
            return groups.SelectMany(g=>
            {
                if (g.Key)
                    return g.Select(dp => new DiffPair<List<DiffPair<string>>>(DiffAction.Equal, new List<DiffPair<string>> { dp })); 

                var removed = g.Where(a=>a.Action == DiffAction.Removed).Select(a=>a.Value).ToArray();
                var added = g.Where(a=>a.Action == DiffAction.Added).Select(a=>a.Value).ToArray();

                var choices = this.LevenshteinChoices<string>(removed, added, weight: c =>
                {
                    if (c.Type == ChoiceType.Add)
                        return c.Added.Length;

                    if (c.Type == ChoiceType.Remove)
                        return c.Removed.Length;

                    var distance = sd.LevenshteinDistance(c.Added, c.Removed);

                    return distance * 2;
                });

                return choices.Select(c =>
                {
                    if (c.Type == ChoiceType.Add)
                        return new DiffPair<List<DiffPair<string>>>(DiffAction.Added, new List<DiffPair<string>> { new DiffPair<string>(DiffAction.Added, c.Added) });

                    if (c.Type == ChoiceType.Remove)
                        return new DiffPair<List<DiffPair<string>>>(DiffAction.Removed, new List<DiffPair<string>> { new DiffPair<string>(DiffAction.Removed, c.Removed) });

                    var diffWords = sd.DiffWords(c.Removed, c.Added);

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

        void DiffPrivate<T>(Slice<T> sliceOld, Slice<T> sliceNew, IEqualityComparer<T> comparer, List<DiffPair<T>> result)
        {
            int length = LongestCommonSubstring<T>(sliceOld, sliceNew, out int posOld, out int posNew, comparer);

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

        static void AddResults<T>(List<DiffPair<T>> list, Slice<T> slice, DiffAction action)
        {
            for (int i = 0; i < slice.Length; i++)
                list.Add(new DiffPair<T>(action, slice[i]));
        }

        void TryDiff<T>(Slice<T> sliceOld, Slice<T> sliceNew, IEqualityComparer<T> comparer, List<DiffPair<T>> result)
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

    public struct Slice<T> :IEnumerable<T>
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

        public Slice<T> SubSlice(int relativeIndex, int length)
        {
            return new Slice<T>(this.Array, this.Offset + relativeIndex, length);
        }

        public Slice<T> SubSliceStart(int relativeIndex)
        {
            return new Slice<T>(this.Array, this.Offset, relativeIndex);
        }

        public Slice<T> SubSliceEnd(int relativeIndex)
        {
            return new Slice<T>(this.Array, this.Offset + relativeIndex, this.Length - relativeIndex);
        }

        public override string ToString()
        {
            return this.Array.Skip(Offset).Take(Length).ToString("");
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.Array.Skip(Offset).Take(Length).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
