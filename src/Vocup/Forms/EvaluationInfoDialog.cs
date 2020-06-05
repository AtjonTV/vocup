using System.Drawing;
using System.Windows.Forms;
using Vocup.Properties;

namespace Vocup.Forms
{
    public partial class EvaluationInfoDialog : Form
    {
        public EvaluationInfoDialog()
        {
            InitializeComponent();
            Icon = Icon.FromHandle(Icons.Info.GetHicon());
        }
    }
}