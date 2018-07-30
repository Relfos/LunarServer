using LunarLabs.WebMVC.Model;
using LunarLabs.WebMVC.Utils;
using System.Text;

namespace LunarLabs.WebMVC.Modules
{
    public class Enums : CRUDModule
    {

        public Enums()
        {
            this.RegisterClass<Enum>();
        }
    }
}