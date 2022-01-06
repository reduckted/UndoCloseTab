#nullable enable

using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;


namespace UndoCloseTab;


[DebuggerDisplay("Count = {Count}")]
public class ClosedWindowRegistry : IVsWindowFrameEvents, IEnumerable<WindowInfo> {

    private readonly LinkedList<WindowInfo> _undoStack = new();
    private readonly Dictionary<WindowInfo, LinkedListNode<WindowInfo>> _nodeLookup = new();


    public static async Task<ClosedWindowRegistry> InitializeAsync(JoinableTaskFactory joinableTaskFactory) {
        ClosedWindowRegistry registry;
        IVsUIShell7 shell;


        await joinableTaskFactory.SwitchToMainThreadAsync();

        registry = new ClosedWindowRegistry();
        shell = (IVsUIShell7)await VS.Services.GetUIShellAsync();
        shell.AdviseWindowFrameEvents(registry);

        return registry;
    }


    public void OnFrameIsVisibleChanged(IVsWindowFrame frame, bool newIsVisible) {
        ThreadHelper.ThrowIfNotOnUIThread();

        // When a window is opened, there is no need to let it be re-opened,
        // so we need to remove it from the stack. Unfortunately, at the point
        // that the window is opened, the file name is unknown, so the best we
        // can do is to remove the window from the stack when it becomes visible.
        if (newIsVisible) {
            WindowInfo? info;


            info = GetWindowInfoFromFrame(frame);

            if (info.HasValue && _nodeLookup.TryGetValue(info.Value, out LinkedListNode<WindowInfo> node)) {
                node.List.Remove(node);
                _nodeLookup.Remove(info.Value);
            }
        }
    }


    public void OnFrameDestroyed(IVsWindowFrame frame) {
        WindowInfo? info;


        ThreadHelper.ThrowIfNotOnUIThread();

        info = GetWindowInfoFromFrame(frame);

        if (info.HasValue) {
            _nodeLookup[info.Value] = _undoStack.AddFirst(info.Value);
        }
    }


    private WindowInfo? GetWindowInfoFromFrame(IVsWindowFrame frame) {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (ErrorHandler.Succeeded(frame.GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out object mkDocument)) && mkDocument is not null) {
            string fileName;


            fileName = (string)mkDocument;

            // Ignore any files that have a GUID appended to the name.
            // These seem to be sub-frames, and we can't re-open a sub-frame.
            if (!EndsWithGuid(fileName)) {
                if (ErrorHandler.Succeeded(frame.GetGuidProperty((int)__VSFPROPID.VSFPROPID_guidEditorType, out Guid editor))) {
                    return new WindowInfo(fileName, editor);
                }
            }
        }

        return null;
    }


    private bool EndsWithGuid(string fileName) {
        int guidStartIndex;


        guidStartIndex = fileName.LastIndexOf(';');

        return (guidStartIndex >= 0) && Guid.TryParse(fileName.Substring(guidStartIndex + 1), out _);
    }


    public bool TryGetLastClosedWindow(out WindowInfo info) {
        if (_undoStack.Count > 0) {
            info = _undoStack.First.Value;
            return true;
        }

        info = default;
        return false;
    }


    public int Count => _nodeLookup.Count;


    public void OnFrameCreated(IVsWindowFrame frame) {
        // At the point that a frame is created, the file name of the file that
        // it's showing is unknown, so there's nothing that we can do here.
    }


    public void OnFrameIsOnScreenChanged(IVsWindowFrame frame, bool newIsOnScreen) {
        // Nothing to do here.
    }


    public void OnActiveFrameChanged(IVsWindowFrame oldFrame, IVsWindowFrame newFrame) {
        // Nothing to do here.
    }


    public IEnumerator<WindowInfo> GetEnumerator() {
        return _undoStack.GetEnumerator();
    }


    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

}
