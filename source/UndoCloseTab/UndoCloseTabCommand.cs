#nullable enable

using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using System;
using System.Threading.Tasks;


namespace UndoCloseTab;


[Command(PackageIds.UndoCommand)]
public sealed class UndoCloseTabCommand : BaseCommand<UndoCloseTabCommand> {

    ClosedWindowRegistry _registry = null!; // Initialized immediately after command creation.


    protected override async Task InitializeCompletedAsync() {
        _registry = await Package.GetServiceAsync<ClosedWindowRegistry, ClosedWindowRegistry>();
    }


    protected override void BeforeQueryStatus(EventArgs e) {
        Command.Enabled = _registry.Count > 0;
    }


    protected override void Execute(object sender, EventArgs e) {
        if (_registry.TryGetLastClosedWindow(out WindowInfo info)) {
            VsShellUtilities.OpenDocumentWithSpecificEditor(
                Package,
                info.FileName,
                info.EditorType,
                VSConstants.LOGVIEWID_Primary
            );
        }
    }

}
