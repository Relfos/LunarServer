using LunarLabs.WebMVC;
using LunarLabs.WebMVC.Model;
using System;
using System.Collections.Generic;

namespace LunarLabs.WebMVC.Modules
{
    public class Users : CRUDModule
    {
        public Users()
        {
            this.RegisterClass<User>();
        }
    }
}
