﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZZero.ZPlanner.Data.Entities;
using ZZero.ZPlanner.Settings;
using ZZero.ZPlanner.ZConfiguration;

namespace ZZero.ZPlanner.UI.Grid
{
    class ZDataGridViewTextBoxCell : DataGridViewTextBoxCell, IZDataGridViewCell
    {
        #region Fields
        private int valueColumnSpan = 1;
        private int valueRowSpan = 1;
        private ZDataGridViewTextBoxCell valueOwnerCell;
        #endregion

        #region Properties
        
        public int ColumnSpan
        {
            get { return valueColumnSpan; }
            set
            {
                if (DataGridView == null || OwnerCell != null)
                    return;
                if (value < 1 || ColumnIndex + value - 1 >= DataGridView.ColumnCount)
                    throw new System.ArgumentOutOfRangeException("value");
                if (valueColumnSpan != value)
                    SetSpan(value, RowSpan);
            }
        }
        
        public int RowSpan
        {
            get { return valueRowSpan; }
            set
            {
                if (DataGridView == null || OwnerCell != null)
                    return;
                if (value < 1 || RowIndex + value - 1 >= DataGridView.RowCount)
                    throw new System.ArgumentOutOfRangeException("value");
                if (valueRowSpan != value)
                    SetSpan(ColumnSpan, value);
            }
        }

        public DataGridViewCell OwnerCell
        {
            get { return valueOwnerCell; }
            set { valueOwnerCell = value as ZDataGridViewTextBoxCell; }
        }
        
        public override bool ReadOnly
        {
            get
            {
                return base.ReadOnly;
            }
            set
            {
                base.ReadOnly = value;

                if (OwnerCell == null
                    && (ColumnSpan > 1 || RowSpan > 1)
                    && DataGridView != null)
                {
                    for (int col = ColumnIndex; col < ColumnIndex + ColumnSpan; col++)
                        for (int row = RowIndex; row < RowIndex + RowSpan; row++ )
                            if (col != ColumnIndex || row != RowIndex)
                            {
                                DataGridView[col, row].ReadOnly = value;
                            }
                }
            }
        }
        
        #endregion

