using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;
using ChromExplorer.Infrastructure.Interactivity;

namespace ChromExplorer.Modules.Licensing.Interactivity
{
    public class LocalInvokeDelegateCommandAction : TriggerAction<DependencyObject>
    {
        /// <summary>
        ///
        /// </summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command", typeof(ICommand), typeof(InvokeDelegateCommandAction), null);

        /// <summary>
        ///
        /// </summary>
        public static readonly DependencyProperty PassArgumentsProperty = DependencyProperty.Register(
            "InvokeParameter", typeof(bool), typeof(InvokeDelegateCommandAction), null);

        private string commandName;

        /// <summary>
        ///
        /// </summary>
        public bool PassArguments
        {
            get
            {
                return (bool)this.GetValue(PassArgumentsProperty);
            }
            set
            {
                this.SetValue(PassArgumentsProperty, value);
            }
        }

        /// <summary>
        ///
        /// </summary>
        public ICommand Command
        {
            get
            {
                return (ICommand)this.GetValue(CommandProperty);
            }
            set
            {
                this.SetValue(CommandProperty, value);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="parameter"></param>
        protected override void Invoke(object parameter)
        {
            if (this.AssociatedObject != null)
            {
                ICommand command = this.ResolveCommand();
                if ((command != null) && command.CanExecute(parameter))
                {
                    command.Execute(this.PassArguments ? parameter : null);
                }
            }
        }

        private ICommand ResolveCommand()
        {
            ICommand command = null;
            if (this.Command != null)
            {
                return this.Command;
            }
            //var frameworkElement = this.AssociatedObject as FrameworkElement;
            //if (frameworkElement != null)
            //{
            //    object dataContext = frameworkElement.DataContext;
            //    if (dataContext != null)
            //    {
            //        PropertyInfo commandPropertyInfo = dataContext
            //            .GetType()
            //            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            //            .FirstOrDefault(
            //                p =>
            //                typeof(ICommand).IsAssignableFrom(p.PropertyType) &&
            //                string.Equals(p.Name, this.CommandName, StringComparison.Ordinal)
            //            );

            //        if (commandPropertyInfo != null)
            //        {
            //            command = (ICommand)commandPropertyInfo.GetValue(dataContext, null);
            //        }
            //    }
            //}
            return command;
        }

    }
}