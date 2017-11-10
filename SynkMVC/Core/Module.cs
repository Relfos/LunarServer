using SynkMVC.Model;
using SynkServer;
using SynkServer.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynkMVC
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class ModuleAttribute : Attribute
    {
    }

    [Module]
    public abstract class Module
    {
        public string name;
	    public string title;

	    public Module()
        {
		    this.name = this.GetType().Name.ToLower();
		    this.title = name + "???";
        }
    
        public virtual List<Entity> Search(SynkContext context, string entityClass, string term)
        {
            List<Entity> entities = null;
            var template = context.database.CreateEntity(entityClass);
            if (template != null)
            {
                var cond = template.GetSearch(term);

                if (cond != null)
                {
                    entities = context.database.FetchAllEntities(entityClass, cond);
                }
            }
            return entities;
        }

        public virtual bool CheckPermissions(SynkContext context, User user, string action)
        {
            return true;
        }

        public string getLink()
        {
            return "synkNav().setModule('"+this.name+"').go();";
        }

        public string getTitle(SynkContext context)
        {
            return context.Translate("module_" + this.name);
        }



        public void progress(SynkContext context)
        {
	        /*$progressFile = "tmp/".session_id(). ".bar";
            if (file_exists($progressFile))
            {
			$progress = file_get_contents($progressFile);
            }
            else
            {
		   $progress = '0';
            }

            echo $progress;*/
        }

        public virtual void beforeRender(SynkContext context)
        {

        }

        public bool render(SynkContext context)
        {
		    var viewFile = context.targetView;
		    var viewPath = context.currentModule.name + "/" + viewFile;

            if (!System.IO.File.Exists("views/" + viewPath + ".html"))
            {
                viewPath = "common/" + viewFile;


                if (!System.IO.File.Exists("views/" + viewPath + ".html"))
                {
                    context.kill("Could not load view '" + context.targetView + "' for " + context.currentModule.name);
                    return false;
                }
            }

		    context.PushTemplate(viewPath);				      		
		    context.Render();
            return true;
        }

        public void paginate(SynkContext context)
        {
            int page;
            int.TryParse(context.request.GetVariable("page"), out page);
	        context.request.session.Set("page", page.ToString());
	        //context.page = page;
	   
	        this.render(context);
        }

        public virtual void afterRender(SynkContext context, string layoutTemplate)
        {
        }

        public virtual void OnInvalidAction(SynkContext context, string action)
        {
            context.kill("Invalid action " + action + " on module " + this.name);
        }

        public void InitRoutes(MVC mvc)
        {
            var type = this.GetType();
            foreach (var method in type.GetMethods())
            {
                if (method.Name.StartsWith("On"))
                {
                    var action = method.Name.Substring(2).ToLower();

                    if (action.Equals("invalidaction"))
                    {
                        continue;
                    }

                    mvc.site.Get(this.name + "." + action, (req) => {
                        var args = "module=" + this.name + "&action=" + action;
                        return new SynkContext(mvc, req);
                    });
                }
            }
        }

        public string GetActionURL(string action)
        {
            return this.name + "." + action;
        }
    }


}