        protected override object GetFormattedValue(object value, int rowIndex, ref DataGridViewCellStyle cellStyle, TypeConverter valueTypeConverter, TypeConverter formattedValueTypeConverter, DataGridViewDataErrorContexts context)
        {
            ZParameter parameter = this.OwningColumn.Tag as ZParameter;
            ZLayerParameter layerParameter = this.Tag as ZLayerParameter;
            ZLayerType? layerType = (layerParameter != null) ? layerParameter.Layer.GetLayerType() : null;

            string isUsed = string.Empty;
            if (parameter != null && parameter.Table == ZTableType.Single)
            {
                if (layerParameter != null) isUsed = layerParameter.Layer.GetLayerParameterValue(ZStringConstants.ParameterIDZo_IsUsed);
            }
            else if (parameter != null && parameter.Table == ZTableType.Pair)
            {
                if (layerParameter != null) isUsed = layerParameter.Layer.GetLayerParameterValue(ZStringConstants.ParameterIDZdiff_IsUsed);
            }

            if ((this.OwningColumn.Name == ZStringConstants.ParameterIDZo_Frequency || this.OwningColumn.Name == ZStringConstants.ParameterIDZdiff_Frequency) && (value == null || value.ToString() == string.Empty))
            {
                ZLayer layer = this.OwningRow.Tag as ZLayer;
                if (layer == null || layer.Stackup == null) value = null;
                else
                {
                    if (layerType == ZLayerType.Signal || layerType == ZLayerType.SplitMixed) value = layer.Stackup.GetFrequencyByLayer(layer);
                    else value = null;
                }
            }

            if ((this.OwningColumn.Name == ZStringConstants.ParameterIDZo_LossTangent || this.OwningColumn.Name == ZStringConstants.ParameterIDZo_DielectricConstant || this.OwningColumn.Name == ZStringConstants.ParameterIDZdiff_LossTangent || this.OwningColumn.Name == ZStringConstants.ParameterIDZdiff_DielectricConstant) && value != null && value.ToString() != string.Empty)
            {
                double frequency = Options.TheOptions.Frequency;
                double dValue;

                if (layerParameter != null)
                {
                    if (layerParameter.Layer != null && layerParameter.Layer.Stackup != null) frequency = layerParameter.Layer.Stackup.Frequency;

                    double layerFrequency = layerParameter.Layer.GetFrequency();
                    if (!double.IsNaN(layerFrequency)) frequency = layerFrequency;
                }

                if (value != null && ZPlannerManager.TryParseDependedValue(value.ToString().Trim(), frequency, out dValue))
                {
                    if (double.IsNaN(dValue)) return string.Empty;
                    if (parameter != null) value = dValue.ToString(ZPlannerManager.GetFormatByParameter(parameter), CultureInfo.InvariantCulture);
                    else value = dValue.ToString("N", CultureInfo.InvariantCulture);
                }  
            }

            if (this.OwningColumn.Name == ZStringConstants.ParameterIDRoughTop && (value == null || string.IsNullOrWhiteSpace(value.ToString())))
            {
                ZLayer layer = this.OwningRow.Tag as ZLayer;
                if (layer == null) value = null;
                else
                {
                    if (layerType == ZLayerType.Plane || layerType == ZLayerType.Signal || layerType == ZLayerType.SplitMixed) value = layer.GetLayerParameterValue(ZStringConstants.ParameterIDCalcRoughTop);
                    else value = null;
                }
            }

            if (this.OwningColumn.Name == ZStringConstants.ParameterIDRoughBottom && (value == null || string.IsNullOrWhiteSpace(value.ToString())))
            {
                ZLayer layer = this.OwningRow.Tag as ZLayer;
                if (layer == null) value = null;
                else
                {
                    if (layerType == ZLayerType.Plane || layerType == ZLayerType.Signal || layerType == ZLayerType.SplitMixed) value = layer.GetLayerParameterValue(ZStringConstants.ParameterIDCalcRoughBottom);
                    else value = null;
                }
            }

            if (this.OwningColumn.Name == ZStringConstants.ParameterIDZdiff_RoughTop)
            {
                ZLayer layer = this.OwningRow.Tag as ZLayer;
                if (layer == null) value = null;
                else
                {
                    ZLayer stackupLayer = layer.Stackup.GetLayerOfStackup(layer.ID);
                    if (stackupLayer != null)
                    {
                        value = stackupLayer.GetLayerParameterValue(ZStringConstants.ParameterIDRoughTop);
                        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            if (layerType == ZLayerType.Plane || layerType == ZLayerType.Signal || layerType == ZLayerType.SplitMixed) value = stackupLayer.GetLayerParameterValue(ZStringConstants.ParameterIDCalcRoughTop);
                        }
                    }
                    else value = null;
                }
            }

            if (this.OwningColumn.Name == ZStringConstants.ParameterIDZdiff_RoughBottom)
            {
                ZLayer layer = this.OwningRow.Tag as ZLayer;
                if (layer == null) value = null;
                else
                {
                    ZLayer stackupLayer = layer.Stackup.GetLayerOfStackup(layer.ID);
                    if (stackupLayer != null)
                    {
                        value = stackupLayer.GetLayerParameterValue(ZStringConstants.ParameterIDRoughBottom);
                        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            if (layerType == ZLayerType.Plane || layerType == ZLayerType.Signal || layerType == ZLayerType.SplitMixed) value = stackupLayer.GetLayerParameterValue(ZStringConstants.ParameterIDCalcRoughBottom);
                        }
                    }
                    else value = null;
                }
            }

            if (this.OwningColumn.Name == ZStringConstants.ParameterIDZdiff_WeavePitch)
            {
                ZLayer layer = this.OwningRow.Tag as ZLayer;
                if (layer == null) value = null;
                else
                {
                    ZLayer stackupLayer = layer.Stackup.GetLayerOfStackup(layer.ID);
                    if (stackupLayer != null)
                    {
                        value = stackupLayer.GetLayerParameterValue(ZStringConstants.ParameterIDWeavePitch);
                    }
                    else value = null;
                }
            }

            if (this.OwningColumn.Name == ZStringConstants.ParameterIDZdiff_FillPitch)
            {
                ZLayer layer = this.OwningRow.Tag as ZLayer;
                if (layer == null) value = null;
                else
                {
                    ZLayer stackupLayer = layer.Stackup.GetLayerOfStackup(layer.ID);
                    if (stackupLayer != null)
                    {
                        value = stackupLayer.GetLayerParameterValue(ZStringConstants.ParameterIDFillPitch);
                    }
                    else value = null;
                }
            }

