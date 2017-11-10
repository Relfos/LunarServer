using SynkMVC.Model;
using SynkMVC.Utils;
using System.Text;

namespace SynkMVC.Modules
{
    public class Enums : CRUDModule
    {

        public Enums()
        {
            this.RegisterClass<Enum>();
        }
    }
}