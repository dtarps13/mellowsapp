using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
using static System.Net.Mime.MediaTypeNames;

namespace MellowsApp2
{
    /// <summary>
    /// Interaction logic for Competition.xaml
    /// </summary>
    public partial class Competition : UserControl
    {
        SqlConnection con = new SqlConnection();
        SqlCommand cmd = new SqlCommand();
        SqlDataAdapter da = new SqlDataAdapter();

        private string query = "";
        private int MemberId = 0;
        private int CompetitionId = 0;
        private List<int> Players = new List<int>();
        private int NoOfRounds = 0;
        private string AddRemovePlayer = "";
        private int HoleSelected = 0;

        public Competition(int competitionId, int noOfRounds, string name, List<int> players)
        {
            InitializeComponent();
            competitionName.Content = name;
            CompetitionId = competitionId;
            Players = players;
            NoOfRounds = noOfRounds;
            DisplayCompetitorsNewComp();
            ScoresHidden();
        }
        public Competition(int competitionId, int noOfRounds, string name, List<int> players, string saved)
        {
            InitializeComponent();
            competitionName.Content = name;
            CompetitionId = competitionId;
            Players = players;
            NoOfRounds = noOfRounds;
            DisplayCompetitorsSavedComp();
            ScoresHidden();
        }
        public void CompetitionConnection()
        {
            //Database connection string
            String Conn = ConfigurationManager.ConnectionStrings["ConnectionStringMellows"].ConnectionString;
            con = new SqlConnection(Conn);
            con.Open(); //Connection Open
        }
        private void DisplayCompetitorsNewComp()
        {
            DataTable dt = new DataTable();

            CompetitionConnection();

            query = "SELECT * from Member WHERE AddToComp = 'Y'";
            SqlCommand();

            da = new SqlDataAdapter(cmd);
            da.Fill(dt);

            if (dt != null && dt.Rows.Count > 0)
            {
                //Assign DataTable data to DataGrid using ItemSource property.   
                competition.ItemsSource = dt.DefaultView;
            }
            else
            {
                competition.ItemsSource = null;
            }
            con.Close();
            int counter = 0;
            do
            {
                counter++;
                Players.Sort();
                foreach (int player in Players)
                {
                    MemberId = player;
                    CompetitionConnection();

                    query = "SELECT Handicap FROM Member WHERE Id = " + MemberId;

                    SqlCommand();
                    SqlDataReader dr = cmd.ExecuteReader();
                    dr.Read();
                    double playerHandicap = (double)dr["Handicap"];
                    dr.Close();
                    playerHandicap -= 0.01;
                    int compHandicap = (int)Math.Round(playerHandicap);

                    query = "INSERT INTO Score(MemberId, CompetitionId, CompRoundNo, CompHandicap) VALUES " +
                        "(@MemberId, @CompetitionId, @CompRoundNo, @CompHandicap)";
                    SqlCommand();
                    cmd.Parameters.AddWithValue("@MemberId", MemberId);
                    cmd.Parameters.AddWithValue("@CompetitionId", CompetitionId);
                    cmd.Parameters.AddWithValue("@CompRoundNo", counter);
                    cmd.Parameters.AddWithValue("@CompHandicap", compHandicap);
                    cmd.ExecuteScalar();
                }
            } while (NoOfRounds > counter);
            query = "UPDATE Member SET AddToComp = 'N' WHERE AddToComp = 'Y'";
            SqlCommand();
            cmd.ExecuteNonQuery();

            con.Close();
        }
        private void DisplayCompetitorsSavedComp()
        {
            int round1 = 0;
            int round2 = 0;
            double playerHandicap = 0;

            Players.Sort();
            HoleSelected = 0;

            DataTable dt = new DataTable();
            
            CompetitionConnection();

            query = "SELECT * FROM Member WHERE Id = ";
            foreach (int player in Players)
            {
                query += player + " OR Id = ";
            }
            query = query.Remove(query.Length - 9);
            SqlCommand();

            da = new SqlDataAdapter(cmd);
            da.Fill(dt);

            // competition is the Datagrid displayed, no null check required as competition cannot be created
            // without a competitor and the last competitor cannot be removed
            competition.ItemsSource = dt.DefaultView;

            // Columns split into three groups as competitions can be one or two rounds and so different columns apply
            List<string> columns1 = new List<string> {"Gross1", "Nett1"};
            List<string> columns2 = new List<string> {"Gross2", "Nett2", "Gross36", "Nett36"};
            List<string> columns3 = new List<string> {"Back9Score", "Back6Score", "Back3Score", "Back2Score", 
                "Back1Score", "Back9ScoreNett", "Back6ScoreNett", "Back3ScoreNett", "Back2ScoreNett", 
                "Back1ScoreNett"};
            foreach (string col in columns1)
            {
                if (!dt.Columns.Contains(col))
                {
                    dt.Columns.Add(col, typeof(int));
                }
            }
            foreach (string col in columns3)
            {
                if (!dt.Columns.Contains(col))
                {
                    dt.Columns.Add(col, typeof(double));
                }
            }
            if (NoOfRounds > 1)
            {
                foreach (string col in columns2)
                {
                    if (!dt.Columns.Contains(col))
                    {
                        dt.Columns.Add(col, typeof(int));
                    }
                }
            }
            // Calculations
            // Used for single round competition calculations
            double sumGross = 0;
            double sumNett = 0;
            // Used for two round competition calculations
            double sumGross36 = 0;
            double sumNett36 = 0;
            // Number of players in competition
            int count = Players.Count;
            for (int i = 0; i < Players.Count; i++)
            {
                query = "SELECT CompHandicap FROM Score WHERE MemberId = " + Players[i] +
                    " AND CompetitionId = " + CompetitionId; ;
                SqlCommand();
                
                SqlDataReader dr = cmd.ExecuteReader();
                dr.Read();
                // CompHandicap is a database field, player handicap can change over time so is
                // calculated when player is added to competition it is an integer value
                int compHandicap = (int)dr["CompHandicap"];
                dr.Close();
                // calculations result in fractional results so cast to double
                playerHandicap = (double)compHandicap;

                query = "SELECT RoundScore FROM Score WHERE CompRoundNo = 1 AND MemberId = " + Players[i] +
                    " AND CompetitionId = " + CompetitionId;
                SqlCommand();

                dr = cmd.ExecuteReader();
                dr.Read();
                if (!dr.IsDBNull("RoundScore")) // No round score added in database
                {
                    round1 = (int)dr["RoundScore"];
                    dt.Rows[i]["Gross1"] = round1; // Gross score is without handicap
                    dt.Rows[i]["Nett1"] = round1 + compHandicap; // Nett score is adjusted with player handicap
                    sumGross += round1;
                    sumNett += round1 + compHandicap;
                }
                else
                {
                    // Set row to zero if no score entered, if score not entered for a player count is decremented
                    // for average score calculations
                    dt.Rows[i]["Gross1"] = 0;
                    dt.Rows[i]["Nett1"] = 0;
                    count -= 1;
                }
                dr.Close();
                if (count == 0) // No scores entered display zero else populate fields with average scores
                {
                    lblAverageGross.Content = string.Format(" {0:0.00}", 0);
                    lblAverageNett.Content = string.Format("{0:0.00}", 0);
                }
                else
                {
                    lblAverageGross.Content = string.Format("  {0:0.00}", sumGross / count);
                    lblAverageNett.Content = string.Format("{0:0.00}", sumNett / count);
                }
                // Additional calculations required if it is a two round competition
                if (NoOfRounds > 1)
                {
                    query = "SELECT RoundScore FROM Score WHERE CompRoundNo = 2 AND MemberId = " + Players[i] +
                    " AND CompetitionId = " + CompetitionId;
                    SqlCommand();

                    dr = cmd.ExecuteReader();
                    dr.Read();
                    if (!dr.IsDBNull("RoundScore"))
                    {
                        round2 = (int)dr["RoundScore"];
                        dt.Rows[i]["Gross2"] = round2;
                        dt.Rows[i]["Nett2"] = round2 + playerHandicap;
                        dt.Rows[i]["Gross36"] = round1 + round2;
                        dt.Rows[i]["Nett36"] = round1 + round2 + (playerHandicap * 2); // Two rounds double handicap
                        sumGross36 += round1 + round2;
                        sumNett36 += round1 + round2 + (playerHandicap * 2);
                    }
                    else
                    {
                        dt.Rows[i]["Gross2"] = 0;
                        dt.Rows[i]["Nett2"] = 0;
                        dt.Rows[i]["Gross36"] = 0;
                        dt.Rows[i]["Nett36"] = 0;
                    }
                    dr.Close();
                    if (count == 0)
                    {
                        lblAverageGross.Content = string.Format("  {0:0.00}", 0);
                        lblAverageNett.Content = string.Format("{0:0.00}", 0);
                    }
                    else
                    {
                        lblAverageGross.Content = string.Format(" {0:0.00}", sumGross36 / count);
                        lblAverageNett.Content = string.Format("{0:0.00}", sumNett36 / count);
                    }
                }
                // Back nine, back six, etc, calculated on different round for one and two round competitions
                // First round for single round comps and second round for two round competitions
                query = "SELECT Back9Score FROM Score WHERE CompRoundNo = " + NoOfRounds + " AND MemberId = " + Players[i] +
                    " AND CompetitionId = " + CompetitionId;
                SqlCommand();

                dr = cmd.ExecuteReader();
                dr.Read();
                if (!dr.IsDBNull("Back9Score"))
                {
                    int score = (int)dr["Back9Score"];
                    // Scores retrieved are integers but will likely result in fractional results
                    double dScore = (double)score;
                    dt.Rows[i]["Back9Score"] = dScore;
                    dt.Rows[i]["Back9ScoreNett"] = Math.Round((dScore + (playerHandicap / 2)), 1);
                }
                else
                {
                    dt.Rows[i]["Back9Score"] = 0;
                    dt.Rows[i]["Back9ScoreNett"] = 0;
                }
                dr.Close();

                query = "SELECT Back6Score FROM Score WHERE CompRoundNo = " + NoOfRounds + " AND MemberId = " + Players[i] +
                    " AND CompetitionId = " + CompetitionId;
                SqlCommand();

                dr = cmd.ExecuteReader();
                dr.Read();
                if (!dr.IsDBNull("Back6Score"))
                {
                    int score = (int)dr["Back6Score"];
                    double dScore = (double)score;
                    dt.Rows[i]["Back6Score"] = dScore;
                    dt.Rows[i]["Back6ScoreNett"] = Math.Round((dScore + (playerHandicap / 3)), 1);
                }
                else
                {
                    dt.Rows[i]["Back6Score"] = 0;
                    dt.Rows[i]["Back6ScoreNett"] = 0;
                }
                dr.Close();

                query = "SELECT Back3Score FROM Score WHERE CompRoundNo = " + NoOfRounds + " AND MemberId = " + Players[i] +
                    " AND CompetitionId = " + CompetitionId;
                SqlCommand();

                dr = cmd.ExecuteReader();
                dr.Read();
                if (!dr.IsDBNull("Back3Score"))
                {
                    int score = (int)dr["Back3Score"];
                    double dScore = (double)score;
                    dt.Rows[i]["Back3Score"] = dScore;
                    dt.Rows[i]["Back3ScoreNett"] = Math.Round((dScore + (playerHandicap / 6)), 1);
                }
                else
                {
                    dt.Rows[i]["Back3Score"] = 0;
                    dt.Rows[i]["Back3ScoreNett"] = 0;
                }
                dr.Close();

                query = "SELECT Back2Score FROM Score WHERE CompRoundNo = " + NoOfRounds + " AND MemberId = " + Players[i] +
                    " AND CompetitionId = " + CompetitionId;
                SqlCommand();

                dr = cmd.ExecuteReader();
                dr.Read();
                if (!dr.IsDBNull("Back2Score"))
                {
                    int score = (int)dr["Back2Score"];
                    double dScore = (double)score;
                    dt.Rows[i]["Back2Score"] = dScore;
                    dt.Rows[i]["Back2ScoreNett"] = Math.Round((dScore + (playerHandicap / 9)), 1);
                }
                else
                {
                    dt.Rows[i]["Back2Score"] = 0;
                    dt.Rows[i]["Back2ScoreNett"] = 0;
                }
                dr.Close();

                query = "SELECT Back1Score FROM Score WHERE CompRoundNo = " + NoOfRounds + " AND MemberId = " + Players[i] +
                    " AND CompetitionId = " + CompetitionId;
                SqlCommand();

                dr = cmd.ExecuteReader();
                dr.Read();
                if (!dr.IsDBNull("Back1Score"))
                {
                    int score = (int)dr["Back1Score"];
                    double dScore = (double)score;
                    dt.Rows[i]["Back1Score"] = dScore;
                    dt.Rows[i]["Back1ScoreNett"] = Math.Round((dScore + (playerHandicap / 18)), 1);
                }
                else
                {
                    dt.Rows[i]["Back1Score"] = 0;
                    dt.Rows[i]["Back1ScoreNett"] = 0;
                }
                dr.Close();
                CalculateStatistics();
            }   
        }
        private void SqlCommand()
        {
            cmd = new SqlCommand(query, con);
            cmd.CommandType = CommandType.Text;
        }
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("^\\d+$"); // Regex only allows 0-9 to be entered in hole score textboxes
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void DeleteComp_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete competition ?", "Information", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                CompetitionConnection();

