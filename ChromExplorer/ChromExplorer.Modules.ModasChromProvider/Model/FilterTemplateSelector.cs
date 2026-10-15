using System.Windows;
using System.Windows.Controls;
using ChromExplorer.ModasReader;

namespace ChromExplorer.Modules.ModasChromProvider.Model

{

    public class FilterTemplateSelector : DataTemplateSelector
    {

        public DataTemplate Empty { get; set; }

        public DataTemplate Method { get; set; }

        public DataTemplate ParentIon { get; set; }

        public DataTemplate MsN { get; set; }

        public DataTemplate InjectionTime { get; set; }

        public DataTemplate MassRange { get; set; }


        public override DataTemplate SelectTemplate(object item,

                                   DependencyObject container)

        {

            var fs = item as ChromFilterSettings;


            if (fs != null)

            {

                switch (fs.FilterType)

                {

                    case ChromFilters.Method:

                        return Method;


                    case ChromFilters.ParentIon:

                        return ParentIon;


                    case ChromFilters.MsN:

                        return MsN;


                    case ChromFilters.InjectionTime:

                        return InjectionTime;


                    case ChromFilters.MassRange:

                        return MassRange;


                    case ChromFilters.None:

                        return Empty;

                }

            }


            return Empty;

        }


    }

}