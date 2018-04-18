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
using BusinessLayer.Events;
using BusinessLayer.Productions;
using DAL.Productions;
using LogicExtensions;


namespace ALSTools
{
    public partial class Production : Form
    {
        private bool CancelEdit = false;
        private BindingSource ProductionBS = new BindingSource();
        private BindingSource AuditTrailBS = new BindingSource();
        private static IDbDataAdapter daAuditTrail;
        private static IDbDataAdapter daProductions;
        private static List<IDbDataAdapter> das = new List<IDbDataAdapter>();
        //private DataSet MasterDS = new DataSet("Master");
        private DataSet MasterDS = ProductionLogic.MasterDS;

        Point mouseDownPoint = Point.Empty;

        public Production()
        {
            InitializeComponent();
            das.Clear();
            das.Add(daProductions);
            das.Add(daAuditTrail);

            GetData();
        }

        private static Production inst;
        public static Production GetForm
        {
            get
            {
                if (inst == null || inst.IsDisposed)
                    inst = new Production();
                return inst;
            }
        }

        private void GetData()
        {
            daProductions = ProductionLogic.GetProductionAdapter();
            daAuditTrail = ProductionLogic.GetBackupAdapter();

            //if (MasterDS.Tables.Count != 0) { MasterDS = new DataSet(); }
            using (DataSet ProductionDS = new DataSet())
            {
                if (MasterDS.Tables.Contains(ProductionLogic.TableName)) { MasterDS.Tables.Remove(ProductionLogic.TableName); }
                daProductions.Fill(ProductionDS);
                MasterDS.Merge(ProductionDS.Tables["Table"], true, MissingSchemaAction.Add);
                MasterDS.Tables["Table"].TableName = ProductionLogic.TableName;
                ProductionBS.DataSource = MasterDS;
                ProductionBS.DataMember = ProductionLogic.TableName.ToString();
                this.dgv_Production.DataSource = ProductionBS;
                this.dgv_Production.Columns["ProductionID"].ReadOnly = true;
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
                this.dgv_AuditTrail.Columns["AffectedID"].Visible = false;

            }
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

        private void ShowEvent(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1) return;
            DataGridView obj = sender as DataGridView;

            //Show event in event form
            frm_Event frm = GetEventForm();
            TextBox ctrl = frm.Controls["txt_productionIDFilter"] as TextBox;

            bool newrow = obj.Rows[e.RowIndex].IsNewRow;
            if (!newrow)
            {
                ctrl.Text = obj["ProductionName", e.RowIndex].Value.ToString();
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
                string backupid = dgv[dgv.Columns["ProductionID"].Index, e.RowIndex].Value.ToString();
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

        private void dgv_Production_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            DisplayAuditTrail(sender, e);


            //ProductionEntity currProd = this.dgv_Production.Rows[e.RowIndex].DataBoundItem as ProductionEntity;
        }

        private frm_Event GetEventForm()
        {
            //Interact with Events form
            //Show events under that production
            if (this.MdiParent.MdiChildren.OfType<frm_Event>().Count() == 0)
            {
                Operations.Instance.ClickEvent();
            }
            frm_Event otherForm;
            otherForm = this.MdiParent.MdiChildren.OfType<frm_Event>().Single();
            otherForm.Show();

            return otherForm;
        }


        private void dgv_Production_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (CancelEdit) { GetData(); return; }

            DataGridView obj = sender as DataGridView;
            ModifiedType mt = UpdateDataSet(obj, e);
            GetData();


            frm_Event frm = GetEventForm();

            if (mt.Equals(ModifiedType.Insert))
            {
                //Create events about production
                if (obj["ProductionName", e.RowIndex].Value.ToString() != string.Empty)
                {
                    frm.AddGeneralEvent(string.Format("New production: {0}", obj["ProductionID", e.RowIndex].Value), obj["ProductionName", e.RowIndex].Value.ToString());
                }
                else
                {
                    frm.AddGeneralEvent(string.Format("New production: {0}", obj["ProductionID", e.RowIndex].Value));
                }

            }
            else if (mt.Equals(ModifiedType.Update))
            {
                //Create events about updateed production
                if (obj["ProductionName", e.RowIndex].Value.ToString() != string.Empty)
                {
                    frm.AddGeneralEvent(string.Format("Modified production: {0}", obj["ProductionID", e.RowIndex].Value), obj["ProductionName", e.RowIndex].Value.ToString());
                }
                else
                {
                    frm.AddGeneralEvent(string.Format("Modified production: {0}", obj["ProductionID", e.RowIndex].Value));
                }
            }
        }
        public enum ModifiedType
        {
            Insert,
            Update
        }

