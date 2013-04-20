using Moonfish.Core;
using Moonfish.Core.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Moonfish.Debug
{
    public partial class VistaViewer : Form
    {
        public VistaViewer()
        {
            InitializeComponent();
        }

        List<Tag> tags;
        MapStream map;

        private void VistaViewer_Load(object sender, EventArgs e)
        {
            map = new MapStream(@"D:\h2v\ascension.map");
            listBox1.BeginUpdate();
            tags = new List<Tag>();
            foreach (var item in map.Tags)
            {
                if (item.Type.ToString() == "mode" && item.VirtualAddress != 0)
                    tags.Add(item);
            }

            listBox1.DataSource = tags;
            listBox1.EndUpdate();
        }

        private void listBox1_Format(object sender, ListControlConvertEventArgs e)
        {
            e.Value = (e.ListItem as Tag).VirtualAddress.ToString();
        }

        private void VistaViewer_DoubleClick(object sender, EventArgs e)
        {
            
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listBox1.SelectedItem != null)
            { 
                var tag = map[(listBox1.SelectedItem as Tag).Identifier].Export();
                
                Model m = new Model(tag as model);
                m.Show();
            }
        }
    }
}
