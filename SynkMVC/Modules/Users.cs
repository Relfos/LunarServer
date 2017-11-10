using SynkMVC;
using SynkMVC.Model;
using System;
using System.Collections.Generic;

namespace SynkMVC.Modules
{
    public class Users : CRUDModule
    {
        public Users()
        {
            this.RegisterClass<User>();
        }
    }
}
