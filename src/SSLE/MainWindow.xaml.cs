using DBLOG;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SSLE
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        DatabaseLog[] logs;
        string sqlcon = "";
        SqlConnection conn;
        public MainWindow()
        {
            InitializeComponent();
            thost.Text = "localhost";
            checkBox.IsChecked = true;
            stable.Items.Add("所有数据表");
            sbegindate.Text = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd HH:mm:ss");
            senddate.Text = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void 按钮退出图标_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void checkBox_Checked(object sender, RoutedEventArgs e)
        {
            tuser.IsEnabled = false;
            tcode.IsEnabled = false;
        }

        private void checkBox_Unchecked(object sender, RoutedEventArgs e)
        {
            tuser.IsEnabled = true;
            tcode.IsEnabled = true;
        }

        private void btconnect_Click(object sender, RoutedEventArgs e)
        {
            if (checkBox.IsChecked == false)
            {
                sqlcon = "data source=" + thost.Text.Trim() + ";initial catalog=master;persist security info=False;user id=" + tuser.Text.Trim() + ";pwd=" + tcode.Password.Trim() + ";";
            }
            else
            {
                sqlcon = "data source=" + thost.Text.Trim() + ";initial catalog=master;persist security info=False;trusted_connection=SSPI;";
            };
            try
            {
                conn = new SqlConnection(sqlcon);
                conn.Open();
                if (conn.State.ToString() == "Open")
                {
                    MessageBox.Show("数据库连接成功！");
                    string sql = "select name from sys.databases where name not in ('master','tempdb','model','msdb')";
                    SqlCommand com = new SqlCommand(sql, conn);
                    SqlDataReader read = com.ExecuteReader();
                    sdatabase.Items.Clear();
                    while (read.Read())
                    {
                        sdatabase.Items.Add(read[0].ToString());
                    }
                    conn.Close();
                    sdatabase.IsEnabled = true;
                    stable.IsEnabled = true;
                    sbegindate.IsEnabled = true;
                    senddate.IsEnabled = true;
                    btdblog.IsEnabled = true;
                    sdatabase.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("数据库连接失败！");
                sdatabase.IsEnabled = false;
                stable.IsEnabled = false;
                sbegindate.IsEnabled = false;
                senddate.IsEnabled = false;
                return;
            }
        }

        private void sdatabase_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                conn = new SqlConnection(sqlcon);
                conn.Open();
                string sql = "select name from " + sdatabase.SelectedValue + ".sys.tables  order by name";
                SqlCommand com = new SqlCommand(sql, conn);
                SqlDataReader read = com.ExecuteReader();
                stable.Items.Clear();
                stable.Items.Add("所有数据表");
                while (read.Read())
                {
                    stable.Items.Add(read[0].ToString());
                }
                conn.Close();
                stable.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("数据库连接失败！");
                return;
            }
        }

        private void btdblog_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                DatabaseLogAnalyzer dbla;
                string dblacon = "";
                if (checkBox.IsChecked == false)
                {
                    dblacon = "data source=" + thost.Text.Trim() + ";initial catalog=" + sdatabase.SelectedValue.ToString() + ";persist security info=False;user id=" + tuser.Text.Trim() + ";pwd=" + tcode.Password.Trim() + ";";
                }
                else
                {
                    dblacon = "data source=" + thost.Text.Trim() + ";initial catalog=" + sdatabase.SelectedValue.ToString() + ";persist security info=False;trusted_connection=SSPI;";
                };

                dbla = new DatabaseLogAnalyzer(dblacon);
                if (stable.SelectedValue.ToString() == "所有数据表")
                {
                    loggird.DataContext = null;
                    logs = dbla.ReadLog(sbegindate.SelectedDate.Value.ToString("yyyy-MM-dd HH:mm:ss"), senddate.SelectedDate.Value.ToString("yyyy-MM-dd HH:mm:ss"), "");
                    loggird.DataContext = logs;
                }
                else
                {
                    loggird.DataContext = null;
                    logs = dbla.ReadLog(sbegindate.SelectedDate.Value.ToString("yyyy-MM-dd HH:mm:ss"), senddate.SelectedDate.Value.ToString("yyyy-MM-dd HH:mm:ss"), stable.SelectedValue.ToString());
                    loggird.DataContext = logs;
                }
                MessageBox.Show("读取到" + logs.Length + "条日志记录！");
                if (logs.Length > 0)
                {
                    btextsql.IsEnabled = true;
                }
                else
                {
                    btextsql.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取日志记录失败！");
                btextsql.IsEnabled = false;
                return;
            }
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void btextsql_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (logs.Length > 0)
                {
                    SaveFileDialog savefile = new SaveFileDialog();
                    savefile.FileName = "SSLE恢复脚本_" + DateTime.Now.AddDays(1).ToString("yyyy-MM-dd-HHmmss");
                    savefile.Filter = "SQL(*.sql)|*.sql";
                    if (savefile.ShowDialog() == true)
                    {
                        string fileName = savefile.FileName;   //获取要保存文件的路径
                        FileStream myFs = new FileStream(fileName, FileMode.Create);
                        StreamWriter mySw = new StreamWriter(myFs, Encoding.UTF8);

                        mySw.WriteLine("--这是SSLE生成的恢复SQL脚本");
                        mySw.WriteLine("--数据库： " + sdatabase.Text);
                        mySw.WriteLine("--数据表： " + stable.Text);
                        mySw.WriteLine("--生成时间： " + DateTime.Now.AddDays(1).ToString("yyyy-MM-dd HH:mm:ss"));
                        mySw.WriteLine("-- ");
                        mySw.WriteLine("-- ");
                        mySw.WriteLine("-- ");
                        for (int i = 1; i <= loggird.Items.Count; i++)
                        {
                            mySw.WriteLine("--原事务ID： " + logs[loggird.Items.Count - i].TransactionID + " 时间：" + logs[loggird.Items.Count - i].BeginTime + " 内容：" + logs[loggird.Items.Count - i].RedoSQL);
                            mySw.WriteLine(logs[loggird.Items.Count - i].UndoSQL);
                        }

                        mySw.Close();
                        myFs.Close();

                        MessageBox.Show("保存成功");
                    }
                }
            }
            catch (Exception ee)
            { MessageBox.Show("导出恢复SQL脚本失败！"); }
        }
    }
}