using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SynkMVC
{
    public enum SortMode
    {
        Ascending,
        Descending
    }

    public class Sorting
    {
        public SortMode mode;
        public string key;

        public Sorting(string key, SortMode mode)
        {
            this.key = key;
            this.mode = mode;
        }
    }

    public class Pagination
    {
        public int itemsPerPage;
        public int targetPage;

        public Pagination(int itemsPerPage, int targetPage)
        {
            this.itemsPerPage = itemsPerPage;
            this.targetPage = targetPage;
        }
    }

    public abstract class Database
    {
        public bool failed = false;
	    public SynkContext context = null;
	
	    // abstract methods
	    public abstract void createDatabase(string dbName);
        public abstract bool createTable(string dbName, string table, Dictionary<string,string> fields, string key = null);
        public abstract long getCount(string dbName, string table, Condition condition = null);
        public abstract Dictionary<string, string> fetchObject(string dbName, string table, Condition condition);
        public abstract List<Dictionary<string, string>> fetchAll(string dbName, string table, Condition condition = null, Pagination pagination = null, Sorting sorting = null);
        public abstract void deleteAll(string dbName, string table, Condition condition);
        public abstract long insertObject(string dbName, string table, Dictionary<string, string> fields);
        public abstract bool saveObject(string dbName, string table, Dictionary<string, string> fields, string key, string value);

        public HashSet<string> dependencies = new HashSet<string>();

        public Database(SynkContext context) 
        {
            this.context = context;	
        }

        public bool prepare()
        {
            if (this.failed)
		    {
                return false;
            }		
		
		    var dbName = context.config.GetFieldValue("database");
		    this.createDatabase(dbName);

            return !this.failed;
        }

        public T CreateEntity<T>() where T : Entity
        {
            var result = (T)Activator.CreateInstance(typeof(T), new object[] { });
            result.InitFromContext(context);

            dependencies.Add(result.className);

            return result;
        }

        private Dictionary<string, Type> _entityTypes;

        public Entity CreateEntity(string entityClassName) 
        {
            if (_entityTypes == null)
            {
                _entityTypes = new Dictionary<string, Type>();
                 
                Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in asms)
                {
                    foreach (Type type in assembly.GetTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(Entity))))
                    {
                        _entityTypes[type.Name.ToLower()] = type;
                    }
                }
            }

            var key = entityClassName.ToLower();
            if (_entityTypes.ContainsKey(key))
            {
                var t = _entityTypes[key];
                var result = (Entity)Activator.CreateInstance(t, new object[] {  });
                result.InitFromContext(context);

                dependencies.Add(entityClassName);

                return result;
            }

            return null;
        }

        public T CreateEntityWithID<T>(long id) where T : Entity
        {
            var key = new EntityCacheEntry(typeof(T).Name.ToLower(), id);
            var cache = context.site.entityCache;
            if (cache.ContainsKey(key))
            {
                return (T)cache[key];
            }

            var entity = CreateEntity<T>();
            cache[key] = entity;
            return entity;
        }

        public Entity CreateEntityWithID(string entityClassName, long id) 
        {
            var key = new EntityCacheEntry(entityClassName, id);
            var cache = context.site.entityCache;
            if (cache.ContainsKey(key))
            {
                return cache[key];
            }

            var entity = CreateEntity(entityClassName);
            cache[key] = entity;
            return entity;
        }

        public T FetchEntityByID<T>(Int64 id) where T: Entity
        {
		    if (id == 0)
		    {
			    return CreateEntity<T>();
            }
		
    		var condition = Condition.Equal("id", id.ToString());
            return FetchEntity<T>(condition);
        }

        public Entity FetchEntityByID(string entityClassName, Int64 id)
        {
            if (id == 0)
            {
                return CreateEntity(entityClassName);
            }

            var condition = Condition.Equal("id", id.ToString());
            return FetchEntity(entityClassName, condition);
        }

        public T FetchEntity<T>(Condition condition) where T:Entity
        {
            if (condition == null)
            {
                return null;
            }

            var templateEntity = CreateEntity<T>();

		    var row = this.fetchObject(templateEntity.dbName, templateEntity.tableName, condition);

            if (row != null)
            {
                var entity = GetEntityFromRow<T>(row);
			    entity.LoadFromRow(context, row);
			    entity.Expand(context);
                return entity;
            }

            return templateEntity;
        }

        public Entity FetchEntity(string entityClassName, Condition condition) 
        {
            if (condition == null)
            {
                return null;
            }

            var templateEntity = CreateEntity(entityClassName);

            var row = this.fetchObject(templateEntity.dbName, templateEntity.tableName, condition);

            if (row != null)
            {
                var entity = GetEntityFromRow(entityClassName, row);
                entity.LoadFromRow(context, row);
                entity.Expand(context);
                return entity;
            }

            return templateEntity;
        }


        public List<T> FetchAllEntities<T>(Condition condition = null, Pagination pagination = null, Sorting sorting = null) where T :Entity
        {
            var entities = new List<T>();
				
		    var templateEntity = this.CreateEntity<T>();
				    				
            var rows = this.fetchAll(templateEntity.dbName, templateEntity.tableName, condition, pagination, sorting);
            
            foreach (var row in rows)
		    {
                var entity = GetEntityFromRow<T>(row);
			    entity.LoadFromRow(context, row);
                entity.Expand(context);
                entities.Add(entity);
		    }
					
		    return entities;
        }

        public List<Entity> FetchAllEntities(string entityClass, Condition condition = null, Pagination pagination = null, Sorting sorting = null) 
        {
            var entities = new List<Entity>();

            var templateEntity = this.CreateEntity(entityClass);

            var rows = this.fetchAll(templateEntity.dbName, templateEntity.tableName, condition, pagination, sorting);
            
            foreach (var row in rows)
            {
                var entity = GetEntityFromRow(entityClass, row);
                entity.LoadFromRow(context, row);
                entity.Expand(context);
                entities.Add(entity);
            }

            return entities;
        }

        private T GetEntityFromRow<T>(Dictionary<string, string> row) where T: Entity
        {
            long id;
            var idStr = row["id"];
            long.TryParse(idStr, out id);
            return CreateEntityWithID<T>(id);
        }

        private Entity GetEntityFromRow(string entityClass, Dictionary<string, string> row)
        {
            long id;
            var idStr = row["id"];
            long.TryParse(idStr, out id);
            return CreateEntityWithID(entityClass, id);
        }

        public long GetEntityCount<T>(Condition condition = null) where T: Entity
	    {					
		    var templateEntity = this.CreateEntity<T>();		    
		    return this.getCount(templateEntity.dbName, templateEntity.tableName);
        }

        public long GetEntityCount(string entityClass, Condition condition = null) 
        {
            var templateEntity = this.CreateEntity(entityClass);
            return this.getCount(templateEntity.dbName, templateEntity.tableName);
        }

        public void ClearEntities<T>(Condition condition) where T: Entity
	    {
    		var templateEntity = this.CreateEntity<T>();		    
    		this.deleteAll(templateEntity.dbName, templateEntity.tableName, condition);
        }

        public void ClearEntities(string entityClass, Condition condition = null) 
        {
            var templateEntity = this.CreateEntity(entityClass);
            this.deleteAll(templateEntity.dbName, templateEntity.tableName, condition);
        }

        public string getPasswordHash(string password)
	    {
            var key = password.MD5().ToLower();
            var s = "./ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789'";
            s = s.Shuffle();
            s = s.Substring(s.Length - 4);
            var salt = "$1$" + s;
            return Crypt.crypt(key, salt);
		}
	
	    public void fail(string error)
	    {
            this.context.die(error);
		    this.failed = true;
		    //session_destroy();		
	    }
    }
}
