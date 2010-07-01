using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using Sage.Platform.Application;
using Sage.Platform.QuickForms.Controls;
using Sage.Platform.QuickForms.Elements;
using Sage.SalesLogix.Migration.Forms.Builders;
using Sage.SalesLogix.Migration.Services;

namespace Sage.SalesLogix.Migration.Forms.Services
{
    public sealed class FormLayoutService : IFormLayoutService
    {
        private MigrationContext _context;

        [ServiceDependency]
        public IMigrationContextHolderService ContextHolder
        {
            set
            {
                value.ContextChanged += delegate
                    {
                        _context = value.Context;
                    };
                _context = value.Context;
            }
        }

        private FormInfo _form;
        //private IList<ControlInfo> _controls;
        //private IList<ColumnStyle> _columns;
        //private IList<RowStyle> _rows;
        //private List<int> _lefts;
        //private List<int> _tops;
        //private int _columnCount;
        //private int _rowCount;
        //private ControlInfo[,] _grid;

        #region IFormLayoutManager Members

        public void Process(FormInfo form)
        {
            _form = form;
            RecurseControls(form);
        }

        #endregion


        private ControlInfo[,] GetSurfaceGrid(IList<ControlInfo> controls, IList<ColumnStyle> columns, IList<RowStyle> rows)
        {
            ControlInfo[,] points = new ControlInfo[columns.Count + 1, rows.Count + 1]; //it will be populated by nulls
            foreach (ControlInfo control in controls)
            {
                if ((control.QfControl != null) && (control.IsVisible) && (!control.IsExcluded) && (!control.IsTool)) //visible controls only
                {
                    for (int x = control.Column; x < control.Column + control.ColumnSpan; x++)
                    {
                        for (int y = control.Row; y < control.Row + control.RowSpan; y++)
                        {
                            if ((x >= 0) && (y >= 0) && (x < columns.Count) && (y < rows.Count)) points[x, y] = control;
                        }
                    }
                }
            }
            return points;
        }

        private class ControlVariation
        {
            public ControlInfo Control = null;
            public int NewLeft, NewTop, NewWidth, NewHeight;
            public ControlVariation(ControlInfo ctrl, int newLeft, int newTop, int newWidth, int newHeight)
            {
                Control = ctrl;
                NewTop = newTop;
                NewLeft = newLeft;
                NewWidth = newWidth;
                NewHeight = newHeight;
            }

            public int AreaChange()
            {
                return (Math.Abs(Control.Width * Control.Height - NewWidth * NewHeight)) + //area change
                       (Math.Abs(Control.Left - NewLeft) * Control.Height + Control.Width * Math.Abs(Control.Height - NewHeight)); //position change
            }
            
        }

