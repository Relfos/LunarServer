using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Text;

namespace LunarLabs.WebMVC
{
    public class MySQLDatabase: Database
    {
        private MySqlConnection connection;
        private string currentDB;

	    public MySQLDatabase(SynkContext context) : base(context)
        {
            var server = context.config.GetFieldValue("sqlHost");
            var user = context.config.GetFieldValue("sqlUser");
            var password = context.config.GetFieldValue("sqlPass");

            var connectionString = "SERVER=" + server + ";" + "UID=" + user+ ";" + "PASSWORD=" + password + "; CharSet=utf8;";

            connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();
            }
            catch (MySqlException e)
            {
			    this.fail("Unable to connect to db: "+e.Message);
                return;

            }
        }

        // method overrides
        public override void createDatabase(string dbName)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText =  "CREATE DATABASE IF NOT EXISTS `"+dbName+"`";
            cmd.ExecuteNonQuery();
        }

        public override bool createTable(string dbName, string table, Dictionary<string, string> fields, string key = null)
        {
		    this.selectDatabase(dbName);

		    var query = "";

            if (key == null)
		    {
			    query += "`id` int(10) unsigned NOT NULL AUTO_INCREMENT, ";
			    query += "`insertion_date` int(10) unsigned NOT NULL, ";
			    key = "id";
            }

            foreach (var entry in fields)
		    {
			    query += "`"+entry.Key+"` "+entry.Value+" NOT NULL, ";
            }

		    query = "CREATE TABLE IF NOT EXISTS `" + table+ "` ( " + query+  " PRIMARY KEY(`"+key+"`))  ENGINE = InnoDB DEFAULT CHARSET = utf8;";

            var cmd = connection.CreateCommand();
            cmd.CommandText = query;
            cmd.ExecuteNonQuery();

            return true;
        }

        public override long getCount(string dbName, string table, Condition condition = null)
        {
		    this.selectDatabase(dbName);
		    var query = "SELECT count(*) FROM `"+table+"`";

            if (condition != null)
		    {
			    query += " WHERE " + this.compileCondition(condition);
            }

            var cmd = connection.CreateCommand();
            cmd.CommandText = query;
            return (long)cmd.ExecuteScalar();
        }

        public override Dictionary<string, string> fetchObject(string dbName, string table, Condition condition)
        {
		    this.selectDatabase(dbName);
		    var cond = this.compileCondition(condition);
		    var query = "SELECT * FROM `"+table+"` WHERE "+cond+ " LIMIT 1";

            MySqlCommand cmd = connection.CreateCommand();
            cmd.CommandText = query;

            using (MySqlDataReader dataReader = cmd.ExecuteReader())
            {
                if (!dataReader.Read())
                {
                    return null;
                }

                var row = new Dictionary<string, string>();

                for (int i = 0; i < dataReader.FieldCount; i++)
                {
                    var name = dataReader.GetName(i);
                    var val = dataReader.GetValue(i);
                    row[name] = val.ToString();
                }

                return row;
            }
        }

        public override List<Dictionary<string, string>> fetchAll(string dbName, string table, Condition condition = null, Pagination pagination = null, Sorting sorting = null)
        {
		    this.selectDatabase(dbName);
		    var query = "SELECT * FROM `"+table+"`";

            if (condition != null)
            {
			    var cond = this.compileCondition(condition);
			    query += " WHERE "+ cond;
            }

            if (sorting != null)
            {
                var mode = sorting.mode == SortMode.Ascending  ? "ASC" : "DESC";
			    query += " ORDER BY "+sorting.key+" "+mode;
            }

            if (pagination != null)
		    {
                int ofs = pagination.targetPage * pagination.itemsPerPage;
				query += " LIMIT "+ofs+" , " + pagination.itemsPerPage;
            }

            MySqlCommand cmd = connection.CreateCommand();
            cmd.CommandText = query;

            var rows = new List<Dictionary<string, string>>();
            using (MySqlDataReader dataReader = cmd.ExecuteReader())
            {

                while (dataReader.Read())
                {
                    var row = new Dictionary<string, string>();
                    for (int i = 0; i < dataReader.FieldCount; i++)
                    {
                        var name = dataReader.GetName(i);
                        var val = dataReader.GetValue(i);
                        row[name] = val.ToString();
                    }
                    rows.Add(row);
                }

            }

            return rows;
        }

        public override void deleteAll(string dbName, string table, Condition condition)
        {
		    this.selectDatabase(dbName);
		    var query = "DELETE FROM " + table;
            if (condition != null)
            {
			    var cond = this.compileCondition(condition);
			    query += " WHERE "+cond;
            }

            var cmd = connection.CreateCommand();
            cmd.CommandText = query;
            cmd.ExecuteNonQuery();
        }

        public void selectDatabase(string name)
        {
            if (this.currentDB != null && this.currentDB.Equals(name))
		    {   
                return;
            }

	    	this.currentDB = name;

            var cmd = connection.CreateCommand();
            cmd.CommandText = "USE " + name + ";";
            cmd.ExecuteNonQuery();
        }

        public override bool saveObject(string dbName, string table, Dictionary<string, string> fields, string key, string value)
        {
		    var query = "";
		    var i = 0;
            foreach (var entry in fields)
		    {
                if (i > 0)
			    {
				    query += ", ";
                }

			    var fieldValue = this.encodeField(entry.Value);
			    query += "`"+ entry.Key +"`="+ fieldValue;
			    i++;
            }

            this.selectDatabase(dbName);
		    value = this.encodeField(value);
		    query = "UPDATE "+table+" SET "+query+" WHERE "+key+" = "+value;

            var cmd = connection.CreateCommand();
            cmd.CommandText = query;
            cmd.ExecuteNonQuery();

            return true;
        }

        public override long insertObject(string dbName, string table, Dictionary<string, string> fields)
        {
		    var fieldList = "";
		    var valueList = "";
		    var i = 0;

            foreach (var entry in fields)
		    {
                if (i > 0)
			    {
				    fieldList += ", ";
				    valueList += ", ";
                }

			    fieldList += "`"+entry.Key+"`";
			    valueList += this.encodeField(entry.Value);

			    i++;
            }

		    this.selectDatabase(dbName);
		    var query = "INSERT INTO `"+table+"` ("+fieldList+") VALUES("+valueList+")";
		    
            MySqlCommand dbcmd = this.connection.CreateCommand();
            dbcmd.CommandText = query;
            dbcmd.ExecuteNonQuery();
            return dbcmd.LastInsertedId;
        }

	    private string encodeField(string value)
	    {
            if (value.Equals("false") || value.Equals("true"))
            {
                return value;
            }

            Int64 temp;
		    if (Int64.TryParse(value, out temp))
		    {
			    return value;
		    }

		    //value = mysqli_real_escape_string(this.client, value);

		    return "'"+value+"'";
    	}

        private void compileCondition(Condition condition, StringBuilder sb)
        {
            if (condition.op == Condition.Operator.And || condition.op == Condition.Operator.Or)
            {
                sb.Append('(');
                compileCondition(condition.childA, sb);
                sb.Append(' ');
                sb.Append(condition.op);
                sb.Append(' ');
                compileCondition(condition.childB, sb);
                sb.Append(')');
                return;
            }

            string op = null;
            string value = condition.opValue;

            switch (condition.op)
            {
                case Condition.Operator.Contains:
                    {
                        op = "like";
                        value = "%" + value + "%";
                        break;
                    }

                case Condition.Operator.BeginsWith:
                    {
                        op = "like";
                        value = value + "%";
                        break;
                    }

                case Condition.Operator.EndsWith:
                    {
                        op = "like";
                        value = "%" + value;
                        break;
                    }

                case Condition.Operator.Equals : op = "="; break;
                case Condition.Operator.LessThan: op = "<"; break;
                case Condition.Operator.GreaterThan: op = ">"; break;
                case Condition.Operator.LessOrEqualThan: op = "<="; break;
                case Condition.Operator.GreaterOrEqualThan: op = ">="; break;
                case Condition.Operator.NotEqual: op = "<>"; break;
            }

            value = this.encodeField(value);

            sb.Append('`');
            sb.Append(condition.fieldName);
            sb.Append('`');
            sb.Append(' ');
            sb.Append(op);
            sb.Append(' ');
            sb.Append(value);
        }


        private string compileCondition(Condition condition)
	    {
		    var result = new StringBuilder();
            compileCondition(condition, result);
		    return result.ToString();
	    }
    }
}
