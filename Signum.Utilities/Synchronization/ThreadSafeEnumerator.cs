using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Signum.Utilities.Synchronization
{
    public class TreadSafeEnumerator<T>: IEnumerable<T>, IEnumerator<T>
    {
        object key = new object();
        IEnumerator<T> enumerator;
        
        volatile bool moveNext = true;

        //lo que necesitaria es una variable de instancia ThreadStatic
        //podriamos imitarla con un diccionario por hilo de instancia
        //pero requeriria locks asi que en lugar de Instancia->Hilo->Valor hacemos Hilo->Instancia->Valor y asi aprovechamos el thread static
        [ThreadStatic]
        static Dictionary<TreadSafeEnumerator<T>, T> dictionary;
      
        T current
        {
            get { return dictionary[this]; }
            set
            {
                if (dictionary == null)
                    dictionary = new Dictionary<TreadSafeEnumerator<T>, T>();

                dictionary[this] = value;
            }
        }

        public TreadSafeEnumerator(IEnumerable<T> source)
        {
            enumerator = source.GetEnumerator(); 
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this; 
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this; 
        }

        public T Current
        {
            get {return current;  }
        }

        object IEnumerator.Current
        {
            get { return current; }
        }

        public bool MoveNext()
        {
            lock (key)
            {
                if (moveNext && (moveNext = enumerator.MoveNext()))
                    current = enumerator.Current;
                else
                    current = default(T);

                return moveNext; 
            }
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (dictionary != null)
            {
                dictionary.Remove(this);
                if (dictionary.Count == 0) dictionary = null; 
            }
        }
    }
}
