using LunarLabs.WebMVC.Model;
using LunarLabs.WebServer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LunarLabs.WebMVC
{
    public class Field
    {
        public string name;
	    public string dbType;
        public string tab = "info";
        public string formType = "text";
	    public string formClass = "";
	    public string controlType = "input";
        public bool required = true;
        public bool grid = false;
        public bool hidden = false;
        public string enumName = null;
        public string entity = null;
        public string fieldLink = null;
        public string unit = null;
        public string target = null;
        public bool writable = true;
        public int lengthLimit = -1;
        public string pattern = null;

        public Func<SynkContext, Entity, Field, bool> autoGenerator;
        public Func<SynkContext, Entity, Field, string> validator;

        public Field(string name)
        {
		    this.name = name;
        }

        public Field makeOptional()
        {
		    this.required = false;
            return this;
        }

        private string defaultValue;
        private Func<SynkContext, string> defaultValueCallback;
        public string GetDefaultValue(SynkContext context)
        {
            if (defaultValueCallback != null && context != null)
            {
                return defaultValueCallback(context);
            }

            return defaultValue;
        }

        public Field setDefaultValue(string val)
        {
		    this.defaultValue = val;
            return this;
        }

        public Field setDefaultValue(Func<SynkContext, string> callback)
        {
            this.defaultValueCallback = callback;
            return this;
        }

        public Field showInGrid()
        {
		    this.grid = true;
            return this;
        }

        public Field makeHidden()
        {
		    this.hidden = true;
            return this;
        }

        public Field asDate()
        {
		    this.dbType = "int unsigned";
		    this.defaultValue = DateTime.Now.ToTimestamp().ToString();
		    this.formType = "date";
            return this;
        }

        public Field asList<T>(string fieldLink = null) where T: Entity
        {
		    this.dbType = "mediumtext";
		    this.defaultValue = "";
		    this.formType = "table";
		    this.entity = typeof(T).Name.ToLower();
            this.fieldLink = fieldLink;
            return this;
        }

        public Field asFloat()
        {
		    this.dbType = "float";
		    this.defaultValue = "0";
            return this;
        }

        public Field asPercent()
        {
		    this.unit = "%";
            return this.asFloat();
        }

        public Field asMoney()
        {
		    this.unit = "€";
            return this.asFloat();
        }

        public Field asSize()
        {
            this.unit = "byte";
            return this.asInteger();
        }

        public Field asString(int maxLength)
        {
		    this.dbType = "varchar("+maxLength+")";
		    this.lengthLimit = maxLength;
		    this.defaultValue = "";
            return this;
        }

        public Field asName(int maxLength)
        {
		    this.asString(maxLength);
		    this.pattern = @"^[ ,A-z\u0080-\u00FF ]{1,}$";
            return this;
        }

        public Field asUsername(int maxLength)
        {
		    this.asString(maxLength);
		    this.pattern = @"^[_A-z0-9]{1,}$";
            return this;
        }

        public Field asPassword(int length)
        {
		    this.asString(length);
    		this.formType = "password";
		    this.writable = false;
            return this;
        }

        public Field asHash(string target)
        {
		    this.asString(40);
		    this.makeHidden();
		    this.target = target;
            this.makeAuto(GenerateHash);
            return this;
        }

        public Field asText()
        {
    		this.dbType = "text";
	    	this.defaultValue = "";
    		this.controlType = "textarea";
            return this;
        }

        public Field asSummary(string target)
        {
		    this.asText();
		    this.makeHidden();
		    this.target = target;
            this.makeAuto(GenerateSummary);
            return this;
        }

        public Field asHTML()
        {
		    this.asText();
		    this.formType = "html";
            return this;
        }

        public Field asLocation()
        {
            return this.asString(40);
        }

        public Field asEnum(string name)
        {
		    this.dbType = "varchar(30)";
		    this.enumName = name;
		    this.defaultValue = "";
		    return this;
	    }

        public Field asBlob()
        {
    		this.dbType = "longtext";
		    this.defaultValue = "";
		    this.makeHidden();
            return this;
        }

        public Field asFile()
        {
    		this.dbType = "int";
		    this.defaultValue = "0";
		    this.formType = "file";
            return this;
        }

        public Field asImage(int maxWidth = 800)
        {
    		this.asFile();
            this.lengthLimit = maxWidth;
            return this;
        }

        public Field asTime()
        {
		    this.dbType = "int";
		    this.defaultValue = "0";
            return this;
        }

        public Field asEntity<T>(string fieldLink = null)
        {
		    this.dbType = "int unsigned";
		    this.entity = typeof(T).Name.ToLower(); 
		    this.defaultValue = "0";
            this.fieldLink = fieldLink;
            return this;
        }

        public Field asBoolean()
        {
		    this.dbType = "tinyint(1)";
		    this.defaultValue = "0";
		    this.required = false;
		    this.formType = "checkbox";
            return this;
        }

        public Field asEmail()
        {
		    this.formType = "email";
            return this.asString(254);
        }

        public Field asCountry()
        {
		    this.asEnum("country");
		    this.defaultValue = "PT";
            return this;
        }

        public Field asURL()
        {
		    this.formType = "url";
            return this.asString(200);
        }

        public Field asPhone()
        {
            return this.asString(13);
        }

        public Field asInteger()
        {
		    this.dbType = "int";
		    this.defaultValue = "0";
            this.formType = "number";
            return this;
        }

        public Field asBitfield()
        {
            this.dbType = "int unsigned";
            this.defaultValue = "0";
            this.formType = "number";
            return this;
        }

        public Field setTab(string tabName)
        {
            this.tab = tabName;
            return this;
        }

        public Field makeAuto(Func<SynkContext, Entity, Field, bool> callback)
        {
            autoGenerator = callback;
            return this;
        }

        public Field setValidator(Func<SynkContext, Entity, Field, string> callback)
        {
            validator = callback;
            return this;
        }

        public Func<SynkContext, PermissonMode> permissionDelegate = null;

        public Field SetPermission(Func<SynkContext, PermissonMode> callback)
        {
            permissionDelegate = callback;
            return this;
        }

        public PermissonMode HasPermissions(SynkContext context)
        {
            if (this.hidden)
            {
                return PermissonMode.Hidden;
            }

            if (permissionDelegate != null)
            {
                return permissionDelegate(context);
            }
            else
            {
                return PermissonMode.Writable;
            }            
        }

        #region GENERATORS
        private bool GenerateSummary(SynkContext context, Entity obj, Field field)
        {
            var srcName = field.target;
            var srcValue = obj.GetFieldValue(srcName).StripHTML();
            obj.SetFieldValue(field.name, srcValue.Summary(20));
            return true;
        }

        private bool GenerateHash(SynkContext context, Entity obj, Field field)
        {
            var srcName = field.target;
            var srcValue = obj.GetFieldValue(srcName);

            if (!string.IsNullOrEmpty(srcValue))
            {
                var fieldValue = context.database.getPasswordHash(srcValue);
                obj.SetFieldValue(field.name, fieldValue);
                //var_dump("password :" .$srcValue. " hash: ".$fieldValue);die();
                return true;
            }

            return false;
        }
        #endregion

        /*function encodeValue($context, $value)
        {
            $value = $context.database.escapeString($value);
            if (strpos(this.dbType, 'varchar') !== false || strpos(this.dbType, 'text') !== false )
            {
                return "'$value'";
            }
            else
            {
                return $value;
            }
        }*/
    }
}
