using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace ShellBoost.Samples.CloudFolderSite.Utilities
{
    public static class SqlExtensions
    {
        public static async Task CreateTableAsync(string connectionString, string tableName, string columns, object parameters = null)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));

            if (tableName == null)
                throw new ArgumentNullException(nameof(tableName));

            if (columns == null)
                throw new ArgumentNullException(nameof(columns));

            var sql = "IF NOT EXISTS (SELECT name from sysobjects WHERE name='" + tableName + "') CREATE TABLE [" + tableName + "] (" + columns + ")";
            await ExecuteNonQueryAsync(connectionString, sql, parameters).ConfigureAwait(false);
            return;
        }

        public static async Task CreateIndexAsync(string connectionString, string indexName, string text, object parameters = null)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));

            if (indexName == null)
                throw new ArgumentNullException(nameof(indexName));

            if (text == null)
                throw new ArgumentNullException(nameof(text));

            var sql = "IF NOT EXISTS (SELECT name from sys.indexes WHERE name='" + indexName + "') " + text;
            await ExecuteNonQueryAsync(connectionString, sql, parameters).ConfigureAwait(false);
            return;
        }

        private static object CoalesceValue(object value)
        {
            if (value == null)
                return null;

            if (value is DateTime dt)
            {
                if (dt < SqlDateTime.MinValue.Value)
                {
                    value = SqlDateTime.MinValue.Value;
                }
                else if (dt > SqlDateTime.MaxValue.Value)
                {
                    value = SqlDateTime.MaxValue.Value;
                }
            }

            if (value is DateTimeOffset dto)
            {
                if (dto < SqlDateTime.MinValue.Value)
                {
                    value = SqlDateTime.MinValue.Value;
                }
                else if (dto > SqlDateTime.MaxValue.Value)
                {
                    value = SqlDateTime.MaxValue.Value;
                }
            }

            return value;
        }

        private static string GetParametersLog(object parameters)
        {
            if (parameters == null)
                return null;

            var dic = new Dictionary<string, object>();
            if (parameters is IEnumerable<KeyValuePair<string, object>> enumerable)
            {
                foreach (var kv in enumerable)
                {
                    dic["@" + kv.Key] = CoalesceValue(kv.Value);
                }
            }
            else
            {
                var type = parameters.GetType();
                foreach (var property in type.GetProperties())
                {
                    dic["@" + property.Name] = CoalesceValue(property.GetValue(parameters));
                }
            }

            return string.Join(", ", dic.Select(kv => kv.Key + " = " + kv.Value));
        }

        private static void AddParameters(SqlCommand cmd, object parameters)
        {
            if (parameters == null)
                return;

            if (parameters is IEnumerable<KeyValuePair<string, object>> enumerable)
            {
                foreach (var kv in enumerable)
                {
                    var p = cmd.CreateParameter();
                    p.ParameterName = "@" + kv.Key;
                    p.Value = CoalesceValue(kv.Value);
                    cmd.Parameters.Add(p);
                }
                return;
            }

            var type = parameters.GetType();
            foreach (var property in type.GetProperties())
            {
                var p = cmd.CreateParameter();
                p.ParameterName = "@" + property.Name;
                p.Value = CoalesceValue(property.GetValue(parameters));
                cmd.Parameters.Add(p);
            }
        }

        public static async Task<int> ExecuteNonQueryAsync(string connectionString, string sql, object parameters = null, ILogger logger = null)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));

            if (sql == null)
                throw new ArgumentNullException(nameof(sql));

            using (var conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync().ConfigureAwait(false);
                using (var cmd = conn.CreateCommand())
                {
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                    cmd.CommandText = sql;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
                    cmd.CommandType = System.Data.CommandType.Text;
                    AddParameters(cmd, parameters);
#if DEBUG
                    logger?.LogTrace(sql + " [" + GetParametersLog(parameters) + "]");
#endif
                    return await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }

        public static async Task<SqlDataReader> ExecuteReaderAsync(string connectionString, string sql, object parameters = null, ILogger logger = null)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));

            if (sql == null)
                throw new ArgumentNullException(nameof(sql));

            var conn = new SqlConnection(connectionString);
            await conn.OpenAsync().ConfigureAwait(false);
            using (var cmd = conn.CreateCommand())
            {
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                cmd.CommandText = sql;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
                cmd.CommandType = System.Data.CommandType.Text;
                AddParameters(cmd, parameters);
#if DEBUG
                logger?.LogTrace(sql + " [" + GetParametersLog(parameters) + "]");
#endif
                return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection).ConfigureAwait(false);
            }
        }

        public static async Task<T> ExecuteScalarAsync<T>(string connectionString, string sql, object parameters = null, ILogger logger = null)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));

            if (sql == null)
                throw new ArgumentNullException(nameof(sql));

            var conn = new SqlConnection(connectionString);
            await conn.OpenAsync().ConfigureAwait(false);
            using (var cmd = conn.CreateCommand())
            {
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                cmd.CommandText = sql;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
                cmd.CommandType = System.Data.CommandType.Text;
                AddParameters(cmd, parameters);
#if DEBUG
                logger?.LogTrace(sql + " [" + GetParametersLog(parameters) + "]");
#endif
                var obj = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                if (Convert.IsDBNull(obj))
                    return default;

                return (T)obj;
            }
        }
    }
}
