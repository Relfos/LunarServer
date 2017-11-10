using SynkMVC.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynkMVC
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class EntityAttribute : Attribute
    {
    }

    [Entity]
    public abstract class Entity
    {
        //$entity_init = array();

        public Int64 id = 0;
        public long insertion_date = 0;
        public string tableName = null;
        public string dbName = null;
        public Dictionary<string, Field> _fields = new Dictionary<string, Field>();

        public IEnumerable<Field> fields { get { return _fields.Values; } }

        public bool exists = false;
        public bool isWritable = true;

        private Dictionary<string, string> _currentValues = new Dictionary<string, string>();
        private Dictionary<string, string> _originalValues = new Dictionary<string, string>();

        public Dictionary<string, string> values { get { return _currentValues; }}        

        public string className { get; private set; }

        public abstract void InitFields();

        public void InitFromContext(SynkContext context)
        {
            className = this.GetType().Name.ToLower();

            InitFields();
            context.site.ProcessEntitySchema(this);

            if (tableName == null)
            {
                tableName = className + "_data";
            }

            if (dbName == null)
            {
                dbName = context.config.GetFieldValue("database");
            }

            insertion_date = DateTime.Now.ToTimestamp();

            foreach (var field in this.fields)
            {
                if (!field.writable)
                {
                    continue;
                }

                var fieldName = field.name;
                var defaultValue = field.GetDefaultValue(context);
                this._currentValues[fieldName] = defaultValue;
                this._originalValues[fieldName] = defaultValue;
            }
        }

        public bool GetFieldBool(string fieldName)
        {
            var val = GetFieldValue(fieldName);
            return val.Equals("1") || val.Equals("true");
        }

        public string GetFieldValue(string fieldName)
        {
            if (_currentValues.ContainsKey(fieldName))
            {
                return _currentValues[fieldName];
            }
            return null;
        }

        public string GetFieldValue(Field field)
        {
            return GetFieldValue(field.name);
        }

        public void SetFieldValue(Field field, string value)
        {
            SetFieldValue(field.name, value);
        }

        public void SetFieldValue(string fieldName, string value)
        {
            _currentValues[fieldName] = value;
        }

        public Dictionary<string, string> GetFields(bool skipHidden = false)
        {
            var result = new Dictionary<string, string>();
            foreach (var field in _fields.Values)
		    {
                if (skipHidden && field.hidden) {
                    continue;
                }

                if (!field.writable) {
                    continue;
                }

			    var fieldName = field.name;
			    var fieldValue = GetFieldValue(fieldName);

    		    result[fieldName] = string.IsNullOrEmpty(fieldValue) ? "" : fieldValue;
            }

            return result;
        }

        public bool LoadFromRow(SynkContext context, Dictionary<string, string> row)
        {
            if (row == null)
            {
                return false;
            }

            Int64.TryParse(row["id"],  out this.id);
            long.TryParse(row["insertion_date"], out this.insertion_date );

            foreach (var field in _fields.Values)
            {
                var fieldName = field.name;

                if (row.ContainsKey(fieldName))
                {
                    var rowVal = row[fieldName];
                    if (field.dbType.Contains("tinyint"))
                    {
                        rowVal = rowVal.Equals("true", StringComparison.InvariantCultureIgnoreCase) ? "1" : "0";
                    }
                    SetFieldValue(fieldName, rowVal);
                }
                else
                {
                    SetFieldValue(fieldName, field.GetDefaultValue(context));
                }

                _originalValues[fieldName] = GetFieldValue(fieldName);
            }

		    this.exists = true;
            return true;
        }

        public void Expand(SynkContext context)
        {
		    var dbName = context.dbName;

            foreach (var field in _fields.Values)
            {        	
                var fieldName = field.name;

                if (field.formType.Equals("file"))
			    {
                    //
                    long fieldValue;
                    long.TryParse(GetFieldValue(fieldName), out fieldValue);

                    if (fieldValue != 0)
                    {
					    var upload = context.database.FetchEntityByID<File>(fieldValue);
					    //var_dump($upload->thumb); die();

                        SetFieldValue(fieldName + "_source", upload.GetFieldValue("local_name"));
                        SetFieldValue(fieldName + "_thumb", upload.GetFieldValue("thumb"));
                        /*var thumbFile = upload.GetFieldValue("thumb");
                        var thumbBytes = 
                        var thumbData = Utility.Base64Decode()
                        SetFieldValue(fieldName+"_thumb", "data:image/png;base64,"+thumbData);*/

                    }
                    else
                    {
                        SetFieldValue(fieldName + "_data", null);
                        SetFieldValue(fieldName + "_thumb", null);
                    }

                    continue;
                }


                if (field.formType.Equals("date"))
			    {
                    long fieldValue;
                    long.TryParse(GetFieldValue(fieldName), out fieldValue);

				    var time = fieldValue.ToDateTime();

                    /*if (field.name.Equals("end_date"))
                    {
                        int dur;
                        int.TryParse(GetFieldValue("duration"), out dur);

                        long.TryParse(GetFieldValue("start_date"), out fieldValue);
                        time = fieldValue.ToDateTime();

                        if (dur>1)
                        {
                            time = time.AddDays(dur);
                        }
                        SetFieldValue(fieldName, time.ToTimestamp().ToString());

                        this.Save(context);
                    }*/

                    SetFieldValue(fieldName+"_year", time.Year.ToString());
				    SetFieldValue(fieldName+"_month", time.Month.ToString());
                    SetFieldValue(fieldName+"_day", time.Day.ToString());

                    var desc = context.Translate("month_" + time.Month.ToString())+" "+ time.Day.ToString("00") +", "+time.Year.ToString();
                    SetFieldValue(fieldName + "_text", desc);
                    continue;
                }

                if (field.dbType.Contains("varchar") || field.dbType.Contains("text"))
                {
				    var fieldValue = GetFieldValue(fieldName);
                    SetFieldValue(fieldName+"_html", fieldValue.FixNewLines());
                    continue;
                }
            }
        }

        /*protected bool VerifyFields(SynkRequest context)
        {
            foreach (var field in _fields.Values)
            {
                var fieldName = field.name;

                if (field.validator != null)
                {
                    if (!field.validator(context, this, field))
                    {
                        if (string.IsNullOrEmpty(context.error) && !context.WaitingForConfirmation())
                        {
                            context.error = "Entity field '"+context.Translate("entity_"+this.GetType().Name.ToLower()+"_"+ field.name) +"' failed validation";
                            context.WriteLog(context.error);
                        }
                        return false;
                    }
                }
            }
            
            return true;
        }*/

        protected void ApplyGenerators(SynkContext context)
        {
            foreach (var field in _fields.Values)
            {
                var fieldName = field.name;

                if (field.autoGenerator != null)
                {                    
                    if (field.autoGenerator(context, this, field))
                    {
                        context.WriteLog("Generating field " + field.name + " in " + this.ToString());
                    }
                }
            }
        }

        protected void ApplyTriggers(SynkContext context)
        {
            foreach (var field in _fields.Values)
            {
                if (string.IsNullOrEmpty(field.entity))
                {
                    continue;
                }

                var fieldName = field.name;
                var fieldValue = GetFieldValue(fieldName);
                var oldValue = _originalValues[field.name];

                if (!string.IsNullOrEmpty(field.fieldLink) && !fieldValue.Equals(oldValue))
                {

                    var old = GetRelationshipIDsFromList(oldValue);
                    var current = GetRelationshipIDsFromList(fieldValue);

                    var deleted = old.Except(current);
                    var added = current.Except(old);

                    //DEBUGGER 
                    context.WriteLog("TRIGGER: RELATIONSHIP CHANGES in " + field.name);
                    context.WriteLog("OLD: " + oldValue);
                    context.WriteLog("NEW: " + fieldValue);

                    foreach (var id in deleted)
                    {
                        var other = context.database.FetchEntityByID(field.entity, id);
                        if (other.exists)
                        {
                            other.RemoveRelationship(field.fieldLink, this);
                            other.Save(context, false);
                        }
                    }

                    foreach (var id in added)
                    {
                        var other = context.database.FetchEntityByID(field.entity, id);
                        if (other.exists)
                        {
                            other.AddRelationship(field.fieldLink, this);
                            other.Save(context, false);
                        }
                    }
                }
            }
        }


        public bool Save(SynkContext context, bool useTriggers = true)
        {            
            if (! this.isWritable) {
                return false;
            }


            if (useTriggers)
            {
                /*if (!VerifyFields(context))
                {
                    return false;
                }*/

                ApplyGenerators(context);
            }

            var dbFields = this.GetFields();

		    if (this.exists)
		    {
                context.WriteLog("Saving into " + this.tableName + ": " + this.ToString());

                context.database.saveObject(dbName, tableName, dbFields, "id", this.id.ToString());
		    }
		    else
		    {
                context.WriteLog("Inserting into " + this.tableName + ": " + this.ToString());

                dbFields["insertion_date"] = this.insertion_date.ToString();
			    this.id = context.database.insertObject(dbName, tableName, dbFields);
			    this.exists = true;
		    }

            if (useTriggers)
            {
                ApplyTriggers(context);
            }

            foreach (var val in this._currentValues)
            {
                context.WriteLog(val.Key + ": " + val.Value);
                _originalValues[val.Key] = val.Value;
            }

            context.site.InvalidateCache(this.className);

		    return true;
	    }

        #region RELATIONSHIPS
        public static string MakeRelationshipIDsFromList(HashSet<long> values)
        {
            var s = new StringBuilder();
            foreach (var id in values)
            {
                s.Append(id.ToString());
                s.Append(',');
            }
            return s.ToString();
        }

        public static HashSet<long> GetRelationshipIDsFromList(string values)
        {
            var result = new HashSet<long>();

            if (!string.IsNullOrEmpty(values))
            {
                var temp = values.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var entry in temp)
                {
                    long id;
                    if (long.TryParse(entry, out id) && id>0)
                    {
                        result.Add(id);
                    }
                }
            }

            //DEBUGGER 
            Console.WriteLine("LISTING RELATIONSHIP: " + values);

            return result;
        }

        public bool RemoveRelationship(string fieldName, Entity entity)
        {
            //DEBUGGER 
            Console.WriteLine("REMOVING RELATIONSHIP: " + fieldName+" to "+entity.ToString());

            var field = this.FindField(fieldName);
            if (field == null)
            {
                return false;
            }

            var val = GetFieldValue(fieldName);

            if (field.formType.Equals("table"))
            {
                var relationships = GetRelationshipIDsFromList(val);
                if (relationships.Contains(entity.id))
                {
                    relationships.Remove(entity.id);
                    val = MakeRelationshipIDsFromList(relationships);
                    SetFieldValue(fieldName, val);
                }
            }
            else
            if (val.Equals(entity.id.ToString()))
            {
                SetFieldValue(fieldName, "");
            }

            return true;
        }

        public bool AddRelationship(string fieldName, Entity entity)
        {
            //DEBUGGER 
            Console.WriteLine("ADDING RELATIONSHIP: " + fieldName + " to " + entity.ToString());

            var field = this.FindField(fieldName);
            if (field == null)
            {
                return false;
            }

            var val = GetFieldValue(fieldName);

            if (field.formType.Equals("table"))
            {
                var relationships = GetRelationshipIDsFromList(val);
                if (!relationships.Contains(entity.id))
                {
                    relationships.Add(entity.id);
                    val = MakeRelationshipIDsFromList(relationships);
                    SetFieldValue(fieldName, val);
                }
            }
            else
            {
                SetFieldValue(fieldName, entity.id.ToString());
            }

            return true;
        }
        #endregion

        public bool Remove(SynkContext context)
	    {
		    if (!this.exists)
            {
                return false;
            }

            var cond = Condition.Equal("id", this.id.ToString());
			context.database.deleteAll(dbName, tableName, cond);

			this.exists = false;
			this.id = 0;

            return true;
		}

        public Field RegisterField(string name) {
		    var field = new Field(name);
            _fields[name] = field;
		    return field;
        }

	    public Field FindField(string name)
	    {
            if (_fields.ContainsKey(name))
            {
                return _fields[name];
            }

            return null;
		}


	    public string translateField(SynkContext context, Field field)
	    {
		    return context.Translate("entity_"+ this.className.ToLower() + "_" + field.name);
	    }
        
    	public override string ToString() {
            return className;
	    }

	    public virtual string ToThumb() {
		    return null;
	    }

        public virtual Condition GetSearch(string term)
        {
            return null;
        }

        public virtual PermissonMode CheckPermissions(SynkContext context, User user)
        {
            return PermissonMode.Writable;
        }

        #region HELPERS
        public T GetFieldAsEntity<T>(SynkContext context, string fieldName) where T: Entity
        {
            var field = FindField(fieldName);
            if (string.IsNullOrEmpty(field.entity))
            {
                return default(T);
            }

            long ID;
            long.TryParse(GetFieldValue(fieldName), out ID);
            return context.database.FetchEntityByID<T>(ID);
        }
        #endregion
    }
}
