using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

using log4net;

namespace sample_netframework
{
    public partial class Form1 : Form
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Form1));

        private Random _rand = new Random();
        private long _counter = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            var i = _rand.Next();
            int level = i % 5;
            string str = $"random number: {i}";
            switch (level)
            {
                case 0:
                    Log.Debug(str);
                    break;
                case 1:
                    Log.Info(str);
                    break;
                case 2:
                    Log.Warn(str);
                    break;
                case 3:
                    Log.Error(str);
                    break;
                case 4:
                    Log.Fatal(str);
                    break;
            }
            _counter++;

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            timer1.Interval = 10;
            timer1.Enabled = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Process process = new Process();
            // Configure the process using the StartInfo properties.
            var str = Environment.CurrentDirectory;
            process.StartInfo.FileName = @"..\..\..\..\src\LiveLogMonitor\bin\Release\netcoreapp3.1\LiveLogMonitor.exe";
            //process.StartInfo.Arguments = "-n";
            //process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
            process.Start();
        }
    }
}
