using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Signum.Utilities.DataStructures;
using Signum.Utilities;
using System.Diagnostics;
using System.IO;
using Signum.Engine.Properties;
using Signum.Entities;
using System.Data.Common;

namespace Signum.Engine
{
    /// <summary>
    /// Allows easy nesting of transaction using 'using' statement
    /// Keeps an implicit stack of Transaction objects over the StackTrace of the current Thread
    /// and an explicit stack of RealTransaction objects on thread variable.
    /// Usually, just the first Transaccion creates a RealTransaction, but you can create more using 
    /// forceNew = true
    /// All Transaction can cancel but only the one that created the RealTransaction can Commit 
    /// </summary>
    public class Transaction : IDisposable
    {
        public static Action<string, StackTrace> UnexpectedBehaviourCallback { get; set; }

        static Transaction()
        {
            UnexpectedBehaviourCallback = (msg, st) => Debug.WriteLine(msg); 
        }

        static void NotifyRollback()
        {
            UnexpectedBehaviourCallback("TRANSACTION ROLLBACKED!", new StackTrace(2, true));
        }

        static readonly Variable<Dictionary<Connector, ICoreTransaction>> currents = Statics.ThreadVariable<Dictionary<Connector, ICoreTransaction>>("transactions");

        bool commited;
        ICoreTransaction coreTransaction; 

        interface ICoreTransaction
        {
            event Action PostRealCommit;
            void CallPostRealCommit();
            event Action PreRealCommit;
            DbConnection Connection { get; }
            DbTransaction Transaction { get; }
            bool RolledBack { get; }
            bool Started { get; }
            void Rollback();
            void Commit();
            ICoreTransaction Finish();
            void Start();

            Dictionary<string, object> UserData { get; }
        }
     
        class FakedTransaction : ICoreTransaction
        {
            ICoreTransaction parent;

            public FakedTransaction(ICoreTransaction parent)
            {
                if (parent != null && parent.RolledBack)
                    throw new InvalidOperationException("The transation can not be created because a parent transaction is rolled back");

                this.parent = parent;
            }

            public event Action PostRealCommit
            {
                add { parent.PostRealCommit += value; }
                remove { parent.PostRealCommit -= value; }
            }

            public event Action PreRealCommit
            {
                add { parent.PreRealCommit += value; }
                remove { parent.PreRealCommit -= value; }
            }

            public DbConnection Connection{ get { return parent.Connection; } }
            public DbTransaction Transaction{ get { return parent.Transaction; } }
            public bool RolledBack { get{ return parent.RolledBack;} }
            public bool Started { get { return parent.Started; } }

            public void Start() { parent.Start(); }

            public void Rollback()
            {
                parent.Rollback(); 
            }

            public void Commit(){ }

            public ICoreTransaction Finish() { return parent; }

            public Dictionary<string, object> UserData
            {
                get { return parent.UserData; }
            }

            public void CallPostRealCommit()
            {

            }
        }

        class RealTransaction : ICoreTransaction
        {
            ICoreTransaction parent; 

            public DbConnection Connection { get; private set; }
            public DbTransaction Transaction { get; private set; }
            public bool RolledBack { get; private set; }
            public bool Started { get; private set; }
            public event Action PostRealCommit;
            public event Action PreRealCommit;

            IsolationLevel? IsolationLevel;

            public RealTransaction(ICoreTransaction parent, IsolationLevel? isolationLevel)
            {
                IsolationLevel = isolationLevel;
                this.parent = parent;
            }

            public void Start()
            {
                if (!Started)
                {
                    Connection = Connector.Current.CreateConnection();
                    
                    Connection.Open();
                    Transaction = Connection.BeginTransaction(IsolationLevel ?? Connector.Current.IsolationLevel);
                    Started = true;
                }
            }

            public virtual void Commit()
            {
                if (Started)
                {
                    OnPreRealCommit();

                    Transaction.Commit();
                }
            }

            protected void OnPreRealCommit()
            {
                while (PreRealCommit != null)
                {
                    foreach (Action item in PreRealCommit.GetInvocationList())
                    {
                        item();
                        PreRealCommit -= item;
                    }
                }
            }