        private int AdjustOverlappingControlSizes(ControlInfo ctrl1, ControlInfo ctrl2, int MaxRow, int MaxColumn)
        {
            Rectangle r1 = new Rectangle(ctrl1.Left, ctrl1.Top, ctrl1.Width, ctrl1.Height);
            Rectangle r2 = new Rectangle(ctrl2.Left, ctrl2.Top, ctrl2.Width, ctrl2.Height);
            Rectangle rr = Rectangle.Intersect(r1, r2);
            if ((rr != null) && (rr.Width > 0) && (rr.Height > 0))
            {
                //yes, the controls do indeed overlap
                //build a list of all possible changes that make controls non-overlapping
                List<ControlVariation> list = new List<ControlVariation>();

                //ctrl1 - vary Left
                if ((ctrl1.Left >= rr.Left) && (ctrl1.Left < rr.Left + rr.Width))
                {
                    list.Add(new ControlVariation(ctrl1, rr.Left + rr.Width, ctrl1.Top, ctrl1.Width - rr.Width, ctrl1.Height)); //vary left
                }
                //ctrl1 - vary Top
                if ((ctrl1.Top >= rr.Top) && (ctrl1.Top < rr.Top + rr.Height))
                {
                    list.Add(new ControlVariation(ctrl1, ctrl1.Left, rr.Top + rr.Height, ctrl1.Width, ctrl1.Height - rr.Height)); //vary top
                }
                //ctrl1 - vary Width 
                if ((ctrl1.Left + ctrl1.Width >= rr.Left) && (ctrl1.Left + ctrl1.Width < rr.Left + rr.Width))
                {
                    list.Add(new ControlVariation(ctrl1, ctrl1.Left, ctrl1.Top, ctrl1.Width - rr.Width, ctrl1.Height)); //vary width
                }
                //ctrl1 - vary Height
                if ((ctrl1.Top + ctrl1.Height >= rr.Top) && (ctrl1.Top + ctrl1.Height < rr.Top + rr.Height))
                {
                    list.Add(new ControlVariation(ctrl1, ctrl1.Left, rr.Top, ctrl1.Width, ctrl1.Height - rr.Height)); //vary height
                }

                //ctrl2 - vary Left
                if ((ctrl2.Left >= rr.Left) && (ctrl2.Left < rr.Left + rr.Width))
                {
                    list.Add(new ControlVariation(ctrl2, rr.Left + rr.Width, ctrl2.Top, ctrl2.Width - rr.Width, ctrl2.Height)); //vary left
                }
                //ctrl2 - vary Top
                if ((ctrl2.Top >= rr.Top) && (ctrl2.Top < rr.Top + rr.Height))
                {
                    list.Add(new ControlVariation(ctrl2, ctrl2.Left, rr.Top + rr.Height, ctrl2.Width, ctrl2.Height - rr.Height)); //vary top
                }
                //ctrl2 - vary Width 
                if ((ctrl2.Left + ctrl2.Width >= rr.Left) && (ctrl2.Left + ctrl2.Width < rr.Left + rr.Width))
                {
                    list.Add(new ControlVariation(ctrl2, ctrl2.Left, ctrl2.Top, ctrl2.Width - rr.Width, ctrl2.Height)); //vary width
                }
                //ctrl2 - vary Height
                if ((ctrl2.Top + ctrl2.Height >= rr.Top) && (ctrl2.Top + ctrl2.Height < rr.Top + rr.Height))
                {
                    list.Add(new ControlVariation(ctrl2, ctrl2.Left, rr.Top, ctrl2.Width, ctrl2.Height - rr.Height)); //vary height
                }

                //now find the smallest change
                int AreaChange = int.MaxValue;
                ControlVariation MinVariation = null;
                foreach (ControlVariation variation in list)
                {
                    //make sure the control size stays within the form 
                    if ((variation.AreaChange() < AreaChange) && (variation.NewLeft >= 0) && (variation.NewTop >= 0) &&
                        (variation.NewLeft + variation.NewWidth <= MaxColumn) && (variation.NewTop + variation.NewHeight <= MaxRow))
                    {
                        AreaChange = variation.AreaChange();
                        MinVariation = variation;
                    }
                }
                if (MinVariation != null)
                {
                    string ModifiedProperties = "";
                    if (MinVariation.Control.Column != MinVariation.NewLeft) ModifiedProperties += "Left, ";
                    if (MinVariation.Control.ColumnSpan != MinVariation.NewWidth) ModifiedProperties += "Width, ";
                    if (MinVariation.Control.Row != MinVariation.NewTop) ModifiedProperties += "Top, ";
                    if (MinVariation.Control.RowSpan != MinVariation.NewHeight) ModifiedProperties += "Height, ";
                    if (ModifiedProperties.Length > 0)
                    {
                        ModifiedProperties = ModifiedProperties.Remove(ModifiedProperties.Length - 2); //remove ", "
                        _context.Log.Warn("Control '{0}': the following properties were adjusted: {1}", new object[] { MinVariation.Control.QfControl.ControlId, ModifiedProperties });
                    }

                    MinVariation.Control.Column = MinVariation.NewLeft;
                    MinVariation.Control.ColumnSpan = MinVariation.NewWidth;
                    MinVariation.Control.Row = MinVariation.NewTop;
                    MinVariation.Control.RowSpan = MinVariation.NewHeight;
                }
                return AreaChange;
            }
            return 0;
        }

