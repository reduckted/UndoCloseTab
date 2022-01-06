#nullable enable

using System;

namespace UndoCloseTab;


public struct WindowInfo : IEquatable<WindowInfo> {

    public WindowInfo(string fileName, Guid editorType) {
        FileName = fileName;
        EditorType = editorType;
    }


    public string FileName { get; }


    public Guid EditorType { get; }


    public bool Equals(WindowInfo other) {
        return string.Equals(FileName, other.FileName) && EditorType.Equals(other.EditorType);
    }


    public override bool Equals(object obj) {
        return (obj is WindowInfo other) && Equals(other);
    }


    public override int GetHashCode() {
        return (FileName, EditorType).GetHashCode();
    }


    public override string ToString() {
        if ((FileName is null) && (EditorType == default)) {
            return "{empty}";
        }

        return $"{FileName} ({EditorType})";
    }

}
