using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Data.SqlClient;
using Signum.Entities;
using Signum.Utilities.DataStructures;
using Signum.Engine;
using System.Data;
using System.Collections;
using Signum.Engine.Maps;
using Signum.Engine.Properties;

namespace Signum.Engine
{
    internal class Retriever
    {
        Dictionary<Table, Dictionary<int, IdentifiableEntity>> reqIdentifiables = new Dictionary<Table, Dictionary<int, IdentifiableEntity>>();
        
        // no se garantiza unicidad para lazys ni colecciones
        Dictionary<Table, Dictionary<int, List<Lazy>>> reqLazy = new Dictionary<Table, Dictionary<int, List<Lazy>>>(); 
        Dictionary<RelationalTable, Dictionary<int, IList>> reqList = new Dictionary<RelationalTable, Dictionary<int, IList>>();

        internal List<Modifiable> PostRetrieving = new List<Modifiable>();

        #region Processing
        public void ProcessAll()
        {
            Action action;
            while ((action = BestAction()) != null)
                action();

            PostRetrieving.ForEach(a => a.PostRetrieving());
            Schema.Current.OnRetrieved(PostRetrieving.OfType<IdentifiableEntity>()); 
            PostRetrieving.Clear();
        }

        //Heurística para saber que es mejor hacer antes
        private Action BestAction()
        {
            Table bestIdentifiable = reqIdentifiables.WithMax(k => k.Value.Count).Key;
            RelationalTable bestList = reqList.WithMax(k => k.Value.Count).Key;

            if (bestIdentifiable == null)
            {
                if (bestList == null)
                {
                    Table mejorLazy = reqLazy.WithMax(k => k.Value.Count).Key;
                    if (mejorLazy == null)
                        return null;
                    else
                        return () => ProcessLazies(mejorLazy);
                }
                else
                    return () => ProcessRelationalTable(bestList);
            }
            else
            {
                if (bestList == null)
                    return () => ProcessIdentifiables(bestIdentifiable);
                else
                {
                    if (reqIdentifiables[bestIdentifiable].Count > reqList[bestList].Count)
                        return () => ProcessIdentifiables(bestIdentifiable);
                    else
                        return () => ProcessRelationalTable(bestList);
                }
            }
        }



        void ProcessIdentifiables(Table table)
        {
            Dictionary<int, IdentifiableEntity> dic = reqIdentifiables[table];

            while (dic.Count > 0)
            {
                var array = dic.Keys.Take(SqlBuilder.MaxParametersInSQL).ToArray();
                SqlPreCommandSimple preComand = table.BatchSelect(array).ToSimple();
                DataTable dt = Executor.ExecuteDataTable(preComand);

                if (array.Length != dt.Rows.Count)
                    throw new ApplicationException(Resources.NoSeHanEncontrado0ConId1.Formato(table.Type.Name, 
                        array.Except(dt.Rows.Cast<DataRow>().Select(row => (int)row[SqlBuilder.PrimaryKeyName])).ToString(",")));

                foreach (DataRow row in dt.Rows)
                {
                    int id = (int)row[SqlBuilder.PrimaryKeyName];
                    IdentifiableEntity ei = dic[id];

                    table.Fill(row, ei, this);

                    EntityCache.Add(ei);
                    PostRetrieving.Add(ei);
                    dic.Remove(id);
                }
            }

            //if (dic.Count == 0) //alquien podria haber metido referencias de hermanos
            reqIdentifiables.Remove(table);
        }

        void ProcessLazies(Table table)
        {
            Dictionary<int, List<Lazy>> dic = reqLazy[table];

            while (dic.Count > 0)
            {
                var array = dic.Keys.Take(SqlBuilder.MaxParametersInSQL).ToArray();

                SqlPreCommandSimple preComand = table.BatchSelectLazy(array).ToSimple();

                DataTable dt = Executor.ExecuteDataTable(preComand);

                if (array.Length != dt.Rows.Count)
                    throw new ApplicationException(Resources.TheProperty0ForType1IsnotFound.Formato(table.Type.Name,
                        array.Except(dt.Rows.Cast<DataRow>().Select(row => (int)row[SqlBuilder.PrimaryKeyName])).ToString(",")));

                foreach (DataRow row in dt.Rows)
                {
                    int id = (int)row[SqlBuilder.PrimaryKeyName];
                    List<Lazy> lazys = dic[id];
                    foreach (var lazy in lazys)
                    {
                        table.FillLazy(row, lazy);
                        PostRetrieving.Add(lazy); 
                    }
                    dic.Remove(id);
                }
            }
            reqLazy.Remove(table); // se puede borrar pues la inclusion de lazys no amplia la corteza
        }

