using Interop.SLXControls;
using Sage.Platform.QuickForms.Controls;
using Sage.SalesLogix.QuickForms.QFControls;

namespace Sage.SalesLogix.Migration.Forms.Builders
{
    [BuilderMapping("AxDateTimeEdit")]
    [BuilderMapping("AxDateTimePicker")]
    public sealed class DateTimePickerBuilder : ControlBuilder
    {
        private const int DateBindingCode = 4;
        private const int TimeBindingCode = 5;
        private const int DateTimeBindingCode = 17;

        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFDateTimePicker();
        }

        protected override void OnBuild()
        {
            DataPath bindingPath;

            if (Control.Bindings != null &&
                (Control.Bindings.TryGetValue(DateBindingCode, out bindingPath) ||
                 Control.Bindings.TryGetValue(TimeBindingCode, out bindingPath) ||
                 Control.Bindings.TryGetValue(DateTimeBindingCode, out bindingPath)))
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

            int kind;

            if (Component.TryGetPropertyValue("Kind", out kind))
            {
                QFDateTimePicker picker = (QFDateTimePicker) QfControl;
                TxDateTimeKind2 value = (TxDateTimeKind2) kind;
                picker.DisplayDate = (value != TxDateTimeKind2.dtk2Time);
                picker.DisplayTime = (value != TxDateTimeKind2.dtk2Date);
            }
        }
    }
}