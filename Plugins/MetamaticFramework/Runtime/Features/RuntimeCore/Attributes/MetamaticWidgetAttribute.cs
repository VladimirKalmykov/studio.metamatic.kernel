using System;

namespace Metamatic.Framework
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MetamaticWidgetAttribute: Attribute
    {
        public readonly string widgetName;
        public readonly string widgetIcon;
        public readonly WidgetApplicability applicability;
        public readonly WidgetAppearance appearance;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="widgetName">Widget name must includes full path to the panel. Used to place in menu</param>
        /// <param name="widgetTitle">Display title (displayed in the panel header)</param>
        /// <param name="widgetIcon">Optional icon</param>
        /// <param name="applicability">Where panel can be used (e.g. editor or application overlay)</param>
        /// <param name="appearance">How widget looks like</param>
        public MetamaticWidgetAttribute(string widgetName, string widgetIcon, WidgetApplicability applicability = WidgetApplicability.Both, WidgetAppearance appearance = WidgetAppearance.Panel)
        {
            this.widgetName = widgetName;
            this.widgetIcon = widgetIcon;
            this.applicability = applicability;
            this.appearance = appearance;
        }
    }
}