            if (this.OwningColumn.Name == ZStringConstants.ParameterIDZo_TopReference ||
                this.OwningColumn.Name == ZStringConstants.ParameterIDZo_BottomReference ||
                this.OwningColumn.Name == ZStringConstants.ParameterIDZdiff_TopReference ||
                this.OwningColumn.Name == ZStringConstants.ParameterIDZdiff_BottomReference)
            {
                string sValue = value as string;
                if (!string.IsNullOrWhiteSpace(sValue))
                {
                    ZLayer layer = this.OwningRow.Tag as ZLayer;
                    if (layer == null) return string.Empty;
                    ZLayer referenceLayer = layer.Stackup.Layers.Find(x => x.ID == value.ToString());
                    if (referenceLayer == null) return string.Empty;
                    value = referenceLayer.GetLayerParameterValue(ZStringConstants.ParameterIDLayerNumber);
                }
            }

            string[] IsUsedIgnoreList = new string[] { ZStringConstants.ParameterIDZo_Frequency, ZStringConstants.ParameterIDZdiff_Frequency };
            if ((layerType == ZLayerType.Signal || layerType == ZLayerType.SplitMixed) && !string.IsNullOrWhiteSpace(isUsed) && isUsed.ToLower() != "true" && !IsUsedIgnoreList.Contains(this.OwningColumn.Name)) return string.Empty;

            if (value == null) return string.Empty;

