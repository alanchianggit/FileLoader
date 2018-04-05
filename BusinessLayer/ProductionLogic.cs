﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entity;
using System.Reflection;

using System.Data;
using System.ComponentModel;

namespace BusinessLayer.Productions
{
    using DAL.Productions;
    using DAL.Factory;
    public static class ProductionLogic
    {
        private static IDbDataAdapter _productionadapter;
        private static IDbTransaction _productiontrans;


        public static IDbDataAdapter ProductionAdapter
        {
            get
            {
                return _productionadapter;
            }

            set
            {
                _productionadapter = value;
            }
        }

        public static IDbTransaction EventTrans
        {
            get
            {
                return _productiontrans;
            }

            set
            {
                _productiontrans = value;
            }
        }
        //public class Productions : ProductionEntity, IDisposable, IFormLogic
        //{
        //    #region Properties
        //    protected DataTable _datatable;
        //    protected List<EventEntity> _events;

        //    #endregion

        //    #region Accessors
        //    private DataTable DataTable
        //    {
        //        get
        //        {
        //            return _datatable;
        //        }
        //    }

        //    public List<EventEntity> Events
        //    {
        //        get
        //        {
        //            return _events;
        //        }
        //    }
        //    #endregion
        //    public Productions()
        //    {

        //    }

        //    public Productions(string productionname)
        //    {
        //        this.ProductionName = productionname;
        //        this.GetProduction();

        //    }


        //    public void UpdateDB()
        //    {
        //        using (ProductionDAL pdal = new ProductionDAL())
        //        {
        //            //DataFactory.CreateConnection();
        //            bool Existence = pdal.CheckExistence(this);

        //            if (Existence==true)
        //            {
        //                pdal.Update(this); 
        //            }
        //            else
        //            {
        //                Console.WriteLine("No record found to be updated");
        //            }
        //        }
        //    }




        //    public Productions GetProduction()
        //    {
        //        using (ProductionDAL pdal = new ProductionDAL())
        //        {
        //            ProductionEntity newProd = new ProductionEntity();
        //            newProd = pdal.RetrieveProductionData(this);

        //            foreach (PropertyInfo pi in typeof(ProductionEntity).GetProperties())
        //            {
        //                pi.SetValue(this, pi.GetValue(newProd));

        //            }
        //            return this;
        //        }
        //    }

        //    public void CreateNew()
        //    {
        //        using (ProductionDAL pdal = new ProductionDAL())
        //        {

        //            //DataFactory.CreateConnection();
        //            bool Existence = pdal.CheckExistence(this);
        //            if (Existence== false)
        //            {
        //                pdal.Add(this);
        //                GetProduction();
        //            }
        //            else
        //            {
        //                Console.WriteLine("Record existed already");
        //            }

        //        }
        //    }

        //    public void UpdateProperty(object sender)
        //    {
        //        // get type of objects
        //        Type objType = this.GetType();
        //        Type senderType = sender.GetType();

        //        //get object property name from control names
        //        string propertyName = senderType.GetProperty("Name").GetValue(sender).ToString();
        //        propertyName = propertyName.Substring(propertyName.IndexOf('_') + 1);

        //        //retrieve control values 
        //        var newValue = senderType.GetProperty("Text").GetValue(sender);
        //        //newValue = string.IsNullOrEmpty((string)newValue) ? string.Empty:newValue;

        //        //switch target property type
        //        switch (objType.GetProperty(propertyName).PropertyType.ToString())
        //        {
        //            case "System.Int32":
        //                if (string.IsNullOrEmpty((string)newValue))
        //                {
        //                    newValue = int.MinValue;
        //                }
        //                else
        //                {
        //                    try
        //                    {
        //                        newValue = int.Parse((string)newValue);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        newValue = int.MinValue;
        //                        Console.WriteLine(ex.Message);
        //                    }
        //                }
        //                break;
        //            case "System.DateTime":
        //                if (string.IsNullOrEmpty((string)newValue))
        //                {
        //                    newValue = DateTime.MinValue;
        //                }
        //                else
        //                {
        //                    try
        //                    {
        //                        newValue = DateTime.Parse((string)newValue);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        newValue = DateTime.MinValue;
        //                        Console.WriteLine(ex.Message);
        //                    }
        //                }
        //                break;
        //            default:
        //                break;
        //        }

        //        //set values to current production object
        //        objType.GetProperty(propertyName).SetValue(this, Convert.ChangeType(newValue, objType.GetProperty(propertyName).PropertyType));
        //    }

        public static void AttachTransaction(List<IDbDataAdapter> objs)
        {
            if (DataLayer.Instance.trans != null) { DataLayer.Instance.trans = null; }

            DataLayer.Instance.trans = DataLayer.ActiveConn.BeginTransaction();
            foreach (IDbDataAdapter obj in objs)
            {
                AttachTransaction(obj);
            }
        }
        public static void AttachTransaction(IDbDataAdapter obj)
        {

            //if (DataLayer.Instance.trans != null) { DataLayer.Instance.trans = null; }

            //DataLayer.Instance.trans = DataLayer.ActiveConn.BeginTransaction();
            if (obj.InsertCommand != null) { obj.InsertCommand.Transaction = DataLayer.Instance.trans; }
            if (obj.DeleteCommand != null) { obj.DeleteCommand.Transaction = DataLayer.Instance.trans; }
            if (obj.UpdateCommand != null) { obj.UpdateCommand.Transaction = DataLayer.Instance.trans; }

        }

        public static void TryCommit()
        {
            try
            {
                DataLayer.Instance.trans.Commit();
                //DataLayer.Instance.trans.Rollback();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                DataLayer.Instance.trans.Rollback();
            }
        }

        public static IDbDataAdapter GetProductionAdapter()
        {
            IDbDataAdapter da;

            if (ProductionAdapter == null)
            {

                using (ProductionDAL pDAL = new ProductionDAL())
                {

                    da = pDAL.AdaptProduction();

                }

                ProductionAdapter = da;
                return da;
            }
            else
            {
                return ProductionAdapter;
            }
        }


    }
}
