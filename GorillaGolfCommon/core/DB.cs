using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace GorillaGolfCommon.core
{
	/// <summary>
	/// Summary description for DB.
	/// </summary>
	public class DB
	{
		private DB()
		{
		}

        public static SqlConnection GetConnection(string dsn)
        {
            SqlConnection conn = null;
            int retryCount = 0;
            while (retryCount < 2)
            {
                try
                {
                    conn = new SqlConnection(dsn);
                    conn.Open();
                    break;
                }
                catch (SqlException)
                {
                    retryCount++;
                    if (retryCount >= 2) throw;
                    GC.Collect();   //-- force closure of unused connections during garbage collection
                }
            }
			return conn;
		}

 public static SqlDataReader ExecSQLQuery(string sql, SqlConnection conn, params string[] args)
		{
			SqlCommand cmd = new SqlCommand(sql);
			cmd.CommandType = CommandType.Text;
			for (int i = 0; i < args.Length - 1; i += 2)
			{
				object arg = args[i+1] == null ? DBNull.Value : (object)args[i+1];
				cmd.Parameters.Add(new SqlParameter(args[i], arg));
			}
			cmd.Connection = conn;
			SqlDataReader rdr = cmd.ExecuteReader(CommandBehavior.Default);
			return rdr;

		}

        public static SqlDataReader ExecSQLQuery(string sql, SqlConnection conn, int timeoutSeconds, params string[] args)
        {
            SqlCommand cmd = new SqlCommand(sql);
            cmd.CommandTimeout = timeoutSeconds;
            cmd.CommandType = CommandType.Text;
            for (int i = 0; i < args.Length - 1; i += 2)
            {
                object arg = args[i + 1] == null ? DBNull.Value : (object)args[i + 1];
                cmd.Parameters.Add(new SqlParameter(args[i], arg));
            }
            cmd.Connection = conn;
            SqlDataReader rdr = cmd.ExecuteReader(CommandBehavior.Default);
            return rdr;

        }

	    public static SqlDataReader ExecSQLQuery(string sql, SqlConnection conn, List<SqlParam> paramList, int timeoutSeconds = -1)
	    {
	        using (SqlCommand cmd = new SqlCommand(sql))
	        {
	            cmd.CommandType = CommandType.Text;
	            cmd.Parameters.AddRange(paramList.Select(x => x.GetParam()).ToArray());
	            cmd.Connection = conn;
	            if (timeoutSeconds >= 0) cmd.CommandTimeout = timeoutSeconds;
	            SqlDataReader rdr = cmd.ExecuteReader(CommandBehavior.Default);
	            return rdr;
	        }
	    }

        public static void ExecSQL(string sql, SqlConnection conn, params string[] args)
		{
			SqlCommand cmd = new SqlCommand(sql);
			cmd.CommandType = CommandType.Text;
			for (int i = 0; i < args.Length - 1; i += 2)
			{
				object arg = args[i+1] == null ? DBNull.Value : (object)args[i+1];
				cmd.Parameters.Add(new SqlParameter(args[i], arg));
			}
			cmd.Connection = conn;
            //-- because of deadlocks retry database updates once. If the second try fails, give up
            int retryCount = 0;
            while (retryCount < 3)
            {
                try
                {
                    cmd.ExecuteNonQuery();
                    break;
                }
                catch (SqlException)
                {
                    retryCount++;
                    if (retryCount >= 3) throw;
                }
            }
		}

	    public static void ExecSQL(string sql, SqlConnection conn, List<SqlParam> paramList, int timeoutSeconds = -1)
	    {
	        using (SqlCommand cmd = new SqlCommand(sql))
	        {
	            cmd.CommandType = CommandType.Text;
	            cmd.Parameters.AddRange(paramList.Select(x => x.GetParam()).ToArray());
	            cmd.Connection = conn;
	            if (timeoutSeconds >= 0) cmd.CommandTimeout = timeoutSeconds;
	            //-- because of deadlocks retry database updates once. If the second try fails, give up
	            int retryCount = 0;
	            while (retryCount < 3)
	            {
	                try
	                {
	                    cmd.ExecuteNonQuery();
	                    break;
	                }
	                catch (SqlException)
	                {
	                    retryCount++;
	                    if (retryCount >= 3) throw;
	                }
	            }
	        }
	    }

        public static void ExecProc(string procname, SqlConnection conn, params string[] args)
		{
			ExecProc(procname, conn, 30, args);
		}

		public static void ExecProc(string procname, SqlConnection conn, int timeoutSeconds, params string[] args)
		{
			SqlCommand cmd = new SqlCommand(procname);
			cmd.CommandType = CommandType.StoredProcedure;
			cmd.Connection = conn;
			cmd.CommandTimeout = timeoutSeconds;
			for (int i = 0; i < args.Length - 1; i += 2)
			{
				cmd.Parameters.Add(new SqlParameter(args[i], args[i+1]));
			}
			cmd.ExecuteNonQuery();
		}

		public static object ExecScalar(string sql, SqlConnection conn, params string[] args)
		{
			SqlCommand cmd = new SqlCommand(sql);
			cmd.CommandType = CommandType.Text;
			for (int i = 0; i < args.Length - 1; i += 2)
			{
				object arg = args[i+1] == null ? DBNull.Value : (object)args[i+1];
				cmd.Parameters.Add(new SqlParameter(args[i], arg));
			}
			cmd.Connection = conn;
			return cmd.ExecuteScalar();

		}

        public static object ExecScalar2(string sql, SqlConnection conn, params object[] args)
        {
            SqlCommand cmd = new SqlCommand(sql);
            cmd.CommandType = CommandType.Text;
            for (int i = 0; i < args.Length - 1; i += 2)
            {
                object arg = args[i + 1] == null ? DBNull.Value : args[i + 1];
                cmd.Parameters.Add(new SqlParameter((string)args[i], arg));
            }
            cmd.Connection = conn;
            return cmd.ExecuteScalar();

        }

        public static SqlDataReader ExecProcQuery(string sql, SqlConnection conn, params string[] args)
		{
			SqlCommand cmd = new SqlCommand(sql);
			cmd.CommandType = CommandType.StoredProcedure;
			for (int i = 0; i < args.Length - 1; i += 2)
			{
				object arg = args[i+1] == null ? DBNull.Value : (object)args[i+1];
				cmd.Parameters.Add(new SqlParameter(args[i], arg));
			}
			cmd.Connection = conn;
			SqlDataReader rdr = cmd.ExecuteReader(CommandBehavior.Default);
			return rdr;

		}
        
        public static string SqlQuote(string instr)
        {
            return "'" + instr.Replace("'", "''") + "'";
        }

        public static string SqlDate(DateTime dt)
        {
            return String.Format("{0}-{1}-{2}", dt.Year, dt.Month, dt.Day);
        }

        public static string SqlDateTime(DateTime dt)
        {
            return String.Format("{0}-{1}-{2} {3}:{4}", dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute);
        }

        public static string SqlDateTimeSec(DateTime dt)
        {
            return String.Format("{0}-{1}-{2} {3}:{4}:{5}.{6}", dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
        }

        public static string SqlList(string[] args)
        {
            if (args.Length == 0) return "''";

            string list = "";
            foreach (string arg in args)
            {
                if (list.Length > 0) list += ",";
                list += SqlQuote(arg);
            }
            return list;
        }

        //-- functions for searching for data across databases
        //-- these functions query each database until one is found containing the requested data; then
        //-- the results are returned.
        public static object ExecScalarAll(string sql, params string[] args)
        {
            string[] dsns = Settings.GetDSNList();
            object result = null;
            foreach (string dsn in dsns)
            {
                using (SqlConnection conn = GetConnection(dsn))
                {
                    result = ExecScalar(sql, conn, args);
                    if (result != null) break;
                }
            }
            return result;
        }
        public class QueryResult {
            public SqlConnection conn;
            public SqlDataReader rdr;
            public void Close()
            {
                rdr.Close();
                conn.Close();
            }
        }

        //-- Execute a query on all databases; return the first one where there are rows
        //-- an open reader and connection are returned, and must be closed by the caller
        public static QueryResult ExecQueryAll(string sql, params string[] args)
        {
            string[] dsns = Settings.GetDSNList();
            QueryResult result = null;
            foreach (string dsn in dsns)
            {
                SqlConnection conn = GetConnection(dsn);
                SqlDataReader rdr = ExecSQLQuery(sql, conn, args);
                if (rdr.HasRows) 
                {
                    result = new QueryResult();
                    result.conn = conn;
                    result.rdr = rdr;
                    break;
                }
                rdr.Close();
                conn.Close();
            }
            return result;
        }

        //-- Execute a statement on all databases
        public static void ExecSQLAll(string sql, params string[] args)
        {
            string[] dsns = Settings.GetDSNList();
            foreach (string dsn in dsns)
            {
                SqlConnection conn = GetConnection(dsn);
                ExecSQL(sql, conn, args);
            }
        }

        public class SqlParam
        {
            public string ParameterName { get; set; }
            public object Value { get; set; }
            public SqlDbType? Type { get; set; }
            public int? Size { get; set; }

            public SqlParam(string name, object val, SqlDbType type, int size)
            {
                ParameterName = name;
                Value = val;
                Type = type;
                Size = size;
            }

            public SqlParam(string name, object val, SqlDbType type)
            {
                ParameterName = name;
                Value = val;
                Type = type;
            }

            public SqlParam(string name, object val)
            {
                ParameterName = name;
                Value = val;
            }

            public SqlParameter GetParam()
            {
                SqlParameter param = new SqlParameter {ParameterName = ParameterName};
                if (Type.HasValue) param.SqlDbType = (SqlDbType)Type;
                if (Size.HasValue) param.Size = (int)Size;
                param.Value = Value ?? DBNull.Value;
                return param;
            }
        }

        public static SqlParameter GetSqlParam(string arg, object value)
        {
            // Explicitly set all .NET strings to sql type of varchar.  
            // Otherwise they get converted to nvarchar which causes a mismatch 
            // in the db and indexes don't get used for those columns
            // that don't have the same type.

            // arg starts with 'N' then make the db type an nvarchar if value is a string
            SqlDbType paramType = SqlDbType.VarChar;
            if (arg.StartsWith("N"))
            {
                arg = arg.Substring(1);
                paramType = SqlDbType.NVarChar;
            }
            SqlParameter param = new SqlParameter(arg, value);
            if (value is string) param.SqlDbType = paramType;
            return param;
        }
    }


}
