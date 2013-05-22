using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Signum.Utilities.DataStructures;
using Signum.Utilities;
using System.Diagnostics;
using System.IO;
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
        static readonly Variable<Dictionary<Connector, ICoreTransaction>> currents = Statics.ThreadVariable<Dictionary<Connector, ICoreTransaction>>("transactions");

        bool commited;
        ICoreTransaction coreTransaction; 

        interface ICoreTransaction
        {
            event Action<Dictionary<string, object>> PostRealCommit;
            void CallPostRealCommit();
            event Action PreRealCommit;
            DbConnection Connection { get; }
            DbTransaction Transaction { get; }
           
            bool Started { get; }

            bool IsRolledback { get; }
            void Rollback();
            event Action Rolledback;

            void Commit();
            void Finish();
            void Start();

            ICoreTransaction Parent { get; } 

            Dictionary<string, object> UserData { get; }
        }
     
        class FakedTransaction : ICoreTransaction
        {
            ICoreTransaction parent;

            public FakedTransaction(ICoreTransaction parent)
            {
                if (parent != null && parent.IsRolledback)
                    throw new InvalidOperationException("The transaction can not be created because a parent transaction is rolled back");

                this.parent = parent;
            }

            public event Action<Dictionary<string, object>> PostRealCommit
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
            public bool IsRolledback { get{ return parent.IsRolledback;} }
            public bool Started { get { return parent.Started; } }

            public void Start() { parent.Start(); }

            public void Rollback()
            {
                parent.Rollback(); 
            }

            public void Commit(){ }

            public void Finish() { }

            public Dictionary<string, object> UserData
            {
                get { return parent.UserData; }
            }

            public void CallPostRealCommit()
            {

            }

            public ICoreTransaction Parent
            {
                get { return parent; }
            }

            public event Action Rolledback
            {
                add { parent.Rolledback += value; }
                remove { parent.Rolledback -= value; }
            }
        }

        class RealTransaction : ICoreTransaction
        {
            ICoreTransaction parent; 

            public DbConnection Connection { get; private set; }
            public DbTransaction Transaction { get; private set; }
            public bool IsRolledback { get; private set; }
            public bool Started { get; private set; }
            public event Action<Dictionary<string, object>> PostRealCommit;
            public event Action PreRealCommit;
            public event Action Rolledback;

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

            internal void OnPreRealCommit()
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
                    foreach (Action<Dictionary<string, object>> item in PostRealCommit.GetInvocationList())
                    {
                        item(this.UserData);
                    }
                }
            }

            public void Rollback()
            {
                if (Started && !IsRolledback)
                {
                    Transaction.Rollback();
                    IsRolledback = true;
                    if (Rolledback != null)
                        Rolledback();
                }
            }

            public virtual void Finish()
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

            Dictionary<string, object> userData;
            public Dictionary<string, object> UserData
            {
                get { return userData ?? (userData = new Dictionary<string, object>());  }
            }

            public ICoreTransaction Parent
            {
                get { return parent; }
            }

        }

        class NamedTransaction : ICoreTransaction
        {
            ICoreTransaction parent;
            string savePointName;
            public bool IsRolledback { get; private set; }
            public bool Started { get; private set; }
            public event Action<Dictionary<string, object>> PostRealCommit;
            public event Action PreRealCommit;
            public event Action Rolledback;

            public NamedTransaction(ICoreTransaction parent, string savePointName)
            {
                if (parent != null && parent.IsRolledback)
                    throw new InvalidOperationException("The transaction can not be created because a parent transaction is rolled back");

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
                if (Started && !IsRolledback)
                {
                    Connector.Current.RollbackTransactionPoint(Transaction, savePointName);
                    IsRolledback = true;
                    if (Rolledback != null)
                        Rolledback();
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
                    foreach (Action<Dictionary<string, object>> item in PostRealCommit.GetInvocationList())
                    {
                        item(this.UserData);
                    }
                }
            }

            public void Finish() { }

            public Dictionary<string, object> UserData
            {
                get { return parent.UserData; }
            }

            public ICoreTransaction Parent
            {
                get { return parent; }
        }
        }

        class NoneTransaction : ICoreTransaction
        {
            ICoreTransaction parent;

            public DbConnection Connection { get; private set; }
            public DbTransaction Transaction { get{return null;}}
            public bool IsRolledback { get; private set; }
            public bool Started { get; private set; }
            public event Action<Dictionary<string, object>> PostRealCommit;
            public event Action PreRealCommit;
            public event Action Rolledback;

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
                    foreach (Action<Dictionary<string, object>> item in PostRealCommit.GetInvocationList())
                    {
                        item(this.UserData);
                    }
                }
            }

            public void Rollback()
            {
                if (Started && !IsRolledback)
                {
                    //Transaction.Rollback();
                    IsRolledback = true;
                    if (Rolledback != null)
                        Rolledback();
                }
            }

            public void Finish()
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
            }

            Dictionary<string, object> userData;
            public Dictionary<string, object> UserData
            {
                get { return userData ?? (userData = new Dictionary<string, object>()); }
            }

            public ICoreTransaction Parent
            {
                get { return parent; }
            }
        }

        public static bool InTestTransaction
        {
            get { return inTestTransaction.Value; }
        }

        static readonly Variable<bool> inTestTransaction = Statics.ThreadVariable<bool>("inTestTransaction");

        class TestTransaction : RealTransaction 
        {
            bool oldTestTransaction;
            public TestTransaction(ICoreTransaction parent, IsolationLevel? isolation)
                : base(parent, isolation)
            {
                oldTestTransaction = inTestTransaction.Value;
                inTestTransaction.Value = true;
            }

    
            public override void Finish()
            {
                inTestTransaction.Value = oldTestTransaction;

                base.Finish();
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
            return new Transaction(parent => inTestTransaction.Value ? 
                (ICoreTransaction)new FakedTransaction(parent) : 
                (ICoreTransaction)new RealTransaction(parent, null));
        }

        public static Transaction ForceNew(IsolationLevel? isolationLevel)
        {
            return new Transaction(parent => inTestTransaction.Value ? 
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

        public static event Action<Dictionary<string, object>> PostRealCommit
        {
            add { GetCurrent().PostRealCommit += value; }
            remove { GetCurrent().PostRealCommit -= value; }
        }

        public static event Action PreRealCommit
        {
            add { GetCurrent().PreRealCommit += value; }
            remove { GetCurrent().PreRealCommit -= value; }
        }

        public static event Action Rolledback
        {
            add { GetCurrent().Rolledback += value; }
            remove { GetCurrent().Rolledback -= value; }
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

        public static string CurrentStatus()
        {
            return GetCurrent().FollowC(a => a.Parent).ToString(t => "{0} Started : {1} Rollbacked: {2} Connection: {3} Transaction: {4}".Formato(
                t.GetType().Name,
                t.Started,
                t.IsRolledback,
                t.Connection == null ? "null" : (t.Connection.State.ToString() + " Hash " + t.Connection.GetHashCode()),
                t.Transaction == null ? "null" : (" Hash " + t.Connection.GetHashCode())), "\r\n");
        }

        public T Commit<T>(T returnValue)
        {
            Commit();
            return returnValue;
        }

        public void Commit()
        {
            if (coreTransaction.IsRolledback)
                throw new InvalidOperationException("The transation is rolled back and can not be commited.");

            coreTransaction.Commit();

            commited = true;
        }

        public void Dispose()
        {
            try
            {
            if (!commited)
                    coreTransaction.Rollback(); //... sqlTransacion.Rollback()

                coreTransaction.Finish(); //... sqlTransaction.Dispose() sqlConnection.Dispose()
            }
            finally
            {
                if (coreTransaction.Parent == null)
                currents.Value.Remove(Connector.Current);
            else
                    currents.Value[Connector.Current] = coreTransaction.Parent;
            }

            if (commited)
                coreTransaction.CallPostRealCommit();
        }

        public static void InvokePreRealCommit(Transaction tr)
        {
            ((RealTransaction)tr.coreTransaction).OnPreRealCommit();
        }
    }
}
