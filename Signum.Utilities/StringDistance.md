# StringDistance

This class contains a set of useful methods for string comparison, but the methods can also be used for any `T[]`.

This methods use dynamic programming and need to create NxM arrays to to the comparison. 

To avoid creating too much big objects in loops, the methods can re-use the previous array objects in this instance. 

### LevenshteinDistance

An implementation of [Levenshtein string distance](http://en.wikipedia.org/wiki/Levenshtein_distance). 

Result the number of editions (insertion, substitution and removal) necessary to convert one string to the other.


```c#
public int LevenshteinDistance(string strOld, string strNew, 
	IEqualityComparer<char> comparer = null, 
    Func<Choice<char>, int> weight = null)

public int LevenshteinDistance<T>(T[] strOld, T[] strNew, 
	IEqualityComparer<T> comparer = null, 
    Func<Choice<T>, int> weight = null)

public struct Choice<T>
{
    public readonly ChoiceType Type;
    public readonly T Removed;
    public readonly T Added;

    public bool HasRemoved { get { return Type != ChoiceType.Add; } }
    public bool HasAdded { get { return Type != ChoiceType.Remove; } }
}

public enum ChoiceType
{
    Equal,
    Substitute,
    Remove,
    Add,
}    
```

Using the lambda `weight`, we can fine-tune the algorithm to, for example, make `a` closer to `á` or `A` than to `W`.  

There are also variations to reconstruct the exact choices that where made (useful for Diffs algorithms)

```c#
public List<Choice<char>> LevenshteinChoices(string strOld, string strNew, 
	IEqualityComparer<char> comparer = null, 
	Func<Choice<char>, int> weight = null)

public List<Choice<T>> LevenshteinChoices<T>(T[] strOld, T[] strNew, 
	IEqualityComparer<T> comparer = null, 
	Func<Choice<T>, int> weight = null)
```

### LongestCommonSubstring

Implements the [longest common substring problem](http://en.wikipedia.org/wiki/Longest_common_substring_problem). The longest consecutive sequence of elements in both strings. 

```c#
public int LongestCommonSubstring(string str1, string str2)
public int LongestCommonSubstring(string str1, string str2, out int startPos1, out int startPos2)
public int LongestCommonSubstring<T>(T[] str1, T[] str2, out int startPos1, out int startPos2, IEqualityComparer<T> comparer = null)
public int LongestCommonSubstring<T>(Slice<T> str1, Slice<T> str2, out int startPos1, out int startPos2, IEqualityComparer<T> comparer = null)        
```

There are overloads that also return, using `out` parameters, the starting position of such sub-string in both strings. 

The most general overload takes [array slices](http://en.wikipedia.org/wiki/Array_slicing) instead of arrays, to simplify diff algorithms.

```C#
public struct Slice<T> :IEnumerable<T>
{
     public readonly T[] Array;
     public readonly int Offset;
     public readonly int Length;

     public Slice(T[] array) 
     public Slice(T[] array, int offset, int length)

     public T this[int index]{get; set;}
  
     public Slice<T> SubSlice(int relativeIndex, int length)
     public Slice<T> SubSliceStart(int relativeIndex)
     public Slice<T> SubSliceEnd(int relativeIndex)  
}

```

### LongestCommonSubsequence

Implements the [longest common subsequence problem](http://en.wikipedia.org/wiki/Longest_common_subsequence_problem). The longest ordered but non-consecutive sequence of elements in both strings. 


```c#
public int LongestCommonSubsequence(string str1, string str2)
public int LongestCommonSubsequence<T>(T[] str1, T[] str2, IEqualityComparer<T> comparer = null)       
```


### Diff

Implements a diff algorithm to compare two sequences using `LongestCommonSubstring` recursively. The result is similar to LevenshteinChoices, but while `LevenshteinChoices` is optimized for unintentional mistakes (misspellings), `Diff` is optized for intentional changes. 

```c#
//Uses 
public List<DiffPair<T>> Diff<T>(T[] strOld, T[] strNew, IEqualityComparer<T> comparer = null)      
```

### DiffWords

Compares two strings splitting by words and using `Diff`. A word is considered any sequence of letters an numbers.  

```c#
//Uses 
public List<DiffPair<string>> DiffWords(string strOld, string strNew)    
```

### DiffText

Compares two long multi-line string (like a code file), using a combination of Diff and LevenshteinChoices at the line level, and DiffWords for each lines. 

```c#
//Uses 
public List<DiffPair<string>> DiffWords(string strOld, string strNew)    
```