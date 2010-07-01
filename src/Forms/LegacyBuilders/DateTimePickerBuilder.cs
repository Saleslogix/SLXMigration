using Sage.Platform.QuickForms.Controls;
using Sage.SalesLogix.QuickForms.QFControls;

namespace Sage.SalesLogix.Migration.Forms.LegacyBuilders
{
    [BuilderMapping("TSLDateEdit")]
    public sealed class DateTimePickerBuilder : ControlBuilder
    {
        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFDateTimePicker();
        }

        protected override void OnBuild()
        {
            DataPath bindingPath;

            if (Control.Bindings != null &&
                (Control.Bindings.TryGetValue("Date", out bindingPath)))
            {
                string propertyString = null;

                try
                {
                    propertyString = DataPathTranslator.TranslateField(bindingPath);
                }
                catch (MigrationException ex)
                {
                    LogError(ex.Message);
                }

                if (propertyString != null)
                {
                    QfControl.DataBindings.Add(new QuickFormPropertyDataBindingDefinition(propertyString, "DateTimeValue"));
                }
            }

            string dateStyle;

            if (Component.TryGetPropertyValue("DateStyle", out dateStyle))
            {
                QFDateTimePicker picker = (QFDateTimePicker) QfControl;
                picker.DisplayDate = (dateStyle != "desTime");
                picker.DisplayTime = (dateStyle != "desDate");
            }
        }
    }
}