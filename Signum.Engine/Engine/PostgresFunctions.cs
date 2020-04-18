using Microsoft.SqlServer.Server;
using System;
using System.Linq;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
namespace Signum.Engine.PostgresCatalog
{
    public class PostgresFunctions
    {
        [SqlMethod(Name = "pg_catalog.string_to_array")]
        public static string[] string_to_array(string str, string separator) => throw new NotImplementedException();

        [SqlMethod(Name = "pg_catalog.encode")]
        public static string encode(byte[] bytea, string format) => throw new NotImplementedException();

        [SqlMethod(Name = "pg_catalog.pg_get_expr")]
        public static string pg_get_expr(string adbin, int adrelid) => throw new NotImplementedException();

        [SqlMethod(Name = "pg_catalog.pg_get_viewdef")]
        public static string pg_get_viewdef(int oid) => throw new NotImplementedException();

        [SqlMethod(Name = "pg_catalog.pg_get_functiondef")]
        public static string pg_get_functiondef(int oid) => throw new NotImplementedException();

        [SqlMethod(Name = "information_schema._pg_char_max_length")]
        public static int? _pg_char_max_length(int atttypeid, int atttypmod) => throw new NotImplementedException();

        [SqlMethod(Name = "pg_catalog.unnest")]
        public static IQueryable<T> unnest<T>(T[] array) => throw new NotImplementedException();

        [SqlMethod(Name = "pg_catalog.generate_series")]
        public static IQueryable<int> generate_series(int start, int stopIncluded) => throw new NotImplementedException();
        
        [SqlMethod(Name = "pg_catalog.generate_series")]
        public static IQueryable<int> generate_series(int start, int stopIncluded, int step) => throw new NotImplementedException();

        [SqlMethod(Name = "pg_catalog.generate_subscripts")]
        public static IQueryable<int> generate_subscripts(Array array, int dimension) => throw new NotImplementedException();

        [SqlMethod(Name = "pg_catalog.generate_subscripts")]
        public static IQueryable<int> generate_subscripts(Array array, int dimension, bool reverse) => throw new NotImplementedException();

        [SqlMethod(Name = "pg_catalog.pg_total_relation_size")]
        public static int pg_total_relation_size(int oid) => throw new NotImplementedException();
    }

}
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