        //the beef of the new algorithm - give a list of controls and lists of rows and columns, 
        //modify (Row, Column, RowSpan, ColumnSpan) properties for each control
        private void NewAlgorithmProcessControls(IList<ControlInfo> controls, IList<ColumnStyle> columns, IList<RowStyle> rows, int MaxX, int MaxY)
        {
            columns.Clear();
            rows.Clear();

            //max coordinates
            foreach (ControlInfo control in controls)
            {
                if (control.Left + control.Width > MaxX) MaxX = control.Left + control.Width;
                if (control.Top + control.Height > MaxY) MaxY = control.Top + control.Height;
            }

            //make columns and rows the same as left/width and top/height
            foreach (ControlInfo control in controls)
            {
                control.Column = control.Left;
                control.Row = control.Top;
                control.ColumnSpan = control.Width;
                control.RowSpan = control.Height;
            }

            //report any overlapping controls
            for (int i = 0; i < controls.Count; i++)
            {
                ControlInfo ctrl1 = controls[i];
                if ((ctrl1.QfControl != null) && (ctrl1.IsVisible) && (!ctrl1.IsExcluded) && (!ctrl1.IsTool))
                {
                    Rectangle r1 = new Rectangle(ctrl1.Left, ctrl1.Top, ctrl1.Width, ctrl1.Height);
                    for (int j = i + 1; j < controls.Count; j++)
                    {
                        ControlInfo ctrl2 = controls[j];
                        if ((ctrl2.QfControl != null) && (ctrl2.IsVisible) && (!ctrl2.IsExcluded) && (!ctrl2.IsTool))
                        {
                            Rectangle r2 = new Rectangle(ctrl2.Left, ctrl2.Top, ctrl2.Width, ctrl2.Height);
                            Rectangle rr = Rectangle.Intersect(r1, r2);
                            if ((rr != null) && (rr.Width > 0) && (rr.Height > 0))
                            {
                                //yep, these two controls overlap. Is that a complete overlap or partial?
                                if ((r1.Left >= r2.Left) && (r1.Top >= r2.Top) && (r1.Left + r1.Width <= r2.Left + r2.Width) && (r1.Top + r1.Height <= r2.Top + r2.Height))
                                {
                                    //ctrl2 completely overlaps ctrl1
                                    _context.Log.Warn("Control '{0}' completely overlaps control '{1}'", new object[] { ctrl2.QfControl.ControlId, ctrl1.QfControl.ControlId });
                                }
                                else if ((r2.Left >= r1.Left) && (r2.Top >= r1.Top) && (r2.Left + r2.Width <= r1.Left + r1.Width) && (r2.Top + r2.Height <= r1.Top + r1.Height))
                                {
                                    //ctrl1 completely overlaps ctrl2
                                    _context.Log.Warn("Control '{0}' completely overlaps control '{1}'", new object[] { ctrl1.QfControl.ControlId, ctrl2.QfControl.ControlId });
                                }
                                else
                                {
                                    //partical overlap
                                    _context.Log.Warn("Control '{0}' partially overlaps control '{1}'", new object[] { ctrl1.QfControl.ControlId, ctrl2.QfControl.ControlId });
                                    //try to fix the partial overlap
                                    AdjustOverlappingControlSizes(ctrl1, ctrl2, rows.Count - 1, columns.Count - 1);
                                }
                            }
                        }
                    }
                }
            }

            //initalize Columns and Rows lists
            for (int x = 0; x < MaxX; x++ ) columns.Add(new ColumnStyle(SizeType.Absolute, 1));
            for (int y = 0; y < MaxY; y++) rows.Add(new RowStyle(SizeType.Absolute, 1));

            //merge label with their adjucent controls
            if (_context.Settings.MergeLabels)
            {
                MergeAdjacentLabels(controls, columns, rows);
            }

            //populate the surface area array
            ControlInfo[,] points = GetSurfaceGrid(controls, columns, rows);

            //re-adjust the sizes of the labels with the Autosize property set
            foreach (ControlInfo control in controls)
            {
                if ((control.Builder != null) && (control.Builder is LabelBuilder))
                {
                    LabelBuilder labelBuilder = (LabelBuilder)control.Builder;
                    if (labelBuilder != null)
                    {
                        if (labelBuilder.AutoSize)
                        {
                            for (int x = control.Column + control.ColumnSpan; x <= MaxX; x++)
                            {
                                bool overlaps = false;
                                for (int y = control.Row; y < control.Row + control.RowSpan; y++)
                                {
                                    if (points[x, y] != null)
                                    {
                                        overlaps = true;
                                        break;
                                    }
                                }
                                if (overlaps) break; //from x
                                control.ColumnSpan++;
                                for (int y = control.Row; y < control.Row + control.RowSpan; y++) points[x, y] = control;
                            }
                        }
                    }
                }
            }

            //here is the beauty - remove duplicate rows and columns

            //Columns
            for (int x = columns.Count-1; x >= 0; x--)
            {
                //the current column
                ControlInfo[] CurrentColumn = new ControlInfo[rows.Count];
                for (int y = 0; y < rows.Count; y++) CurrentColumn[y] = points[x, y];

                //now go left and see if the same column repeats itself and how many times
                int RepeatColumns = 1; //at least the current one
                for (int x2 = x - 1; x2 >= 0; x2--)
                {
                    bool ColumnMatches = true;
                    for (int y = 0; y < rows.Count; y++)
                    {
                        if (points[x2, y] != CurrentColumn[y])
                        {
                            ColumnMatches = false;
                            break; //from y loop
                        }
                    }
                    if (!ColumnMatches) break; //from x2 loop
                    RepeatColumns++; 
                }
                if (RepeatColumns > 1)
                {
                    //yes, we had at least 2 repeating columns. 
                    //readjust the current column's width
                    columns[x].Width = columns[x].Width + RepeatColumns - 1;
                    //remove the duplicate columns
                    for (int x2 = x - 1; x2 >= x - RepeatColumns + 1; x2--) columns.RemoveAt(x2);
                    //for all controls that span to the right of x, re-asjust the column and, posibly, the column span
                    foreach (ControlInfo control in controls)
                    {
                        if (control.Column >= x) control.Column = control.Column - RepeatColumns + 1;
                        else if ((control.Column + control.ColumnSpan) >= x) control.ColumnSpan = control.ColumnSpan - RepeatColumns + 1;
                    }
                    //re-adjust the counter
                    x = x - RepeatColumns + 1; 
                }
            }

            //re-populate the surface area array
            points = GetSurfaceGrid(controls, columns, rows);

            //Rows
            for (int y = rows.Count - 1; y >= 0; y--)
            {
                //the current row
                ControlInfo[] CurrentRow = new ControlInfo[columns.Count];
                for (int x = 0; x < columns.Count; x++) CurrentRow[x] = points[x, y];

                //now go left and see if the same row repeats itself and how many times
                int RepeatRows = 1; //at least the current one
                for (int y2 = y - 1; y2 >= 0; y2--)
                {
                    bool RowMatches = true;
                    for (int x = 0; x < columns.Count; x++)
                    {
                        if (points[x, y2] != CurrentRow[x])
                        {
                            RowMatches = false;
                            break; //from y loop
                        }
                    }
                    if (!RowMatches) break; //from y2 loop
                    RepeatRows++;
                }
                if (RepeatRows > 1)
                {
                    //yes, we had at least 2 repeating rows. 
                    //readjust the current row's width
                    rows[y].Height = rows[y].Height + RepeatRows - 1;
                    //remove the duplicate rows
                    for (int y2 = y - 1; y2 >= y - RepeatRows + 1; y2--) rows.RemoveAt(y2);
                    //for all controls that span to the right of y, re-asjust the row and, posibly, the row span
                    foreach (ControlInfo control in controls)
                    {
                        if (control.Row >= y) control.Row = control.Row - RepeatRows + 1;
                        else if ((control.Row + control.RowSpan) >= y) control.RowSpan = control.RowSpan - RepeatRows + 1;
                    }
                    //re-adjust the counter
                    y = y - RepeatRows + 1;
                }
            }

            //apply the changes from ControlInfo to QuickFormControlBase (ControlInfo.QfControl)
            foreach (ControlInfo control in controls)
            {
                if (control.QfControl != null)
                {
                    control.QfControl.Column = control.Column;
                    control.QfControl.ColumnSpan = Math.Max(1, control.ColumnSpan);
                    control.QfControl.Row = control.Row;
                    control.QfControl.RowSpan = Math.Max(1, control.RowSpan);
                }
            }
        }

