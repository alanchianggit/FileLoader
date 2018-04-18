﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Entity;
using Auth;
using BusinessLayer.Events;
using System.Reflection;
using LogicExtensions;



namespace ALSTools
{
    //using LogicExtensions;
    public partial class frm_Event : Form
    {
        //private LogEvent currEvent;
        private BindingSource EventBS = new BindingSource();
        private BindingSource AuditTrailBS = new BindingSource();
        private static List<IDbDataAdapter> das = new List<IDbDataAdapter>();
        private static IDbDataAdapter daEvents;
        private static IDbDataAdapter daAuditTrail;
        private DataTable dtLogs;
        //private DataSet MasterDS = new DataSet("Master");
        private DataSet MasterDS = EventLogic.MasterDS;
        Point mouseDownPoint = Point.Empty;

        public frm_Event()
        {
            InitializeComponent();
            das.Clear();
            das.Add(daEvents);
            das.Add(daAuditTrail);
            GetData();

        }

        private void GetData()
        {
            dtLogs = EventLogic.GetLogIDs();
            if (daEvents == null)
            {
                daEvents = EventLogic.GetEventAdapter();
            }
            if (daAuditTrail == null)
            {
                daAuditTrail = EventLogic.GetBackupAdapter();
            }

            //if (MasterDS.Tables.Count != 0) { MasterDS.Tables.Clear(); }

            using (DataSet EventDS = new DataSet())
            {
                if (MasterDS.Tables.Contains(EventLogic.TableName)) { MasterDS.Tables.Remove(EventLogic.TableName); }
                daEvents.Fill(EventDS);
                MasterDS.Merge(EventDS.Tables["Table"], true, MissingSchemaAction.Add);
                MasterDS.Tables["Table"].TableName = EventLogic.TableName;
                EventBS.DataSource = MasterDS;
                EventBS.DataMember = EventLogic.TableName;
                this.dgv_Events.DataSource = EventBS;
                this.dgv_Events.Columns["EventID"].ReadOnly = true;
            }
            using (DataSet AuditDS = new DataSet())
            {
                if (MasterDS.Tables.Contains("tbl_Backup")) { MasterDS.Tables.Remove("tbl_Backup"); }
                daAuditTrail.Fill(AuditDS);

                MasterDS.Merge(AuditDS.Tables["Table"], true, MissingSchemaAction.Add);
                MasterDS.Tables["Table"].TableName = "tbl_Backup";
                AuditTrailBS.DataSource = MasterDS;
                AuditTrailBS.DataMember = "tbl_Backup";
                this.dgv_AuditTrail.DataSource = AuditTrailBS;
                this.dgv_AuditTrail.Columns["TableName"].Visible = false;
                this.dgv_AuditTrail.ReadOnly = true;
                this.dgv_AuditTrail.Columns["AffectedID"].Visible = false;
            }
            using (DataTable dt = new DataTable())
            {

                this.cmb_InstrumentFilter.DataSource = dtLogs;
                this.cmb_InstrumentFilter.ValueMember = "LogID";
                this.cmb_InstrumentFilter.SelectedIndex = -1;
            }
        }

        private static frm_Event inst;
        public static frm_Event GetForm
        {
            get
            {
                if (inst == null || inst.IsDisposed)
                    inst = new frm_Event();
                return inst;
            }
        }




        public void AddGeneralEvent(string str)
        {
            AddGeneralEvent(str, string.Empty);
        }

        public void AddGeneralEvent(string str, string ProdID)
        {
            try
            {
                DataTable dt = MasterDS.Tables[EventLogic.TableName];
                DataRow dr = dt.NewRow();
                dr["ProductionID"] = ProdID;
                dr["Details"] = str;
                dr["LogName"] = "General";
                dr["TimeCreated"] = DateTimeExtension.GetDateWithoutMilliseconds(DateTime.Now);
                dt.Rows.Add(dr);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                TryCommitDB();
            }
            GetData();

        }


