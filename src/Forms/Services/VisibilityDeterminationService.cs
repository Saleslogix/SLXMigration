using System.Drawing;
using Sage.SalesLogix.LegacyBridge.Delphi;

namespace Sage.SalesLogix.Migration.Forms.Services
{
    public sealed class VisibilityDeterminationService : IVisibilityDeterminationService
    {
        public void Determine(DelphiComponent component)
        {
            InternalProcess(component, true);
        }

        private static void InternalProcess(DelphiComponent component, bool parentVisible)
        {
            int count = component.Components.Count;

            for (int i = 0; i < count - 1; i++)
            {
                DelphiComponent component1 = component.Components[i];
                bool visible = (parentVisible && (!component1.TryGetPropertyValue("Visible", out visible) || visible));
                Rectangle rect1;

                if (visible && TryGetRectangle(component1, out rect1))
                {
                    int area1 = rect1.Width*rect1.Height;
                    int areaThreshold = area1 - (area1 >> 3); // 7/8ths rounded up

                    for (int j = i + 1; j < count; j++)
                    {
                        DelphiComponent component2 = component.Components[j];
                        Rectangle rect2;

                        if (TryGetRectangle(component2, out rect2))
                        {
                            Rectangle intersection = Rectangle.Intersect(rect1, rect2);

                            if (intersection != Rectangle.Empty && (intersection.Width*intersection.Height) > areaThreshold)
                            {
                                visible = false;
                                break;
                            }
                        }
                    }
                }

                if (!visible)
                {
                    component1.Properties["Visible"] = false;
                }

                InternalProcess(component1, visible);
            }
        }

        private static bool TryGetRectangle(DelphiComponent component, out Rectangle rectangle)
        {
            int left;
            int top = 0;
            int width = 0;
            int height = 0;
            bool success = (component.TryGetPropertyValue("Left", out left) &&
                            component.TryGetPropertyValue("Top", out top) &&
                            component.TryGetPropertyValue("Width", out width) &&
                            component.TryGetPropertyValue("Height", out height));
            rectangle = (success
                             ? new Rectangle(left, top, width, height)
                             : Rectangle.Empty);
            return success;
        }
    }
}