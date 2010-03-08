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
    /// Allows easy nesting of transaction using 'using' statement
    /// Keeps an implicit stack of Transaction objects over the StackTrace of the current Thread
    /// and an explicit ThreadStatic stack of RealTransaction objects.
    /// Usually, just the first Transaccion creates a RealTransaction, but you can create more using 
    /// forceNew = true
    /// All Transaction can cancel but only the one that created the RealTransaction can Commit 
    /// </summary>
    public class Transaction : IDisposable
    {
        [ThreadStatic]
        static Dictionary<BaseConnection, ICoreTransaction> currents;

        bool commited;
        ICoreTransaction coreTransaction; 

        interface ICoreTransaction
        {
            event Action RealCommit;
            SqlConnection Connection { get; }
            SqlTransaction Transaction { get; }
            DateTime Time { get; }
            bool RolledBack { get; }
            bool Started { get; }
            void Rollback();
            void Commit();
            ICoreTransaction Finish();
            void Start();
        }
     
        class FakedTransaction : ICoreTransaction
        {
            ICoreTransaction parent;

            public FakedTransaction(ICoreTransaction parent)
            {
                this.parent = parent;
            }

            public event Action RealCommit
            {
                add { parent.RealCommit += value; }
                remove { parent.RealCommit -= value; }
            }

            public SqlConnection Connection{ get { return parent.Connection; } }
            public SqlTransaction Transaction{ get { return parent.Transaction; } }
            public DateTime Time{ get { return parent.Time;} }
            public bool RolledBack { get{ return parent.RolledBack;} }
            public bool Started { get { return parent.Started; } }

            public void Start() { parent.Start(); }

            public void Rollback()
            {
                parent.Rollback(); 
            }

            public void Commit(){ }

            public ICoreTransaction Finish() { return parent; }
            
        }

        class RealTransaction : ICoreTransaction
        {
            ICoreTransaction parent; 

            public SqlConnection Connection { get; private set; }
            public SqlTransaction Transaction { get; private set; }
            public DateTime Time { get; private set; }
            public bool RolledBack { get; private set; }
            public bool Started { get; private set; }
            public event Action RealCommit;

            IsolationLevel? IsolationLevel;

            public RealTransaction(ICoreTransaction parent, IsolationLevel? isolationLevel)
            {
                IsolationLevel = isolationLevel;
                Time = DateTime.Now;
                this.parent = parent;
            }

            public void Start()
            {
                if (!Started)
                {
                    Connection con = (Connection)ConnectionScope.Current;
                    Connection = new SqlConnection(con.ConnectionString);
                    Connection.Open();
                    Transaction = Connection.BeginTransaction(IsolationLevel ?? con.IsolationLevel);
                    Started = true;
                }
            }

            public void Commit()
            {
                if (Started)
                {
                    Transaction.Commit();
                    if (RealCommit != null)
                        RealCommit();  
                }
            }

            public void Rollback()
            {
                if (Started && !RolledBack)
                {
                    Transaction.Rollback();
                    Debug.WriteLine(Resources.TransactionRollbacked);
                    RolledBack = true;
                }
            }

            public ICoreTransaction Finish()
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

                return parent;
            }
        }

        class NamedTransaction : ICoreTransaction
        {
            ICoreTransaction parent;
            string savePointName;
            public bool RolledBack { get; private set; }
            public bool Started { get; private set; }

            public NamedTransaction(ICoreTransaction parent, string savePointName)
            {
                this.parent = parent;
                this.savePointName = savePointName;
            }

            public event Action RealCommit
            {
                add { parent.RealCommit += value; }
                remove { parent.RealCommit -= value; }
            }

            public SqlConnection Connection { get { return parent.Connection; } }
            public SqlTransaction Transaction { get { return parent.Transaction; } }
            public DateTime Time { get { return parent.Time; } }

            public void Start()
            {
                if (!Started)
                {
                    parent.Start();
                    Transaction.Save(savePointName);
                    Started = true;
                }
            }

            public void Rollback()
            {
                if (Started && !RolledBack)
                {
                    Transaction.Rollback(savePointName);
                    Debug.WriteLine(Resources.TransactionRollbacked);
                    RolledBack = true;
                }
            }

            public void Commit() { }

            public ICoreTransaction Finish() { return parent; }
        }

        class MockRealTransaction : ICoreTransaction
        {
            ICoreTransaction parent;

            public SqlConnection Connection { get{return null;} }
            public SqlTransaction Transaction { get{return null;} }
            public DateTime Time { get; private set; }
            public bool RolledBack { get; private set; }
            public bool Started { get; set; }
            public event Action RealCommit;
            

            public MockRealTransaction(ICoreTransaction parent)
            {
                Time = DateTime.Now;
                this.parent = parent;
            }

            public void Start()
            {
                Started = true;
            }

            public void Commit()
            {
                if (Started)
                {
                    if (RealCommit != null)
                        RealCommit();
                }
            }

            public void Rollback()
            {
                if (Started && !RolledBack)
                {
                    Debug.WriteLine(Resources.TransactionRollbacked);
                    RolledBack = true;
                }
            }

            public ICoreTransaction Finish()
            {
                return parent;
            }
        }

        class MockNamedTransaction : ICoreTransaction
        {
            ICoreTransaction parent;
            string savePointName;
            public bool RolledBack { get; private set; }
            public bool Started { get; private set; }

            public MockNamedTransaction(ICoreTransaction parent, string savePointName)
            {
                this.parent = parent;
                this.savePointName = savePointName;
            }

            public event Action RealCommit
            {
                add { parent.RealCommit += value; }
                remove { parent.RealCommit -= value; }
            }

            public SqlConnection Connection { get { return parent.Connection; } }
            public SqlTransaction Transaction { get { return parent.Transaction; } }
            public DateTime Time { get { return parent.Time; } }

            public void Start()
            {
                if (!Started)
                {
                    parent.Start();
                    Started = true;
                }
            }

            public void Rollback()
            {
                if (Started && !RolledBack)
                {
                    RolledBack = true;
                }
            }

            public void Commit() { }

            public ICoreTransaction Finish() { return parent; }
        }

        public Transaction() : this(false, null) { }

        public Transaction(bool forceNew) : this(forceNew, null) { }

        public Transaction(bool forceNew, IsolationLevel? isolationLevel)
        {
            if (currents == null)
                currents = new Dictionary<BaseConnection, ICoreTransaction>();

            BaseConnection bc = ConnectionScope.Current;

            if (bc == null)
                throw new InvalidOperationException(Resources.NoCurrentConnectionEstablishedUseConnectionScopeDefaultToDoIt);

            ICoreTransaction parent = currents.TryGetC(bc);
            if (parent == null || forceNew)
            {
                if (bc.IsMock)
                    currents[bc] = coreTransaction = new MockRealTransaction(parent);
                else
                    currents[bc] = coreTransaction = new RealTransaction(parent, isolationLevel);
            }
            else
            {
                AssertTransaction();
                currents[bc] = coreTransaction = new FakedTransaction(parent);
            }
        }

        public Transaction(string savePointName)
        {
            if (currents == null)
                currents = new Dictionary<BaseConnection, ICoreTransaction>();

            BaseConnection bc = ConnectionScope.Current;

            if (bc == null)
                throw new InvalidOperationException(Resources.NoCurrentConnectionEstablishedUseConnectionScopeDefaultToDoIt);

            ICoreTransaction parent = GetCurrent();
            if (bc.IsMock)
                currents[bc] = coreTransaction = new MockNamedTransaction(parent, savePointName);
            else
                currents[bc] = coreTransaction = new NamedTransaction(parent, savePointName);
        }

        void AssertTransaction()
        {
            if (GetCurrent().RolledBack)
                throw new InvalidOperationException(Resources.TheTransactionIsRolledBack);
        }

        static ICoreTransaction GetCurrent()
        {
            return currents.GetOrThrow(ConnectionScope.Current, "No Transaction created yet");
        }

        public static event Action RealCommit
        {
            add { GetCurrent().RealCommit += value; }
            remove { GetCurrent().RealCommit -= value; }
        }

        public static bool HasTransaction
        {
            get { return currents != null && currents.ContainsKey(ConnectionScope.Current); }
        }

        public static SqlConnection CurrentConnection
        {
            get
            {
                ICoreTransaction tran = GetCurrent();
                tran.Start();
                return tran.Connection;
            }
        }

        public static SqlTransaction CurrentTransaccion
        {
            get
            {
                ICoreTransaction tran = GetCurrent();
                tran.Start();
                return tran.Transaction;
            }
        }


        public static DateTime StartTime
        {
            get
            {
                ICoreTransaction tran = GetCurrent();
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

            coreTransaction.Commit();

            commited = true;
        }

        public void Dispose()
        {
            if (!commited)
                coreTransaction.Rollback();

            ICoreTransaction parent = coreTransaction.Finish();

            if (parent == null)
                currents.Remove(ConnectionScope.Current);
            else
                currents[ConnectionScope.Current] = parent;
        }
    }
}
