using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
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

namespace MellowsApp2
{
    /// <summary>
    /// Interaction logic for Player.xaml
    /// </summary>
    public partial class Player : UserControl
    {
        SqlConnection con = new SqlConnection();
        SqlCommand cmd = new SqlCommand();
        SqlDataAdapter da = new SqlDataAdapter();

        private int HoleSelected;
        private int MemberId;
        private string query;
        private int ScoreId;

        public Player(string playerSelected, double playerHandicap, int playerRegNo, int memberId)
        {
            InitializeComponent();
            // Use variables passed to populate display
            SelectedPlayer.Content = playerSelected;
            FullHandicap.Content = playerHandicap.ToString("0.#");
            RegNo.Content = playerRegNo;
            PlayingHandicap.Content = Math.Round(playerHandicap);
            MemberId = memberId;
            // Populate all player scores
            query = "SELECT * from Score WHERE MemberId = " + MemberId;
            UpdateScores();
        }
        public void ScoreConnection()
        {
            try
            {
                //Database connection string
                String Conn = ConfigurationManager.ConnectionStrings["ConnectionStringMellows"].ConnectionString;
                con = new SqlConnection(Conn);
                con.Open();
            }
            catch
            {
                MessageBox.Show("Problem encountered connecting to data!");
            }
        }
        private void UpdateScores()
        {
            try
            {
                ScoreConnection();
                DataTable dt = new DataTable();

                cmd = new SqlCommand(query, con);
                cmd.CommandType = CommandType.Text;

                da = new SqlDataAdapter(cmd);
                da.Fill(dt);

                if (dt != null && dt.Rows.Count > 0)
                {
                    //Assign DataTable data to playerScores DataGrid using ItemSource property.   
                    playerScores.ItemsSource = dt.DefaultView;
                }
                else
                {
                    playerScores.ItemsSource = null;
                }
                // Create variables for score calculations, doubles used as will have fractional results
                double sum = 0;
                double averageScore = 0;
                // Check if any scores added
                double count = Convert.ToDouble(dt.Compute("COUNT(RoundScore)", "(Roundscore) > 0"));
                // If scores are added for player sum all scores and divide by no. of scores for average
                if (count != 0)
                {
                    sum = Convert.ToDouble(dt.Compute("SUM(RoundScore)", string.Empty));
                    count = Convert.ToDouble(dt.Compute("COUNT(RoundScore)", "(Roundscore) > 0"));
                    averageScore = sum / count;
                    AverageScore.Content = string.Format("{0:0.00}", averageScore);// Populate average score Label
                    tbstrs.Text = (Math.Round(averageScore, 2)).ToString(); // Populate average score textbox in Player Stats
                }
                // Get players lowest score and populate lowest score Label
                int min = Convert.ToInt32(dt.Compute("MIN(RoundScore)", string.Empty));
                MinScore.Content = min;

                // 
                List<TextBox> list = new List<TextBox> {tbst1,tbst2,tbst3,tbst4,tbst5,tbst6,tbst7,tbst8,tbst9,
                tbst10,tbst11,tbst12,tbst13,tbst14,tbst15,tbst16,tbst17,tbst18};
                int counter = 0;
                string hole = "";
                foreach (TextBox tb in list)
                {
                    counter++;
                    hole = "Hole" + counter.ToString();
                    count = Convert.ToDouble(dt.Compute("COUNT(" + hole + ")", "(" + hole + ") > 0"));
                    if (count != 0)
                    {
                        sum = Convert.ToDouble(dt.Compute("SUM(" + hole + ")", string.Empty));
                        averageScore = sum / count;
                        tb.Text = (Math.Round(averageScore, 2)).ToString();
                        if (averageScore > 3)
                        {
                            tb.Background = Brushes.LightPink;
                        }
                        else if (averageScore < 3)
                        {
                            tb.Background = Brushes.LightGreen;
                        }
                        else
                        {
                            tb.Background = Brushes.LightBlue;
                        }
                    }
                    else
                    {
                        tb.Text = "0";
                        tb.Background = Brushes.White;
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
            }
            catch (Exception e)
            {
                MessageBox.Show("No Scores added for this Member");
            }
            finally
            {
                con.Close();
            }
        }
        private void AddScoreVisible()
        {
            var textboxes = new List<TextBox> {tb1,tb2,tb3,tb4,tb5,tb6,tb7,tb8,tb9,
                tb10,tb11,tb12,tb13,tb14,tb15,tb16,tb17,tb18};
            foreach (TextBox textbox in textboxes)
            {
                // Make textboxes for setting hole scores visible
                textbox.Visibility = Visibility.Visible;
            }
            var labels = new List<Label> {lbl1,lbl2,lbl3,lbl4,lbl5,lbl6,lbl7,lbl8,lbl9,
                lbl10,lbl11,lbl12,lbl13,lbl14,lbl15,lbl16,lbl17,lbl18};
            int counter = 0;
            foreach (Label label in labels)
            {
                // Set labels to 1 to 18 aand make visible
                counter++;
                label.Content = counter.ToString();
                label.Visibility = Visibility.Visible;
            }
            if (HoleSelected > 0)
            {
                // Change label of excluded hole to 'SH'
                labels[HoleSelected - 1].Content = "SH";
            }
            // Hide Add Score button and add buttons to submit or cancel the score
            addNewScore.Visibility = Visibility.Hidden;
            cancelScore.Visibility = Visibility.Visible;
            submitScore.Visibility = Visibility.Visible;
        }
        //Reverse of score visible
        private void RemoveScoreVisible()
        {
            var textboxes = new List<TextBox> {tb1,tb2,tb3,tb4,tb5,tb6,tb7,tb8,tb9,
                tb10,tb11,tb12,tb13,tb14,tb15,tb16,tb17,tb18,tbf9,tbb9,tbrs};
            foreach (TextBox textbox in textboxes)
            {
                textbox.Visibility = Visibility.Hidden;
                textbox.Text = "";
            }
            var labels = new List<Label> {lbl1,lbl2,lbl3,lbl4,lbl5,lbl6,lbl7,lbl8,lbl9,
                lbl10,lbl11,lbl12,lbl13,lbl14,lbl15,lbl16,lbl17,lbl18,lblf9,lblb9,lblrs};
            foreach (Label label in labels)
            {
                label.Visibility = Visibility.Hidden;
            }
            cancelScore.Visibility = Visibility.Hidden;
            submitScore.Visibility = Visibility.Hidden;
            addNewScore.Visibility = Visibility.Visible;
        }

        private void btn_AddNewScore(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Is the Spare Hole being used ?", "Information", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                // Make fields to select the hole being substituted for the spare hole visible
                lblSH.Visibility = Visibility.Visible;
                tbSH.Visibility = Visibility.Visible;
                addNewScore.Visibility = Visibility.Hidden;
                btn_Cancel.Visibility = Visibility.Visible;
                btn_Confirm.Visibility = Visibility.Visible;
            }
            else
            {
                AddScoreVisible();
            }
        }

        private void Members_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
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
                    // Scores to display??
                    if (playerScores.Items.Count > 0)
                    {
                        if (grd.SelectedCells.Count > 0)
                        {
                            // Set variable for selected row
                            ScoreId = Int32.Parse(row_selected["Id"].ToString());
                            // Column 29 is the delete column
                            if (grd.SelectedCells[0].Column.DisplayIndex == 29)
                            {
                                //Show confirmation dialogue box
                                if (MessageBox.Show("Are you sure you want to delete ?", "Information", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                                {
                                    ScoreConnection();
                                    DataTable dt = new DataTable();

                                    //Execute delete query for delete record from table using Id
                                    query = "DELETE FROM Score WHERE Id = @Id";
                                    cmd = new SqlCommand(query, con);
                                    cmd.CommandType = CommandType.Text;

                                    //Id set in @Id parameter and send it in delete statement
                                    cmd.Parameters.AddWithValue("@Id", ScoreId);
                                    cmd.ExecuteNonQuery();
                                    con.Close();

                                    // Display scores, deleted score removed
                                    query = "SELECT * from Score WHERE MemberId=" + MemberId;
                                    UpdateScores();
                                    MessageBox.Show("Score deleted successfully", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("^\\d+$"); // Regex only allows 0-9 for hole score fields
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void confirm_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(tbSH.Text, out int num))
            {
                // Check if number is a valid hole number between 1 and 18
                if (num >= 1 && num <= 18)
                {
                    // Set hole being replaced to variable
                    HoleSelected = num;
                    // Hide fields to set spare hole 
                    lblSH.Visibility = Visibility.Hidden;
                    tbSH.Visibility = Visibility.Hidden;
                    btn_Cancel.Visibility = Visibility.Hidden;
                    btn_Confirm.Visibility = Visibility.Hidden;
                    // Make hole score fields visible
                    AddScoreVisible();
                }
                else
                {
                    MessageBox.Show("Please enter a number between 1 and 18");
                    tbSH.Clear();
                    tbSH.Focus();
                }
            }
        }

        private void cancel_Click(object sender, RoutedEventArgs e)
        {
            // Hide spare hole and show add score button
            lblSH.Visibility = Visibility.Hidden;
            tbSH.Visibility = Visibility.Hidden;
            btn_Cancel.Visibility = Visibility.Hidden;
            btn_Confirm.Visibility = Visibility.Hidden;
            addNewScore.Visibility = Visibility.Visible;
        }
        // Filters to show competition, practice and all rounds for member
        private void btn_CompButton(object sender, RoutedEventArgs e)
        {
            query = "SELECT * from Score WHERE MemberId=" + MemberId + " AND Competition='Y'";
            UpdateScores();
        }

        private void btn_PracticeButton(object sender, RoutedEventArgs e)
        {
            query = "SELECT * from Score WHERE MemberId=" + MemberId + " AND Competition='N'";
            UpdateScores();
        }

        private void btn_AllButton(object sender, RoutedEventArgs e)
        {
            query = "SELECT * from Score WHERE MemberId=" + MemberId;
            UpdateScores();
        }
        // Return to Members control
        private void btn_ChangePlayer(object sender, RoutedEventArgs e)
        {
            this.Content = new Members();
        }

        private void btn_SubmitScore(object sender, RoutedEventArgs e)
        {
            var textboxes = new List<TextBox> {tb1,tb2,tb3,tb4,tb5,tb6,tb7,tb8,tb9,
        tb10,tb11,tb12,tb13,tb14,tb15,tb16,tb17,tb18};
            foreach (TextBox textbox in textboxes)
            {
                // Ensures score entered for each hole
                if (textbox.Text.Length == 0 || textbox.Text == "0")
                {
                    textbox.Clear();
                    textbox.Focus();
                    MessageBox.Show("Please enter a valid score between 1 and 9", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }
            // Calculate front nine score and display
            int frontNine = int.Parse(tb1.Text) + int.Parse(tb2.Text) + int.Parse(tb3.Text) + int.Parse(tb4.Text)
                    + int.Parse(tb5.Text) + int.Parse(tb6.Text) + int.Parse(tb7.Text) + int.Parse(tb8.Text)
                    + int.Parse(tb9.Text);
            tbf9.Text = frontNine.ToString();
            lblf9.Visibility = Visibility.Visible;
            tbf9.Visibility = Visibility.Visible;
            // Calculate back nine score and display
            int backNine = int.Parse(tb10.Text) + int.Parse(tb11.Text) + int.Parse(tb12.Text) + int.Parse(tb13.Text)
                    + int.Parse(tb14.Text) + int.Parse(tb15.Text) + int.Parse(tb16.Text) + int.Parse(tb17.Text)
                    + int.Parse(tb18.Text);
            tbb9.Text = backNine.ToString();
            lblb9.Visibility = Visibility.Visible;
            tbb9.Visibility = Visibility.Visible;
            // Calculate back six score, back three, back two and back one to add to competition database
            int backSix = int.Parse(tb13.Text)+ int.Parse(tb14.Text) + int.Parse(tb15.Text) + int.Parse(tb16.Text) 
                    + int.Parse(tb17.Text) + int.Parse(tb18.Text);

            int backThree = int.Parse(tb16.Text) + int.Parse(tb17.Text) + int.Parse(tb18.Text);

            int backTwo = int.Parse(tb17.Text) + int.Parse(tb18.Text);

            int backOne = int.Parse(tb18.Text);
            // Calculate full round score and display
            int roundScore = frontNine + backNine;
            tbrs.Text = roundScore.ToString();
            lblrs.Visibility = Visibility.Visible;
            tbrs.Visibility = Visibility.Visible;
            // Set field for database to determine whether the spare hole was included in a round
            string spareHoleIncluded;
            if (HoleSelected > 0)
            {
                spareHoleIncluded = "Y";
            }
            else
            {
                spareHoleIncluded = "N";
            }

            if (MessageBox.Show("Are you sure you want to Save ?", "Information", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ScoreConnection();
                //Insert query to Save data in the table
                cmd = new SqlCommand("INSERT INTO Score(Hole1, Hole2, Hole3, Hole4, Hole5," +
                    "Hole6, Hole7, Hole8, Hole9, SpareF9, Hole10, Hole11, Hole12, Hole13, Hole14," +
                    "Hole15, Hole16, Hole17, Hole18, SpareB9, Front9Score, Back9Score, Back6Score, " +
                    "Back3Score, Back2Score, Back1Score, RoundScore, " +
                    "Date, Competition, SpareHoleIncluded, MemberId) VALUES(@Hole1, @Hole2, @Hole3, @Hole4, " +
                    "@Hole5, @Hole6, @Hole7, @Hole8, @Hole9, @SpareF9, @Hole10, @Hole11, @Hole12, " +
                    "@Hole13, @Hole14, @Hole15, @Hole16, @Hole17, @Hole18, @SpareB9, @Front9Score, " +
                    "@Back9Score, @Back6Score, @Back3Score, @Back2Score, @Back1Score, @RoundScore, @Date, " +
                    "@Competition, @SpareHoleIncluded, @MemberId)", con);
                cmd.CommandType = CommandType.Text;

                // Spare hole not used, set both fields to zero
                if (HoleSelected == 0)
                {
                    cmd.Parameters.AddWithValue("@SpareF9", 0);
                    cmd.Parameters.AddWithValue("@SpareB9", 0);
                }

                int counter = 0;
                foreach (TextBox textbox in textboxes)
                {
                    counter++; // counter (hole count) starts at 1 not zero

                    if (counter == HoleSelected)
                    {
                        // Set the hole being replaced by the spare hole value to zero
                        cmd.Parameters.AddWithValue("@Hole" + counter.ToString(), 0);
                        // If spare hole being used on front nine or back nine
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
                        // Set all other hole values from textbox entries
                        cmd.Parameters.AddWithValue("@Hole" + counter.ToString(), textbox.Text);
                    }
                }
                // Front nine, back nine and round scores were calculated and added to tbf9, tbb9 and tbrs fields 
                cmd.Parameters.AddWithValue("@Front9Score", tbf9.Text);
                cmd.Parameters.AddWithValue("@Back9Score", tbb9.Text);
                cmd.Parameters.AddWithValue("@Back6Score", backSix);
                cmd.Parameters.AddWithValue("@Back3Score", backThree);
                cmd.Parameters.AddWithValue("@Back2Score", backTwo);
                cmd.Parameters.AddWithValue("@Back1Score", backOne);
                cmd.Parameters.AddWithValue("@RoundScore", tbrs.Text);
                cmd.Parameters.AddWithValue("@Date", DateTime.Now);
                cmd.Parameters.AddWithValue("@Competition", "N"); // Rounds added by member are not competition rounds
                cmd.Parameters.AddWithValue("@SpareHoleIncluded", spareHoleIncluded);
                cmd.Parameters.AddWithValue("@MemberId", MemberId);

                cmd.ExecuteNonQuery();
                con.Close();
                // Redisplay all scores
                query = "SELECT * from Score WHERE MemberId=" + MemberId;
                UpdateScores();
                MessageBox.Show("Data saved successfully", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                RemoveScoreVisible();
            }
        }

        private void btn_CancelScore(object sender, RoutedEventArgs e)
        {
            RemoveScoreVisible();
        }
    }
}
