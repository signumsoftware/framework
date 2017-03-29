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
using System.Linq;

namespace Signum.Engine
{
    /// <summary>
    /// Allows easy nesting of transaction by making use of 'using' statement
    /// Keeps an implicit stack of Transaction objects over the call stack of the current Thread
    /// and an explicit stack of RealTransaction objects on thread variable.
    /// Usually, just the first Transaccion creates a RealTransaction, but you can create more using 
    /// forceNew = true
    /// All Transaction can cancel but only the one that created the RealTransaction can Commit 
    /// </summary>
    public class Transaction : IDisposableException
    {
        static readonly Variable<Dictionary<Connector, ICoreTransaction>> currents = Statics.ThreadVariable<Dictionary<Connector, ICoreTransaction>>("transactions", avoidExportImport: true);

        bool commited;
        ICoreTransaction coreTransaction;

        interface ICoreTransaction
        {
            event Action<Dictionary<string, object>> PostRealCommit;
            void CallPostRealCommit();
            event Action<Dictionary<string, object>> PreRealCommit;
            DbConnection Connection { get; }
            DbTransaction Transaction { get; }

            bool Started { get; }

            Exception IsRolledback { get; }
            void Rollback(Exception ex);
            event Action<Dictionary<string, object>> Rolledback;

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
                if (parent != null && parent.IsRolledback != null)
                    throw new InvalidOperationException("The transaction can not be created because a parent transaction is rolled back. Exception:\r\n\t" + parent.IsRolledback.Message, parent.IsRolledback);

                this.parent = parent;
            }

            public event Action<Dictionary<string, object>> PostRealCommit
            {
                add { parent.PostRealCommit += value; }
                remove { parent.PostRealCommit -= value; }
            }

            public event Action<Dictionary<string, object>> PreRealCommit
            {
                add { parent.PreRealCommit += value; }
                remove { parent.PreRealCommit -= value; }
            }

            public DbConnection Connection { get { return parent.Connection; } }
            public DbTransaction Transaction { get { return parent.Transaction; } }
            public Exception IsRolledback { get { return parent.IsRolledback; } }
            public bool Started { get { return parent.Started; } }

            public void Start() { parent.Start(); }

            public void Rollback(Exception ex)
            {
                parent.Rollback(ex);
            }

            public void Commit() { }

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

            public event Action<Dictionary<string, object>> Rolledback
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
            public Exception IsRolledback { get; private set; }
            public bool Started { get; private set; }
            public event Action<Dictionary<string, object>> PostRealCommit;
            public event Action<Dictionary<string, object>> PreRealCommit;
            public event Action<Dictionary<string, object>> Rolledback;

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
                    foreach (var item in PreRealCommit.GetInvocationListTyped())
                    {
                        item(this.UserData);
                        PreRealCommit -= item;
                    }
                }
            }

            public void CallPostRealCommit()
            {
                if (PostRealCommit != null)
                {
                    foreach (var item in PostRealCommit.GetInvocationListTyped())
                    {
                        item(this.UserData);
                    }
                }
            }

            public void Rollback(Exception ex)
            {
                if (Started && IsRolledback == null)
                {
                    Transaction.Rollback();
                    IsRolledback = ex;
                    Rolledback?.Invoke(this.userData);
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
                get { return userData ?? (userData = new Dictionary<string, object>()); }
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
            public Exception IsRolledback { get; private set; }
            public bool Started { get; private set; }
            public event Action<Dictionary<string, object>> PostRealCommit;

            public event Action<Dictionary<string, object>> PreRealCommit;
            public event Action<Dictionary<string, object>> Rolledback;

            public NamedTransaction(ICoreTransaction parent, string savePointName)
            {
                if (parent == null)
                    throw new InvalidOperationException("Named transactions should be nested inside another transaction");

                if (parent != null && parent.IsRolledback != null)
                    throw new InvalidOperationException("The transaction can not be created because a parent transaction is rolled back. Exception:\r\n\t" + parent.IsRolledback.Message, parent.IsRolledback);

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

            public void Rollback(Exception ex)
            {
                if (Started && IsRolledback  == null)
                {
                    Connector.Current.RollbackTransactionPoint(Transaction, savePointName);
                    IsRolledback = ex;
                    Rolledback?.Invoke(this.UserData);
                }
            }

            public void Commit()
            {
            }

            public void CallPostRealCommit()
            {
                if (PreRealCommit != null)
                    foreach (var item in PreRealCommit.GetInvocationListTyped())
                        parent.PreRealCommit += parentUserData => item(this.UserData);

                if (PostRealCommit != null)
                    foreach (var item in PostRealCommit.GetInvocationListTyped())
                        parent.PostRealCommit += parentUserData => item(this.UserData);
            }

            public void Finish() { }

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

        class NoneTransaction : ICoreTransaction
        {
            ICoreTransaction parent;

            public DbConnection Connection { get; private set; }
            public DbTransaction Transaction { get { return null; } }
            public Exception IsRolledback { get; private set; }
            public bool Started { get; private set; }
            public event Action<Dictionary<string, object>> PostRealCommit;
            public event Action<Dictionary<string, object>> PreRealCommit;
            public event Action<Dictionary<string, object>> Rolledback;

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
                        foreach (var item in PreRealCommit.GetInvocationListTyped())
                        {
                            item(this.UserData);
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
                    foreach (var item in PostRealCommit.GetInvocationListTyped())
                    {
                        item(this.UserData);
                    }
                }
            }

            public void Rollback(Exception ex)
            {
                if (Started && IsRolledback == null)
                {
                    //Transaction.Rollback();
                    IsRolledback = ex;
                    Rolledback?.Invoke(this.UserData);
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

        public static event Action<Dictionary<string, object>> PreRealCommit
        {
            add { GetCurrent().PreRealCommit += value; }
            remove { GetCurrent().PreRealCommit -= value; }
        }

        public static event Action<Dictionary<string, object>> Rolledback
        {
            add { GetCurrent().Rolledback += value; }
            remove { GetCurrent().Rolledback -= value; }
        }

        public static Dictionary<string, object> UserData
        {
            get { return GetCurrent().UserData; }
        }

        public static Dictionary<string, object> TopParentUserData()
        {
            return GetCurrent().Follow(a => a.Parent).Last().UserData;
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
            return GetCurrent().Follow(a => a.Parent).ToString(t => "{0} Started : {1} Rollbacked: {2} Connection: {3} Transaction: {4}".FormatWith(
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
            if (coreTransaction.IsRolledback != null)
                throw new InvalidOperationException("The transation is rolled back and can not be commited.");

            coreTransaction.Commit();

            commited = true;
        }

        public void PreRealCommitOnly()
        {
            if (coreTransaction.IsRolledback != null)
                throw new InvalidOperationException("The transation is rolled back and can not be commited.");

            var rt = coreTransaction as RealTransaction;

            if (rt == null)
                throw new InvalidOperationException("This method is meant for testing purposes, and only Real and Test transactions can execute it");

            rt.OnPreRealCommit();
        }

        public void Dispose()
        {
            try
            {
                if (!commited)
                    coreTransaction.Rollback(this.savedException ??
                        new Exception("Unkwnown exception. Consider 'EndUsing' or 'Using' methods instead of 'using' statement.")); //... sqlTransacion.Rollback()

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

        Exception savedException; 

        public void OnException(Exception ex)
        {
            this.savedException = ex;
        }
    }
}
