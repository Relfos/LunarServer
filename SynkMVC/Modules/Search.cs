using System.Collections.Generic;
using LunarParser;
using LunarParser.JSON;

namespace LunarLabs.WebMVC.Modules
{
    public class Search : Module
    {

        public Search()
        {
        }

        public void OnDefault(SynkContext context)
        {
            var entityClass = context.request.GetVariable("entity");
            var term = context.request.GetVariable("term");
            var required = context.request.GetVariable("required").Equals("true");

            List<Entity> entities = context.currentModule.Search(context, entityClass, term);

            var result = DataNode.CreateArray();

            if (!required)
            {
                var item = DataNode.CreateObject();
                item.AddField("value", "0");
                item.AddField("label", context.Translate("system_none"));
                result.AddNode(item);
            }

            if (entities != null)
            {
                foreach (var entity in entities)
                {
                    var item = DataNode.CreateObject();
                    item.AddField("value", entity.id.ToString());
                    item.AddField("label", entity.ToString());
                    result.AddNode(item);
                }
            }

            var json = JSONWriter.WriteToString(result);
            context.Echo(json);
        }

    }

}