        private void RecurseControls(FormInfo form)
        {
            //form itself
            NewAlgorithmProcessControls(form.Controls, form.QuickForm.Columns, form.QuickForm.Rows, 0, 0);
            //child controls (if they are containers)
            foreach (ControlInfo control in form.Controls)
            {
                RecurseControls(control);
            }
        }

        private void RecurseControls(ControlInfo control)
        {
            if (control.QfControl is IQuickFormsControlContainer)
            {
                IQuickFormsControlContainer container = control.QfControl as IQuickFormsControlContainer;
                IList<IControlSurface> lst = container.GetControlSurfaces();
                if (lst != null)
                {
                    foreach (IControlSurface surface in lst)
                    {
                        IList<ControlInfo> controls = new List<ControlInfo>();
                        foreach (ControlInfo childControl in control.Controls)
                        {
                            QuickFormsControlBase qfControl = childControl.QfControl;
                            foreach (var qfe in surface.Controls)
                            {
                                if (qfe.Control == qfControl)
                                {
                                    controls.Add(childControl);
                                    break;
                                }
                            }
                        }
                        //calculate the layout
                        NewAlgorithmProcessControls(controls, surface.Columns, surface.Rows, control.Width, control.Height);
                        //process child controls
                        foreach (ControlInfo childControl in controls) RecurseControls(childControl);
                    }
                }
            }
        }

