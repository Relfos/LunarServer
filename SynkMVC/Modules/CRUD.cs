using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LunarLabs.WebMVC.Utils;
using LunarLabs.WebMVC.Model;
using LunarParser;
using LunarParser.JSON;
using LunarLabs.WebServer.Core;

namespace LunarLabs.WebMVC.Modules
{
    /*
     * Actions: default, edit, save, remove, clear, detail
     */
    public abstract class CRUDModule : Module
    {
        #region STRUCTS
        public class Export
        {
            public string format;
            public string label;

            public Export(string format, string label)
            {
                this.format = format;
                this.label = label;
            }
        }

        public class Page
        {
            public string id;
            public int value;
            public bool selected;
            public bool disabled;

            public Page(string id, int value, bool selected, bool disabled)
            {
                this.id = id;
                this.value = value;
                this.selected = selected;
                this.disabled = disabled;
            }

        }

        public class Header
        {
            public string name;
            public string label;
            public bool visible;

            public Header(string name, string label, bool visible)
            {
                this.name = name;
                this.label = label;
                this.visible = visible;
            }
        }

        public class Item
        {
            public long id;
            public string label;
            public string thumb;

            public Item(long id, string label, string thumb)
            {
                this.id = id;
                this.label = label;
                this.thumb = thumb;
            }
        }

        public class Tab
        {
            public string name;
            public int index;
            public bool active;
            public List<Row> rows;
        }

        public class Row
        {
            public List<Column> columns;
            public long rowID;
            public string @class;
        }

        public class Column
        {
            public int index;
            public string name;
            public string label;
            public string value;
            public string maskedValue;
            public bool visible;
            public bool writable;
            public string type;
            public string @class;
            public string control;
            public bool required;
            public string extra_attributes;
            public List<Option> options;
            public bool odd;
            public string entity;
            public string entityID;
            public string thumb;
            public string unit;
            public bool isUpload;
            public bool isTable;
            public bool isSearchable;
            public bool isHTML;
            public List<Item> items;
            public bool hasContent;
            public bool validable;
        }

        public class Option
        {
            public string key;
            public string value;
            public bool selected;

            public Option(string key, string value, bool selected)
            {
                this.key = key;
                this.value = value;
                this.selected = selected;
            }
        }
        #endregion

        private string _entityClass = null;
        public void RegisterClass<T>() where T:Entity
        {
            _entityClass = typeof(T).Name.ToLower();
        }

        public int itemsPerPage = 20;
        public int currentPage = 1;
        public long entityID = 0;
        public Condition filter;

        public Entity detail;

        public List<Header> headers;
        public List<Tab> tabs;
        public List<Page> pages;
        public List<Export> exports;
        public string error;

        public List<Row> rows;

        public List<Entity> entities;

        /*public string addFilter(string filter)
	    {
		    if (is_null($this->filter))
		    {
    			$this->filter = $filter;
		    }
		    else
		    {
			    $this->filter = array('and' => array($this->filter, $filter));
		    }

		    $_SESSION['filter'] = $this->filter;
	    }*/

        public override bool CheckPermissions(SynkContext context, User user, string action)
        {
            return context.hasLogin || action.Equals("detail");
        }

        public virtual void OnDefault(SynkContext context)
        {
            FetchPage(context);
            GenerateData(context);

            context.PushTemplate("crud/list");
            context.Render();
        }

        public virtual void OnEdit(SynkContext context)
        {
            FetchEntityID(context);
            GenerateData(context);

            context.PushTemplate("crud/edit");
            context.Render();
        }

        public virtual void OnSave(SynkContext context)
        {
            long id;
            long.TryParse(context.request.GetVariable("entity"), out id);

            var entity = context.database.FetchEntityByID(_entityClass, id);
                       
            foreach (var field in entity.fields)
    		{	        		
                if (context.request.HasVariable(field.name))
                {
                    var val = context.request.GetVariable(field.name);

                    if (field.formType.Equals("date"))
                    {
                        int year, month, day;
                        var temp = val.Split('-');
                        int.TryParse(temp[0], out year);
                        int.TryParse(temp[1], out month);
                        int.TryParse(temp[2], out day);
                        DateTime date = new DateTime(year, month, day);
                        val = date.ToTimestamp().ToString();
                    }

                    entity.SetFieldValue(field.name, val);
                }
            }

		    if (entity.Save(context))
            {
                FetchPage(context);
                GenerateData(context);

                context.PushTemplate("crud/list");
                context.Render();
            }
            else
            {
                if (string.IsNullOrEmpty(context.error) && !context.WaitingForConfirmation())
                {
                    context.error = "Entity saving entity";
                }

                context.die();
            }
        }

