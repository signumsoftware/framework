using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using Signum.Utilities.DataStructures;
using Signum.Utilities;
using System.Diagnostics;
using System.IO;
using Signum.Engine.Properties;

namespace Signum.Engine
{
    /// <summary>
    /// Allows easy nesting of transaction 
    /// keeping una pila implícita de objetos Transaccion apoyada sobre la pila del hilo
    /// y una pila explicita thread static de transacciones reales.
    /// Por lo general solo la primera Transaccion genera una TransaccionReal, a no ser que se cree 
    /// otra transaccion interior con forzarNueva = true
    /// Todas pueden cancelar pero solo la Transaccion que ha creado una TransaccionReal puede confirmar
    /// </summary>
    public class Transaction : IDisposable
    {
        [ThreadStatic]
        static StackDictionary<BaseConnection, RealTransaction> reals;

        bool faked;
        bool confirmed;

        class RealTransaction
        {
            public SqlConnection Connection;
            public SqlTransaction Transaction;
            public IsolationLevel? IsolationLevel;
            public DateTime Time;
            public bool RolledBack = false;

            public RealTransaction(IsolationLevel? isolationLevel)
            {
                IsolationLevel = isolationLevel;
                Time = DateTime.Now;
            }

            public void Start()
            {
                if (Connection == null)
                {
                    Connection con = (Connection)ConnectionScope.Current;
                    Connection = new SqlConnection(con.ConnectionString);
                    Connection.Open();
                    Transaction = Connection.BeginTransaction(IsolationLevel ?? con.IsolationLevel);
                }
            }

            public void Commit()
            {
                if (Connection != null &&
                    Transaction != null &&
                    Connection.State == ConnectionState.Open)
                {
                    Transaction.Commit();
                    if (RealCommit != null)
                        RealCommit();  
                }
            }

            public void Rollback()
            {
                if (Transaction != null && Transaction.Connection != null && !RolledBack)
                {
                    Transaction.Rollback();
                    Debug.WriteLine(Resources.TransactionRollbacked);
                    RolledBack = true;
                }
            }

            public void Finish()
            {
                if (Transaction != null)
                {
                    Transaction.Dispose();
                    Transaction = null;
                }

                if (Connection != null)
                {
                    Connection.Dispose();
                    Connection = null;
                }
            }

            public event Action RealCommit; 
        }

        public Transaction() : this(false, null) { }

        public Transaction(bool forceNew) : this(forceNew, null) { }

        public Transaction(IsolationLevel isolationLevel) : this(false, isolationLevel) { }

        public Transaction(bool forceNew, IsolationLevel? isolationLevel)
        {
            if (reals == null)
                reals = new StackDictionary<BaseConnection, RealTransaction>();

            BaseConnection bc = ConnectionScope.Current;

            if (bc == null)
                throw new ApplicationException(Resources.NoCurrentConnectionEstablishedUseConnectionScopeDefaultToDoIt);

            if ((reals.Count(bc) == 0 || forceNew) && !bc.IsMock)
                reals.Push(bc, new RealTransaction(isolationLevel));
            else
            {
                AssertTransaction();
                faked = true;
            }
        }

        public static event Action RealCommit
        {
            add { reals.Peek(ConnectionScope.Current).RealCommit += value; }
            remove { reals.Peek(ConnectionScope.Current).RealCommit -= value; }
        }

        public static bool HasTransaction
        {
            get { return reals != null && reals.Count(ConnectionScope.Current) > 0; }
        }

        public static SqlConnection CurrentConnection
        {
            get
            {
                RealTransaction tran = reals.Peek(ConnectionScope.Current);
                tran.Start();
                return tran.Connection;
            }
        }

        public static SqlTransaction CurrentTransaccion
        {
            get
            {
                RealTransaction tran = reals.Peek(ConnectionScope.Current);
                tran.Start();
                return tran.Transaction;
            }
        }


        public static DateTime StartTime
        {
            get
            {
                RealTransaction tran = reals.Peek(ConnectionScope.Current);
                return tran.Time;
            }
        }

        public T Commit<T>(T returnValue)
        {
            Commit();
            return returnValue;
        }

        public void Commit()
        {
            AssertTransaction();

            if (!faked)
                reals.Peek(ConnectionScope.Current).Commit();

            confirmed = true;
        }

        public void Dispose()
        {
            if (!confirmed)
                reals.Peek(ConnectionScope.Current).Rollback();

            if (!faked)
                reals.Pop(ConnectionScope.Current).Finish();
        }

        void AssertTransaction()
        {
            if (reals.Peek(ConnectionScope.Current).RolledBack)
                throw new InvalidOperationException(Resources.TheTransactionIsRolledBack);
        }
    }

    public class StackDictionary<K, V>
    {
        Dictionary<K, Stack<V>> dictionary = new Dictionary<K, Stack<V>>();

        public void Push(K key, V value)
        {
            dictionary.GetOrCreate(key).Push(value);
        }

        public V Pop(K key)
        {
            Stack<V> stack = GetStack(key);

            var result = stack.Pop();

            if (stack.Count == 0)
                dictionary.Remove(key);

            return result;
        }


        public V Peek(K key)
        {
            return GetStack(key).Peek();
        }

        public int Count(K key)
        {
            Stack<V> stack = dictionary.TryGetC(key);

            return stack.TryCS(s => s.Count) ?? 0;
        }

        private Stack<V> GetStack(K key)
        {
            Stack<V> stack = dictionary.TryGetC(key);

            if (stack == null)
                throw new InvalidOperationException(Resources.NoStackFoundForThisKey);
            return stack;
        }
    }

}
