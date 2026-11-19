using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

// Keep COM discovery on the app's existing STA worker.
internal sealed class AppConnection : IDisposable {
    static readonly string[] ProcessNames = {"WINWORD", "EXCEL", "POWERPNT", "Photoshop", "Illustrator"};
    static readonly string[] OfficeFrames = {"OpusApp", "XLMAIN", "PPTFrameClass"};
    static readonly string[] OfficeEditors = {"_WwG", "EXCEL7", "paneClassDC"};
    static readonly Guid DispatchId = new Guid("00020400-0000-0000-C000-000000000046");
    const uint NativeObjectModel = 0xfffffff0;
    const int ObjectUnavailable = unchecked((int)0x800401E3);
    const int ClassNotRegistered = unchecked((int)0x80040154);

    internal readonly List<object> Applications = new List<object>();
    readonly HashSet<IntPtr> identities = new HashSet<IntPtr>();
    internal bool Running;
    internal bool EditorFound;
    internal bool Partial;
    internal bool PresenceUnknown;
    internal int ErrorCode;

    delegate bool EnumProc(IntPtr hwnd, IntPtr data);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc callback, IntPtr data);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent, EnumProc callback, IntPtr data);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr hwnd, StringBuilder name, int count);
    [DllImport("user32.dll")] static extern bool IsHungAppWindow(IntPtr hwnd);
    [DllImport("oleacc.dll")] static extern int AccessibleObjectFromWindow(IntPtr hwnd, uint id, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out object result);

    internal static bool IsRunningInSession(int index) {
        int session;
        using (Process current = Process.GetCurrentProcess()) session = current.SessionId;
        Process[] processes = Process.GetProcessesByName(ProcessNames[index]);
        try {
            foreach (Process process in processes) {
                try { if (!process.HasExited && process.SessionId == session) return true; }
                catch (InvalidOperationException) {} // Process exited during enumeration.
            }
            return false;
        } finally {
            foreach (Process process in processes) process.Dispose();
        }
    }

    static bool HasClass(IntPtr window, string expected, StringBuilder buffer) {
        return GetClassName(window, buffer, buffer.Capacity) != 0 &&
            String.Equals(buffer.ToString(), expected, StringComparison.OrdinalIgnoreCase);
    }

    void AddApplication(object application) {
        if (application == null) return;
        bool retained = false;
        try {
            IntPtr identity = Marshal.GetIUnknownForObject(application);
            try {
                if (identities.Add(identity)) {
                    Applications.Add(application);
                    retained = true;
                }
            } finally { Marshal.Release(identity); }
        } finally {
            if (!retained) Release(application);
        }
    }

    internal static AppConnection Open(int index, string name) {
        AppConnection connection = new AppConnection();
        try {
            try { connection.AddApplication(Marshal.GetActiveObject(name + ".Application")); }
            catch (COMException ex) {
                if (ex.ErrorCode != ObjectUnavailable && ex.ErrorCode != ClassNotRegistered)
                    connection.ErrorCode = ex.ErrorCode;
            }
            if (index < OfficeFrames.Length) connection.FindOfficeWindows(index);
            return connection;
        } catch {
            connection.Dispose();
            throw;
        }
    }

    void FindOfficeWindows(int index) {
        StringBuilder buffer = new StringBuilder(128);
        List<IntPtr> roots = new List<IntPtr>();
        if (!EnumWindows(delegate(IntPtr root, IntPtr data) {
            if (HasClass(root, OfficeFrames[index], buffer)) roots.Add(root);
            return true;
        }, IntPtr.Zero)) {
            PresenceUnknown = true;
            Partial = true;
        }

        Running = roots.Count > 0;
        foreach (IntPtr root in roots) {
            List<IntPtr> editors = new List<IntPtr>();
            EnumChildWindows(root, delegate(IntPtr child, IntPtr data) {
                if (HasClass(child, OfficeEditors[index], buffer)) editors.Add(child);
                return true;
            }, IntPtr.Zero);
            if (editors.Count == 0) continue;

            EditorFound = true;
            bool connected = false;
            if (!IsHungAppWindow(root)) {
                foreach (IntPtr editor in editors) {
                    if (ConnectToEditor(editor)) { connected = true; break; }
                }
            }
            if (!connected) Partial = true;
        }
    }

    bool ConnectToEditor(IntPtr editor) {
        object window = null;
        try {
            Guid iid = DispatchId;
            int hr = AccessibleObjectFromWindow(editor, NativeObjectModel, ref iid, out window);
            if (hr < 0) { ErrorCode = hr; return false; }
            if (window == null) return false;
            object application = SaveWorker.Get(window, "Application");
            if (application == null) return false;
            AddApplication(application);
            return true;
        } catch (Exception ex) {
            ErrorCode = ex.GetBaseException().HResult;
            return false;
        } finally { Release(window); }
    }

    static void Release(object value) {
        if (value != null && Marshal.IsComObject(value)) Marshal.ReleaseComObject(value);
    }

    public void Dispose() {
        foreach (object application in Applications) Release(application);
        Applications.Clear();
        identities.Clear();
    }
}