            public void CallPostRealCommit()
            {
                if (PostRealCommit != null)
                {
                    foreach (Action item in PostRealCommit.GetInvocationList())
                    {
                        item();
                    }
                }
            }

            public void Rollback()
            {
                if (Started && !RolledBack)
                {
                    Transaction.Rollback();
                    NotifyRollback();
                    RolledBack = true;
                }
            }

            public virtual ICoreTransaction Finish()
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

            Dictionary<string, object> userData;
            public Dictionary<string, object> UserData
            {
                get { return userData ?? (userData = new Dictionary<string, object>());  }
            }
        }

        class NamedTransaction : ICoreTransaction
        {
            ICoreTransaction parent;
            string savePointName;
            public bool RolledBack { get; private set; }
            public bool Started { get; private set; }
            public event Action PostRealCommit;
            public event Action PreRealCommit;

            public NamedTransaction(ICoreTransaction parent, string savePointName)
            {
                if (parent != null && parent.RolledBack)
                    throw new InvalidOperationException("The transation can not be created because a parent transaction is rolled back");

                this.parent = parent;
                this.savePointName = savePointName;
            }

        
            public DbConnection Connection { get { return parent.Connection; } }
            public DbTransaction Transaction { get { return parent.Transaction; } }
            
            public void Start()
            {
                if (!Started)
                {
                    parent.Start();
                    Connector.Current.SaveTransactionPoint(Transaction, savePointName);
                    Started = true;
                }
            }

            public void Rollback()
            {
                if (Started && !RolledBack)
                {
                    Connector.Current.RollbackTransactionPoint(Transaction, savePointName);
                    NotifyRollback();
                    RolledBack = true;
                }
            }

            public void Commit() 
            {
                while (PreRealCommit != null)
                {
                    foreach (Action item in PreRealCommit.GetInvocationList())
                    {
                        item();
                        PreRealCommit -= item;
                    }
                }
            }

            public void CallPostRealCommit()
            {
                if (PostRealCommit != null)
                {
                    foreach (Action item in PostRealCommit.GetInvocationList())
                    {
                        item();
                    }
                }
            }

            public ICoreTransaction Finish() { return parent; }

            public Dictionary<string, object> UserData
            {
                get { return parent.UserData; }
            }
        }

        class NoneTransaction : ICoreTransaction
        {
            ICoreTransaction parent;

            public DbConnection Connection { get; private set; }
            public DbTransaction Transaction { get{return null;}}
            public bool RolledBack { get; private set; }
            public bool Started { get; private set; }
            public event Action PostRealCommit;
            public event Action PreRealCommit;

            public NoneTransaction(ICoreTransaction parent)
            {
                this.parent = parent;
            }

            public void Start()
            {
                if (!Started)
                {
                    Connection = Connector.Current.CreateConnection();

                    Connection.Open();
                    //Transaction = Connection.BeginTransaction(IsolationLevel ?? con.IsolationLevel);
                    Started = true;
                }
            }

            public void Commit()
            {
                if (Started)
                {
                    while (PreRealCommit != null)
                    {
                        foreach (Action item in PreRealCommit.GetInvocationList())
                        {
                            item();
                            PreRealCommit -= item;
                        }
                    }

                    //Transaction.Commit();
                }
            }

            public void CallPostRealCommit()
            {
                if (PostRealCommit != null)
                {
                    foreach (Action item in PostRealCommit.GetInvocationList())
                    {
                        item();
                    }
                }
            }

            public void Rollback()
            {
                if (Started && !RolledBack)
                {
                    //Transaction.Rollback();
                    NotifyRollback();
                    RolledBack = true;
                }
            }

            public ICoreTransaction Finish()
            {
                //if (Transaction != null)
                //{
                //    Transaction.Dispose();
                //    Transaction = null;
                //}

                if (Connection != null)
                {
                    Connection.Dispose();
                    Connection = null;
                }

                return parent;
            }

            Dictionary<string, object> userData;
            public Dictionary<string, object> UserData
            {
                get { return userData ?? (userData = new Dictionary<string, object>()); }
            }
        }