        private bool TryCommitDB()
        {
            try
            {
                EventLogic.AttachTransaction(das);
                for (int i = 0; i < MasterDS.Tables.Count; i++)
                {
                    using (DataSet DS = new DataSet())
                    {
                        DS.Merge(MasterDS.Tables[i], true, MissingSchemaAction.Add);
                        DS.Tables[0].TableName = "Table";
                        das[i].Update(DS);
                    }
                }

                EventLogic.TryCommit();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        private void DisplayAuditTrail(object sender)
        {
            DataGridView obj = sender as DataGridView;
            DataGridViewCell e = obj.CurrentCell;
            if (obj.SelectedCells.Count == 1)
            {
                DisplayAuditTrail(sender, e);
            }
        }
        private void DisplayAuditTrail(object sender, DataGridViewCell e)
        {

            if (e.RowIndex != -1)
            {
                DataGridView dgv = sender as DataGridView;
                //Can't find backupid from new event row to exisiting row
                string backupid = dgv[dgv.Columns["EventID"].Index, e.RowIndex].Value.ToString();
                string filter = string.Format("[AffectedID] = '{0}'", backupid);
                AuditTrailBS.Filter = filter;
            }
            else
            {
                AuditTrailBS.RemoveFilter();
            }
        }

        private void DisplayAuditTrail(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView obj = sender as DataGridView;
            DataGridViewCell cell = obj.CurrentCell;

            if (cell.RowIndex != -1)
            {
                DisplayAuditTrail(sender, cell);
            }
        }

        private void dgv_Events_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dgv = sender as DataGridView;
            UpdateDataSet(dgv, e);
            GetData();
            DisplayAuditTrail(sender, e);
        }

        private bool HasRowAt(DataTable dt, int index)
        {
            return dt.Rows.Count <= index;
        }

        private void UpdateDataSet(DataGridView dgv, DataGridViewCellEventArgs e)
        {
            try
            {
                //dgv.EndEdit();


                BindingSource bs = (BindingSource)dgv.DataSource;
                

                string tablename = bs.DataMember.ToString();

                //if modified cell's row is greater (newer) than current dataset

                DataRowView obj = (DataRowView)bs.Current;
                if (obj.IsNew)
                //if (e.RowIndex > MasterDS.Tables[tablename].Rows.Count - 1)
                {
                    //Create new row
                    DataRow dr = MasterDS.Tables[tablename].NewRow();
                    //Get old value from datagridview and put into row's cell
                    dr[e.ColumnIndex] = dgv[e.ColumnIndex, e.RowIndex].Value;
                    dr["TimeCreated"] = DateTimeExtension.GetDateWithoutMilliseconds(DateTime.Now);
                    // Add row to current dataset
                    MasterDS.Tables[tablename].Rows.Add(dr);
                    //Remove datagridview row
                    dgv.Rows.RemoveAt(e.RowIndex);

                    //End edit
                    dgv.EndEdit();
                    //dgv.Refresh();

                }

                //triggering updates eventhough new row, 
                //if (!HasRowAt(MasterDS.Tables[tablename], e.RowIndex))
                //{
                ////Create Timestamp if timecreated is empty
                //if (MasterDS.Tables[tablename].Rows[e.RowIndex]["TimeCreated"].ToString() == string.Empty)
                //{
                //    MasterDS.Tables[tablename].Rows[e.RowIndex]["TimeCreated"] = DateTimeExtension.GetDateWithoutMilliseconds(DateTime.Now);
                //}

                //MasterDS.Tables[tablename].Rows[e.RowIndex].EndEdit();

                //if (obj["TimeCreated"].ToString() == string.Empty)
                //{
                //    obj.BeginEdit();
                //    obj["TimeCreated"] = DateTimeExtension.GetDateWithoutMilliseconds(DateTime.Now);
                //    obj.EndEdit();
                //}

                //bs.EndEdit();

                MasterDS.Tables[tablename].Rows[e.RowIndex].EndEdit();
                //Audit trail if update succeeds
                //dt = null when txt_productionid is valid
                DataTable dt = MasterDS.Tables[tablename].GetChanges();
                    //DataSet ds = (DataSet)bs.DataSource;
                    //DataTable dt = ds.Tables[tablename].GetChanges();
                    if (dt != null)
                    {
                        foreach (DataRow drc in dt.Rows)
                        {
                            for (int i = 0; i < dt.Columns.Count; i++)
                            {
                                if (drc.HasVersion(DataRowVersion.Original))
                                {

                                    //compare current and original versions
                                    if (!drc[i, DataRowVersion.Current].Equals(drc[i, DataRowVersion.Original]))
                                    {
                                        string tname = AuditTrailBS.DataMember.ToString();
                                        DataTable dtbackup = MasterDS.Tables[tname];
                                        DataRow drbackup = dtbackup.NewRow();
                                        //Import row values from old table to new
                                        drbackup["TimeLogged"] = DateTimeExtension.GetDateWithoutMilliseconds(DateTime.Now);
                                        drbackup["TableName"] = tablename;
                                        drbackup["ColumnName"] = dt.Columns[i].ColumnName;
                                        drbackup["OldValue"] = drc[i, DataRowVersion.Original].ToString();
                                        drbackup["NewValue"] = drc[i, DataRowVersion.Current].ToString();
                                        drbackup["AffectedID"] = drc["EventID"].ToString();
                                        dtbackup.Rows.Add(drbackup);
                                    }
                                }
                            }
                        }
                    }
                //}
            }
            catch (Exception excep)
            {
                MessageBox.Show(excep.Message);
            }
            finally
            {
                

                TryCommitDB();
                
            }

        }

        private void dgv_Events_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {

            DataGridView dgv = sender as DataGridView;
            //Validate data type
            DataGridViewCell dgc = dgv[e.ColumnIndex, e.RowIndex];
            if (dgc.ValueType == typeof(DateTime))
            {
                DateTime result = new DateTime();
                if (!DateTime.TryParse(e.FormattedValue.ToString(), out result))
                {
                    if (e.FormattedValue.ToString() == string.Empty) { dgc.Value = DBNull.Value; }
                    else
                    {
                        e.Cancel = true;
                        dgc.ErrorText = "Not a valid Format.";
                    }
                }
            }
            else if (dgc.ValueType == typeof(int))
            {
                int result = new int();
                if (!int.TryParse(e.FormattedValue.ToString(), out result))
                {
                    if (e.FormattedValue.ToString() == string.Empty) { dgc.Value = DBNull.Value; }
                    else
                    {
                        e.Cancel = true;
                        dgc.ErrorText = "Not a valid Format.";
                    }

                }
            }
            else
            {
                e.Cancel = false;
            }
        }
        private void txt_ProductionID_TextChanged(object sender, EventArgs e)
        {
            TextBox txtbox = sender as TextBox;
            string strFilter = null;
            if (!txtbox.Text.ToString().Equals(string.Empty))
            {
                strFilter = string.Format("[ProductionID] Like '%{0}%'", txtbox.Text.ToString());
            }
            FilterEvents(strFilter);
        }

        private void cmb_InstrumentFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox txtbox = sender as ComboBox;
            string value = null;
            if (txtbox.SelectedText.Length > 0)
            {
                value = txtbox.SelectedValue.ToString();
            }
            else
            {
                value = txtbox.SelectedText;
            }
            if (value != string.Empty)
            {
                string strFilter = string.Format("[LogName] Like '%{0}%'", value);
                FilterEvents(strFilter);
            }


        }