        /*
	public function filter($context)
	{
		$entityClass = $_REQUEST['class'];
		$fieldName = $_REQUEST['field_name'];
		$fieldValue = $_REQUEST['field_value'];

		$entity = $context->database->createEntity($context, $entityClass);

	   //var_dump($_REQUEST);	   die();

		$field = $entity->findField($fieldName);

		if (is_null($field))
		{
			die("Invalid field for filtering");
		}
		else
		{
			if (strpos($fieldValue, '*') !== false)
			{
				$op = 'like';
			}
			else
			{
				$op = 'eq';
			}

			$condition = array($fieldName => array($op => $fieldValue));

			$context->addFilter($condition);
			$this->render($context);
		}
	}

	public function unfilter($context)
	{
	   $id = $_REQUEST['id'];
	   //var_dump($_REQUEST);	   die();

	   $context->removeFilters();

	   $this->render($context);
	}
*/

        public virtual void OnRemove(SynkContext context)
        {
            long id;            
            long.TryParse(context.request.GetVariable("entity"), out id);
		    
            var entity = context.database.FetchEntityByID(_entityClass, id);
            if (entity.exists)
            {
                entity.Remove(context);

                FetchPage(context);
                GenerateData(context);

                if (this.entities != null && this.entities.Count<=0 && this.currentPage>0)
                {
                    this.currentPage--;
                    GenerateData(context);
                }

                context.PushTemplate("crud/list");
                context.Render();
            }
            else
            {
                context.die("Entity " + id + " not found!");
            }
        }

        public virtual void OnClear(SynkContext context)
        {
		    context.database.ClearEntities(_entityClass);

            FetchPage(context);
            GenerateData(context);

            context.PushTemplate("crud/list");
            context.Render();
        }

        public virtual void OnDetail(SynkContext context)
        {
            long id;
            long.TryParse(context.request.GetVariable("entity"), out id);

            this.detail = context.database.FetchEntityByID(_entityClass, id);

            context.PushTemplate("crud/detail_"+_entityClass);
            context.Render();
        }

        public virtual void OnValidate(SynkContext context)
        {
            long id;
            long.TryParse(context.request.GetVariable("entity"), out id);

            var entity = context.database.FetchEntityByID(_entityClass, id);

            var obj = DataNode.CreateObject();
            foreach (var field in entity.fields)
            {
                if (context.request.HasVariable(field.name))
                {
                    var newValue = context.request.GetVariable(field.name);

                    var currentValue = entity.GetFieldValue(field);
                    var defaultValue = field.GetDefaultValue(context);

                    string error = null;

                    if (field.validator != null)
                    {
                        if (field.required || !string.IsNullOrEmpty(newValue))
                        {
                            entity.SetFieldValue(field, newValue);
                            error = field.validator(context, entity, field);
                            entity.SetFieldValue(field, currentValue);
                        }
                    }
                    else 
                    if (field.required && (string.IsNullOrEmpty(newValue) || (newValue.Equals("0")  && (field.entity!=null || field.formType.Equals("file")))))
                    {
                        error = context.Translate("system_field_required");
                    }

                    if (error != null)
                    {
                        var result = DataNode.CreateObject(field.name);
                        result.AddField("label", entity.translateField(context, field));
                        result.AddField("error", error);
                        obj.AddNode(result);
                    }
                }
            }

            var json = JSONWriter.WriteToString(obj);
            context.Echo(json);
        }

        #region GENERATE DATA
        public long FetchEntityID(SynkContext context)
        {
            long.TryParse(context.loadVar("entity", "-1"), out this.entityID);
            return this.entityID;
        }

        public Entity FetchEntity(SynkContext context)
        {
            FetchEntityID(context);

            return context.database.FetchEntityByID(_entityClass, this.entityID);
        }

        private void FetchPage(SynkContext context)
        {
            this.entityID = -1;
            //this.filter = context.loadVar("filter", null);
            var pageVal = context.loadVarFromRequest("page", "0");
            int.TryParse(pageVal, out this.currentPage);
        }

