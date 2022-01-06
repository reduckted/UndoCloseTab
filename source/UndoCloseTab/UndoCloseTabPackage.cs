#nullable enable

using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;


namespace UndoCloseTab;


[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[ProvideAutoLoad(AutoLoadContext, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideUIContextRule(
    AutoLoadContext,
    "UndoCloseTabPackage Auto Load",
    "NoSolution | SolutionExists",
    new[] { "NoSolution", "SolutionExists" },
    new[] { VSConstants.UICONTEXT.NoSolution_string, VSConstants.UICONTEXT.SolutionExists_string }
)]
[Guid(PackageGuids.PackageString)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideService(typeof(ClosedWindowRegistry), IsAsyncQueryable = true)]
public sealed class UndoCloseTabPackage : ToolkitPackage {

    public const string AutoLoadContext = "4656a7c9-7f96-4479-9120-4375e9632e09";


    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
        ClosedWindowRegistry registry;


        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        registry = await ClosedWindowRegistry.InitializeAsync(JoinableTaskFactory);
        AddService(typeof(ClosedWindowRegistry), (_, _, _) => Task.FromResult<object>(registry));

        await UndoCloseTabCommand.InitializeAsync(this);
    }

}
