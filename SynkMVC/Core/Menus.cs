using System.Collections.Generic;

namespace LunarLabs.WebMVC
{
    public class Menu
    {
        public class Item
        {
            public string label;
            public string action;
            public string link;

            public Item(string label, string action, string link)
            {
                this.label = label;
                this.action = action;
                this.link = link;
            }
        }

        public string title;
        public string link;
        public List<Item> items = new List<Item>();

        public Menu(string title, string link)
        {
            this.title = title;
            this.link = link;
        }
    }

}
