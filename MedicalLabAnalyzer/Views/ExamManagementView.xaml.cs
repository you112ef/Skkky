using System.Windows.Controls;
using MedicalLabAnalyzer.ViewModels;

namespace MedicalLabAnalyzer.Views
{
    /// <summary>
    /// Interaction logic for ExamManagementView.xaml
    /// </summary>
    public partial class ExamManagementView : UserControl
    {
        public ExamManagementView()
        {
            InitializeComponent();
        }

        public ExamManagementView(ExamViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // Focus on search box when the view is loaded
            SearchTextBox?.Focus();
        }
    }
}