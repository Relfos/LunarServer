using SynkMVC.Model;
using SynkMVC.Utils;
using System.Text;
using System;
using LunarParser;
using LunarParser.JSON;

namespace SynkMVC.Modules
{
    public class Files : CRUDModule
    {

        public Files()
        {
            this.RegisterClass<File>();
        }

        public void OnUpload(SynkContext context)
        {
            foreach (var upload in context.request.uploads)
            {
                var entity = context.UploadFile(upload.fileName, upload.bytes);

                var result = DataNode.CreateObject();
                result.AddField("id", entity.id.ToString());
                result.AddField("name", upload.fileName);
                result.AddField("hash", entity.GetFieldValue("hash"));

                var json = JSONWriter.WriteToString(result);
                context.Echo(json);
                break;
            }           
        }

        public override void OnDetail(SynkContext context)
        {
            long id;
            long.TryParse(context.request.GetVariable("entity"), out id);

            var file = context.database.FetchEntityByID<File>(id);

            if (file.exists)
            {
                var fileName = file.GetFieldValue("real_name");
                var bytes = file.GetBytes(context);
                context.SendDownload(fileName, bytes);
            }
            else
            {
                context.die("File not found, id " + id);
            }

        }
    }
    
}