        private void FilterEvents(string value)
        {
            if (value == null)
            {
                return;
            }
            else if (value.Length > 0)
            {
                EventBS.Filter = value;
            }
            else
            {
                EventBS.RemoveFilter();
            }

        }


        private void txt_SearchPhrase_TextChanged(object sender, EventArgs e)
        {
            using (EventEntity obj = new EventEntity())
            {
                TextBox txtbox = sender as TextBox;
                PropertyInfo[] pis = obj.GetType().GetProperties();
                string strFilter = string.Empty;

                foreach (PropertyInfo pi in pis)
                {
                    switch (pi.PropertyType.ToString())
                    {
                        case "System.String":
                            strFilter += string.Format("[{0}] LIKE '%{1}%'", pi.Name, txtbox.Text.ToString());
                            if (pis[pis.Length - 1].Name != pi.Name)
                            {
                                strFilter += " OR ";
                            }
                            break;
                        default:
                            break;
                    }
                }

                FilterEvents(strFilter);

            }
        }

        private void dgv_Events_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            DisplayAuditTrail(sender, e);
        }


        private void frmMsDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mouseDownPoint = new Point(e.X, e.Y);
            }

        }


        private void frmMsMove(object sender, MouseEventArgs e)
        {
            if (mouseDownPoint.IsEmpty)
                return;
            Form f = sender as Form;
            f.Location = new Point(f.Location.X + (e.X - mouseDownPoint.X), f.Location.Y + (e.Y - mouseDownPoint.Y));
        }

        private void frmMsUp(object sender, MouseEventArgs e)
        {
            mouseDownPoint = Point.Empty;
        }

        private void dgv_Events_SelectionChanged(object sender, EventArgs e)
        {
            DisplayAuditTrail(sender);
        }

        private void frm_Event_DoubleClick(object sender, EventArgs e)
        {
            this.Hide();
        }
    }
}