                // Delete all scores from the Score database associated with the Competition Id
                query = "DELETE FROM Score WHERE CompetitionId = " + CompetitionId;
                SqlCommand();
                cmd.ExecuteNonQuery();
                // Delete the competition from Competition database
                query = "DELETE FROM Competition WHERE Id = " + CompetitionId;
                SqlCommand();
                cmd.ExecuteNonQuery();
                // Reset players added in Member database
                query = "UPDATE Member SET AddToComp = 'N' WHERE AddToComp = 'Y'";
                SqlCommand();
                cmd.ExecuteNonQuery();
                con.Close();
            }
            // Return to Members control
            this.Content = new Members();
        }
        private void AddRemovePlayerVisibile()
        {
            //Show fields required to add/remove a player from the competition
            playerRegNo.Visibility = Visibility.Visible;
            playerRegText.Visibility = Visibility.Visible;
            confirmAdd.Visibility = Visibility.Visible;
            cancelAdd.Visibility = Visibility.Visible;
            // Hide the add/remove buttons
            addPlayer.Visibility = Visibility.Hidden;
            removePlayer.Visibility = Visibility.Hidden;
            // Set focus to RegNo textbox
            playerRegText.Focus();
        }
        private void AddPlayer_Click(object sender, RoutedEventArgs e)
        {
            AddRemovePlayerVisibile();
            AddRemovePlayer = "Add";
        }
        private void RemovePlayer_Click(object sender, RoutedEventArgs e)
        {
            if(Players.Count == 1)
            {
                // Inform user they cannot delete the last player
                MessageBox.Show("Unable to remove sole competitor");
            }
            else
            {
                AddRemovePlayerVisibile();
                AddRemovePlayer = "Remove";
            }
        }

        private void ConfirmPlayerAddRemove_Click(object sender, RoutedEventArgs e)
        {
            if (AddRemovePlayer == "Add")
            {
                CompetitionConnection();

                // Todo come up with a better check here
                SqlCommand check_User_Name = new SqlCommand("SELECT COUNT(*) FROM Member WHERE (RegNo = @RegNo)", con);
                check_User_Name.Parameters.AddWithValue("@RegNo", playerRegText.Text);
                int UserExist = (int)check_User_Name.ExecuteScalar();

                if (UserExist > 0)
                {
                    query = "SELECT * FROM Member WHERE RegNo = " + playerRegText.Text;
                    SqlCommand();

                    SqlDataReader dr = cmd.ExecuteReader();
                    dr.Read();
                    int addedPlayer = (int)dr["Id"];
                    dr.Close();
                    // First check if the player with that registration number is already added
                    if (!Players.Contains(addedPlayer))
                    {
                        Players.Add(addedPlayer); // Add the player to the Players List<> 
                        int counter = 0;
                        do
                        {
                            counter++;
                            // Get this players handicap from Member database
                            query = "SELECT Handicap FROM Member WHERE Id = " + addedPlayer;
                            SqlCommand();

                            dr = cmd.ExecuteReader();
                            dr.Read();
                            // Cast to double 
                            double playerHandicap = (double)dr["Handicap"];
                            dr.Close();
                            // Adjustment for rounding of 0.5 values
                            if (playerHandicap > 0)
                            {
                                playerHandicap += 0.01;
                            }
                            else
                            {
                                playerHandicap -= 0.01;
                            }
                            // Set the competition handicap value for database addition
                            int compHandicap = (int)Math.Round(playerHandicap);
                            // Add a score record to the Score database, will add a second record if competition 
                            // is a two round competition
                            query = "INSERT INTO Score(MemberId, CompetitionId, CompRoundNo, CompHandicap) VALUES " +
                                "(@MemberId, @CompetitionId, @CompRoundNo, @CompHandicap)";
                            SqlCommand();
                            cmd.Parameters.AddWithValue("@MemberId", addedPlayer);
                            cmd.Parameters.AddWithValue("@CompetitionId", CompetitionId);
                            cmd.Parameters.AddWithValue("@CompRoundNo", counter);
                            cmd.Parameters.AddWithValue("@CompHandicap", compHandicap);
                            cmd.ExecuteScalar();
                        } while (NoOfRounds > counter);
                        // Display competitors with new player added
                        DisplayCompetitorsSavedComp();
                        MessageBox.Show("Players list updated.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // Player with the registration number entered is already in the competition
                        MessageBox.Show("This player has already been added to the competition");
                    }
                }
                else
                {
                    // No player exists with the registration number entered
                    MessageBox.Show("No player with that registration number exists");
                }
            }
            if (AddRemovePlayer == "Remove")
            {
                CompetitionConnection();
                // Todo come up with a better check here
                SqlCommand check_User_Name = new SqlCommand("SELECT COUNT(*) FROM Member WHERE (RegNo = @RegNo)", con);
                check_User_Name.Parameters.AddWithValue("@RegNo", playerRegText.Text);
                int UserExist = (int)check_User_Name.ExecuteScalar();

                if (UserExist > 0)
                {
                    query = "SELECT * FROM Member WHERE RegNo = " + playerRegText.Text;
                    SqlCommand();

                    SqlDataReader dr = cmd.ExecuteReader();
                    dr.Read();
                    int addedPlayer = (int)dr["Id"];
                    dr.Close();
                    if (Players.Contains(addedPlayer))
                    {
                        query = "DELETE FROM Score WHERE MemberId = " + addedPlayer + " AND CompetitionId = " 
                            + CompetitionId;
                        SqlCommand();
                        cmd.ExecuteNonQuery();
                        Players.Remove(addedPlayer);
                        DisplayCompetitorsSavedComp();
                    }
                    else
                    {
                        MessageBox.Show("No player with that registration number is entered");
                    }
                }
                else
                {
                    MessageBox.Show("No player with that registration number exists");
                }
            }
            con.Close();

            HideAddRemovePlayerControls();
        }

        private void CancelPlayerAddRemove_Click(object sender, RoutedEventArgs e)
        {
            HideAddRemovePlayerControls();
        }
        private void HideAddRemovePlayerControls()
        {
            playerRegNo.Visibility = Visibility.Hidden;
            playerRegText.Visibility = Visibility.Hidden;
            confirmAdd.Visibility = Visibility.Hidden;
            cancelAdd.Visibility = Visibility.Hidden;
            addPlayer.Visibility = Visibility.Visible;
            removePlayer.Visibility = Visibility.Visible;
            playerRegText.Clear();
        }

        private void Competition_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            try
            {
                //Create object for DataGrid
                DataGrid grd = (DataGrid)sender;

                //Create object for DataRowView
                DataRowView? row_selected = grd.CurrentItem as DataRowView;

                //row_selected is not null
                if (row_selected != null)
                {

                    //Datagrid items count greater than zero
                    if (competition.Items.Count > 0)
                    {
                        if (grd.SelectedCells.Count > 0)
                        {
                            //Set MemberId variable for row selected
                            MemberId = Int32.Parse(row_selected["Id"].ToString());

                            // Column three is the add player score column
                            if (grd.SelectedCells[0].Column.DisplayIndex == 3)
                            {
                                if (MessageBox.Show("Is the Spare Hole being used ?", "Information", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                                {
                                    // Add controls to set the spare hole value
                                    lblSH.Visibility = Visibility.Visible;
                                    tbSH.Visibility = Visibility.Visible;
                                    addPlayerScore.Visibility = Visibility.Hidden;
                                    btn_Cancel.Visibility = Visibility.Visible;
                                    btn_Confirm.Visibility = Visibility.Visible;
                                }
                                else if (NoOfRounds == 1)
                                {
                                    //Single round competition
                                    RoundOneVisible();
                                }
                                else
                                {
                                    // Two round competition
                                    RoundTwoVisible();
                                }
                            }
                            // Row 4 will remove that player from 
                            if (grd.SelectedCells[0].Column.DisplayIndex == 4)
                            {
                                if (MessageBox.Show("Are you sure you want to hide this player ?", "Information", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                                {
                                    row_selected.Delete();
                                }
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        // Hide the add score controls
        private void ScoresHidden()
        {
            var textboxes = new List<TextBox> {tb1,tb2,tb3,tb4,tb5,tb6,tb7,tb8,tb9,
                                tb10,tb11,tb12,tb13,tb14,tb15,tb16,tb17,tb18,tbf9,tbb9,tbrs,tb1_2,
                                tb2_2,tb3_2,tb4_2,tb5_2,tb6_2,tb7_2,tb8_2,tb9_2,tb10_2,tb11_2,tb12_2,
                                tb13_2,tb14_2,tb15_2,tb16_2,tb17_2,tb18_2,tbf9_2,tbb9_2,tbrs_2};

            foreach (TextBox textbox in textboxes)
            {
                textbox.Visibility = Visibility.Hidden;
                textbox.Text = "";
            }

            var labels = new List<Label> {lbl1,lbl2,lbl3,lbl4,lbl5,lbl6,lbl7,lbl8,lbl9,
                                lbl10,lbl11,lbl12,lbl13,lbl14,lbl15,lbl16,lbl17,lbl18,lblf9,lblb9,lblrs,
                                lbl1_2,lbl2_2,lbl3_2,lbl4_2,lbl5_2,lbl6_2,lbl7_2,lbl8_2,lbl9_2,lbl10_2,
                                lbl11_2,lbl12_2,lbl13_2,lbl14_2,lbl15_2,lbl16_2,lbl17_2,lbl18_2,lblf9_2,
                                lblb9_2,lblrs_2};

            foreach (Label label in labels)
            {
                label.Visibility = Visibility.Hidden;
            }
            submitScore.Visibility = Visibility.Hidden;
            cancelScore.Visibility = Visibility.Hidden;
        }

        private void CancelScoreClick(object sender, RoutedEventArgs e)
        {
            ScoresHidden();
            // Re-enable datagrid functionality and ability to add/remove players and delete the competition
            competition.IsEnabled = true;
            addPlayer.IsEnabled = true;
            removePlayer.IsEnabled = true;
            deleteComp.IsEnabled = true;
        }

        private void SubmitScoreClick(object sender, RoutedEventArgs e)
        {
            var textboxes = new List<TextBox> {tb1,tb2,tb3,tb4,tb5,tb6,tb7,tb8,tb9,tb10,tb11,tb12,tb13,
            tb14,tb15,tb16,tb17,tb18};
            foreach (TextBox textbox in textboxes)
            {
                // Validate score entries
                if (textbox.Text.Length == 0 || textbox.Text == "0")
                {
                    textbox.Clear();
                    textbox.Focus();
                    MessageBox.Show("Please enter a valid score between 1 and 9", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }
            // Calculation of scores 
            int frontNine = int.Parse(tb1.Text) + int.Parse(tb2.Text) + int.Parse(tb3.Text) + int.Parse(tb4.Text)
                    + int.Parse(tb5.Text) + int.Parse(tb6.Text) + int.Parse(tb7.Text) + int.Parse(tb8.Text)
                    + int.Parse(tb9.Text);
            tbf9.Text = frontNine.ToString();
            lblf9.Visibility = Visibility.Visible;
            tbf9.Visibility = Visibility.Visible;

            int backNine = int.Parse(tb10.Text) + int.Parse(tb11.Text) + int.Parse(tb12.Text) + int.Parse(tb13.Text)
                    + int.Parse(tb14.Text) + int.Parse(tb15.Text) + int.Parse(tb16.Text) + int.Parse(tb17.Text)
                    + int.Parse(tb18.Text);
            tbb9.Text = backNine.ToString();
            lblb9.Visibility = Visibility.Visible;
            tbb9.Visibility = Visibility.Visible;

            int backSix = int.Parse(tb13.Text) + int.Parse(tb14.Text) + int.Parse(tb15.Text) +
                int.Parse(tb16.Text) + int.Parse(tb17.Text) + int.Parse(tb18.Text);

            int backThree = int.Parse(tb16.Text) + int.Parse(tb17.Text) + int.Parse(tb18.Text);

            int backTwo = int.Parse(tb17.Text) + int.Parse(tb18.Text);

            int backOne = int.Parse(tb18.Text);

            int roundScore = frontNine + backNine;
            tbrs.Text = roundScore.ToString();
            lblrs.Visibility = Visibility.Visible;
            tbrs.Visibility = Visibility.Visible;

            string spareHoleIncluded;
            if (HoleSelected > 0)
            {
                spareHoleIncluded = "Y";
            }
            else
            {
                spareHoleIncluded = "N";
            }

                CompetitionConnection();
                //Insert query to Save data in the table
                query = "UPDATE Score SET Hole1 = @Hole1, Hole2 = @Hole2, Hole3 = @Hole3, Hole4 = @Hole4, " +
                    "Hole5 = @Hole5, Hole6 = @Hole6, Hole7 = @Hole7, Hole8 = @Hole8, Hole9 = @Hole9, " +
                    "Hole10 = @Hole10, Hole11 = @Hole11, Hole12 = @Hole12, Hole13 = @Hole13, " +
                    "Hole14 = @Hole14, Hole15 = @Hole15, Hole16 = @Hole16, Hole17 = @Hole17, Hole18 = @Hole18, " +
                    "SpareF9 = @SpareF9, SpareB9 = @SpareB9, Front9Score = @Front9Score, Back9Score = @Back9Score, " +
                    "Back6Score = @Back6Score, Back3Score = @Back3Score, Back2Score = @Back2Score," +
                    "Back1Score = @Back1Score, RoundScore = @Roundscore, Date = @Date, Competition = 'Y', " +
                    "SpareHoleIncluded = @SpareHoleIncluded WHERE MemberId = " + MemberId +
                    " AND CompetitionId = " + CompetitionId + " AND CompRoundNo = 1";
                SqlCommand();

                if (HoleSelected == 0)
                {
                    cmd.Parameters.AddWithValue("@SpareF9", 0);
                    cmd.Parameters.AddWithValue("@SpareB9", 0);
                }

                int counter = 0;
                foreach (TextBox textbox in textboxes)
                {
                    counter++;
                    if (counter == HoleSelected)
                    {
                        cmd.Parameters.AddWithValue("@Hole" + counter.ToString(), 0);
                        if (counter > 0 && counter <= 9)
                        {
                            cmd.Parameters.AddWithValue("@SpareF9", textbox.Text);
                            cmd.Parameters.AddWithValue("@SpareB9", 0);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@SpareF9", 0);
                            cmd.Parameters.AddWithValue("@SpareB9", textbox.Text);
                        }
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@Hole" + counter.ToString(), textbox.Text);
                    }
                }

                cmd.Parameters.AddWithValue("@Front9Score", frontNine);
                cmd.Parameters.AddWithValue("@Back9Score", backNine);
                cmd.Parameters.AddWithValue("@Back6Score", backSix);
                cmd.Parameters.AddWithValue("@Back3Score", backThree);
                cmd.Parameters.AddWithValue("@Back2Score", backTwo);
                cmd.Parameters.AddWithValue("@Back1Score", backOne);
                cmd.Parameters.AddWithValue("@RoundScore", roundScore);
                cmd.Parameters.AddWithValue("@Date", DateTime.Now);
                cmd.Parameters.AddWithValue("@SpareHoleIncluded", spareHoleIncluded);

                cmd.ExecuteNonQuery();

                con.Close();
            // If two round competition add second score
            if (NoOfRounds > 1)
            {
                var textbxes = new List<TextBox> {tb1_2,tb2_2,tb3_2,tb4_2,tb5_2,tb6_2,tb7_2,tb8_2,tb9_2,tb10_2,tb11_2,
                tb12_2,tb13_2,tb14_2,tb15_2,tb16_2,tb17_2,tb18_2};
                foreach (TextBox textbx in textbxes)
                {
                    if (textbx.Text.Length == 0 || textbx.Text == "0")
                    {
                        textbx.Clear();
                        textbx.Focus();
                        MessageBox.Show("Please enter a valid score between 1 and 9", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }
                int frontNine_2 = int.Parse(tb1_2.Text) + int.Parse(tb2_2.Text) + int.Parse(tb3_2.Text) +
                int.Parse(tb4_2.Text) + int.Parse(tb5_2.Text) + int.Parse(tb6_2.Text) + int.Parse(tb7_2.Text) +
                int.Parse(tb8_2.Text) + int.Parse(tb9_2.Text);
                tbf9_2.Text = frontNine_2.ToString();
                lblf9_2.Visibility = Visibility.Visible;
                tbf9_2.Visibility = Visibility.Visible;

                int backNine_2 = int.Parse(tb10_2.Text) + int.Parse(tb11_2.Text) + int.Parse(tb12_2.Text) +
                    int.Parse(tb13_2.Text) + int.Parse(tb14_2.Text) + int.Parse(tb15_2.Text) + int.Parse(tb16_2.Text) +
                    int.Parse(tb17_2.Text) + int.Parse(tb18_2.Text);
                tbb9_2.Text = backNine_2.ToString();
                lblb9_2.Visibility = Visibility.Visible;
                tbb9_2.Visibility = Visibility.Visible;

                int backSix_2 = int.Parse(tb13_2.Text) + int.Parse(tb14_2.Text) + int.Parse(tb15_2.Text) +
                int.Parse(tb16_2.Text) + int.Parse(tb17_2.Text) + int.Parse(tb18_2.Text);

                int backThree_2 = int.Parse(tb16_2.Text) + int.Parse(tb17_2.Text) + int.Parse(tb18_2.Text);

                int backTwo_2 = int.Parse(tb17_2.Text) + int.Parse(tb18_2.Text);

                int backOne_2 = int.Parse(tb18_2.Text);

                int roundScore_2 = frontNine_2 + backNine_2;
                tbrs_2.Text = roundScore_2.ToString();
                lblrs_2.Visibility = Visibility.Visible;
                tbrs_2.Visibility = Visibility.Visible;

                CompetitionConnection();
                query = "UPDATE Score SET Hole1 = @Hole1, Hole2 = @Hole2, Hole3 = @Hole3, Hole4 = @Hole4, " +
                    "Hole5 = @Hole5, Hole6=@Hole6, Hole7 = @Hole7, Hole8 = @Hole8, Hole9 = @Hole9, " +
                    "Hole10 = @Hole10, Hole11 = @Hole11, Hole12 = @Hole12, Hole13 = @Hole13, " +
                    "Hole14 = @Hole14, Hole15 = @Hole15, Hole16 = @Hole16, Hole17 = @Hole17, Hole18 = @Hole18, " +
                    "SpareF9 = @SpareF9, SpareB9 = @SpareB9, Front9Score = @Front9Score, Back9Score = @Back9Score, " +
                    "Back6Score = @Back6Score, Back3Score = @Back3Score, Back2Score = @Back2Score," +
                    "Back1Score = @Back1Score, RoundScore = @Roundscore, Date = @Date, Competition = 'Y', " +
                    "SpareHoleIncluded = @SpareHoleIncluded WHERE MemberId = " + MemberId +
                    " AND CompetitionId = " + CompetitionId + " AND CompRoundNo = 2";
                SqlCommand();

                if (HoleSelected == 0)
                {
                    cmd.Parameters.AddWithValue("@SpareF9", 0);
                    cmd.Parameters.AddWithValue("@SpareB9", 0);
                }

                counter = 0;
                foreach (TextBox textbx in textbxes)
                {
                    counter++;
                    if (counter == HoleSelected)
                    {
                        cmd.Parameters.AddWithValue("@Hole" + counter.ToString(), 0);
                        if (counter > 0 && counter <= 9)
                        {
                            cmd.Parameters.AddWithValue("@SpareF9", textbx.Text);
                            cmd.Parameters.AddWithValue("@SpareB9", 0);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@SpareF9", 0);
                            cmd.Parameters.AddWithValue("@SpareB9", textbx.Text);
                        }
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@Hole" + counter.ToString(), textbx.Text);
                    }
                }

                cmd.Parameters.AddWithValue("@Front9Score", frontNine_2);
                cmd.Parameters.AddWithValue("@Back9Score", backNine_2);
                cmd.Parameters.AddWithValue("@Back6Score", backSix_2);
                cmd.Parameters.AddWithValue("@Back3Score", backThree_2);
                cmd.Parameters.AddWithValue("@Back2Score", backTwo_2);
                cmd.Parameters.AddWithValue("@Back1Score", backOne_2);
                cmd.Parameters.AddWithValue("@RoundScore", roundScore_2);
                cmd.Parameters.AddWithValue("@Date", DateTime.Now);
                cmd.Parameters.AddWithValue("@SpareHoleIncluded", spareHoleIncluded);

                cmd.ExecuteNonQuery();

                con.Close();
            }
                
            MessageBox.Show("Data saved successfully", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            ScoresHidden();

            // Re-enable datagrid
            competition.IsEnabled = true;
            // Re-enable ability to add/remove players and delete competition
            addPlayer.IsEnabled = true;
            removePlayer.IsEnabled = true;
            deleteComp.IsEnabled = true;

            // Refresh the display with added scores
            DisplayCompetitorsSavedComp();
            // Recalculate the competition statistics and display
            CalculateStatistics();
        }
        // Show all neccessary to add a score for a single round
        private void RoundOneVisible()
        {
            var textboxes = new List<TextBox> {tb1,tb2,tb3,tb4,tb5,tb6,tb7,tb8,tb9,
                                tb10,tb11,tb12,tb13,tb14,tb15,tb16,tb17,tb18};

            foreach (TextBox textbox in textboxes)
            {
                textbox.Visibility = Visibility.Visible;
            }
            var labels = new List<Label> {lbl1,lbl2,lbl3,lbl4,lbl5,lbl6,lbl7,lbl8,lbl9,
                                lbl10,lbl11,lbl12,lbl13,lbl14,lbl15,lbl16,lbl17,lbl18};
            int counter = 0;
            foreach (Label label in labels)
            {
                counter++;
                label.Content = counter.ToString();
                label.Visibility = Visibility.Visible;
            }
            if (HoleSelected > 0)
            {
                labels[HoleSelected - 1].Content = "SH";
            }
            AddingScoresEnableDisable();
        }
        // Two round competition, second set of holes available
        private void RoundTwoVisible()
        {
            var textboxes = new List<TextBox> {tb1,tb2,tb3,tb4,tb5,tb6,tb7,tb8,tb9,
                                tb10,tb11,tb12,tb13,tb14,tb15,tb16,tb17,tb18,tb1_2,
                                tb2_2,tb3_2,tb4_2,tb5_2,tb6_2,tb7_2,tb8_2,tb9_2,tb10_2,tb11_2,tb12_2,
                                tb13_2,tb14_2,tb15_2,tb16_2,tb17_2,tb18_2};

            foreach (TextBox textbox in textboxes)
            {
                textbox.Visibility = Visibility.Visible;
            }
            var labels = new List<Label> {lbl1,lbl2,lbl3,lbl4,lbl5,lbl6,lbl7,lbl8,lbl9,
                                lbl10,lbl11,lbl12,lbl13,lbl14,lbl15,lbl16,lbl17,lbl18};

            int counter = 0;
            foreach (Label lbels in labels)
            {
                counter++;
                lbels.Content = counter.ToString();
                lbels.Visibility = Visibility.Visible;
            }
            if (HoleSelected > 0)
            {
                labels[HoleSelected - 1].Content = "SH";
            }
            var label = new List<Label> {lbl1_2,lbl2_2,lbl3_2,
                                lbl4_2,lbl5_2,lbl6_2,lbl7_2,lbl8_2,lbl9_2,lbl10_2,lbl11_2,lbl12_2,lbl13_2,
                                lbl14_2,lbl15_2,lbl16_2,lbl17_2,lbl18_2};
            counter = 0;
            foreach (Label labls in label)
            {
                counter++;
                labls.Content = counter.ToString();
                labls.Visibility = Visibility.Visible;
            }
            if (HoleSelected > 0)
            {
                label[HoleSelected - 1].Content = "SH";
            }
            AddingScoresEnableDisable();
        }
        private void AddingScoresEnableDisable()
        {
            // Disable any change to Datagrid
            competition.IsEnabled = false;
            // Disable ability to add/remove players and delete competition
            addPlayer.IsEnabled = false;
            removePlayer.IsEnabled = false;
            deleteComp.IsEnabled = false;
            // Display buttons to submit or cancel the score addition
            submitScore.Visibility = Visibility.Visible;
            cancelScore.Visibility = Visibility.Visible;
        }

        private void confirmSpareHoleClick(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(tbSH.Text, out int num))
            {
                // Validate entry for spare hole to be valid hole number, set it to HoleSelected variable
                // and hide setting capability
                if (num >= 1 && num <= 18)
                {
                    HoleSelected = num;
                    HideSpareHoleSettings();

                    if (NoOfRounds == 1)
                    {
                        RoundOneVisible();
                    }
                    else
                    {
                        RoundTwoVisible();
                    }
                }
                else
                {
                    MessageBox.Show("Please enter a number between 1 and 18");
                    tbSH.Clear();
                    tbSH.Focus();
                }
            }
        }

        private void cancelSpareHoleClick(object sender, RoutedEventArgs e)
        {
            HideSpareHoleSettings();
        }
        private void HideSpareHoleSettings()
        {
            lblSH.Visibility = Visibility.Hidden;
            tbSH.Visibility = Visibility.Hidden;
            addPlayerScore.Visibility = Visibility.Visible;
            btn_Cancel.Visibility = Visibility.Hidden;
            btn_Confirm.Visibility = Visibility.Hidden;
        }
        // Return to Members control
        private void ReturnToMembersClick(object sender, RoutedEventArgs e)
        {
            this.Content = new Members();
        }
        // Calculate hole statistics from all scores entered
        private void CalculateStatistics()
        {
            DataTable dt = new DataTable();

            CompetitionConnection();

            query = "SELECT * FROM Score WHERE CompetitionId = " + CompetitionId; ;
            SqlCommand();
            da = new SqlDataAdapter(cmd);
            da.Fill(dt);

            double sum = 0;
            double averageScore = 0;
            double count = Convert.ToDouble(dt.Compute("COUNT(RoundScore)", "(Roundscore) > 0"));
            if (count != 0)
            {
                sum = Convert.ToDouble(dt.Compute("SUM(RoundScore)", string.Empty));
                count = Convert.ToDouble(dt.Compute("COUNT(RoundScore)", "(Roundscore) > 0"));
                averageScore = sum / count;
            }

            List<TextBox> holeList = new List<TextBox> {tbst1,tbst2,tbst3,tbst4,tbst5,tbst6,tbst7,tbst8,tbst9,
                tbst10,tbst11,tbst12,tbst13,tbst14,tbst15,tbst16,tbst17,tbst18};
            int counter = 0;
            string hole = "";
            foreach (TextBox textbox in holeList)
            {
                counter++;
                hole = "Hole" + counter.ToString();
                // Get the count of scores where the score is not zero
                count = Convert.ToDouble(dt.Compute("COUNT(" + hole + ")", "(" + hole + ") > 0"));
                if (count != 0)
                {
                    // Sum all hole scores
                    sum = Convert.ToDouble(dt.Compute("SUM(" + hole + ")", string.Empty));
                    averageScore = sum / count;
                    textbox.Text = (Math.Round(averageScore, 2)).ToString();
                    // Set different background colours of textboxes for scores above/below par
                    if (averageScore > 3)
                    {
                        textbox.Background = Brushes.LightPink;
                    }
                    else if (averageScore < 3)
                    {
                        textbox.Background = Brushes.LightGreen;
                    }
                    else
                    {
                        textbox.Background = Brushes.LightBlue;
                    }
                }
                else
                {
                    textbox.Text = "0";
                    textbox.Background = Brushes.White;
                }
            }

            count = Convert.ToDouble(dt.Compute("COUNT(Front9Score)", "(Front9Score) > 0"));
            if (count != 0)
            {
                sum = Convert.ToDouble(dt.Compute("SUM(Front9Score)", string.Empty));
                averageScore = sum / count;
                tbstf9.Text = (Math.Round(averageScore, 2)).ToString();
            }
            else
            {
                tbstf9.Text = "0";
            }
            count = Convert.ToDouble(dt.Compute("COUNT(Back9Score)", "(Back9Score) > 0"));
            if (count != 0)
            {
                sum = Convert.ToDouble(dt.Compute("SUM(Back9Score)", string.Empty));
                averageScore = sum / count;
                tbstb9.Text = (Math.Round(averageScore, 2)).ToString();
            }
            else
            {
                tbstb9.Text = "0";
            }
            count = Convert.ToDouble(dt.Compute("COUNT(RoundScore)", "(RoundScore) > 0"));
            if (count != 0)
            {
                sum = Convert.ToDouble(dt.Compute("SUM(RoundScore)", string.Empty));
                averageScore = sum / count;
                tbstrs.Text = (Math.Round(averageScore, 2)).ToString();
            }
            else
            {
                tbstrs.Text = "0";
            }
        }
        // This will re-add all players hidden in datagrid
        private void ShowAllCompetitors_Click(object sender, RoutedEventArgs e)
        {
            DisplayCompetitorsSavedComp();
        }
    }
}
