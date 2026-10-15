using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ChromExplorer.Infrastructure.InteractionRequest;
using ChromExplorer.Modules.Viewer.ViewModels;
using log4net;

namespace ChromExplorer.Modules.Viewer.Views
{
   
    public partial class QuanInfoView : InteractionDialogBase
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public QuanInfoView()
        {
            Log.Debug("QuanInfoView() called");
            InitializeComponent();
        }
        

     

    }
}
