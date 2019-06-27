using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using UCLouvain.BDDSharp;

namespace WindowsFormsApp17
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            saveFileDialog1.Filter = "jpg file(*.jpg)|*.jpg|png file(*.png)|*.png";
            
        }
        List<String> var = new List<String>();

        private void button5_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != "")
            {
                string input = textBox1.Text;
                List<Token> rpn = Arithmetic.Calculate(input);
                if (rpn != null)
                {
                    Dictionary<string, bool> variables = Arithmetic.GetVariables(rpn);
                    var manager = new BDDManager(variables.Count);
                    var root = Arithmetic.CreateTree(rpn, variables, manager);
                    if (root != null)
                    {
                        List<BDDNode> list = root.Nodes.ToList();

                        var v1 = new Dictionary<int, string>();
                        int j = 0;
                        foreach (KeyValuePair<string, bool> kv in variables)
                        {
                            v1.Add(j, kv.Key);
                            j++;
                        }
                        manager.Reduce(root);
                        Func<BDDNode, string> labelFunction = (x) => v1[x.Index];
                        Bitmap bm = new Bitmap(Graphviz.RenderImage(manager.ToDot(root, labelFunction, false), "jpg"));
                        pictureBox1.Image = bm;
                        label1.Text = "Готово";
                    }
                    else
                    {
                        label1.Text = "Ошибка при вычислении логической формулы";
                    }
                }
                else
                {
                    label1.Text = "Ошибка ввода логической формулы";
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                if (saveFileDialog1.ShowDialog() == DialogResult.Cancel)
                    return;
                string filename = saveFileDialog1.FileName;
                pictureBox1.Image.Save(filename);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string[] Rformula = new string[]
            {
                "a&b&c&d&e",
                "a&b>c&d",
                "!a|b^c&b(a|e)",
                "a|b>c|d",
                "a&b|c&d|e&f|g&h",
                "(a=b)>(c=d)",
                "a>b>c>d>e",
                "a&b|b&c|c&d",
                "a&b|a&!b&c",
                "a&!b&c|a&b&c|a&b&!c",
                "a&b>c^d^e|f|g&(h|i)>l&m&n",
                "a^b^c^d^e^f",
                "(a=b=c=d=e)&(f=g=h=i)"
            };
            Random rand = new Random();
            textBox1.Text = Rformula[rand.Next(0, 12)];
            button5_Click(sender, e);
        }
    }
}
