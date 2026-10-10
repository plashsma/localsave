using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

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
// Deliberately excludes Office-only Path, ReadOnly and AutoSaveOn properties.
public class AdobeDocument {
    public string FullName {get;set;}
    public bool Saved {get;set;}
    public bool CancelSave;
    public int Calls,ErrorCode;
    public Action OnSave;
    public void Save() {Calls++;if(OnSave!=null) OnSave();if(ErrorCode!=0) throw new COMException("Adobe busy",ErrorCode);if(!CancelSave) Saved=true;}
}
public class AdobeDocuments {
    readonly AdobeDocument[] Files;
    public AdobeDocuments(params AdobeDocument[] files) {Files=files;}
    public int Count {get {return Files.Length;}}
    public object this[int i] {get {return Files[i-1];}}
}
public class FakeAdobe {public AdobeDocuments Documents {get;set;}}
public class MethodCollection {public object Item(int i) {return "method:"+i;}}
public class PropertyCollection {public object this[int i] {get {return "property:"+i;}}}
internal static class Tests {
    static int Count;
    static void Assert(bool result,string name) {if(!result) throw new Exception(name);Count++;Console.WriteLine("PASS "+name);}
    static void Reject(Action action,string name) {bool rejected=false;try {action();}catch {rejected=true;}Assert(rejected,name);}
    static TestDocument Doc(string folder) {return new TestDocument {Path=folder,FullName=System.IO.Path.Combine(folder,"test.docx")};}
    static void UpgradePreferences(string root) {
        Preferences before=new Preferences {Interval=9,WordInterval=1,ExcelInterval=2,PowerPointInterval=3,PhotoshopInterval=30,IllustratorInterval=15,AfterChanges=true,SaveHealthWarnings=false,HealthWarningSeconds=300,Word=false,Excel=true,PowerPoint=false,Photoshop=true,Illustrator=true,LimitFolder=true,Folder=root};
        Store.Save(before);string xml=File.ReadAllText(Store.SettingsPath);Preferences after=Store.Load();
        Assert(after.Interval==9 && after.WordInterval==1 && after.ExcelInterval==2 && after.PowerPointInterval==3 && after.PhotoshopInterval==30 && after.IllustratorInterval==15,"update reloads all app intervals from shared AppData settings");
        Assert(after.AfterChanges && !after.SaveHealthWarnings && after.HealthWarningSeconds==300 && !after.Word && after.Excel && !after.PowerPoint && after.Photoshop && after.Illustrator && after.LimitFolder && after.Folder==root,"update reloads mode, apps, warning options and folder scope");
        string newExe=System.IO.Path.Combine(root,"new-version","LocalSave.exe"),updated=null;int writes=0;
        Action<bool,string> writer=delegate(bool enabled,string exe) {Assert(enabled,"automatic startup migration preserves enabled choice");updated=exe;writes++;};
        Assert(Store.RefreshStartup(newExe,delegate {return true;},delegate(string exe) {return false;},writer) && updated==newExe && writes==1,"first launch redirects enabled startup to the new EXE");
        Assert(!Store.RefreshStartup(newExe,delegate {return true;},delegate(string exe) {return true;},writer) && writes==1,"subsequent launches leave the current startup entry untouched");
        Assert(!Store.RefreshStartup(newExe,delegate {return false;},delegate(string exe) {throw new Exception("Should not inspect disabled startup");},writer) && writes==1,"updates never enable startup for an opted-out user");
        Assert(File.ReadAllText(Store.SettingsPath)==xml,"startup migration never rewrites or resets saved preferences");
        Reject(delegate {Store.RefreshStartup(newExe,delegate {return true;},delegate(string exe) {return false;},delegate(bool enabled,string exe) {throw new UnauthorizedAccessException();});},"startup update failure is reported to the caller");
        Assert(Store.Load().Folder==root && Store.Load().Illustrator && Store.Load().HealthWarningSeconds==300,"preferences remain usable when startup update fails");
    }
    static void HealthWarnings(string root,string psd) {
        Preferences p=new Preferences {Word=false,Excel=true,PowerPoint=false,AfterChanges=true,HealthWarningSeconds=15};
        DateTime now=new DateTime(2026,10,11,0,0,0,DateTimeKind.Utc);
        ProgressState state=new ProgressState();SaveHealth health=new SaveHealth();
        Assert(health.Evaluate(state,p,false,now).Notify.Count==0,"idle apps never warn just because no save occurred");
        state.PendingSince[1]=now;
        Assert(!health.Evaluate(state,p,false,now.AddSeconds(14)).Warnings[1],"unsaved warning waits for the configured threshold");
        HealthReport report=health.Evaluate(state,p,false,now.AddSeconds(15));
        Assert(report.Warnings[1] && report.Notify.Count==1 && report.Notify[0]=="Excel","confirmed pending changes trigger an app warning");
        Assert(health.Evaluate(state,p,false,now.AddHours(1)).Notify.Count==0,"one notice per unresolved episode without repeat spam");
        Assert(!health.Evaluate(state,p,true,now.AddHours(1)).Warnings[1],"pause suppresses pending warnings");
        p.Excel=false;Assert(!health.Evaluate(state,p,false,now.AddHours(1)).Warnings[1],"disabled apps do not warn");p.Excel=true;
        p.SaveHealthWarnings=false;Assert(!health.Evaluate(state,p,false,now.AddHours(1)).Warnings[1],"warning preference suppresses status and notifications");p.SaveHealthWarnings=true;
        state.PendingSince[1]=null;Assert(!health.Evaluate(state,p,false,now.AddSeconds(16)).Warnings[1],"confirmed recovery clears the warning");
        state.PendingSince[1]=now.AddSeconds(17);report=health.Evaluate(state,p,false,now.AddSeconds(32));
        Assert(report.Warnings[1] && report.Notify.Count==0,"new episode remains visible during notification cooldown");
        Assert(health.Evaluate(state,p,false,now.AddSeconds(75)).Notify.Count==1,"new episode can notify after the global cooldown");
        p.AfterChanges=false;p.ExcelInterval=300;Assert(SaveHealth.Threshold(p,1)==330,"long intervals receive a 30-second warning grace period");p.AfterChanges=true;
        p.HealthWarningSeconds=14;Reject(p.Validate,"too-short warning delay rejected");p.HealthWarningSeconds=3601;Reject(p.Validate,"oversized warning delay rejected");p.HealthWarningSeconds=15;
        Store.Save(p);Assert(Store.Load().HealthWarningSeconds==15 && Store.Load().SaveHealthWarnings,"warning options persist");
        File.WriteAllText(Store.SettingsPath,"<Preferences><Interval>30</Interval></Preferences>");Assert(Store.Load().SaveHealthWarnings && Store.Load().HealthWarningSeconds==120,"old settings get safe two-minute warning defaults");
        TestDocument failed=Doc(root),healthy=Doc(root);healthy.FullName=System.IO.Path.Combine(root,"healthy.docx");failed.ErrorCode=unchecked((int)0x80010001);
        FakeExcel excel=new FakeExcel {Workbooks=new FakeWorkbooks(failed,healthy)};ProgressState latest=null;
        SaveWorker worker=new SaveWorker(p,delegate(int index) {return new List<object>{excel};});worker.Changed+=delegate(ProgressState value) {latest=value;};
        worker.Scan(p,true);DateTime? since=latest.PendingSince[1];
        Assert(since.HasValue && latest.Saves==1,"healthy workbook save does not clear a different unsaved workbook");
        healthy.Saved=false;worker.Scan(p,true);Assert(latest.PendingSince[1]==since,"retrying and other saves preserve the original pending age");
        ProgressState copy=latest.Copy();copy.PendingSince[1]=null;Assert(latest.PendingSince[1].HasValue,"progress snapshots isolate pending timestamps");
        failed.ErrorCode=0;failed.Saved=true;worker.Scan(p,true);Assert(!latest.PendingSince[1].HasValue,"manual save clears pending health on the next scan");
        failed.Saved=false;failed.CancelSave=true;worker.Scan(p,true);Assert(latest.PendingSince[1].HasValue,"unconfirmed save remains pending");
        excel.Workbooks=new FakeWorkbooks(healthy);worker.Scan(p,true);Assert(!latest.PendingSince[1].HasValue,"closed documents are pruned after a complete scan");
        failed.ReadOnly=true;excel.Workbooks=new FakeWorkbooks(failed);worker.Scan(p,true);Assert(!latest.PendingSince[1].HasValue,"excluded read-only files do not trigger save-health warnings");
        using(ManualResetEvent entered=new ManualResetEvent(false)) using(ManualResetEvent release=new ManualResetEvent(false)) {
            p.Excel=false;p.Photoshop=true;
            AdobeDocument photo=new AdobeDocument {FullName=psd,OnSave=delegate {entered.Set();release.WaitOne();}};
            SaveCoordinator coordinator=new SaveCoordinator(p,delegate(int index) {return new List<object>{new FakeAdobe {Documents=new AdobeDocuments(photo)}};});
            coordinator.Changed+=delegate(ProgressState value) {latest=value;};coordinator.Start();
            try {
                Assert(entered.WaitOne(3000),"test app blocks inside the native Save call");
                Assert(latest.PendingSince[3].HasValue && new SaveHealth().Evaluate(latest,p,false,DateTime.UtcNow.AddSeconds(16)).Warnings[3],"watchdog can warn while native Save is still blocked");
            } finally {coordinator.Stop();release.Set();for(int i=0;i<300 && !coordinator.IsStopped;i++) Thread.Sleep(10);}
            Assert(coordinator.IsStopped && !latest.PendingSince[3].HasValue,"successful blocked save clears health and shuts down cleanly");
        }
        Assert(!File.ReadAllText(Store.LogPath).Contains(failed.FullName),"health tracking never logs file paths");
    }
    static void IndependentWorkers(string root,string psd,string ai) {
        Preferences p=new Preferences {Word=false,Excel=true,PowerPoint=false,Photoshop=true,Illustrator=true,Interval=3600,ExcelInterval=1};
        using(ManualResetEvent blocked=new ManualResetEvent(false)) using(ManualResetEvent release=new ManualResetEvent(false)) using(ManualResetEvent saved=new ManualResetEvent(false)) {
            int[] ids=new int[5];ApartmentState[] apartments=new ApartmentState[5];int calls=0;ProgressState latest=null;
            TestDocument workbook=Doc(root);AdobeDocument photo=new AdobeDocument {FullName=psd},art=new AdobeDocument {FullName=ai};
            SaveCoordinator coordinator=new SaveCoordinator(p,delegate(int index) {
                Interlocked.Increment(ref calls);ids[index]=Thread.CurrentThread.ManagedThreadId;apartments[index]=Thread.CurrentThread.GetApartmentState();
                if(index==3) {blocked.Set();release.WaitOne();}
                if(index==1) return new List<object>{new FakeExcel {Workbooks=new FakeWorkbooks(workbook)}};
                return new List<object>{new FakeAdobe {Documents=new AdobeDocuments(index==3?photo:art)}};
            });
            coordinator.Changed+=delegate(ProgressState state) {latest=state;if(state.Saves>=2) saved.Set();};
            coordinator.Pause(true);coordinator.Start();
            try {
                Thread.Sleep(300);Assert(Interlocked.CompareExchange(ref calls,0,0)==0,"pause prevents automatic discovery in every worker");
                coordinator.SaveNow();Assert(blocked.WaitOne(3000),"Photoshop worker can be held in a slow call");
                Assert(saved.WaitOne(3000),"manual Save now saves Excel and Illustrator while Photoshop is blocked and saving is paused");
                Assert(latest.Saves==2 && latest.Busy && latest.LastSave.HasValue,"coordinator combines independent counts, busy state and last save");
                Assert(ids[1]!=ids[3] && ids[1]!=ids[4] && ids[3]!=ids[4],"apps use distinct threads");
                Assert(apartments[1]==ApartmentState.STA && apartments[3]==ApartmentState.STA && apartments[4]==ApartmentState.STA,"COM discovery stays on app-owned STA threads");
                coordinator.Stop();Assert(!coordinator.IsStopped,"shutdown waits for a blocked app instead of claiming it stopped");
            } finally {coordinator.Stop();release.Set();for(int i=0;i<300 && !coordinator.IsStopped;i++) Thread.Sleep(10);}
            Assert(coordinator.IsStopped,"all workers stop after the blocked call returns");
        }
        using(ManualResetEvent secondScan=new ManualResetEvent(false)) {
            int excelScans=0,photoScans=0;
            SaveCoordinator coordinator=new SaveCoordinator(p,delegate(int index) {
                if(index==1 && Interlocked.Increment(ref excelScans)>=2) secondScan.Set();
                if(index==3) Interlocked.Increment(ref photoScans);
                return new List<object>();
            });
            coordinator.Start();try {Assert(secondScan.WaitOne(3000),"Excel follows its one-second override despite a one-hour default");Assert(Interlocked.CompareExchange(ref photoScans,0,0)==1,"Photoshop retains its separate default schedule");}
            finally {coordinator.Stop();for(int i=0;i<300 && !coordinator.IsStopped;i++) Thread.Sleep(10);}
            Assert(coordinator.IsStopped,"scheduled workers shut down cleanly");
        }
    }
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
        p=new Preferences {Word=false,Excel=false,PowerPoint=false,Photoshop=true,Illustrator=true};
        string psd=System.IO.Path.Combine(root,"art.psd"),ai=System.IO.Path.Combine(root,"art.ai");
        File.WriteAllText(psd,"test fixture");File.WriteAllText(ai,"test fixture");
        AdobeDocument photo=new AdobeDocument {FullName=psd},illustration=new AdobeDocument {FullName=ai};
        Assert(SaveWorker.SaveDocument(photo,"Photoshop",p)==1 && photo.Calls==1,"Photoshop uses native save without Office properties");
        Assert(SaveWorker.SaveDocument(photo,"Photoshop",p)==0 && photo.Calls==1,"unchanged Photoshop project not saved again");
        Assert(SaveWorker.SaveDocument(illustration,"Illustrator",p)==1 && illustration.Calls==1,"Illustrator AI native save confirmed");
        string psb=System.IO.Path.Combine(root,"large.psb");File.WriteAllText(psb,"test fixture");
        Assert(SaveWorker.SaveDocument(new AdobeDocument {FullName=psb},"Photoshop",p)==1,"Photoshop PSB supported");
        photo=new AdobeDocument {FullName=""};Assert(SaveWorker.SaveDocument(photo,"Photoshop",p)==-1 && photo.Calls==0,"untitled Adobe document skipped");
        photo=new AdobeDocument {FullName="cloud:project.psdc"};Assert(SaveWorker.SaveDocument(photo,"Photoshop",p)==-1 && photo.Calls==0,"Adobe cloud document excluded");
        photo=new AdobeDocument {FullName=System.IO.Path.Combine(root,"export.jpg")};Assert(SaveWorker.SaveDocument(photo,"Photoshop",p)==-1 && photo.Calls==0,"Photoshop export format skipped");
        photo=new AdobeDocument {FullName=psd};Assert(SaveWorker.SaveDocument(photo,"Illustrator",p)==-1,"Illustrator non-AI format skipped");
        photo=new AdobeDocument {FullName=System.IO.Path.Combine(root,"missing.psd")};Assert(SaveWorker.SaveDocument(photo,"Photoshop",p)==-1 && photo.Calls==0,"missing Adobe file never invokes Save As");
        photo=new AdobeDocument {FullName=psd};File.SetAttributes(psd,FileAttributes.ReadOnly);
        try {Assert(SaveWorker.SaveDocument(photo,"Photoshop",p)==-1 && photo.Calls==0,"read-only Adobe file skipped");} finally {File.SetAttributes(psd,FileAttributes.Normal);}
        p.LimitFolder=true;p.Folder=System.IO.Path.Combine(root,"permitted");photo=new AdobeDocument {FullName=psd};
        Assert(SaveWorker.SaveDocument(photo,"Photoshop",p)==-1 && photo.Calls==0,"Adobe folder restriction enforced");p.LimitFolder=false;
        photo=new AdobeDocument {FullName=psd,CancelSave=true};Reject(delegate {SaveWorker.SaveDocument(photo,"Photoshop",p);},"unconfirmed Adobe save never counted");
        Store.Save(p);Assert(Store.Load().Photoshop && Store.Load().Illustrator,"Adobe selections persist");
        File.WriteAllText(Store.SettingsPath,"<Preferences><Interval>30</Interval><Word>true</Word><Excel>true</Excel><PowerPoint>true</PowerPoint></Preferences>");
        Assert(!Store.Load().Photoshop && !Store.Load().Illustrator,"old settings keep Adobe saving opt-in");
        photo=new AdobeDocument {FullName=psd,ErrorCode=unchecked((int)0x80010001)};illustration=new AdobeDocument {FullName=ai};
        worker=new SaveWorker(p,delegate(int index) {return new List<object>{new FakeAdobe {Documents=new AdobeDocuments(index==3?photo:illustration)}};});
        worker.Changed+=delegate(ProgressState state) {latest=state;};worker.Scan(p,true);
        Assert(latest.Saves==1 && latest.Apps.Length==5 && latest.Apps[3].Contains("Retry"),"busy Photoshop does not block Illustrator");
        photo.ErrorCode=0;worker.Scan(p,true);Assert(latest.Saves==2 && latest.Apps[3].Contains("eligible"),"Photoshop resumes after busy failure");
        Assert(!File.ReadAllText(Store.LogPath).Contains(psd) && !File.ReadAllText(Store.LogPath).Contains(ai),"Adobe logs exclude artwork paths");
        int discoveryCalls=0;worker=new SaveWorker(new Preferences {Word=false,Excel=false,PowerPoint=false},delegate(int index) {discoveryCalls++;return new List<object>();});worker.Scan(new Preferences {Word=false,Excel=false,PowerPoint=false},true);
        Assert(discoveryCalls==0,"disabled apps never contacted");
        p=new Preferences {Interval=30,WordInterval=1,ExcelInterval=2,PowerPointInterval=5,PhotoshopInterval=60,IllustratorInterval=15};
        p.Validate();Store.Save(p);loaded=Store.Load();
        Assert(loaded.WordInterval==1 && loaded.ExcelInterval==2 && loaded.PowerPointInterval==5 && loaded.PhotoshopInterval==60 && loaded.IllustratorInterval==15,"all per-app intervals persist");
        Assert(SaveWorker.Delay(p,0)==1000 && SaveWorker.Delay(p,3)==60000,"app-specific schedules use their own intervals");
        p.WordInterval=0;Assert(p.AppInterval(0)==30,"zero app interval inherits the shared default");
        p.AfterChanges=true;Assert(SaveWorker.Delay(p,3)==250,"after-changes mode overrides app intervals consistently");
        p.ExcelInterval=-1;Reject(p.Validate,"negative app interval rejected");p.ExcelInterval=3601;Reject(p.Validate,"oversized app interval rejected");
        File.WriteAllText(Store.SettingsPath,"<Preferences><Interval>7</Interval></Preferences>");loaded=Store.Load();
        Assert(loaded.AppInterval(0)==7 && loaded.AppInterval(4)==7,"old settings inherit the original shared interval");
        IndependentWorkers(root,psd,ai);
        HealthWarnings(root,psd);
        UpgradePreferences(root);
        Console.WriteLine("All "+Count+" tests passed.");return 0;
    }
}
