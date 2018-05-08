using System;
using System.Collections.Generic;

namespace SynkServer.Entity
{
    public abstract class EntityConnector
    {
        public Type objectType { get; private set; }
        public EntityStore Store { get; private set; }

        internal void Initialize(EntityStore store, Type objectType)
        {
            this.Store = store;
            this.objectType = objectType;
            LoadConnector();
        }

        internal abstract void LoadConnector();
        internal abstract void SaveConnector();

        private Dictionary<string, Entity> _objects = new Dictionary<string, Entity>();
        public IEnumerable<Entity> Objects { get { return _objects.Values; } }

        protected Entity AllocObject()
        {
            lock (this)
            {
                var obj = (Entity)Activator.CreateInstance(objectType);
                obj.SetStore(this.Store);
                return obj;
            }
        }

        protected void AddObject(Entity obj)
        {
            _objects[obj.id] = obj;
        }

        internal Entity CreateObject()
        {
            var obj = AllocObject();

            obj.id = Guid.NewGuid().ToString();

            lock (this)
            {
                _objects[obj.id] = obj;
            }

            return obj;
        }

        internal Entity FindObject(string ID)
        {
            lock (this)
            {
                if (_objects.ContainsKey(ID))
                {
                    return _objects[ID];
                }
                return null;
            }
        }

        internal void DeleteObject(string ID)
        {
            lock (this)
            {
                if (_objects.ContainsKey(ID))
                {
                    _objects.Remove(ID);
                }
            }
        }

        public void Save()
        {
            lock (this)
            {
                if (_shouldSave)
                {
                    SaveConnector();

                    _shouldSave = false;
                    //currentVersion++;
                }
            }
        }

        private bool _shouldSave = false;

        public void RequestSave()
        {
            lock (this)
            {
                _shouldSave = true;
            }
        }
    }

}
