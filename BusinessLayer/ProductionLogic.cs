﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entity;
using DAL;
using System.Reflection;


namespace BusinessLayer
{
    public class Productions : ProductionEntity, IDisposable, IProductionLogic
    {
        public Productions CreateNew(Productions currProd)
        {
            Productions newProd = new Productions();

            return newProd;
        }
        public void UpdateProperty(object sender)
        {
            // get type of objects
            Type objType = this.GetType();
            Type senderType = sender.GetType();

            //get object property name from control names
            string propertyName = senderType.GetProperty("Name").GetValue(sender).ToString();
            propertyName = propertyName.Substring(propertyName.IndexOf('_') + 1);

            //retrieve control values 
            var newValue = senderType.GetProperty("Text").GetValue(sender);
            //newValue = string.IsNullOrEmpty((string)newValue) ? string.Empty:newValue;

            //switch target property type
            switch (objType.GetProperty(propertyName).PropertyType.ToString())
            {
                case "System.Int32":
                    if (string.IsNullOrEmpty((string)newValue))
                    {
                        newValue = "0";
                    }
                    break;
                default:
                    break;
            }

            //set values to current production object
            objType.GetProperty(propertyName).SetValue(this, Convert.ChangeType(newValue, objType.GetProperty(propertyName).PropertyType));
        }
    }

    public interface IProductionLogic
    {
        void UpdateProperty(object sender);
        Productions CreateNew(Productions currProd);
    }
}