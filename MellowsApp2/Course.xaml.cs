using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics.Metrics;
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
using Microsoft.Data.SqlClient;

namespace MellowsApp2
{
    /// <summary>
    /// Interaction logic for Competition.xaml
    /// </summary>
    public partial class Course : UserControl
    {
        SqlConnection con = new SqlConnection();
        SqlCommand cmd = new SqlCommand();
        SqlDataAdapter da = new SqlDataAdapter();

        public Course()
        {
            InitializeComponent();
            UpdateHoleAverages();
        }
        private void ScoreConnection()
        {
            //Database connection string
            String Conn = ConfigurationManager.ConnectionStrings["ConnectionStringMellows"].ConnectionString;
            con = new SqlConnection(Conn);
            con.Open(); //Connection Open
        }
        private void UpdateHoleAverages()
        {
            ScoreConnection();
            //List<string> holes = new List<string>{"Hole1","Hole2"};
            DataTable dt = new DataTable();

            String query = "SELECT * FROM Score";
            cmd = new SqlCommand(query, con);
            cmd.CommandType = CommandType.Text;

            da = new SqlDataAdapter(cmd);

            da.Fill(dt);

            double sum = 0;
            double averageScore = 0;
            double count = 0;
            string hole = "";
            int counter = 0;
            List<Label> labels = new List<Label> {Hole1,Hole2,Hole3,Hole4,Hole5,Hole6,Hole7,Hole8,Hole9,
                Hole10,Hole11,Hole12,Hole13,Hole14,Hole15,Hole16,Hole17,Hole18};
            foreach (Label label in labels)
            {
                counter++;
                hole = "Hole" + counter.ToString();                
                count = Convert.ToDouble(dt.Compute("COUNT(" + hole + ")", "("+ hole + ") > 0"));
                if (count > 0)
                {
                    sum = Convert.ToDouble(dt.Compute("SUM(" + hole + ")", string.Empty));
                    averageScore = sum / count;
                    label.Content = "Average Score = " + (Math.Round(averageScore, 2)).ToString();
                }
                else
                {
                    Hole1.Content = "Average Score = N/A";
                }
            }
            con.Close();
        }

        private void MembersClick(object sender, RoutedEventArgs e)
        {
            this.Content = new Members();
        }
    }
}
