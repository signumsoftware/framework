using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.DataStructures;
using System.Linq.Expressions;
using System.Xml;
using Signum.Utilities;

namespace Signum.Excel
{
    public class OffsetCollection<T> : IEnumerable<T>, IExpressionWriter, IWriter 
        where T: Indexed, IWriter, IExpressionWriter
    {
        public List<T> lista = new List<T>(); 

        public void Add(T element)
        {
            if (element == null)
                return; 
     
            lista.Add(element); 
        }

        public void Add(IEnumerable<T> collection)
        {
            if (collection == null)
                return; 
            foreach (var item in collection)
	        {
		         Add(item);
	        }
        }

        public void UpdateOffsets()
        {
            int currentIndex = 1;
            foreach (var item in lista)
            {
                if (item.Index != 0)
                {
                    if (item.Index < currentIndex)
                        throw new InvalidOperationException("El elemento {0} está establecido en el indice {1} pero vamos por el {2}".Formato(item, item.Index, currentIndex));

                    if (item.Index == currentIndex)
                        item.Offset = 0; // just to be clear

                    if (item.Index > currentIndex)
                    {
                        item.Offset = item.Index - currentIndex;
                        currentIndex = item.Index;
                    }
                }
                currentIndex++; 
            }
        }

        public int UpdateIndices()
        {
            int index = 0;
            foreach (var item in lista)
            {
                if (item.Offset < 0)
                    throw new InvalidOperationException("El offset del elemento {0} es {1}".Formato(item, item.Offset));

                if (item.Offset == 0)
                {
                    index++;
                    item.Index = 0;
                }

                if (item.Offset > 0)
                {
                    index += (item.Offset + 1);
                    item.Index = index;
                }
            }

            return index;
        }


        public IEnumerator<T> GetEnumerator()
        {
            return lista.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator(); 
        }

        public Expression CreateExpression()
        {
            return UtilExpression.ListInit(this);
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (var item in this)
            {
                item.WriteXml(writer);
            }
        }
    }

    public abstract class Indexed
    {
        protected int _index = 0;
        protected int _offset = 0;

        protected internal int Index
        {
            get { return this._index; }
            set { this._index = value; }
        }

        public int Offset
        {
            get { return this._offset; }
            set { this._offset = value; }
        }
    }
}
