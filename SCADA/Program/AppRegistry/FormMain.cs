using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AppRegistry
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }
        public static void WriteKey(string keyName, string keyValue)
        {
            try
            {
                Microsoft.Win32.RegistryKey regKey;
                regKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\HMI");
                regKey.SetValue(keyName, keyValue);
                regKey.Close();
            }
            catch (Exception ex) { throw ex; }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            WriteKey("HMI_Database", ComDatabase.Text);
            Close();
        }
        public void FillComboServers(ComboBox Combo)
        {
            System.Data.Sql.SqlDataSourceEnumerator SqlEnumerator = System.Data.Sql.SqlDataSourceEnumerator.Instance;
            System.Data.DataTable dTable = SqlEnumerator.GetDataSources();
            foreach (DataRow Dr in dTable.Rows)
            {
                Combo.Items.Add(Dr[0]);
            }
        }
        private void btnDatabase_Click(object sender, EventArgs e)
        {
            if (FBD1.ShowDialog() == DialogResult.OK)
            {
                WriteKey("HMI_Database", FBD1.SelectedPath);


            }
        }

        private void btnDataConfig_Click(object sender, EventArgs e)
        {
            if (FBD1.ShowDialog() == DialogResult.OK)
            {
                txtDataConfig.Text = FBD1.SelectedPath;
                WriteKey("HMI_DataConfig", FBD1.SelectedPath);


            }
        }
        public static string ReadKey(string keyName)
        {
            string result = string.Empty;
            try
            {
                Microsoft.Win32.RegistryKey regKey;
                regKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\HMI");//HKEY_CURRENR_USER\Software\VSSCD
                if (regKey != null) result = (string)regKey.GetValue(keyName);
            }
            catch (Exception ex) { throw ex; }
            return result;
        }
        private void FormMain_Load(object sender, EventArgs e)
        {
            FillComboServers(ComDatabase);
            txtDataConfig.Text = ReadKey("HMI_DataConfig");


        }
    }
}
