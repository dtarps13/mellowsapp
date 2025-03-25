using System;
using System.Collections;
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
    /// Interaction logic for Members.xaml
    /// </summary>
    public partial class Members : UserControl
    {
        SqlConnection con = new SqlConnection();
        SqlCommand cmd = new SqlCommand();
        SqlDataAdapter da = new SqlDataAdapter();

        private string query = "";
        private int MemberId = 0;
        private string PlayerSelected = "";
        private double PlayerHandicap = 0;
        private int PlayerRegNo = 0;
        private List<int> CompetitionPlayer = new List<int>();
        private string CompetitionName = "";
        private int CompetitionRounds = 0;
        private int CompetitionId = 0;

        public Members()
        {
            InitializeComponent();
            DisplayMembers();
            DisplayCompetitionList();
        }
        public void MembersConnection()
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
        public void SqlTextCommand()
        {
            cmd = new SqlCommand(query, con);
            cmd.CommandType = CommandType.Text;
        }
        private void PopulateDataGrid(string query, DataGrid dg)
        {
            DataTable dt = new DataTable();

            SqlTextCommand();

            da = new SqlDataAdapter(cmd);
            da.Fill(dt);

            if (dt != null && dt.Rows.Count > 0)
            {
                //Assign DataTable data to Members Table using ItemSource property.   
                dg.ItemsSource = dt.DefaultView;
            }
            else
            {
                dg.ItemsSource = null;
            }
        }
        public void DisplayMembers()
        {
            MembersConnection();

            query = "SELECT * from Member WHERE AddToComp = 'N'";
            PopulateDataGrid(query, membersList);

            con.Close();
        }
        public void DisplayCompetitionPlayers()
        {
            MembersConnection();

            query = "SELECT * from Member WHERE AddToComp = 'Y'";
            PopulateDataGrid(query, competitionPlayers);

            con.Close();
        }
        public void DisplayCompetitionList()
        {
            MembersConnection();

            query = "SELECT * from Competition";
            PopulateDataGrid(query, competitionList);

            con.Close();
        }
        // Members table filters
        private void FilterFemaleClick(object sender, RoutedEventArgs e)
        {
            query = "SELECT * from Member WHERE MaleOrFemale = 'F'";
            PopulateDataGrid(query, membersList);
        }
        private void FilterMaleClick(object sender, RoutedEventArgs e)
        {
            query = "SELECT * from Member WHERE MaleOrFemale = 'M'";
            PopulateDataGrid(query, membersList);
        }
        private void FilterJuvenileClick(object sender, RoutedEventArgs e)
        {
            query = "SELECT * from Member WHERE Juvenile = 'Y'";
            PopulateDataGrid(query, membersList);
        }
        private void ShowAllMembersClick(object sender, RoutedEventArgs e)
        {
            query = "SELECT * from Member ";
            PopulateDataGrid(query, membersList);
        }
        private void FilterSeniorClick(object sender, RoutedEventArgs e)
        {
            query = "SELECT * from Member WHERE Handicap > -2.5 ";
            PopulateDataGrid(query, membersList);
        }
        private void FilterIntermediateClick(object sender, RoutedEventArgs e)
        {
            query = "SELECT * from Member WHERE Handicap > -7.5 AND Handicap <= -2.5 ";
            PopulateDataGrid(query, membersList);
        }
        private void FilterJuniorClick(object sender, RoutedEventArgs e)
        {
            query = "SELECT * from Member WHERE Handicap < -7.4 ";
            PopulateDataGrid(query, membersList);
        }
        private void ClearMemberAddEditText()
        {
            memberName.Text = string.Empty;
            memberRegNo.Text = string.Empty;
            handicap.Text = string.Empty;
            maleOrFemale.Text = string.Empty;
            juvenile.Text = string.Empty;

            btnSave.Content = "Save";
        }
        private void ClearViewPlayer()
        {
            viewPlayer.Text = string.Empty;
            btnView.IsEnabled = false;
            btnView.Content = "Select Player From Table";
        }
        private void EnableViewPlayer()
        {
            btnView.IsEnabled = true;
            btnView.Content = "View Player";
        }
        private void ClearMaster()
        {
            ClearMemberAddEditText();
            ClearViewPlayer();

            MemberId = 0;

            DisplayMembers();
        }
        // Method to handle functionality of membership datagrid
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

                    //membership items count greater than zero
                    if (membersList.Items.Count > 0)
                    {
                        if (grd.SelectedCells.Count > 0)
                        {

                            //Get selected row Id column value and Set variables
                            MemberId = Int32.Parse(row_selected["Id"].ToString());
                            PlayerSelected = row_selected["Member"].ToString();
                            PlayerRegNo = Int32.Parse(row_selected["RegNo"].ToString());
                            PlayerHandicap = double.Parse(row_selected["Handicap"].ToString());

                            // Adjusting handicaps for rounding (0.5 should round up but doesn't)
                            if (PlayerHandicap > 0)
                            {
                                PlayerHandicap += 0.01;
                            }
                            else
                            {
                                PlayerHandicap -= 0.01;
                            }

                            //DisplayIndex is equal to zero then it is Edit cell
                            if (grd.SelectedCells[0].Column.DisplayIndex == 0)
                            {
                                //Get selected row values and add to relevant textboxes
                                memberName.Text = row_selected["Member"].ToString();
                                memberRegNo.Text = row_selected["RegNo"].ToString();
                                handicap.Text = row_selected["Handicap"].ToString();
                                maleOrFemale.Text = row_selected["MaleOrFemale"].ToString();
                                juvenile.Text = row_selected["Juvenile"].ToString();

                                //Change Save button text 'Save' to Update as we are updating an existing member
                                btnSave.Content = "Update";
                                // Disable and Clear View Player functionality
                                ClearViewPlayer();
                            }
                            // If non functional columns selected just clear data
                            else if (grd.SelectedCells[0].Column.DisplayIndex == 2
                                || grd.SelectedCells[0].Column.DisplayIndex == 3
                                || grd.SelectedCells[0].Column.DisplayIndex == 4)
                            {
                                ClearMaster();
                            }

                            //DisplayIndex is equal to five then it is Delete cell                    
                            else if (grd.SelectedCells[0].Column.DisplayIndex == 5)
                            {
                                if (MessageBox.Show("Are you sure you want to delete ?", "Information", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                                {
                                    MembersConnection();

                                    //Execute delete query for delete record from table using Id
                                    query = "DELETE FROM Member WHERE Id = @Id";
                                    SqlTextCommand();

                                    cmd.Parameters.AddWithValue("@Id", MemberId);
                                    cmd.ExecuteNonQuery();

                                    con.Close();

                                    DisplayMembers();

                                    MessageBox.Show("Data deleted successfully", "Information", MessageBoxButton.OK, MessageBoxImage.Information);

                                    ClearMaster();
                                }
                            }
                            // This column selects a player to view, player name added to View Player textbox and
                            // button enabled, other textboxes cleared
                            else if (grd.SelectedCells[0].Column.DisplayIndex == 6)
                            {
                                viewPlayer.Text = row_selected["Member"].ToString();
                                EnableViewPlayer();
                                ClearMemberAddEditText();
                            }
                            // This column only visible when new competitiion is created, adds player to competitor
                            // list by updating AddToComp field and adding to CompetitionPlayer List<>
                            else if (grd.SelectedCells[0].Column.DisplayIndex == 7)
                            {
                                MembersConnection();

                                query = "UPDATE Member SET AddToComp = 'Y' WHERE Id = @Id";
                                SqlTextCommand();

                                cmd.Parameters.AddWithValue("@Id", MemberId);
                                cmd.ExecuteNonQuery();

                                con.Close();

                                CompetitionPlayer.Add(MemberId);

                                DisplayMembers();
                                DisplayCompetitionPlayers();
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
        // Method to handle functionality of competitionPlayer datagrid
        private void CompetitionPlayers_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
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

                    //competition items count greater than zero
                    if (competitionPlayers.Items.Count > 0)
                    {
                        if (grd.SelectedCells.Count > 0)
                        {
                            //DisplayIndex is equal to two then it is Remove Player cell
                            if (grd.SelectedCells[0].Column.DisplayIndex == 2)
                            {
                                //Get selected row MemberId column value and set variable
                                MemberId = Int32.Parse(row_selected["Id"].ToString());

                                MembersConnection();

                                // Removes player selected from competition players and CompetitionPlayer List<>
                                query = "UPDATE Member SET AddToComp = 'N' WHERE Id = @Id";
                                SqlTextCommand();

                                cmd.Parameters.AddWithValue("@Id", MemberId);
                                cmd.ExecuteNonQuery();

                                con.Close();

                                CompetitionPlayer.Remove(MemberId);

                                DisplayMembers();
                                DisplayCompetitionPlayers();
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
        // Method to handle functionality of competitionList datagrid
        private void CompetitionList_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
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

                    //competitionList items count greater than zero
                    if (competitionList.Items.Count > 0)
                    {
                        if (grd.SelectedCells.Count > 0)
                        {
                            //DisplayIndex is equal to three then it is remove cell, set variables
                            if (grd.SelectedCells[0].Column.DisplayIndex == 3)
                            {
                                CompetitionId = Int32.Parse(row_selected["Id"].ToString());
                                CompetitionName = row_selected["CompName"].ToString();
                                CompetitionRounds = Int32.Parse(row_selected["NoOfRounds"].ToString());

                                MembersConnection();

                                //Will use CompetitionId to populate the competitors from the Score
                                // database table and also add them to the CompetitionPlayer List<>
                                query = "SELECT * FROM Score WHERE CompetitionId = " +
                                    CompetitionId + " AND CompRoundNo = '1'";
                                SqlTextCommand();

                                SqlDataReader dr = cmd.ExecuteReader();
                                while (dr.Read())
                                {
                                    if (!CompetitionPlayer.Contains((int)(dr["MemberId"])))
                                    {
                                        CompetitionPlayer.Add((int)(dr["MemberId"]));
                                    }
                                }

                                dr.Close();

                                con.Close();

                                // Transfer control to Competition using relevant constructor
                                this.Content = new Competition(CompetitionId, CompetitionRounds, CompetitionName, CompetitionPlayer, "Y");
                            }

                            //DisplayIndex is equal to zero then it is Delete cell
                            if (grd.SelectedCells[0].Column.DisplayIndex == 4)
                            {
                                if (MessageBox.Show("Are you sure you want to delete competition ?", "Information", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                                {
                                    CompetitionId = Int32.Parse(row_selected["Id"].ToString());

                                    MembersConnection();

                                    //Deletes scores of players from Score database and then deletes competition record
                                    query = "DELETE FROM Score WHERE CompetitionId = " + CompetitionId;
                                    SqlTextCommand();

                                    cmd.ExecuteNonQuery();

                                    query = "DELETE FROM Competition WHERE Id = " + CompetitionId;
                                    SqlTextCommand();

                                    cmd.ExecuteNonQuery();

                                    con.Close();
                                }
                                DisplayCompetitionList();
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
        // Regex for validation of fields required to add/edit a new member
        public void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("^\\d+$"); // Regex only allows 0-9
            e.Handled = !regex.IsMatch(e.Text);
        }
        public void NumberValidationTextBoxHandicap(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[\\d.-]"); // Regex only allows 0-9 plus . and - symbols
            e.Handled = !regex.IsMatch(e.Text);
        }
        public void MaleOrFemaleValidateTextEntry(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[\\'M''F']"); // Regex only allows M or F
            e.Handled = !regex.IsMatch(e.Text);
        }
        public void YesOrNoValidateTextEntry(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[\\'Y''N']"); // Regex only allows Y or N
            e.Handled = !regex.IsMatch(e.Text);
        }
        private void btnCancelMemberAddUpdate_Click(object sender, RoutedEventArgs e)
        {
            ClearMaster();
        }
        private void btnSaveMemberAddUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Variable to check handicap entered is a valid number
                bool isNum = double.TryParse(handicap.Text, out double num);
                //Check the validation of all required fields to add/update member
                if (memberName.Text == null || memberName.Text.Trim() == "")
                {
                    MessageBox.Show("Please enter member name", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    memberName.Focus();
                    return;
                }
                else if (memberRegNo.Text == null || memberRegNo.Text.Trim() == "")
                {
                    MessageBox.Show("Please enter member registration number", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    memberRegNo.Focus();
                    return;
                }
                else if (handicap.Text == null || handicap.Text.Trim() == "" || !isNum)
                {
                    MessageBox.Show("Please enter handicap", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    handicap.Focus();
                    return;
                }
                else if (num < -21 || num > 3)
                {
                    MessageBox.Show("Handicap range is 3.0 to -21.0", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    handicap.Focus();
                    return;
                }
                else if (maleOrFemale.Text == null || maleOrFemale.Text.Trim() == "")
                {
                    MessageBox.Show("Please enter (M) Male or (F) Female", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    maleOrFemale.Focus();
                    return;
                }
                else if (juvenile.Text == null || juvenile.Text.Trim() == "")
                {
                    MessageBox.Show("Is the member a juvenile, please enter (Y) or (N)", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    juvenile.Focus();
                    return;
                }
                else
                {
                    if (MemberId > 0)
                    {
                        if (MessageBox.Show("Are you sure you want to Update Member ?", "Information", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            MembersConnection();

                            //Update existing member
                            query = "UPDATE Member SET Member = @Member, RegNo = @RegNo, " +
                                "Handicap = @Handicap, Juvenile = @Juvenile, MaleOrFemale = @MaleOrFemale " +
                                "WHERE Id = @Id";
                            SqlTextCommand();

                            cmd.Parameters.AddWithValue("@Id", MemberId);
                            cmd.Parameters.AddWithValue("@Member", memberName.Text);
                            cmd.Parameters.AddWithValue("@RegNo", memberRegNo.Text);
                            cmd.Parameters.AddWithValue("@Handicap", handicap.Text);
                            cmd.Parameters.AddWithValue("@Juvenile", juvenile.Text);
                            cmd.Parameters.AddWithValue("@MaleOrFemale", maleOrFemale.Text);
                            cmd.ExecuteNonQuery();

                            con.Close();

                            DisplayMembers();

                            MessageBox.Show("Data updated successfully", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        if (MessageBox.Show("Are you sure you want to Save Member ?", "Information", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            MembersConnection();
                            //Save new member
                            query = "INSERT INTO Member(Member, RegNo, Handicap, AddToComp, Juvenile," +
                                "MaleOrFemale) VALUES (@Member, @RegNo, @Handicap, @AddToComp, @Juvenile," +
                                "@MaleOrFemale)";
                            SqlTextCommand();

                            cmd.Parameters.AddWithValue("@Member", memberName.Text);
                            cmd.Parameters.AddWithValue("@RegNo", memberRegNo.Text);
                            cmd.Parameters.AddWithValue("@Handicap", handicap.Text);
                            cmd.Parameters.AddWithValue("@AddToComp", "N");
                            cmd.Parameters.AddWithValue("@Juvenile", juvenile.Text);
                            cmd.Parameters.AddWithValue("@MaleOrFemale", maleOrFemale.Text);
                            cmd.ExecuteNonQuery();

                            con.Close();

                            DisplayMembers();

                            MessageBox.Show("Data saved successfully", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    ClearMaster();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        // Transfer to Player constructor with relevant information of selected player
        private void btnViewPlayer_Click(object sender, RoutedEventArgs e)
        {
            this.Content = new Player(PlayerSelected, PlayerHandicap, PlayerRegNo, MemberId);
        }
        private void btn_AddCompetition(object sender, RoutedEventArgs e)
        {
            // Adjust width of membership DataGrid and hide columns with functionality
            membersList.Width = 515;
            tableView.Visibility = Visibility.Hidden;
            tableEdit.Visibility = Visibility.Hidden;
            tableDelete.Visibility = Visibility.Hidden;
            // Make add player column visible
            addPlayer.Visibility = Visibility.Visible;
            // Hide competitionList DataGrid and show competition DataGrid
            competitionList.Visibility = Visibility.Hidden;
            competitionPlayers.Visibility = Visibility.Visible;
            // Adding Competition Textboxes, Labels and Buttons visible
            competitionName.Visibility = Visibility.Visible;
            compName.Visibility = Visibility.Visible;
            rounds.Visibility = Visibility.Visible;
            noOfRounds.Visibility = Visibility.Visible;
            confirmCompetition.Visibility = Visibility.Visible;
            cancelCompetition.Visibility = Visibility.Visible;
            competitionName.Focus();
            // Clearing and Disabling Add/Edit player functionality
            ClearMaster();
            memberName.IsEnabled = false;
            memberRegNo.IsEnabled = false;
            handicap.IsEnabled = false;
            juvenile.IsEnabled = false;
            maleOrFemale.IsEnabled = false;
            btnSave.IsEnabled = false;
        }

        private void btn_ConfirmCompetition(object sender, RoutedEventArgs e)
        {
            // Validate competition has required fields
            if (competitionName.Text == "")
            {
                MessageBox.Show("Enter Competition Name.");
                competitionName.Focus();
            }
            else if (!CompetitionPlayer.Any())
            {
                MessageBox.Show("No Competitors added to competition, use '+' from Members list to add");
            }
            else
            {
                CompetitionName = competitionName.Text;
                if (noOfRounds.Text == null || noOfRounds.Text.Trim() == "" ||
                    int.Parse(noOfRounds.Text.ToString()) < 1 || int.Parse(noOfRounds.Text.ToString()) > 2)
                {
                    MessageBox.Show("Number of Rounds should be 1 or 2.");
                    noOfRounds.Clear();
                    noOfRounds.Focus();
                }
                else  // Create new competition and transfer to competition using relevant constructor
                {
                    CompetitionRounds = int.Parse(noOfRounds.Text.ToString());

                    MembersConnection();

                    query = "INSERT INTO Competition(CompName, Date, NoOfRounds) VALUES(@CompName, " +
                        "@Date, @NoOfRounds);" + " SELECT CAST(scope_identity() AS int);";
                    SqlTextCommand();

                    cmd.Parameters.AddWithValue("@CompName", CompetitionName);
                    cmd.Parameters.AddWithValue("@Date", DateTime.Now);
                    cmd.Parameters.AddWithValue("@NoOfRounds", CompetitionRounds);

                    // Set competitionId to the unique Id assigned to the competition when adding to database
                    int competitionId = (int)cmd.ExecuteScalar();

                    con.Close();

                    this.Content = new Competition(competitionId, CompetitionRounds, CompetitionName, CompetitionPlayer);
                }
            }
        }

        private void btn_CancelCompetition(object sender, RoutedEventArgs e)
        {
            DisplayMembers();

            // Reset membership DataGrid and columns
            membersList.Width = 595;
            tableView.Visibility = Visibility.Visible;
            tableEdit.Visibility = Visibility.Visible;
            tableDelete.Visibility = Visibility.Visible;
            addPlayer.Visibility = Visibility.Hidden;
            //Hide competition DataGrid and show competitionList Gatagrid
            competitionPlayers.Visibility = Visibility.Hidden;
            competitionList.Visibility = Visibility.Visible;
            // Hide add new competition functionality
            competitionName.Visibility = Visibility.Hidden;
            compName.Visibility = Visibility.Hidden;
            rounds.Visibility = Visibility.Hidden;
            noOfRounds.Visibility = Visibility.Hidden;
            confirmCompetition.Visibility = Visibility.Hidden;
            cancelCompetition.Visibility = Visibility.Hidden;
            // Re-enable add/edit player functionality
            memberName.IsEnabled = true;
            memberRegNo.IsEnabled = true;
            handicap.IsEnabled = true;
            juvenile.IsEnabled = true;
            maleOrFemale.IsEnabled = true;
            btnSave.IsEnabled = true;

            RemoveAllPlayersFromCompetition();

            DisplayMembers();

            con.Close();
        }

        // Transfer to Course View
        private void ViewCourseStats_Click(object sender, RoutedEventArgs e)
        {
            RemoveAllPlayersFromCompetition();

            this.Content = new Course();
        }
        // Thus method used to clean up players added to a competition in the event of a competition being cancelled
        // or user navigating away without removing players
        public void RemoveAllPlayersFromCompetition()
        {
            MembersConnection();

            // Remove all added players from the competition and CompetitionPlayer List<>
            query = "UPDATE Member SET AddToComp = 'N' WHERE AddToComp = 'Y'";
            SqlTextCommand();

            cmd.ExecuteNonQuery();

            CompetitionPlayer.Clear();
        }
    }
}
