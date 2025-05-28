# MyRandom
MyRandom class has some improvements over [Random class](http://msdn.microsoft.com/en-us/library/system.random.aspx).

It has a static property to access a `ThreadStatic` field, so you won't need to instantiate a `Random` class. 

```C#
[ThreadStatic]
static Random random;

public static Random Current
{
    get { return random ?? (random = new Random()); }
}
```

Also, it provides some extension methods for Random class: 

```C#
public static bool NextBool(this Random r)

public static char NextUppercase(this Random r) //ABCDEFGHJKLMNPQRSTWXYZ
public static char NextLowercase(this Random r) //abcdefgijkmnopqrstwxyz
public static char NextChar(this Random r) //randomly upper and lowercase 

//Same but returning string of a given length
public static string NextUppercaseString(this Random r, int length)
public static string NextLowercaseString(this Random r, int length)
public static string NextString(this Random r, int length)

//Returns a 32-bit int representing a color
public static int NextAlphaColor(this Random r)
public static int NextColor(this Random r)
public static int NextColor(this Random r, int minR, int maxR, int minG, int maxG, int minB, int maxB)

//A random DateTime between min and max
public static DateTime NextDateTime(this Random r, DateTime min, DateTime max)

public static long NextLong(this Random r, long max)
public static long NextLong(this Random r, long min, long max)

//Picks randomly an element or the array
public static T NextElement<T>(this Random r, params T[] elements)
```

Example: 

```C#
MyRandom.Current.NextBool(); //False

MyRandom.Current.NextUppercase();  //E
MyRandom.Current.NextChar(); //b
MyRandom.Current.NextLowercaseString(5); //jmdym
MyRandom.Current.NextString(5); //GWtWJ

MyRandom.Current.NextColor(); //-1193927 (just a number, but a color in soul)
MyRandom.Current.NextDateTime(new DateTime(2000, 1, 1), new DateTime(2008, 1, 1));
MyRandom.Current.NextLong(1000000000); //262778249
MyRandom.Current.NextElement("eins", "zwai", "drei"); //drei
```