using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace CppReferenceDocs
{
    [Guid("86140af6-ad13-4087-b42f-4c9d16496fff")]
    public sealed class ToolWindow1 : ToolWindowPane
    {
        public ToolWindow1() : base(null)
        {
            this.Caption = "ToolWindow1";
            this.Content = new ToolWindow1Control();
        }
    }
}
