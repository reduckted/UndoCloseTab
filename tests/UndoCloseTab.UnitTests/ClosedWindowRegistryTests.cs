using Microsoft.VisualStudio.Sdk.TestFramework;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;


namespace UndoCloseTab;


[Collection(VisualStudioTests.CollectionName)]
public class ClosedWindowRegistryTests {

    private static readonly WindowInfo FirstWindow = new("Foo.cs", new Guid("abaddd79-4ee6-4567-bbf4-15b055c37286"));
    private static readonly WindowInfo SecondWindow = new("Bar.cs", new Guid("7b884310-cddc-4f5f-84a9-3404e0e5a8f0"));
    private static readonly WindowInfo ThirdWindow = new("Meep.cs", new Guid("9cdfa8af-76eb-436a-bd99-9b5cc880a0e6"));


    private readonly GlobalServiceProvider _serviceProvider;
    private IVsWindowFrameEvents? _eventSink;


    public ClosedWindowRegistryTests(GlobalServiceProvider serviceProvider) {
        Mock<IVsUIShell> shell;


        _serviceProvider = serviceProvider;
        _serviceProvider.Reset();

        shell = new Mock<IVsUIShell>();

        shell
            .As<IVsUIShell7>()
            .Setup((x) => x.AdviseWindowFrameEvents(It.IsAny<IVsWindowFrameEvents>()))
            .Callback((IVsWindowFrameEvents eventSink) => _eventSink = eventSink);

        _serviceProvider.AddService(typeof(SVsUIShell), shell.Object);
    }


    [Fact]
    public async Task RecordsClosedWindows() {
        ClosedWindowRegistry registry;


        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        registry = await ClosedWindowRegistry.InitializeAsync(ThreadHelper.JoinableTaskFactory);

        CloseWindow(FirstWindow);
        VerifyLastClosedWindow(registry, FirstWindow);

        CloseWindow(SecondWindow);
        VerifyLastClosedWindow(registry, SecondWindow);

        Assert.Equal(
            new[] { SecondWindow, FirstWindow },
            registry
        );
    }


    [Fact]
    public async Task RemovesLastClosedWindowWhenItIsReopened() {
        ClosedWindowRegistry registry;


        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        registry = await ClosedWindowRegistry.InitializeAsync(ThreadHelper.JoinableTaskFactory);

        CloseWindow(FirstWindow);
        CloseWindow(SecondWindow);
        CloseWindow(ThirdWindow);

        Assert.Equal(
            new[] { ThirdWindow, SecondWindow, FirstWindow },
            registry
        );

        OpenWindow(ThirdWindow);

        Assert.Equal(
            new[] { SecondWindow, FirstWindow },
            registry
        );

        VerifyLastClosedWindow(registry, SecondWindow);
    }


    [Fact]
    public async Task RemovesFirstClosedWindowWhenItIsReopened() {
        ClosedWindowRegistry registry;


        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        registry = await ClosedWindowRegistry.InitializeAsync(ThreadHelper.JoinableTaskFactory);

        CloseWindow(FirstWindow);
        CloseWindow(SecondWindow);
        CloseWindow(ThirdWindow);

        Assert.Equal(
            new[] { ThirdWindow, SecondWindow, FirstWindow },
            registry
        );

        OpenWindow(FirstWindow);

        Assert.Equal(
            new[] { ThirdWindow, SecondWindow },
            registry
        );

        VerifyLastClosedWindow(registry, ThirdWindow);
    }


    [Fact]
    public async Task RemovesMiddleClosedWindowWhenItIsReopened() {
        ClosedWindowRegistry registry;


        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        registry = await ClosedWindowRegistry.InitializeAsync(ThreadHelper.JoinableTaskFactory);

        CloseWindow(FirstWindow);
        CloseWindow(SecondWindow);
        CloseWindow(ThirdWindow);

        Assert.Equal(
            new[] { ThirdWindow, SecondWindow, FirstWindow },
            registry
        );

        OpenWindow(SecondWindow);

        Assert.Equal(
            new[] { ThirdWindow, FirstWindow },
            registry
        );

        VerifyLastClosedWindow(registry, ThirdWindow);
    }


    [Fact]
    public async Task DoesNothingWhenWindowThatHasNeverBeenClosedIsOpened() {
        ClosedWindowRegistry registry;


        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        registry = await ClosedWindowRegistry.InitializeAsync(ThreadHelper.JoinableTaskFactory);

        CloseWindow(FirstWindow);
        CloseWindow(SecondWindow);

        Assert.Equal(
            new[] { SecondWindow, FirstWindow },
            registry
        );

        OpenWindow(ThirdWindow);

        Assert.Equal(
            new[] { SecondWindow, FirstWindow },
            registry
        );

        VerifyLastClosedWindow(registry, SecondWindow);
    }


    [Fact]
    public async Task IgnoresWindowsWithFileNameEndingWithGuid() {
        ClosedWindowRegistry registry;


        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        registry = await ClosedWindowRegistry.InitializeAsync(ThreadHelper.JoinableTaskFactory);

        CloseWindow(new WindowInfo("Foo.cs;9cdf42e3-4db2-43df-aabe-1ee1a0c703dd", new Guid("a9bc9318-ab77-40e2-8822-329c2bd3a97e")));

        Assert.Empty(registry);
        Assert.False(registry.TryGetLastClosedWindow(out _));
    }


    private void OpenWindow(WindowInfo info) {
        if (_eventSink is not null) {
            _eventSink.OnFrameIsVisibleChanged(MockFrame(info), true);
        }
    }


    private void CloseWindow(WindowInfo info) {
        if (_eventSink is not null) {
            _eventSink.OnFrameDestroyed(MockFrame(info));
        }
    }


    private static IVsWindowFrame MockFrame(WindowInfo info) {
        Mock<IVsWindowFrame> frame;


        frame = new Mock<IVsWindowFrame>();

        frame
            .Setup((x) => x.GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out It.Ref<object>.IsAny))
            .Callback((int propID, out object value) => { value = info.FileName; });

        frame
            .Setup((x) => x.GetGuidProperty((int)__VSFPROPID.VSFPROPID_guidEditorType, out It.Ref<Guid>.IsAny))
            .Callback((int propID, out Guid value) => { value = info.EditorType; });

        return frame.Object;
    }


    private void VerifyLastClosedWindow(ClosedWindowRegistry registry, WindowInfo expected) {
        Assert.True(registry.TryGetLastClosedWindow(out WindowInfo info));
        Assert.Equal(expected, info);
    }

}
