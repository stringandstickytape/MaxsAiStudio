using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTool3
{
    public static class MenuHelper
    {
        public static ToolStripMenuItem CreateMenu(string menuText)
        {
            var menu = new ToolStripMenuItem(menuText);
            menu.BackColor = Color.Black;
            menu.ForeColor = Color.White;
            return menu;
        }

        public static ToolStripMenuItem CreateMenuItem(string text, ref ToolStripMenuItem dropDownItems, bool isTemplate = false)
        {
            if (isTemplate)
                return new TemplateMenuItem(text, ref dropDownItems);

            var retVal = new ToolStripMenuItem(text);
            dropDownItems.DropDownItems.Add(retVal);
            return retVal;
        }

        public static void AddSpecial(ToolStripMenuItem specialsMenu, string label, EventHandler clickHandler)
        {
            var specialMenuItem = CreateMenuItem(label, ref specialsMenu);
            specialMenuItem.Click += clickHandler;
        }
        
        public static void AddSpecials(ToolStripMenuItem specialsMenu, List<LabelAndEventHander> specials)
        {
            foreach (var special in specials)
            {
                AddSpecial(specialsMenu, special.Label, special.Handler);
            }
        }

        public static void RemoveOldTemplateMenus(MenuStrip menuBar)
        {
            menuBar.Items.OfType<ToolStripMenuItem>().Where(x => x.Text == "Templates").ToList().ForEach(x => menuBar.Items.Remove(x));
        }
    }

    public class LabelAndEventHander
    {
        public string Label { get; set; }
        public EventHandler Handler { get; set; }

        public LabelAndEventHander(string label, EventHandler handler)
        {
            Label = label;
            Handler = handler;
        }
    }
}
