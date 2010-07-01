using System;
using System.Collections.Generic;
using System.Drawing;

namespace Sage.SalesLogix.Migration.Forms.Services
{
    public sealed class ControlAlignmentService : IControlAlignmentService
    {
        private IList<ControlInfo> _controls;

        #region IControlAlignmentService Members

        public void Align(IList<ControlInfo> controls)
        {
            _controls = controls;
            try
            {
                int x = 8;
                int y = 8;
                int maxLoops = Math.Min(x, y);
                int forwardCounter = 1;
                int backwardCounter = x + y - 1;

                while (x > 0)
                {
                    int loopCount = Math.Min(maxLoops, Math.Min(forwardCounter, backwardCounter));

                    for (int i = 0; i < loopCount; i++)
                    {
                        AlignToGrid(x - i, y + i);

                        if (ContainsOverlaps())
                        {
                            foreach (ControlInfo control in _controls)
                            {
                                if (!(control.IsExcluded || !control.IsVisible || control.IsTool))
                                {
                                    control.ResetLocation();
                                }
                            }
                        }
                        else
                        {
                            return;
                        }
                    }

                    if (y == 1)
                    {
                        x--;
                    }
                    else
                    {
                        y--;
                    }

                    forwardCounter++;
                    backwardCounter--;
                }
            }
            finally
            {
                _controls = null;
            }
        }

        #endregion

        private void AlignToGrid(int xSize, int ySize)
        {
            foreach (ControlInfo control in _controls)
            {
                if (!(control.IsExcluded || !control.IsVisible || control.IsTool))
                {
                    int offset = control.Left%xSize;

                    if (offset != 0)
                    {
                        if (offset > (xSize/2))
                        {
                            offset -= xSize;
                        }

                        control.Left -= offset;
                    }

                    offset = control.Top%ySize;

                    if (offset != 0)
                    {
                        if (offset > (ySize/2))
                        {
                            offset -= ySize;
                        }

                        control.Top -= offset;
                    }
                }
            }
        }

        private bool ContainsOverlaps()
        {
            int count = _controls.Count;

            for (int i = 0; i < count; i++)
            {
                ControlInfo control1 = _controls[i];

                if (!(control1.IsExcluded || !control1.IsVisible || control1.IsTool))
                {
                    Rectangle rectangle1 = CreateRectangle(control1);

                    for (int j = i + 1; j < count; j++)
                    {
                        ControlInfo control2 = _controls[j];

                        if (!(control2.IsExcluded || !control2.IsVisible || control2.IsTool))
                        {
                            Rectangle rectangle2 = CreateRectangle(control2);

                            if (rectangle1.IntersectsWith(rectangle2))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static Rectangle CreateRectangle(ControlInfo control)
        {
            return new Rectangle(
                control.Left,
                control.Top,
                control.Width,
                control.Height);
        }
    }
}