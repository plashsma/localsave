using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class TestDocument {
    public string Path {get;set;}
    public string FullName {get;set;}
    public bool ReadOnly {get;set;}
    public bool Saved {get;set;}
    public bool IsAddin {get;set;}
    public bool AutoSaveOn {get;set;}
    public int Calls;
    public bool CancelSave;
    public int ErrorCode;
    public void Save() {Calls++;if(ErrorCode!=0) throw new COMException("Test Office failure",ErrorCode);if(!CancelSave) Saved=true;}
}
public class FakeWorkbooks {
    readonly TestDocument[] Documents;
    public FakeWorkbooks(params TestDocument[] docs) {Documents=docs;}
    public int Count {get {return Documents.Length;}}
    public object this[int i] {get {return Documents[i-1];}}
}
public class FakeExcel {public FakeWorkbooks Workbooks {get;set;}}
public class MethodCollection {public object Item(int i) {return "method:"+i;}}
public class PropertyCollection {public object this[int i] {get {return "property:"+i;}}}
internal static class Tests {
    static int Count;
    static void Assert(bool result,string name) {if(!result) throw new Exception(name);Count++;Console.WriteLine("PASS "+name);}
    static void Reject(Action action,string name) {bool rejected=false;try {action();}catch {rejected=true;}Assert(rejected,name);}
    static TestDocument Doc(string folder) {return new TestDocument {Path=folder,FullName=System.IO.Path.Combine(folder,"test.docx")};}
    static int Main(string[] args) {
        string root=System.IO.Path.GetFullPath(args[0]);Directory.CreateDirectory(root);Store.DirectoryPath=System.IO.Path.Combine(root,"settings");
        Preferences p=new Preferences {Interval=1};
        Assert(SaveWorker.Delay(p)==1000,"one-second mode");p.AfterChanges=true;Assert(SaveWorker.Delay(p)==250,"changes mode timing");
        Assert(!Policy.LocalPath(@"\\server\share\file.docx"),"network path excluded");
        Assert(!Policy.LocalPath("https://example.com/file.docx"),"web path excluded");
        Assert(!Policy.LocalPath(@"C:\test.docx:hidden"),"alternate stream excluded");
        Assert(Policy.LocalPath(root),"local folder accepted");
        Assert(Policy.Within(System.IO.Path.Combine(root,"file.docx"),root),"folder child accepted");
        Assert(!Policy.Within(root+"-other\\file.docx",root),"sibling prefix excluded");
        Assert(!Policy.Within(System.IO.Path.Combine(root,"..","file.docx"),root),"traversal excluded");
        Assert((string)SaveWorker.Item(new MethodCollection(),1)=="method:1","Word-style Item method");
        Assert((string)SaveWorker.Item(new PropertyCollection(),1)=="property:1","Excel-style Item property");
        TestDocument doc=Doc(root);Assert(SaveWorker.SaveDocument(doc,"Word",p)==1 && doc.Calls==1 && doc.Saved,"dirty document saved");
        Assert(SaveWorker.SaveDocument(doc,"Word",p)==0 && doc.Calls==1,"unchanged document not saved again");
        doc=Doc(root);doc.ReadOnly=true;Assert(SaveWorker.SaveDocument(doc,"Word",p)==-1 && doc.Calls==0,"read-only document skipped");
        doc=Doc(root);doc.AutoSaveOn=true;Assert(SaveWorker.SaveDocument(doc,"Word",p)==-1 && doc.Calls==0,"native AutoSave respected");
        doc=Doc(root);doc.IsAddin=true;Assert(SaveWorker.SaveDocument(doc,"Excel",p)==-1 && doc.Calls==0,"Excel add-in skipped");
        doc=Doc(root);doc.Path="";Assert(SaveWorker.SaveDocument(doc,"Word",p)==-1 && doc.Calls==0,"untitled document skipped");
        p.LimitFolder=true;p.Folder=System.IO.Path.Combine(root,"permitted");Directory.CreateDirectory(p.Folder);
        doc=Doc(root);Assert(SaveWorker.SaveDocument(doc,"Word",p)==-1 && doc.Calls==0,"folder restriction enforced");
        doc=Doc(p.Folder);Assert(SaveWorker.SaveDocument(doc,"PowerPoint",p)==1 && doc.Calls==1,"allowed PowerPoint file saved");
        doc=Doc(p.Folder);doc.CancelSave=true;Reject(delegate {SaveWorker.SaveDocument(doc,"Word",p);},"cancelled save never reported successful");
        Store.Save(p);Preferences loaded=Store.Load();Assert(loaded.Interval==1 && loaded.AfterChanges && loaded.LimitFolder && loaded.Folder==p.Folder,"preferences round-trip");
        p.Interval=8;Store.Save(p);Assert(Store.Load().Interval==8,"atomic settings replacement");
        File.WriteAllText(Store.SettingsPath,"<!DOCTYPE Preferences [<!ENTITY xxe SYSTEM 'file:///C:/test'>]><Preferences><Folder>&xxe;</Folder></Preferences>");
        Reject(delegate {Store.Load();},"external XML entities prohibited");
        p.Interval=0;Reject(delegate {p.Validate();},"invalid interval rejected");
        p.Interval=1;Store.Save(p);Assert(Store.Log("Word: saved 1 file(s)."),"private activity log written");
        Assert(!File.ReadAllText(Store.LogPath).Contains(root),"activity log has no document path");
        File.WriteAllText(Store.LogPath,new string('x',1048577));Assert(Store.Log("Rotation test") && File.Exists(Store.LogPath+".previous") && new FileInfo(Store.LogPath).Length<1024,"bounded log rotation");
        string link=System.IO.Path.Combine(root,"test-junction");
        if(Directory.Exists(link)) {Assert(Policy.HasLink(link),"junction detected");Assert(!Policy.Allows(System.IO.Path.Combine(link,"file.docx"),new Preferences()),"junction document excluded");}
        else Console.WriteLine("SKIP junction integration checks: no test junction available.");
        p=new Preferences {Word=false,Excel=true,PowerPoint=false,Interval=1};
        doc=Doc(root);Assert(SaveWorker.SaveDocument(doc,"Excel",p)==1 && doc.Calls==1,"dirty Excel workbook saved");
        string reason;doc=Doc(root);doc.Path="";SaveWorker.SaveDocument(doc,"Excel",p,out reason);Assert(reason=="Save new file once first","unsaved workbook explains required first save");
        doc=Doc(root);doc.AutoSaveOn=true;SaveWorker.SaveDocument(doc,"Excel",p,out reason);Assert(reason=="Office AutoSave already on","native AutoSave skip explained");
        Assert(SaveWorker.IsExcelBusy(new COMException("busy",unchecked((int)0x800AC472))),"Excel edit-mode HRESULT recognized");
        TestDocument failing=Doc(root);failing.ErrorCode=unchecked((int)0x800A03EC);TestDocument healthy=Doc(root);
        FakeExcel excel=new FakeExcel {Workbooks=new FakeWorkbooks(failing,healthy)};
        SaveWorker worker=new SaveWorker(p,delegate(int index) {return new List<object>{excel};});
        ProgressState latest=null;worker.Changed+=delegate(ProgressState state) {latest=state;};
        worker.Scan(p,true);healthy.Saved=false;worker.Scan(p,true);healthy.Saved=false;worker.Scan(p,true);
        Assert(healthy.Calls==3,"one failed workbook does not block other workbook saves");
        Assert(latest.Saves==3,"only confirmed workbook saves counted");
        Assert(latest.Apps[1].StartsWith("Retry in 4s"),"persistent file failures back off instead of resetting every scan");
        Assert(File.ReadAllText(Store.LogPath).Contains("save file deferred/failed 0x800A03EC"),"failure log identifies operation and HRESULT");
        failing.ErrorCode=unchecked((int)0x800AC472);worker.Scan(p,true);Assert(latest.Apps[1].Contains("Press Enter"),"Excel edit-mode status explains how to resume");
        failing.ErrorCode=0;worker.Scan(p,true);Assert(latest.Apps[1].Contains("eligible"),"Excel resumes saving after edit mode ends");
        FakeExcel other=new FakeExcel {Workbooks=new FakeWorkbooks(Doc(root))};healthy.Saved=false;
        worker=new SaveWorker(p,delegate(int index) {return new List<object>{excel,other};});worker.Changed+=delegate(ProgressState state) {latest=state;};worker.Scan(p,true);
        Assert(latest.Saves==2,"multiple discovered Excel instances are scanned");
        Console.WriteLine("All "+Count+" tests passed.");return 0;
    }
}