        private ModifiedType UpdateDataSet(DataGridView dgv, DataGridViewCellEventArgs e)
        {
            ModifiedType modType;
            try
            {
                dgv.EndEdit();
                BindingSource bs = (BindingSource)dgv.DataSource;
                string tablename = bs.DataMember.ToString();
                DataRowView obj = (DataRowView)bs.Current;
                if (obj.IsNew)
                //if (e.RowIndex > MasterDS.Tables[tablename].Rows.Count - 1)
                {
                    //Create new row
                    DataRow dr = MasterDS.Tables[tablename].NewRow();
                    //Get old value from datagridview and put into row's cell
                    dr[e.ColumnIndex] = dgv[e.ColumnIndex, e.RowIndex].Value;
                    // Add row to current dataset
                    MasterDS.Tables[tablename].Rows.Add(dr);
                    //Remove datagridview row
                    dgv.Rows.RemoveAt(e.RowIndex);

                    //End edit
                    dgv.EndEdit();
                    modType = ModifiedType.Insert;

                    //return ModifiedType.Insert;
                }

                if (!HasRowAt(MasterDS.Tables[tablename], e.RowIndex))
                {
                    MasterDS.Tables[tablename].Rows[e.RowIndex].EndEdit();

                    //Audit trail if update succeeds
                    DataTable dt = MasterDS.Tables[tablename].GetChanges();
                    foreach (DataRow dr in dt.Rows)
                    {
                        for (int i = 0; i < dt.Columns.Count; i++)
                        {
                            if (dr.HasVersion(DataRowVersion.Original))
                            {

                                //compare current and original versions
                                if (!dr[i, DataRowVersion.Current].Equals(dr[i, DataRowVersion.Original]))
                                {
                                    string tname = AuditTrailBS.DataMember.ToString();
                                    DataTable dtbackup = MasterDS.Tables[tname];
                                    DataRow drbackup = dtbackup.NewRow();
                                    //Import row values from old table to new
                                    drbackup["TimeLogged"] = DateTimeExtension.GetDateWithoutMilliseconds(DateTime.Now);
                                    drbackup["TableName"] = tablename;
                                    drbackup["ColumnName"] = dt.Columns[i].ColumnName;
                                    drbackup["OldValue"] = dr[i, DataRowVersion.Original].ToString();
                                    drbackup["NewValue"] = dr[i, DataRowVersion.Current].ToString();
                                    drbackup["AffectedID"] = dr["ProductionID"].ToString();
                                    dtbackup.Rows.Add(drbackup);
                                }
                            }
                        }
                    }
                }

                //return ModifiedType.Update;
                modType = ModifiedType.Update;
            }
            catch (Exception excep)
            {
                MessageBox.Show(excep.Message.ToString());
                return ModifiedType.Update;
            }
            finally
            {
                ProductionLogic.AttachTransaction(das);
                for (int i = 0; i < MasterDS.Tables.Count; i++)
                {
                    using (DataSet DS = new DataSet())
                    {
                        DS.Merge(MasterDS.Tables[i], true, MissingSchemaAction.Add);
                        DS.Tables[0].TableName = "Table";
                        das[i].Update(DS);
                    }
                }

                ProductionLogic.TryCommit();

            }
            return modType;

        }

        private bool HasRowAt(DataTable dt, int index)
        {
            return dt.Rows.Count <= index;
        }

        private void dgv_Production_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            DataGridView dgv = (DataGridView)sender;

            if (dgv.Rows[e.RowIndex] != null && !dgv.Rows[e.RowIndex].IsNewRow && dgv.IsCurrentRowDirty)
            {
                CancelEdit = false;
            }
            else
            {
                CancelEdit = true;
                return;
            }

        }

        private void dgv_Production_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            ShowEvent(sender, e);
        }
    }
}
