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
                int[] requested = dic.Keys.Take(SqlBuilder.MaxParametersInSQL).ToArray();
                List<int> found = new List<int>(requested.Length);
                SqlPreCommandSimple preComand = table.BatchSelect(requested).ToSimple();
                Executor.ExecuteDataReader(preComand, reader =>
                {
                    int id = (int)reader.GetInt32(0); //SqlBuilder.PrimaryKeyName
                    IdentifiableEntity ie = dic[id];

                    table.Fill(ie, reader, this);

                    ie.Modified = null;
                    ie.IsNew = false; 

                    EntityCache.Add(ie);
                    PostRetrieving.Add(ie);
                    found.Add(id);
                    dic.Remove(id);
                });

                if (found.Count != requested.Length)
                    throw new EntityNotFoundException(table.Type, requested.Except(found).ToArray());
            }

            //if (dic.Count == 0) //alquien podria haber metido referencias de hermanos
            reqIdentifiables.Remove(table);
        }

        void ProcessLazies(Table table)
        {
            Dictionary<int, List<Lite>> dic = reqLite[table];

            while (dic.Count > 0)
            {
                int[] requested = dic.Keys.Take(SqlBuilder.MaxParametersInSQL).ToArray();
                List<int> found = new List<int>(requested.Length);
                SqlPreCommandSimple preComand = table.BatchSelectLite(requested).ToSimple();

                Executor.ExecuteDataReader(preComand, reader =>
                {
                    int id = (int)reader.GetInt32(0); //SqlBuilder.PrimaryKeyName;
                    List<Lite> lites = dic[id];
                    foreach (var lite in lites)
                    {
                        lite.ToStr = reader.GetString(1);
                        lite.Modified = null;
                        PostRetrieving.Add(lite);
                    }
                    dic.Remove(id);
                    found.Add(id);
                });

                if (found.Count != requested.Length)
                    throw new EntityNotFoundException(table.Type, requested.Except(found).ToArray());
            }
            reqLite.Remove(table); // se puede borrar pues la inclusion de lites no amplia la corteza
        }

        void ProcessRelationalTable(RelationalTable relationalTable)
        {
            Dictionary<int, IList> dic = reqList.Extract(relationalTable);

            while (dic.Count > 0)
            {
                int[] requested = dic.Keys.Take(SqlBuilder.MaxParametersInSQL).ToArray();
                SqlPreCommandSimple preComand = relationalTable.BatchSelect(requested).ToSimple();

                Executor.ExecuteDataReader(preComand, reader =>
                {
                    int id = reader.GetInt32(1);
                    IList list = dic[id];

                    relationalTable.AddInList(list, reader, this);
                });

                foreach (var id in requested)
                {
                    ((Modifiable)dic[id]).Modified = null;
                }

                dic.RemoveRange(requested);
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