        private void GenerateData(SynkContext context)
        {
            if (this._entityClass == null)
            {
                return;
            }

            var templateEntity = context.database.CreateEntity(_entityClass);

            var tabEntries = new HashSet<string>();

            this.headers = new List<Header>();
            foreach (var field in templateEntity.fields)
            {
                if (!field.hidden)
                {
                    headers.Add(new Header(field.name, templateEntity.translateField(context, field), field.grid));

                    tabEntries.Add(field.tab);
                }
            }

            long total;

            if (entityID >= 0)
            {
                var entity = context.database.FetchEntityByID(_entityClass, entityID);

                entities = new List<Entity>();
                entities.Add(entity);
                total = 1;
            }
            else
            {
                total = context.database.GetEntityCount(_entityClass, this.filter);
                entities = context.database.FetchAllEntities(_entityClass, this.filter, new Pagination(itemsPerPage, currentPage));
            }

            this.tabs = new List<Tab>();
            this.rows = new List<Row>();
            foreach (var tabName in tabEntries)
            {
                var tab = new Tab();
                tab.name = context.Translate("entity_"+_entityClass+"_"+tabName);

                var rows = new List<Row>();
                tab.rows = rows;

                tabs.Add(tab);
                tab.index = tabs.Count;
                tab.active = tab.index == 1;

                foreach (var entity in entities)
                {
                    var entityPermission = entity.exists ? entity.CheckPermissions(context, context.currentUser) : PermissonMode.Writable;
                    if (entityPermission == PermissonMode.Hidden)
                    {
                        continue;
                    }

                    var columns = new List<Column>();
                    int i = 0;
                    foreach (var field in entity.fields)
                    {
                        if (!field.tab.Equals(tabName) && entityID>=0)
                        {
                            continue;
                        }

                        var mode = field.HasPermissions(context);
                        if (mode == PermissonMode.Hidden)
                        {
                            continue;
                        }

                        if (entityPermission == PermissonMode.Readable)
                        {
                            mode = PermissonMode.Readable;
                        }

                        var column = new Column();

                        column.name = field.name;
                        column.value = entity.GetFieldValue(column.name);
                        column.maskedValue = column.value;
                        column.required = field.required;
                        column.odd = (i % 2) != 0;

                        column.extra_attributes = "";

                        if (field.formType.Equals("checkbox"))
                        {
                            if (column.value.Equals("1"))
                            {
                                column.extra_attributes += "checked='true' ";
                            }                            
                        }

                        column.isHTML = false;
                        column.isUpload = false;
                        column.thumb = null;

                        if (field.lengthLimit > 0)
                        {
                            column.extra_attributes += "maxlength='" + field.lengthLimit + "' ";
                        }

                        /*if (field.pattern != null)
                        {
                            column.extra_attributes += "pattern='" + field.pattern + "' ";
                        }*/

                        if (field.formType.Equals("html"))
                        {
                            column.isHTML = true;
                            column.extra_attributes += "style='width:100%; height:300px;' ";
                        }
                        else
                        if (field.formType.Equals("password"))
                        {
                            column.extra_attributes = "data-minlength='6' ";
                        }
                        else
                        if (field.formType == "file")
                        {
                            column.isUpload = true;
                            var fieldData = column.name + "_thumb";
                            column.thumb = entity.GetFieldValue(fieldData);

                            if (string.IsNullOrEmpty(column.thumb))
                            {
                                column.maskedValue = "-";
                            }
                        }

                        column.hasContent = (field.controlType.Equals("textarea"));

                        column.options = new List<Option>();
                        if (!string.IsNullOrEmpty(field.enumName))
                        {
                            var enumValues = context.FetchEnum(field.enumName);
                            var opLen = enumValues.Count;

                            foreach (var enumVal in enumValues)
                            {
                                var enumSelected = (enumVal.Equals(column.value));
                                var translateKey = "enum_" + field.enumName + "_" + enumVal;
                                var enumTranslation = context.Translate(translateKey);

                                var option = new Option(enumVal, enumTranslation, enumSelected);

                                if (enumSelected)
                                {
                                    column.maskedValue = enumTranslation;
                                }

                                column.options.Add(option);
                            }
                        }

                        column.items = new List<Item>();


                        if (field.entity != null)
                        {
                            column.entityID = column.value;
                            column.isTable = field.formType.Equals("table");

                            var template = context.database.CreateEntity(field.entity);
                            column.isSearchable = template.GetSearch("test") != null;

                            if (column.isTable)
                            {
                                var entityList = column.entityID.Split(',');
                                foreach (var id in entityList)
                                {
                                    if (id.Length == 0)
                                    {
                                        break;
                                    }

                                    long otherID;
                                    long.TryParse(id, out otherID);

                                    if (otherID == 0)
                                    {
                                        continue;
                                    }

                                    var otherEntity = context.database.FetchEntityByID(field.entity, otherID);
                                    var otherThumb = otherEntity.ToThumb();
                                    column.items.Add(new Item(otherID, otherEntity.ToString(), otherThumb));
                                }

                                column.maskedValue = "";
                            }
                            else
                            {

                                if (string.IsNullOrEmpty(column.value) || column.value.Equals("0"))
                                {
                                    column.maskedValue = "-";
                                }
                                else
                                {
                                    long otherID;
                                    long.TryParse(column.value, out otherID);

                                    var otherEntity = context.database.FetchEntityByID(field.entity, otherID);
                                    column.maskedValue = otherEntity.ToString();
                                }
                            }
                        }
                        else
                        {
                            column.isTable = false;
                        }

                        if (field.formType.Equals("date"))
                        {
                            long dateval;
                            long.TryParse(column.value, out dateval);
                            DateTime date = dateval.ToDateTime();
                            column.value = date.Year + "-" + date.Month.ToString("00") + "-" + date.Day.ToString("00");
                            column.maskedValue = entity.GetFieldValue(field.name + "_text");
                        }

                        column.unit = field.unit;

                        if (column.unit != null && column.unit.Equals("byte"))
                        {
                            int val;
                            int.TryParse(column.value, out val);
                            column.maskedValue = Utility.SizeSuffix(val);
                            column.unit = null;
                        }

                        if (string.IsNullOrEmpty(column.maskedValue))
                        {
                            column.maskedValue = column.value;
                        }

                        column.label = entity.translateField(context, field);
                        column.visible = field.grid;
                        column.type = field.formType;
                        column.@class = field.formClass;
                        column.control = field.controlType;
                        column.entity = field.entity;
                        column.writable = mode == PermissonMode.Writable;
                        column.index = columns.Count;

                        column.validable = !(/*field.formType.Equals("file") ||*/ field.formType.Equals("checkbox") || field.formType.Equals("table"));

                        columns.Add(column);

                        i++;
                    }


                    var row = new Row();
                    row.columns = columns;
                    row.rowID = entity.id;
                    row.@class = _entityClass;
                    rows.Add(row);

                    this.rows.Add(row);
                }

                if (entityID < 0)
                {
                    break;
                }
            }

            int totalPages = (int)Math.Ceiling(total / (float)itemsPerPage);

            this.pages = new List<Page>();
            this.pages.Add(new Page("«", currentPage<=0 ? 0 : currentPage - 1, false, currentPage <= 1));
            var lastPage = totalPages - 1;
            for (int j = 0; j <= lastPage; j++)
            {
                this.pages.Add(new Page((j+1).ToString(), j, currentPage == j, false));
            }
            this.pages.Add(new Page("»", currentPage>=lastPage ? lastPage : currentPage + 1, false, currentPage >= lastPage));

            if (this.pages.Count <= 2)
            {
                this.pages.Clear();
            }

            this.exports = new List<Export>();

            /*foreach (glob('plugins/export/*.php') as $file) 
        {
            $extensionName = pathinfo($file, PATHINFO_FILENAME);
            require_once($file);
            $exports[] = array('format' => $extensionName, 'label' => $extensionName);
        }	*/

            this.error = null;

            if (entities.Count == 0)
            {
                error = context.Translate("grid_error_empty");
                error = error.Replace("$name", context.currentModule.getTitle(context));
            }
        }

#endregion

        #region API
        /*public override string API(SynkRequest context, string method)
        {
            var error = false;
            var content = new JSON();

            if (string.IsNullOrEmpty(this.entityClass))
            {
                error = true;
                content.AddChild(new JSON("error", "method $method not supported for module " + name));
            }
            else
            {
                switch (method)
                {
                    case "get":
                        {
                            break;
                        }

                    case "list":
                        {
                            break;
                        }

                    case "insert":
                        {
                            break;
                        }

                    case "delete":
                        {
                            break;
                        }

                    default:
                        {
                            error = true;
                            content.AddChild(new JSON("error", "API method " + method + " is invalid"));
                            break;
                        }
                }
            }

            var result = new JSON();
            result.AddChild(new JSON("result", error ? "error" : "ok"));
            result.AddChild(content);

            var sb = new StringBuilder();
            result.Append(sb);
            return sb.ToString();
        }*/
        #endregion
    }
}