        public static bool AvoidIndependentTransactions
        {
            get { return avoidIndependentTransactions.Value; }
        }

        static readonly Variable<bool> avoidIndependentTransactions = Statics.ThreadVariable<bool>("avoidIndependentTransactions");

        class TestTransaction : RealTransaction 
        {
            public TestTransaction(ICoreTransaction parent, IsolationLevel? isolation)
                : base(parent, isolation)
            {
                avoidIndependentTransactions.Value = true;
            }

    
            public override ICoreTransaction Finish()
            {
                avoidIndependentTransactions.Value = false;

                return base.Finish();
            }
        }

        public Transaction()
            : this(parent => parent == null ?
                (ICoreTransaction)new RealTransaction(parent, null) :
                (ICoreTransaction)new FakedTransaction(parent))
        {
        }

        Transaction(Func<ICoreTransaction, ICoreTransaction> factory)
        {
            if (currents.Value == null)
                currents.Value = new Dictionary<Connector, ICoreTransaction>();

            Connector bc = Connector.Current;

            if (bc == null)
                throw new InvalidOperationException("ConnectionScope.Current not established. Use ConnectionScope.Default to set it.");

            ICoreTransaction parent = currents.Value.TryGetC(bc);

            currents.Value[bc] = coreTransaction = factory(parent);
        }

        public static Transaction None()
        {
            return new Transaction(parent => new NoneTransaction(parent));
        }

        public static Transaction NamedSavePoint(string savePointName)
        {
            return new Transaction(parent => new NamedTransaction(parent, savePointName));
        }

        public static Transaction ForceNew()
        {
            return new Transaction(parent => avoidIndependentTransactions.Value ? 
                (ICoreTransaction)new FakedTransaction(parent) : 
                (ICoreTransaction)new RealTransaction(parent, null));
        }

        public static Transaction ForceNew(IsolationLevel? isolationLevel)
        {
            return new Transaction(parent => avoidIndependentTransactions.Value ? 
                (ICoreTransaction)new FakedTransaction(parent) : 
                (ICoreTransaction)new RealTransaction(parent, isolationLevel));
        }

        public static Transaction Test()
        {
            return new Transaction(parent => new TestTransaction(parent, null));
        }

        public static Transaction Test(IsolationLevel? isolationLevel)
        {
            return new Transaction(parent => new TestTransaction(parent, isolationLevel));
        }
    
        static ICoreTransaction GetCurrent()
        {
            return currents.Value.GetOrThrow(Connector.Current, "No Transaction created yet");
        }

        public static event Action PostRealCommit
        {
            add { GetCurrent().PostRealCommit += value; }
            remove { GetCurrent().PostRealCommit -= value; }
        }

        public static event Action PreRealCommit
        {
            add { GetCurrent().PreRealCommit += value; }
            remove { GetCurrent().PreRealCommit -= value; }
        }

        public static Dictionary<string, object> UserData
        {
            get { return GetCurrent().UserData; }
        }

        public static bool HasTransaction
        {
            get { return currents.Value != null && currents.Value.ContainsKey(Connector.Current); }
        }

        public static DbConnection CurrentConnection
        {
            get
            {
                ICoreTransaction tran = GetCurrent();
                tran.Start();
                return tran.Connection;
            }
        }

        public static DbTransaction CurrentTransaccion
        {
            get
            {
                ICoreTransaction tran = GetCurrent();
                tran.Start();
                return tran.Transaction;
            }
        }

        public T Commit<T>(T returnValue)
        {
            Commit();
            return returnValue;
        }

        public void Commit()
        {
            if (coreTransaction.RolledBack)
                throw new InvalidOperationException("The transation is rolled back and can not be commited.");

            coreTransaction.Commit();

            commited = true;
        }

        public void Dispose()
        {
            if (!commited)
                coreTransaction.Rollback();

            ICoreTransaction parent = coreTransaction.Finish();

            if (parent == null)
                currents.Value.Remove(Connector.Current);
            else
                currents.Value[Connector.Current] = parent;

            if (commited)
                coreTransaction.CallPostRealCommit();
        }
    }
}
