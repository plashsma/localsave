using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

// Excel can expose several desktop instances and may not yet be in the ROT.
// OBJID_NATIVEOM is the documented Office accessibility bridge, not a keyboard hook.
internal sealed class ExcelConnection : IDisposable {
    internal readonly List<object> Applications=new List<object>();
    readonly HashSet<long> Identities=new HashSet<long>();
    internal int WindowsFound;
    internal int ErrorCode;
    delegate bool EnumProc(IntPtr hwnd,IntPtr data);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc callback,IntPtr data);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent,EnumProc callback,IntPtr data);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr hwnd,StringBuilder name,int count);
    [DllImport("oleacc.dll")] static extern int AccessibleObjectFromWindow(IntPtr hwnd,uint id,ref Guid iid,[MarshalAs(UnmanagedType.Interface)] out object result);
    static string ClassName(IntPtr hwnd) {StringBuilder name=new StringBuilder(128);GetClassName(hwnd,name,name.Capacity);return name.ToString();}
    void Add(object app) {
        IntPtr identity=Marshal.GetIUnknownForObject(app);
        try {if(Identities.Add(identity.ToInt64())) Applications.Add(app);else Marshal.ReleaseComObject(app);}
        finally {Marshal.Release(identity);}
    }
    internal static ExcelConnection Open() {
        ExcelConnection result=new ExcelConnection();
        try {
        try {result.Add(Marshal.GetActiveObject("Excel.Application"));}
        catch(COMException ex) {if(ex.ErrorCode!=unchecked((int)0x800401E3)) result.ErrorCode=ex.ErrorCode;}
        List<IntPtr> windows=new List<IntPtr>();
        EnumWindows(delegate(IntPtr root,IntPtr ignored) {
            if(ClassName(root)=="XLMAIN") {
                result.WindowsFound++;
                EnumChildWindows(root,delegate(IntPtr child,IntPtr data) {if(ClassName(child)=="EXCEL7") windows.Add(child);return true;},IntPtr.Zero);
            }
            return true;
        },IntPtr.Zero);
        foreach(IntPtr window in windows) {
            object native=null;
            try {
                Guid iid=new Guid("00020400-0000-0000-C000-000000000046");
                int hr=AccessibleObjectFromWindow(window,0xfffffff0,ref iid,out native);
                if(hr<0) {result.ErrorCode=hr;continue;}
                if(native!=null) result.Add(SaveWorker.Get(native,"Application"));
            } catch(Exception ex) {result.ErrorCode=ex.GetBaseException().HResult;}
            finally {if(native!=null && Marshal.IsComObject(native)) Marshal.ReleaseComObject(native);}
        }
        return result;
        } catch {result.Dispose();throw;}
    }
    public void Dispose() {foreach(object app in Applications) if(Marshal.IsComObject(app)) Marshal.ReleaseComObject(app);Applications.Clear();}
}