            return value.ToString();
        }

        #region Painting.
        
        protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex, DataGridViewElementStates cellState, object value, object formattedValue, string errorText, DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
        {
            ZDataGridView grid = this.DataGridView as ZDataGridView;
            if (grid != null) grid.SetToolTipText(this, true);

            formattedValue = this.GetFormattedValue(value, rowIndex, ref cellStyle, null, null, DataGridViewDataErrorContexts.Formatting);
            if (OwnerCell != null && OwnerCell.DataGridView == null)
                OwnerCell = null; //owner cell was removed.

            if (DataGridView == null
                || (OwnerCell == null && ColumnSpan == 1 && RowSpan == 1))
            {
                NativePaint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText, cellStyle, advancedBorderStyle,
                        paintParts);
                return;
            }

            var ownerCell = this;
            var columnIndex = ColumnIndex;
            var columnSpan = ColumnSpan;
            var rowSpan = RowSpan;
            if (valueOwnerCell != null)
            {
                ownerCell = valueOwnerCell;
                columnIndex = ownerCell.ColumnIndex;
                rowIndex = ownerCell.RowIndex;
                columnSpan = ownerCell.ColumnSpan;
                rowSpan = ownerCell.RowSpan;
                value = ownerCell.GetValue(rowIndex);
                errorText = ownerCell.GetErrorText(rowIndex);
                cellState = ownerCell.State;
                cellStyle = ownerCell.GetInheritedStyle(null, rowIndex, true);
                formattedValue = ownerCell.GetFormattedValue(value,
                    rowIndex, ref cellStyle, null, null, DataGridViewDataErrorContexts.Display);
            }
            if (CellsRegionContainsSelectedCell(columnIndex, rowIndex, columnSpan, rowSpan))
                cellState |= DataGridViewElementStates.Selected;

            var cellBounds2 = ZDataGridView.GetSpannedCellBoundsFromChildCellBounds(
                this,
                cellBounds,
                ZDataGridView.SingleVerticalCellBorderAdded(DataGridView),
                ZDataGridView.SingleHorizontalCellBorderAdded(DataGridView));
            clipBounds = ZDataGridView.GetSpannedCellClipBounds(ownerCell, cellBounds2,
                ZDataGridView.SingleVerticalCellBorderAdded(DataGridView),
                ZDataGridView.SingleHorizontalCellBorderAdded(DataGridView));
            using (var g = DataGridView.CreateGraphics())
            {
                g.SetClip(clipBounds);
                //Paint the content.
                advancedBorderStyle = ZDataGridView.AdjustCellBorderStyle(ownerCell);
                ownerCell.NativePaint(g, clipBounds, cellBounds2, rowIndex, cellState,
                    value, formattedValue, errorText,
                    cellStyle, advancedBorderStyle,
                    paintParts & ~DataGridViewPaintParts.Border);
                //Paint the borders.
                if ((paintParts & DataGridViewPaintParts.Border) != DataGridViewPaintParts.None)
                {
                    var leftTopCell = ownerCell;
                    var advancedBorderStyle2 = new DataGridViewAdvancedBorderStyle
                                                   {
                        Left = advancedBorderStyle.Left,
                        Top = advancedBorderStyle.Top,
                        Right = DataGridViewAdvancedCellBorderStyle.None,
                        Bottom = DataGridViewAdvancedCellBorderStyle.None
                    };
                    leftTopCell.PaintBorder(g, clipBounds, cellBounds2, cellStyle, advancedBorderStyle2);

                    var rightBottomCell = DataGridView[columnIndex + columnSpan - 1, rowIndex + rowSpan - 1] as ZDataGridViewTextBoxCell 
                                          ?? this;
                    var advancedBorderStyle3 = new DataGridViewAdvancedBorderStyle
                    {
                        Left = DataGridViewAdvancedCellBorderStyle.None,
                        Top = DataGridViewAdvancedCellBorderStyle.None,
                        Right = advancedBorderStyle.Right,
                        Bottom = advancedBorderStyle.Bottom
                    };
                    rightBottomCell.PaintBorder(g, clipBounds, cellBounds2, cellStyle, advancedBorderStyle3);
                }
            }
        }

        private void NativePaint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex, DataGridViewElementStates cellState, object value, object formattedValue, string errorText, DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
        {
            if (this.DataGridView.CurrentCell != null && this.DataGridView.CurrentCell == this || this.OwningRow.Selected)
            {
                cellState = DataGridViewElementStates.Selected;
            }

            base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);
        }

        #endregion
        #region Spanning.

        private void SetSpan(int columnSpan, int rowSpan)
        {
            int prevColumnSpan = ColumnSpan;
            int prevRowSpan = RowSpan;
            valueColumnSpan = columnSpan;
            valueRowSpan = rowSpan;

            if (DataGridView != null)
            {
                // clear.
                for (int rowIndex = RowIndex; rowIndex < RowIndex + prevRowSpan; rowIndex++)
                    for (int columnIndex = ColumnIndex; columnIndex < ColumnIndex + prevColumnSpan; columnIndex++)
                    {
                        var cell = DataGridView[columnIndex, rowIndex] as ZDataGridViewTextBoxCell;
                        if (cell != null)
                            cell.OwnerCell = null;
                    }

                // set.
                for (int rowIndex = RowIndex; rowIndex < RowIndex + RowSpan; rowIndex++)
                    for (int columnIndex = ColumnIndex; columnIndex < ColumnIndex + ColumnSpan; columnIndex++)
                    {
                        var cell = DataGridView[columnIndex, rowIndex] as ZDataGridViewTextBoxCell;
                        if (cell != null && cell != this)
                        {
                            if (cell.ColumnSpan > 1) cell.ColumnSpan = 1;
                            if (cell.RowSpan > 1) cell.RowSpan = 1;
                            cell.OwnerCell = this;
                        }
                    }

                OwnerCell = null;
                DataGridView.Invalidate();
            }
        }

        #endregion

        #region Editing.

        public override Rectangle PositionEditingPanel(Rectangle cellBounds, Rectangle cellClip, DataGridViewCellStyle cellStyle, bool singleVerticalBorderAdded, bool singleHorizontalBorderAdded, bool isFirstDisplayedColumn, bool isFirstDisplayedRow)
        {
            if (OwnerCell == null
                && ColumnSpan == 1 && RowSpan == 1)
            {
                return base.PositionEditingPanel(cellBounds, cellClip, cellStyle, singleVerticalBorderAdded, singleHorizontalBorderAdded, isFirstDisplayedColumn, isFirstDisplayedRow);
            }

            var ownerCell = this;
            if (valueOwnerCell != null)
            {
                var rowIndex = valueOwnerCell.RowIndex;
                cellStyle = valueOwnerCell.GetInheritedStyle(null, rowIndex, true);
                valueOwnerCell.GetFormattedValue(valueOwnerCell.Value, rowIndex, ref cellStyle, null, null, DataGridViewDataErrorContexts.Formatting);
                var editingControl = DataGridView.EditingControl as IDataGridViewEditingControl;
                if (editingControl != null)
                {
                    editingControl.ApplyCellStyleToEditingControl(cellStyle);
                    var editingPanel = DataGridView.EditingControl.Parent;
                    if (editingPanel != null)
                        editingPanel.BackColor = cellStyle.BackColor;
                }
                ownerCell = valueOwnerCell;
            }
            cellBounds = ZDataGridView.GetSpannedCellBoundsFromChildCellBounds(
                this, 
                cellBounds, 
                singleVerticalBorderAdded, 
                singleHorizontalBorderAdded);
            cellClip = ZDataGridView.GetSpannedCellClipBounds(ownerCell, cellBounds, singleVerticalBorderAdded, singleHorizontalBorderAdded);
            return base.PositionEditingPanel(
                 cellBounds, cellClip, cellStyle,
                 singleVerticalBorderAdded, 
                 singleHorizontalBorderAdded,
                 ZDataGridView.InFirstDisplayedColumn(ownerCell),
                 ZDataGridView.InFirstDisplayedRow(ownerCell));
        }
        
        protected override object GetValue(int rowIndex)
        {
            if (OwnerCell != null)
                return valueOwnerCell.GetValue(OwnerCell.RowIndex);
            return base.GetValue(rowIndex);
        }
        
        protected override bool SetValue(int rowIndex, object value)
        {
            if (OwnerCell != null)
                return valueOwnerCell.SetValue(OwnerCell.RowIndex, value);
            return base.SetValue(rowIndex, value);
        }
        
        #endregion

        #region Other overridden

        protected override void OnDataGridViewChanged()
        {
            base.OnDataGridViewChanged();

            if (DataGridView == null)
            {
                ColumnSpan = 1;
                RowSpan = 1;
            }
        }

        protected override Rectangle BorderWidths(DataGridViewAdvancedBorderStyle advancedBorderStyle)
        {
            if (OwnerCell == null
                && ColumnSpan == 1 && RowSpan == 1)
            {
                return base.BorderWidths(advancedBorderStyle);
            }

            if (OwnerCell != null)
                return valueOwnerCell.BorderWidths(advancedBorderStyle);
            
            var leftTop = base.BorderWidths(advancedBorderStyle);
            var rightBottomCell = DataGridView[
                ColumnIndex + ColumnSpan - 1,
                RowIndex + RowSpan - 1] as ZDataGridViewTextBoxCell;
            var rightBottom = rightBottomCell != null
                ? rightBottomCell.NativeBorderWidths(advancedBorderStyle)
                : leftTop;
            return new Rectangle(leftTop.X, leftTop.Y, rightBottom.Width, rightBottom.Height);
        }

        private Rectangle NativeBorderWidths(DataGridViewAdvancedBorderStyle advancedBorderStyle)
        {
            return base.BorderWidths(advancedBorderStyle);
        }

        protected override Size GetPreferredSize(Graphics graphics, DataGridViewCellStyle cellStyle, int rowIndex, Size constraintSize)
        {
            if (OwnerCell != null) return new Size(0, 0);
            Size size = base.GetPreferredSize(graphics, cellStyle, rowIndex, constraintSize);
            DataGridView grid = DataGridView;
            int width = size.Width;
            for (int col = ColumnIndex + 1; col < ColumnIndex + ColumnSpan; col++)
                width -= grid.Columns[col].Width;
            int height = size.Height;
            for(int row = RowIndex + 1; row < RowIndex + RowSpan; row++)
                height -= grid.Rows[row].Height;
            return new Size(width, height);
        }

        #endregion

        public void ZLayerParameter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ZLayerParameter layerParameter = sender as ZLayerParameter;

            if (layerParameter == null) return;

            switch (e.PropertyName)
            {
                case "Value":
                    this.Value = layerParameter.Value;
                    break;
                default:
                    break;
            }
        }

        public void ZMaterialParameter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ZMaterialParameter materialParameter = sender as ZMaterialParameter;

            if (materialParameter == null) return;

            switch (e.PropertyName)
            {
                case "Value":
                    this.Value = materialParameter.Value;
                    break;
                default:
                    break;
            }
        }

        #region Private Methods

        private bool CellsRegionContainsSelectedCell(int columnIndex, int rowIndex, int columnSpan, int rowSpan)
        {
            if (DataGridView == null)
                return false;

            for (int col = columnIndex; col < columnIndex + columnSpan; col++)
                for (int row = rowIndex; row < rowIndex + rowSpan; row++)
                    if (DataGridView[col, row].Selected) return true;
            return false;
        }
        
        #endregion
    }
}