        void ProcessRelationalTable(RelationalTable relationalTable)
        {
            Dictionary<int, IList> dic = reqList.Extract(relationalTable);

            while (dic.Count > 0)
            {
                int[] ids = dic.Keys.Take(SqlBuilder.MaxParametersInSQL).ToArray();

                SqlPreCommandSimple preComand = relationalTable.BatchSelect(ids).ToSimple();

                DataTable dt = Executor.ExecuteDataTable(preComand);
                Dictionary<int, List<DataRow>> rows = dt.Rows.Cast<DataRow>().GroupToDictionary(r => (int)r[relationalTable.BackReference.Name]);
                foreach (var gr in rows)
                {
                    int id = gr.Key;
                    IList list = dic[id];
                    foreach (DataRow row in gr.Value)
                    {
                        object item = relationalTable.FillItem(row, this);
                        list.Add(item);
                    }
                    ((Modifiable)list).Modified = false;
                }
                dic.RemoveRange(ids);
            }
        } 
        #endregion

        #region Interface Database
        public IdentifiableEntity Retrieve(Type type, int id)
        {
            return GetIdentifiable(Schema.Current.Table(type), id);
        }

        public IdentifiableEntity Retrieve(Lazy lazy)
        {
            return GetIdentifiable(Schema.Current.Table(lazy.RuntimeType), lazy.Id);
        }

        public List<IdentifiableEntity> RetrieveAll(Type type)
        {
            SqlPreCommandSimple spc = Schema.Current.Table(type).SelectAllIDs().ToSimple();

            DataTable table = Executor.ExecuteDataTable(spc);

            return RetrieveList(type, table.Rows.Cast<DataRow>().Select(r => (int)r.Cell(SqlBuilder.PrimaryKeyName)).ToList());
        }

        public List<Lazy> RetrieveAllLazy(Type type)
        {
            SqlPreCommandSimple spc = Schema.Current.Table(type).SelectAllIDs().ToSimple();

            DataTable table = Executor.ExecuteDataTable(spc);

            return RetrieveListLazy(type, table.Rows.Cast<DataRow>().Select(r => (int)r.Cell(SqlBuilder.PrimaryKeyName)).ToList());
        }

        public List<IdentifiableEntity> RetrieveList(Type type, List<int> list)
        {
            Table table = Schema.Current.Table(type);

            return list.Select(id => GetIdentifiable(table, id)).ToList();
        }

        public List<Lazy> RetrieveListLazy(Type type, List<int> list)
        {
            Table table = Schema.Current.Table(type);

            return list.Select(id => GetLazy(table, type, id)).ToList();
        }
        #endregion

        #region Schema Interface
        public IdentifiableEntity GetIdentifiable(Table table, int id)
        {
            Schema.Current.OnRetrieving(table.Type, id); 

            IdentifiableEntity result = EntityCache.Get(table.Type, id);

            if (result != null) 
                return result;
      
            return reqIdentifiables.GetOrCreate(table)
                .GetOrCreate(id, () =>
                {
                    var ie = (IdentifiableEntity)table.Constructor();
                    ie.id = id;
                    return ie; 
                });
        }

        public Lazy GetLazy(Table table, Type lazyType, int id)
        {
            IdentifiableEntity ident = EntityCache.Get(table.Type, id);

            if (ident != null) return Lazy.Create(lazyType, ident);

            Lazy req = Lazy.Create(lazyType, id, table.Type);

            List<Lazy> lista = reqLazy.GetOrCreate(table).GetOrCreate(id);

            lista.Add(req);

            return req;
        }

        public IList GetList(RelationalTable table, int id)
        {
            return reqList.GetOrCreate(table).GetOrCreate(id, table.Constructor);
        } 
        #endregion
    }
}
