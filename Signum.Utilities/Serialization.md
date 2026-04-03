
# Serialization
Simplifies binary serialization and deserialization of objects using `BinaryFormatter` and `SoapFormatter`. 

```C#
public static class Serialization
{
   //BinaryFormatter serialize
   public static byte[] ToBytes(object graph)
   public static void ToBinaryFile(object graph, string fileName)

   //BinaryFormatter deserialize
   public static object FromBytes(byte[] bytes)
   public static object FromBinaryFile(string fileName)

   //SoapFormatter serialize
   public static string ToString(object graph)
   public static void ToStringFile(object graph, string fileName)

   //SoapFormatter deserialize
   public static object FromString(string str)
   public static object FromStringFile(string fileName)
}
```