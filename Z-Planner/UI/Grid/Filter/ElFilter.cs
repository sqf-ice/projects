﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZZero.ZPlanner.UI.Grid.Filter
{
    public class ElFilter : IFilter
    {
        ZDataGridViewColumn Column {get;set;}
        string Value {get;set;}

        public ElFilter(ZDataGridViewColumn column, string value)
        {
            Column = column;
            Value = value;
        }

        public bool IsMatch(DataGridViewRow row)
        {
            return Column.GetFormatedValue(row).EndsWith(Value);
        }
    }
}