        private void MergeAdjacentLabels(IList<ControlInfo> controls, IList<ColumnStyle> columns, IList<RowStyle> rows)
        {

            IList<ControlInfo> lstLabels = new List<ControlInfo>();
            IList<ControlInfo> lstNonLabels = new List<ControlInfo>();
            foreach (ControlInfo control in controls)
            {
                if (!(control.IsExcluded || !control.IsVisible || control.IsTool || (control.Builder is ButtonBuilder)))
                {
                    if (control.Builder is LabelBuilder)
                    {
                        lstLabels.Add(control);
                    }
                    else
                    {
                        lstNonLabels.Add(control);
                    }
                }
            }

            ControlInfo[,] points = GetSurfaceGrid(controls, columns, rows);

            for (int i = 0; i < lstNonLabels.Count; i++)
            {
                ControlInfo control = lstNonLabels[i];
                if (lstNonLabels.IndexOf(control) >= 0)
                {
                    ControlInfo label1 = null;

                    for (int col = control.Column - 1; col >= 0; col--)
                    {
                        ControlInfo label2 = null;
                        bool failed = false;

                        for (int row = control.Row; row < control.Row + control.RowSpan; row++)
                        {
                            ControlInfo ctrl = points[col, row];

                            if (ctrl != null && ctrl != label2)
                            {
                                if (ctrl.Builder is LabelBuilder && label2 == null && (lstLabels.IndexOf(ctrl) >= 0))
                                {
                                    label2 = ctrl;
                                }
                                else
                                {
                                    failed = true;
                                    break;
                                }
                            }
                        }

                        if (failed)
                        {
                            break;
                        }

                        if (label2 != null && label2 != label1)
                        {
                            if (label2.Row >= control.Row && (label2.Row + label2.RowSpan) <= (control.Row + control.RowSpan + 4) && label1 == null) //4 extra pixels just to give it some room for error
                            {
                                label1 = label2;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    if (label1 != null)
                    {
                        for (int c = label1.Column; c < control.Column; c++)
                        {
                            for (int r = control.Row; r < control.Row + control.RowSpan; r++)
                            {
                                points[c, r] = control;
                            }
                        }

                        control.ColumnSpan += (control.Column - label1.Column);
                        control.Column = label1.Column;
                        control.QfControl.Caption = (label1.Caption == null) ? string.Empty : label1.Caption;
                        control.RowSpan = Math.Max(control.Row + control.RowSpan, label1.Row + label1.RowSpan) - Math.Min(control.Row, label1.Row);
                        control.Row = Math.Min(control.Row, label1.Row);
                        controls.Remove(label1);
                        label1.IsExcluded = true;
                        RemoveControl(label1);
                    }
                }
            }
        }

        //*************** END new stuff

        private void RemoveControl(ControlInfo control)
        {
            RemoveControlFromList(control, _form.QuickForm.Elements);
        }

        private void RemoveControlFromList(ControlInfo control, IList<QuickFormElement> controls)
        {
            foreach (QuickFormElement qfe in controls)
            {
                if (qfe.Control == control.QfControl)
                {
                    int i = controls.IndexOf(qfe);
                    if (i >= 0)
                    {
                        controls.RemoveAt(i);
                    }
                    //controls.Remove(qfe);
                    break;
                }
                if (qfe.Control is IQuickFormsControlContainer)
                {
                    IQuickFormsControlContainer container = qfe.Control as IQuickFormsControlContainer;
                    IList<IControlSurface> lst = container.GetControlSurfaces();
                    if (lst != null)
                    {
                        foreach (IControlSurface surface in lst)
                        {
                            RemoveControlFromList(control, surface.Controls);
                        }
                    }
                }
                    
            }
        }

    }
}