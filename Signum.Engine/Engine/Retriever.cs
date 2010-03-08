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
using Signum.Engine.Exceptions;

namespace Signum.Engine
{
    internal class Retriever
    {
        Dictionary<Table, Dictionary<int, IdentifiableEntity>> reqIdentifiables = new Dictionary<Table, Dictionary<int, IdentifiableEntity>>();
        
        // no se garantiza unicidad para lites ni colecciones
        Dictionary<Table, Dictionary<int, List<Lite>>> reqLite = new Dictionary<Table, Dictionary<int, List<Lite>>>(); 
        Dictionary<RelationalTable, Dictionary<int, IList>> reqList = new Dictionary<RelationalTable, Dictionary<int, IList>>();

        internal List<Modifiable> PostRetrieving = new List<Modifiable>();

        internal HashSet<IdentifiableEntity> roots = new HashSet<IdentifiableEntity>(); 

        #region Processing
        public void ProcessAll()
        {
            Action action;
            while ((action = BestAction()) != null)
                action();

            PostRetrieving.ForEach(a => a.PostRetrieving());
            foreach (var ident in PostRetrieving.OfType<IdentifiableEntity>())
                Schema.Current.OnRetrieved(ident, roots.Contains(ident)); 
            PostRetrieving.Clear();
        }

        //Heurística para saber que es mejor hacer antes
        Action BestAction()
        {
            Table bestIdentifiable = reqIdentifiables.WithMax(k => k.Value.Count).Key;
            RelationalTable bestList = reqList.WithMax(k => k.Value.Count).Key;

            if (bestIdentifiable == null)
            {
                if (bestList == null)
                {
                    Table mejorLite = reqLite.WithMax(k => k.Value.Count).Key;
                    if (mejorLite == null)
                        return null;
                    else
                        return () => ProcessLazies(mejorLite);
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
                {
                    int[] ids = array.Except(dt.Rows.Cast<DataRow>().Select(row => (int)row[SqlBuilder.PrimaryKeyName])).ToArray();

                    throw new EntityNotFoundException(table.Type, ids);
                }

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
            Dictionary<int, List<Lite>> dic = reqLite[table];

            while (dic.Count > 0)
            {
                var array = dic.Keys.Take(SqlBuilder.MaxParametersInSQL).ToArray();

                SqlPreCommandSimple preComand = table.BatchSelectLite(array).ToSimple();

                DataTable dt = Executor.ExecuteDataTable(preComand);

                if (array.Length != dt.Rows.Count)
                    throw new InvalidOperationException(Resources.TheProperty0ForType1IsnotFound.Formato(table.Type.Name,
                        array.Except(dt.Rows.Cast<DataRow>().Select(row => (int)row[SqlBuilder.PrimaryKeyName])).ToString(",")));

                foreach (DataRow row in dt.Rows)
                {
                    int id = (int)row[SqlBuilder.PrimaryKeyName];
                    List<Lite> lites = dic[id];
                    foreach (var lite in lites)
                    {
                        table.FillLite(row, lite);
                        PostRetrieving.Add(lite); 
                    }
                    dic.Remove(id);
                }
            }
            reqLite.Remove(table); // se puede borrar pues la inclusion de lites no amplia la corteza
        }

        void ProcessRelationalTable(RelationalTable relationalTable)
        {
            Dictionary<int, IList> dic = reqList.Extract(relationalTable);

            while (dic.Count > 0)
            {
                int[] ids = dic.Keys.Take(SqlBuilder.MaxParametersInSQL).ToArray();

                SqlPreCommandSimple preComand = relationalTable.BatchSelect(ids).ToSimple();

                DataTable dt = Executor.ExecuteDataTable(preComand);
                Dictionary<int, List<DataRow>> rowGroups = dt.Rows.Cast<DataRow>().GroupToDictionary(r => (int)r[relationalTable.BackReference.Name]);
                foreach (var id in ids)
                {
                    IList list = dic[id];
                    List<DataRow> rows = rowGroups.TryGetC(id);
                    if (rows != null)
                    {
                        foreach (DataRow row in rows)
                        {
                            object item = relationalTable.FillItem(row, this);
                            list.Add(item);
                        }
                    }
                    ((Modifiable)list).Modified = false;
                }
                dic.RemoveRange(ids);
            }
        } 
        #endregion

        #region Schema Interface
        public IdentifiableEntity GetIdentifiable(Lite lite, bool isRoot)
        {
            return GetIdentifiable(Schema.Current.Table(lite.RuntimeType), lite.Id, isRoot);
        }


        internal IdentifiableEntity GetIdentifiable(Table table, int id, bool isRoot)
        {
            IdentifiableEntity result = EntityCache.Get(table.Type, id);
            if (result != null) 
                return result;

            return reqIdentifiables.GetOrCreate(table)
                .GetOrCreate(id, () =>
                {
                    var ie = (IdentifiableEntity)table.Constructor();
                    ie.id = id;
                    Schema.Current.OnRetrieving(table.Type, id, isRoot);
                    if (isRoot)
                        roots.Add(ie); 
                    return ie; 
                });
        }

        internal Lite GetLite(Table table, Type liteType, int id)
        {
            IdentifiableEntity ident = EntityCache.Get(table.Type, id);

            if (ident != null) return Lite.Create(liteType, ident);

            Lite req = Lite.Create(liteType, id, table.Type);

            List<Lite> lista = reqLite.GetOrCreate(table).GetOrCreate(id);

            lista.Add(req);

            return req;
        }

        internal IList GetList(RelationalTable table, int id)
        {
            return reqList.GetOrCreate(table).GetOrCreate(id, table.Constructor);
        }

        public List<IdentifiableEntity> UnsafeRetrieveAll(Type type)
        {
            SqlPreCommandSimple spc = Schema.Current.Table(type).SelectAllIDs().ToSimple();

            DataTable dataTable = Executor.ExecuteDataTable(spc);
            Table table = Schema.Current.Table(type);

            return dataTable.Rows.Cast<DataRow>().Select(r =>
                GetIdentifiable(table, (int)r.Cell(SqlBuilder.PrimaryKeyName), true)).ToList();
        }
        #endregion
    }
}
