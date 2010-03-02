using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Signum.Utilities.DataStructures
{
    [Serializable]
    public struct Tuple<F, S> : IEquatable<Tuple<F, S>>, ISerializable
    {
        public readonly F First;
        public readonly S Second;

        public Tuple(F first, S second)
        {
            this.First = first;
            this.Second = second;
        }

    

        public override string ToString()
        {
            return "[{0},{1}]".Formato(First, Second);
        }

        public bool Equals(Tuple<F, S> value)
        {
            return
                EqualityComparer<F>.Default.Equals(First, value.First) && 
                EqualityComparer<S>.Default.Equals(Second, value.Second);
        }

        public override bool Equals(object obj)
        {
            return obj is Tuple<F, S> && Equals((Tuple<F, S>)obj);
        }

        public override int GetHashCode()
        {
            int num = 0;
            num ^= EqualityComparer<F>.Default.GetHashCode(this.First);
            num ^= EqualityComparer<S>.Default.GetHashCode(this.Second);
            return num;
        }

        public Tuple(SerializationInfo info, StreamingContext context)
        {
            First = (F)info.GetValue("First", typeof(F));
            Second = (S)info.GetValue("Second", typeof(S));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("First", First);
            info.AddValue("Second", Second);
        }

    }

    [Serializable]
    public struct Tuple<F, S, T> : IEquatable<Tuple<F, S, T>>, ISerializable
    {
        public readonly F First;
        public readonly S Second;
        public readonly T Third;

        public Tuple(F first, S second, T third)
        {
            this.First = first;
            this.Second = second;
            this.Third = third;
        }

        public override string ToString()
        {
            return "[{0},{1},{2}]".Formato(First, Second, Third);
        }

        public bool Equals(Tuple<F, S, T> value)
        {
            return
                EqualityComparer<F>.Default.Equals(First, value.First) &&
                EqualityComparer<S>.Default.Equals(Second, value.Second) &&
                EqualityComparer<T>.Default.Equals(Third, value.Third);
        }

        public override bool Equals(object obj)
        {
            return obj is Tuple<F, S, T> && Equals((Tuple<F, S, T>)obj);
        }

        public override int GetHashCode()
        {
            int num = 0;
            num ^= EqualityComparer<F>.Default.GetHashCode(this.First);
            num ^= EqualityComparer<S>.Default.GetHashCode(this.Second);
            num ^= EqualityComparer<T>.Default.GetHashCode(this.Third);
            return num;
        }

        public Tuple(SerializationInfo info, StreamingContext context)
        {
            First = (F)info.GetValue("First", typeof(F));
            Second = (S)info.GetValue("Second", typeof(S));
            Third = (T)info.GetValue("Third", typeof(T));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("First", First);
            info.AddValue("Second", Second);
            info.AddValue("Third", Third);
        }
    }

    public static class Tuple
    {
        public static Tuple<F, S> New<F, S>(F first, S second)
        {
            return new Tuple<F, S>(first, second);
        }

        public static Tuple<F, S, T> New<F, S, T>(F first, S second, T third)
        {
            return new Tuple<F, S, T>(first, second, third);
        }    
    }

    public static class KVP
    {
        public static KeyValuePair<F, S> New<F, S>(F first, S second)
        {
            return new KeyValuePair<F, S>(first, second);
        }
    }
}


