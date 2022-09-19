#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Project:     XbimXplorer
// Published:   01, 2012

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using log4net;
using log4net.Config;
using log4net.Repository.Hierarchy;
using Microsoft.Win32;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.IO.Esent;
using Xbim.ModelGeometry.Scene;
using Xbim.Presentation;
using Xbim.Presentation.FederatedModel;
using Xbim.Presentation.ModelGeomInfo;
using Xbim.Presentation.XplorerPluginSystem;
using XbimXplorer.Dialogs;
using XbimXplorer.Dialogs.ExcludedTypes;
using XbimXplorer.LogViewer;
using XbimXplorer.Properties;
using System.Windows.Forms.Integration;
using THBimEngine.Presention;
using XbimXplorer.ThBIMEngine;
using System.IO.Pipes;
using ProtoBuf;
using System.Windows.Media;
using XbimXplorer.LeftTabItme;
using System.Threading;
using Xbim.Common.Geometry;
#endregion

namespace XbimXplorer
{
    /// <summary>
    /// Interaction logic for XplorerMainWindow
    /// </summary>
    public partial class XplorerMainWindow : IXbimXplorerPluginMasterWindow, INotifyPropertyChanged
    {
        private BackgroundWorker _loadFileBackgroundWorker;
        private DispatcherTimer _dispatcherTimer;
        private int _selectIndex=-1;
        private Dictionary<int, int> _geoIndexIfcIndexMap;
        /// <summary>
        /// Used for the creation of a new federation file
        /// </summary>
        public static RoutedCommand CreateFederationCmd = new RoutedCommand();
        /// <summary>
        /// Edit the current federation environment
        /// </summary>
        public static RoutedCommand EditFederationCmd = new RoutedCommand();
        /// <summary>
        /// Currently supoorts the export function for Wexbim
        /// </summary>
        public static RoutedCommand OpenExportWindowCmd = new RoutedCommand();

        private string _temporaryXbimFileName;

        private string _openedModelFileName;
		private string _tempMidFileName;
        private ThBimDataController bimDataController = new ThBimDataController();
        private XbimMatrix3D projectMatrix3D = XbimMatrix3D.CreateTranslation(XbimVector3D.Zero);

        /// <summary>
        /// Deals with the user-defined model file name.
        /// The underlying XbimModel might be pointing to a temporary file elsewhere.
        /// </summary>
        /// <returns>String pointing to the file or null if the file is not defined (e.g. not saved federation).</returns>
        public string GetOpenedModelFileName()
        {
            return _openedModelFileName;
        }
        private ThTCHProjectData thProject=null;
        private ThSUProjectData suProject = null;
        NamedPipeServerStream pipeServer = null;
        NamedPipeServerStream SU_pipeServer = null;
        BackgroundWorker backgroundWorker = null;
        BackgroundWorker SU_backgroundWorker = null;
        private void SetOpenedModelFileName(string ifcFilename)
        {
            _openedModelFileName = ifcFilename;
            // try to update the window title through a delegate for multithreading
            Dispatcher.BeginInvoke(new Action(delegate
            {
                Title = string.IsNullOrEmpty(ifcFilename)
                    ? "TianHua Bim Xplorer" :
                    "TianHua Bim Xplorer - [" + ifcFilename + "]";
            }));
        }

        public XplorerMainWindow(bool preventPluginLoad = false)
        {

            InitializeComponent();
            _tempMidFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".midfile");
            PreventPluginLoad = preventPluginLoad;

            // initialise the internal elements of the UI that behave like plugins
            EvaluateXbimUiType(typeof(IfcValidation.ValidationWindow), true);
            EvaluateXbimUiType(typeof(LogViewer.LogViewer), true);
            EvaluateXbimUiType(typeof(Commands.wdwCommands), true);
            
            
            // attach window managment functions
            Closed += XplorerMainWindow_Closed;
            Loaded += XplorerMainWindow_Loaded;
            Closing += XplorerMainWindow_Closing;

            // notify the user of changes in the measures taken in the 3d viewer.
            //--DrawingControl.UserModeledDimensionChangedEvent += DrawingControl_MeasureChangedEvent;

            // Get the settings
            InitFromSettings();
            RefreshRecentFiles();

            // initialise the logging repository
            LoggedEvents = new ObservableCollection<EventViewModel>();
            // any logging event required should happen after XplorerMainWindow_Loaded

            _geoIndexIfcIndexMap = new Dictionary<int, int>();
            _dispatcherTimer = new DispatcherTimer();
            _dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            _dispatcherTimer.Tick += DispatcherTimer_Tick;

            pipeServer = new NamedPipeServerStream("THCAD2P3DPIPE", PipeDirection.In);
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += Background_DoWork;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
            backgroundWorker.RunWorkerAsync();

            //New Code
            SU_pipeServer = new NamedPipeServerStream("THSU2P3DPIPE", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            SU_backgroundWorker = new BackgroundWorker();
            SU_backgroundWorker.DoWork += SU_Background_DoWork;
            SU_backgroundWorker.RunWorkerCompleted += SU_BackgroundWorker_RunWorkerCompleted;
            SU_backgroundWorker.RunWorkerAsync();
        }

        #region 接收数据并解析数据渲染
        private void Background_DoWork(object sender, DoWorkEventArgs e)
        {
            thProject = null;
            if (null == pipeServer)
                pipeServer = new NamedPipeServerStream("THCAD2P3DPIPE", PipeDirection.In);
            pipeServer.WaitForConnection();
            try
            {
                //thProject = Serializer.Deserialize<ThTCHProject>(pipeServer);
                thProject = new ThTCHProjectData();
                Google.Protobuf.MessageExtensions.MergeFrom(thProject, pipeServer);
            }
            catch (IOException ioEx)
            {
                thProject = null;
                Log.Error(string.Format("ERROR: {0}", ioEx.Message));
            }
            pipeServer.Dispose();
        }
        
        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (null != thProject)
            {
                ExampleScene.ifcre_set_sleep_time(1000);
                DateTime startTime = DateTime.Now;
                bimDataController.AddProject(thProject);
                thProject = null;
                pipeServer = null;
                backgroundWorker.RunWorkerAsync();
                DateTime endTime = DateTime.Now;
                var totalTime = (endTime - startTime).TotalSeconds;
                Log.Info(string.Format("数据解析完成，耗时：{0}s", totalTime));
                LoadIfcFile("");
            }
        }

        private void SU_Background_DoWork(object sender, DoWorkEventArgs e)
        {
            if (null == SU_pipeServer)
                SU_pipeServer = new NamedPipeServerStream("THSU2P3DPIPE", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            SU_pipeServer.WaitForConnection();
            try
            {
                //byte[] data = new byte[pipeServer.Length];
                //pipeServer.Read(data, 0, data.Length);
                suProject = new ThSUProjectData();
                Google.Protobuf.MessageExtensions.MergeFrom(suProject, SU_pipeServer);
            }
            catch (IOException ioEx)
            {
                suProject = null;
                Log.Error(string.Format("ERROR: {0}", ioEx.Message));
            }
            SU_pipeServer.Dispose();
        }

        private void SU_BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (null != suProject)
            {
                ExampleScene.ifcre_set_sleep_time(1000);
                DateTime startTime = DateTime.Now;
                bimDataController.AddProject(suProject, projectMatrix3D);
                suProject = null;
                SU_pipeServer = null;
                SU_backgroundWorker.RunWorkerAsync();
                DateTime endTime = DateTime.Now;
                var totalTime = (endTime - startTime).TotalSeconds;
                Log.Info(string.Format("数据解析完成，耗时：{0}s", totalTime));
                LoadIfcFile("");
            }
        }
        #endregion
        private void DispatcherTimer_Tick(object sender, EventArgs e)
		{
            var selectId = ExampleScene.GetCurrentCompID();
            if (selectId < 0)
                return;
            //var pro = bimDataController.GetSelectEntityProperties(selectId);


        }
        
        public Visibility DeveloperVisible => Settings.Default.DeveloperMode 
            ? Visibility.Visible 
            : Visibility.Collapsed;

        private void InitFromSettings()
        {
            FileAccessMode = Settings.Default.FileAccessMode;
            OnPropertyChanged("DeveloperVisible");           
        }

        private ObservableMruList<string> _mruFiles = new ObservableMruList<string>();
        private void RefreshRecentFiles()
        {
            var s = new List<string>();
            if (Settings.Default.MRUFiles != null)
                s.AddRange(Settings.Default.MRUFiles.Cast<string>());

            _mruFiles = new ObservableMruList<string>(s, 4, StringComparer.InvariantCultureIgnoreCase);
            MnuRecent.ItemsSource = _mruFiles;
        }

        private void AddRecentFile()
        {
            _mruFiles.Add(_openedModelFileName);
            Settings.Default.MRUFiles = new StringCollection();
            foreach (var item in _mruFiles)
            {
                Settings.Default.MRUFiles.Add(item);
            }
            Settings.Default.Save();
        }

        
        #region "Model File Operations"

        void XplorerMainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (null != winFormHost.Child) 
            {
                if (winFormHost.Child is GLControl glControl)
                    Win32.CloseRender(glControl.Handle);
            }
            if (null != backgroundWorker) 
            {
                backgroundWorker.Dispose();
            }
            if (null == pipeServer) 
            {
                pipeServer.Dispose();
            }
        }

        void XplorerMainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // this enables a basic configuration for the logger.
            //
            BasicConfigurator.Configure();
            var model = IfcStore.Create(null,IfcSchemaVersion.Ifc2X3, XbimStoreType.InMemoryModel);
            ModelProvider.ObjectInstance = model;
            ModelProvider.Refresh();

            // logging information warnings
            //
            _appender = new EventAppender {Tag = "MainWindow"};
            _appender.Logged += appender_Logged;

            var hier = LogManager.GetRepository() as Hierarchy;
            hier?.Root.AddAppender(_appender);

            //GMap.NET.WindowsForms.GMapControl mapControl = new GMap.NET.WindowsForms.GMapControl();
            //winFormHost.Child = mapControl;

            //InitGLControl();
            InitLeftTabItemValues();
        }

        private void XplorerMainWindow_Closed(object sender, EventArgs e)
        {
            CloseAndDeleteTemporaryFiles();
        }
        
        public XbimDBAccess FileAccessMode { get; set; } = XbimDBAccess.Read;
        
        private void OpenAcceptableExtension(object s, DoWorkEventArgs args)
        {
            var worker = s as BackgroundWorker;
            var selectedFilename = args.Argument as string;
            var startTime = DateTime.Now;
            try
            {
                if (worker == null)
                    throw new Exception("Background thread could not be accessed");
                _temporaryXbimFileName = Path.GetTempFileName();
                SetOpenedModelFileName(selectedFilename);
                var model = IfcStore.Open(selectedFilename, null, null, worker.ReportProgress, FileAccessMode);
                if (_meshModel)
                {
                    // mesh direct model
                    if (model.GeometryStore.IsEmpty)
                    {
                        try
                        {
                            var context = new Xbim3DModelContext(model);
                            if (!_multiThreading)
                                context.MaxThreads = 1;
                            context.UseSimplifiedFastExtruder = _simpleFastExtrusion;
                            SetDeflection(model);
                            //upgrade to new geometry representation, uses the default 3D model
                            context.CreateContext(worker.ReportProgress, App.ContextWcsAdjustment);
                        }
                        catch (Exception geomEx)
                        {
                            var sb = new StringBuilder();
                            sb.AppendLine($"Error creating geometry context of '{selectedFilename}' {geomEx.StackTrace}.");
                            var newexception = new Exception(sb.ToString(), geomEx);
                            Log.Error(sb.ToString(), newexception);
                        }
                    }

                    // mesh references
                    foreach (var modelReference in model.ReferencedModels)
                    {
                        // creates federation geometry contexts if needed
                        Debug.WriteLine(modelReference.Name);
                        if (modelReference.Model == null)
                            continue;
                        if (!modelReference.Model.GeometryStore.IsEmpty)
                            continue;
                        var context = new Xbim3DModelContext(modelReference.Model);
                        if (!_multiThreading)
                            context.MaxThreads = 1;
                        context.UseSimplifiedFastExtruder = _simpleFastExtrusion;
                        SetDeflection(modelReference.Model);
                        //upgrade to new geometry representation, uses the default 3D model
                        context.CreateContext(worker.ReportProgress, App.ContextWcsAdjustment);
                    }
                    if (worker.CancellationPending)
                    //if a cancellation has been requested then don't open the resulting file
                    {
                        try
                        {
                            model.Close();
                            if (File.Exists(_temporaryXbimFileName))
                                File.Delete(_temporaryXbimFileName); //tidy up;
                            _temporaryXbimFileName = null;
                            SetOpenedModelFileName(null);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.Message, ex);
                        }
                        return;
                    }
                }
                else
                {
                    Log.WarnFormat("Settings prevent mesh creation.");
                }
                //IfcStoreToMidFile(model);
                args.Result = model;
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Error opening '{selectedFilename}' {ex.StackTrace}.");
                var newexception = new Exception(sb.ToString(), ex);
                Log.Error(sb.ToString(), ex);
                args.Result = newexception;
            }
            var endTime = DateTime.Now;
            var totalTime = (endTime - startTime).TotalSeconds;
            Log.Info(string.Format("Ifc数据解析完成，耗时：{0}s", totalTime));
        }

        private void SetDeflection(IModel model)
        {
            var mf = model.ModelFactors;
            if (mf == null)
                return;
            if (!double.IsNaN(_angularDeflectionOverride))
                mf.DeflectionAngle = _angularDeflectionOverride;
            if (!double.IsNaN(_deflectionOverride))
                mf.DeflectionTolerance = mf.OneMilliMetre * _deflectionOverride;
        }

        private void dlg_OpenAnyFile(object sender, CancelEventArgs e)
        {
            var dlg = sender as OpenFileDialog;
            if (dlg != null) 
                LoadAnyModel(dlg.FileName);
            //var fInfo = new FileInfo(dlg.FileName);
            //var ext = fInfo.Extension.ToLower();
            //if (ext == ".midfile")
            //{
            //    LoadIfcFile(dlg.FileName);
            //}
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelFileName"></param>
        public void LoadAnyModel(string modelFileName, XbimMatrix3D? prjMatrix3D =null)
        {
            if (prjMatrix3D.HasValue)
            {
                projectMatrix3D = prjMatrix3D.Value;
            }
            else 
            {
                projectMatrix3D = XbimMatrix3D.CreateTranslation(XbimVector3D.Zero);
            }
            var fInfo = new FileInfo(modelFileName);
            if (!fInfo.Exists) // file does not exist; do nothing
                return;
            if (fInfo.FullName.ToLower() == GetOpenedModelFileName()) //same file do nothing
                return;
            _dispatcherTimer.Stop();
            _selectIndex = -1;
            _geoIndexIfcIndexMap.Clear();
            //InitGLControl();
            var ext = fInfo.Extension.ToLower();
            if (ext == ".midfile")
            {
                //LoadIfcFile(modelFileName);
            }
            else if (ext ==".thbim")
            {
                LoadTHBimFile(modelFileName);
                LoadIfcFile("");
            }
            else 
            {
                // there's no going back; if it fails after this point the current file should be closed anyway
                CloseAndDeleteTemporaryFiles();
                SetOpenedModelFileName(modelFileName.ToLower());
                ProgressStatusBar.Visibility = Visibility.Visible;
                SetWorkerForFileLoad();
                switch (ext)
                {
                    case ".ifc": //it is an Ifc File
                    case ".ifcxml": //it is an IfcXml File
                    case ".ifczip": //it is a zip file containing xbim or ifc File
                    case ".zip": //it is a zip file containing xbim or ifc File
                    case ".xbimf":
                    case ".xbim":
                        _loadFileBackgroundWorker.RunWorkerAsync(modelFileName);
                        break;
                    default:
                        Log.WarnFormat("Extension '{0}' has not been recognised.", ext);
                        break;
                }
            }
        }
        public void RemoveModel(string projectId) 
        {
            bimDataController.DeleteProject(new List<string> { projectId });
            LoadIfcFile("");
        }
        private void LoadTHBimFile(string fileName)
        {
            FileStream fsRead = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            BinaryReader r = new BinaryReader(fsRead);
            byte[] fileArray = r.ReadBytes((int)fsRead.Length);
            fsRead.Dispose();
            try
            {
                /* 通用准则
                 * thbim文件分为两部分 10byte长度的头部Head标识和剩余的Data数据
                 * Head 前两位分别是 'T','H'，用于防错标识
                 * 第三位 1 -> CAD 配置文件  2 -> SU 配置文件  其余不认
                 * 剩余七位还未使用
                 */

                var DataHead = fileArray.Take(10).ToArray();
                //84 = 'T' 72 = 'H' 
                if(DataHead[0] == 84 && DataHead[1] == 72)
                {
                    switch(DataHead[2])
                    {
                        case 1:
                            {
                                //CAD THBim 文件
                                var DataBody = fileArray.Skip(10).ToArray();
                                var th_Project = new ThTCHProjectData();
                                Google.Protobuf.MessageExtensions.MergeFrom(th_Project, DataBody);
                                //bimDataController.AddProject(th_Project);
                                break;
                            }
                        case 2:
                            {
                                //SU THBim 文件
                                var DataBody = fileArray.Skip(10).ToArray();
                                var su_Project = new ThSUProjectData();
                                Google.Protobuf.MessageExtensions.MergeFrom(su_Project, DataBody);
                                su_Project.Root.GlobalId = fileName;
                                bimDataController.AddProject(su_Project, projectMatrix3D);
                                break;
                            }
                        default:
                            {
                                //不支持的文件类型
                                Log.WarnFormat($"{fileName}导入失败，不支持的文件数据");
                                break;
                            }
                    }
                }
                
            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="args"></param>
        public delegate void LoadingCompleteEventHandler(object s, RunWorkerCompletedEventArgs args);
        /// <summary>
        /// 
        /// </summary>
        public event LoadingCompleteEventHandler LoadingComplete;

        private void FireLoadingComplete(object s, RunWorkerCompletedEventArgs args)
        {
            if (LoadingComplete != null)
            {
                LoadingComplete(s, args);
            }
        }

        private void SetWorkerForFileLoad()
        {
            _loadFileBackgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            ExampleScene.ifcre_set_sleep_time(1000);
            _loadFileBackgroundWorker.ProgressChanged += OnProgressChanged;
            _loadFileBackgroundWorker.DoWork += OpenAcceptableExtension;
            _loadFileBackgroundWorker.RunWorkerCompleted += FileLoadCompleted;
        }

        private void FileLoadCompleted(object s, RunWorkerCompletedEventArgs args)
        {
            if (args.Result is IfcStore ifcStore) //all ok
            {
                DateTime startTime = DateTime.Now;
                bimDataController.AddProject(ifcStore, projectMatrix3D);
                DateTime endTime = DateTime.Now;
                var totalTime = (endTime - startTime).TotalSeconds;
                Log.Info(string.Format("数据解析完成，耗时：{0}s", totalTime));
                LoadIfcFile("");
                //ShowIfcStore(ifcStore);
            }
            else //we have a problem
            {
                var errMsg = args.Result as string;
                if (!string.IsNullOrEmpty(errMsg))
                    MessageBox.Show(this, errMsg, "Error Opening File", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.None);
                var exception = args.Result as Exception;
                if (exception != null)
                {
                    var sb = new StringBuilder();

                    var indent = "";
                    while (exception != null)
                    {
                        sb.AppendFormat("{0}{1}\n", indent, exception.Message);
                        exception = exception.InnerException;
                        indent += "\t";
                    }
                    MessageBox.Show(this, sb.ToString(), "Error Opening Ifc File", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.None);
                }
                ProgressBar.Value = 0;
                StatusMsg.Text = "Error/Ready";
                SetOpenedModelFileName("");
            }
            FireLoadingComplete(s, args);
        }

        private void IfcStoreToMidFile(IfcStore ifcStore) 
        {
            var engineFile = new IfcStoreToEngineFile();
            engineFile.ProgressChanged += OnProgressChanged;
            _geoIndexIfcIndexMap = engineFile.LoadGeometry(ifcStore, _tempMidFileName);
        }
        private void ShowIfcStore(IfcStore ifcStore) 
        {
            //this Triggers the event to load the model into the views 
            ModelProvider.ObjectInstance = ifcStore;
            ModelProvider.Refresh();
            ProgressBar.Value = 0;
            StatusMsg.Text = "";
            LoadIfcFile("");
            AddRecentFile();
        }
        private void OnProgressChanged(object s, ProgressChangedEventArgs args)
        {
            if (args.ProgressPercentage < 0 || args.ProgressPercentage > 100)
                return;

            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Send,
                new Action(() =>
                {
                    ProgressBar.Value = args.ProgressPercentage;
                    StatusMsg.Text = (string) args.UserState;
                }));

        }

        private void dlg_FileSaveAs(object sender, CancelEventArgs e)
        {
            var dlg = sender as SaveFileDialog;
            if (dlg == null) 
                return;
            var fInfo = new FileInfo(dlg.FileName);
            try
            {
                if (fInfo.Exists)
                {
                    // the user has been asked to confirm deletion previously
                    fInfo.Delete();
                }
                if (Model != null)
                {
                    Model.SaveAs(dlg.FileName);
                    SetOpenedModelFileName(dlg.FileName);
                    var s = Path.GetExtension(dlg.FileName);
                    if (string.IsNullOrWhiteSpace(s)) 
                        return;
                    var extension = s.ToLowerInvariant();
                    if (extension != "xbim" || string.IsNullOrWhiteSpace(_temporaryXbimFileName)) 
                        return;
                    File.Delete(_temporaryXbimFileName);
                    _temporaryXbimFileName = null;
                }
                else throw new Exception("Invalid Model Server");
            }
            catch (Exception except)
            {
                MessageBox.Show(except.Message, "Error Saving as", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CommandBinding_Refresh(object sender, ExecutedRoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_openedModelFileName))
                return;
            if (!File.Exists(_openedModelFileName))
                return;
            LoadAnyModel(_openedModelFileName);
        }
        
        private void CommandBinding_SaveAs(object sender, ExecutedRoutedEventArgs e)
        {
            var dlg = new SaveFileDialog();
            var ext = "";
            if (GetOpenedModelFileName() != null)
            {
                var f = new FileInfo(GetOpenedModelFileName());
                dlg.DefaultExt = f.Extension;
                ext = f.Extension.ToLower();
                dlg.InitialDirectory = f.DirectoryName;
                dlg.FileName = f.Name;
            }

            Dictionary<string, string> options = new Dictionary<string, string>();
            options.Add(".ifc", "Ifc File (*.ifc)|*.ifc");
            //options.Add(".xbim", "xBIM File (*.xBIM)|*.xBIM");
            //options.Add(".ifcxml", "IfcXml File (*.IfcXml)|*.ifcxml");
            //options.Add(".ifczip", "IfcZip File (*.IfcZip)|*.ifczip");

            var filters = new List<string>();
            if (options.ContainsKey(ext))
            {
                filters.Add(options[ext]);
                options.Remove(ext);
            }
            filters.AddRange(options.Values);

            // now set dialog
            dlg.Filter = string.Join("|", filters.ToArray());
            dlg.Title = "Save As";
            dlg.AddExtension = true;

            // Show open file dialog box 
            dlg.FileOk += dlg_FileSaveAs;
            dlg.ShowDialog(this);
        }

        private void CommandBinding_Close(object sender, ExecutedRoutedEventArgs e)
        {
            CloseAndDeleteTemporaryFiles();
        }
        
        private void CommandBinding_Open(object sender, ExecutedRoutedEventArgs e)
        {
            var corefilters = new[] {
                "IFC Files|*.ifc;*.midfile;*.thbim",
                "Ifc File (*.ifc)|*.ifc",
                "Engin Midel File (*.midfile)|*.midfile",
                "thbim File (*.thbim)|*.thbim"
            };

            // Filter files by extension 
            var dlg = new OpenFileDialog
            {
                Filter = string.Join("|", corefilters)
            };
            dlg.FileOk += dlg_OpenAnyFile;
            dlg.ShowDialog(this);
        }

        /// <summary>
        /// Tidies up any open files and closes any open models
        /// </summary>
        private void CloseAndDeleteTemporaryFiles()
        {
            try
            {
                if (_loadFileBackgroundWorker != null && _loadFileBackgroundWorker.IsBusy)
                    _loadFileBackgroundWorker.CancelAsync(); //tell it to stop
                
                SetOpenedModelFileName(null);
                if (Model != null)
                {
                    Model.Dispose();
                    ModelProvider.ObjectInstance = null;
                    ModelProvider.Refresh();
                }
                //if (!(DrawingControl.DefaultLayerStyler is SurfaceLayerStyler))
                //    SetDefaultModeStyler(null, null);
            }
            finally
            {
                if (!(_loadFileBackgroundWorker != null && _loadFileBackgroundWorker.IsBusy && _loadFileBackgroundWorker.CancellationPending)) //it is still busy but has been cancelled 
                {
                    if (!string.IsNullOrWhiteSpace(_temporaryXbimFileName) && File.Exists(_temporaryXbimFileName))
                        File.Delete(_temporaryXbimFileName);
                    _temporaryXbimFileName = null;
                } //else do nothing it will be cleared up in the worker thread
            }
        }

        private void CanExecuteIfFileOpen(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (Model != null) && (!string.IsNullOrEmpty(GetOpenedModelFileName()));
        }

        private void CanExecuteIfModelNotNull(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (Model != null);
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_loadFileBackgroundWorker != null && _loadFileBackgroundWorker.IsBusy)
                e.CanExecute = false;
            else
            {
                if (e.Command == ApplicationCommands.Close || e.Command == ApplicationCommands.SaveAs)
                {
                    e.CanExecute = (Model != null);
                }
                else if (e.Command == OpenExportWindowCmd)
                {   
                    e.CanExecute = (Model != null) && (!string.IsNullOrEmpty(GetOpenedModelFileName()));
                }
                else
                    e.CanExecute = true; //for everything else
            }
        }


        #endregion

        # region "Federation Model operations"
        private void EditFederationCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var fdlg = new FederatedModelDialog {DataContext = Model};
            var done = fdlg.ShowDialog();
            if (done.HasValue && done.Value)
            {
                // todo: is there something that needs to happen here?
            }
            //DrawingControl.ReloadModel();
        }
        private void EditFederationCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Model != null && Model.IsFederation;
        }
       
        private void CreateFederationCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select model files to federate.",
                Filter = "Model Files|*.ifc;*.ifcxml;*.ifczip", // Filter files by extension 
                CheckFileExists = true,
                Multiselect = true
            };

            var done = dlg.ShowDialog(this);

            if (!done.Value)
                return;

            FederationFromDialogbox(dlg);
        }


        private void FederationFromDialogbox(OpenFileDialog dlg)
        {
            if (!dlg.FileNames.Any())
                return;
            //use the first filename it's extension to decide which action should happen
            var s = Path.GetExtension(dlg.FileNames[0]);
            if (s == null)
                return;
            var firstExtension = s.ToLower();

            IfcStore fedModel = null;
            switch (firstExtension)
            {
                case ".xbimf":
                    if (dlg.FileNames.Length > 1)
                    {
                        var res = MessageBox.Show("Multiple files selected, open " + dlg.FileNames[0] + "?",
                            "Cannot open multiple Xbim files",
                            MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                        if (res == MessageBoxResult.Cancel)
                            return;
                        fedModel = IfcStore.Open(dlg.FileNames[0]);
                    }
                    break;
                case ".ifc":
                case ".ifczip":
                case ".ifcxml":
                    // create temp file as a placeholder for the temporory xbim file                   
                    fedModel = IfcStore.Create(null,IfcSchemaVersion.Ifc2X3, XbimStoreType.InMemoryModel);                    
                    using (var txn = fedModel.BeginTransaction())
                    {
                        var project = fedModel.Instances.New<Xbim.Ifc2x3.Kernel.IfcProject>();
                        project.Name = "Default Project Name";
                        project.Initialize(ProjectUnits.SIUnitsUK);
                        txn.Commit();
                    }

                    var informUser = true;
                    for (var i = 0; i < dlg.FileNames.Length; i++)
                    {
                        var fileName = dlg.FileNames[i];
                        var temporaryReference = new XbimReferencedModelViewModel
                        {
                            Name = fileName,
                            OrganisationName = "OrganisationName " + i,
                            OrganisationRole = "Undefined"
                        };

                        var buildRes = false;
                        Exception exception = null;
                        try
                        {
                            buildRes = temporaryReference.TryBuildAndAddTo(fedModel);
                        }
                        catch (Exception ex)
                        {
                            //usually an EsentDatabaseSharingViolationException, user needs to close db first
                            exception = ex;
                        }

                        if (buildRes || !informUser)
                            continue;
                        var msg = exception == null ? "" : "\r\nMessage: " + exception.Message;
                        var res = MessageBox.Show(fileName + " couldn't be opened." + msg + "\r\nShow this message again?",
                            "Failed to open a file", MessageBoxButton.YesNoCancel, MessageBoxImage.Error);
                        if (res == MessageBoxResult.No)
                            informUser = false;
                        else if (res == MessageBoxResult.Cancel)
                        {
                            fedModel = null;
                            break;
                        }
                    }
                    break;
            }
            if (fedModel == null)
                return;
            CloseAndDeleteTemporaryFiles();
            IfcStoreToMidFile(fedModel);
            ShowIfcStore(fedModel);
            //ModelProvider.ObjectInstance = fedModel;
            //ModelProvider.Refresh();
        }
        
        #endregion

        /// <summary>
        /// 
        /// </summary>
        public IPersistEntity SelectedItem
        {
            get { return (IPersistEntity)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedItem.  This enables animation, styling, binding, etc...
        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(IPersistEntity), typeof(XplorerMainWindow),
                                        new UIPropertyMetadata(null, OnSelectedItemChanged));


        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //var mw = d as XplorerMainWindow;
            //if (mw != null && e.NewValue is IPersistEntity)
            //{
            //    var label = (IPersistEntity)e.NewValue;
            //    mw.EntityLabel.Text = label != null ? "#" + label.EntityLabel : "";
            //}
            //else if (mw != null) mw.EntityLabel.Text = "";
        }


        private ObjectDataProvider ModelProvider
        {
            get
            {
                return MainFrame.DataContext as ObjectDataProvider;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IfcStore Model
        {
            get
            {
                var op = MainFrame.DataContext as ObjectDataProvider;
                return op == null ? null : op.ObjectInstance as IfcStore;
            }
        }

        ///// <summary>
        ///// this variable is used to determine when the user is trying again to double click on the selected item
        ///// from this we detect that he's probably not happy with the view, therefore we add a cutting plane to make the 
        ///// element visible.
        ///// </summary>
        //private bool _camChanged;

        /// <summary>
        /// determines if models need to be meshed on opening
        /// </summary>
        private bool _meshModel = true;

        
        private double _deflectionOverride = double.NaN;
        private double _angularDeflectionOverride = double.NaN;
        
        /// <summary>
        /// determines if the geometry engine will run on parallel threads.
        /// </summary>
        private bool _multiThreading = true;

        /// <summary>
        /// determines if the geometry engine will run on parallel threads.
        /// </summary>
        private bool _simpleFastExtrusion = false;

        private void SpatialControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //_camChanged = false;
            //DrawingControl.Viewport.Camera.Changed += Camera_Changed;
            //DrawingControl.ZoomSelected();
            //DrawingControl.Viewport.Camera.Changed -= Camera_Changed;
            //if (!_camChanged)
            //    DrawingControl.ClipBaseSelected(0.15);
        }

        void Camera_Changed(object sender, EventArgs e)
        {
            //_camChanged = true;
        }


        private void MenuItem_ZoomExtents(object sender, RoutedEventArgs e)
        {
            //DrawingControl.ViewHome();
        }
        
        private void OpenExportWindow(object sender, ExecutedRoutedEventArgs executedRoutedEventArgs)
        {
            var wndw = new ExportWindow(this);
            wndw.ShowDialog();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var w = new AboutWindow
            {
                Model = Model,
                Assemblies = _pluginAssemblies,
                MainWindow = this
            };
            w.Show();
        }
        
        private void DisplaySettingsPage(object sender, RoutedEventArgs e)
        {
            var sett = new SettingsWindow();
            // geom engine
            sett.ComputeGeometry.IsChecked = _meshModel;
            sett.MultiThreading.IsChecked = _multiThreading;
            sett.SimpleFastExtrusion.IsChecked = _simpleFastExtrusion;
            if (!double.IsNaN(_angularDeflectionOverride))
                sett.AngularDeflection.Text = _angularDeflectionOverride.ToString();
            if (!double.IsNaN(_deflectionOverride))
                sett.Deflection.Text = _deflectionOverride.ToString();
            
            // visuals
            //sett.SimplifiedRendering.IsChecked = DrawingControl.HighSpeed;
            //sett.ShowFps.IsChecked = DrawingControl.ShowFps;

            // show dialog
            sett.ShowDialog();
            
            
            // dialog closed
            if (!sett.SettingsChanged)
                return;
            InitFromSettings();

            // all settings that are not saved
            //

            // geom engine
            if (sett.ComputeGeometry.IsChecked != null)
                _meshModel = sett.ComputeGeometry.IsChecked.Value;
            if (sett.MultiThreading.IsChecked != null)
                _multiThreading = sett.MultiThreading.IsChecked.Value;
            if (sett.SimpleFastExtrusion.IsChecked != null)
                _simpleFastExtrusion = sett.SimpleFastExtrusion.IsChecked.Value;

            _deflectionOverride = double.NaN;
            _angularDeflectionOverride = double.NaN;
            if (!string.IsNullOrWhiteSpace(sett.AngularDeflection.Text))
                double.TryParse(sett.AngularDeflection.Text, out _angularDeflectionOverride);
            
            if (!string.IsNullOrWhiteSpace(sett.Deflection.Text))
                double.TryParse(sett.Deflection.Text, out _deflectionOverride);

            if (!string.IsNullOrWhiteSpace(sett.BooleanTimeout.Text))
                ConfigurationManager.AppSettings["BooleanTimeOut"] = sett.BooleanTimeout.Text;

            // visuals
            //if (sett.SimplifiedRendering.IsChecked != null)
            //    DrawingControl.HighSpeed = sett.SimplifiedRendering.IsChecked.Value;
            //if (sett.ShowFps.IsChecked != null)
            //    DrawingControl.ShowFps = sett.ShowFps.IsChecked.Value;

        }

        private void RecentFileClick(object sender, RoutedEventArgs e)
        {
            var obMenuItem = e.OriginalSource as MenuItem;
            if (obMenuItem == null) 
                return;
            var fileName = obMenuItem.Header.ToString();
            if (!File.Exists(fileName))
            {
                return;
            }
            LoadAnyModel(fileName);
        }

        private void SetDefaultModeStyler(object sender, RoutedEventArgs e)
        {
            //DrawingControl.DefaultLayerStyler = new SurfaceLayerStyler(this.Logger);
            ConnectStylerFeedBack();
            //DrawingControl.ReloadModel();
        }

        private void ConnectStylerFeedBack()
        {
            //if (DrawingControl.DefaultLayerStyler is IProgressiveLayerStyler)
            //{
            //    ((IProgressiveLayerStyler)DrawingControl.DefaultLayerStyler).ProgressChanged += OnProgressChanged;
            //}
        }

        DrawingControl3D IXbimXplorerPluginMasterWindow.DrawingControl
        {
            get { return null; }// DrawingControl; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ShowErrors(object sender, MouseButtonEventArgs e)
        {
            OpenOrFocusPluginWindow(typeof (LogViewer.LogViewer));
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            // todo: should we persist UI appearence across sessions?
#if PERSIST_UI
            // experiment
            using (var fs = new StringWriter())
            {
                var xmlLayout = new XmlLayoutSerializer(DockingManager);
                xmlLayout.Serialize(fs);
                var xmlLayoutString = fs.ToString();
                Clipboard.SetText(xmlLayoutString);
            }
#endif
            Close();
        }

        /// <summary>
        /// this event is run after the window is fully rendered.
        /// </summary>
        private void RenderedEvents(object sender, EventArgs e)
        {
            // command line arg can prevent plugin loading
            if (Settings.Default.PluginStartupLoad && !PreventPluginLoad)
                RefreshPlugins();
            ConnectStylerFeedBack();
            _appender.EventsLimit = 100;
        }
        
        private void EntityLabel_KeyDown()
        {
            //var input = EntityLabel.Text;
            //var re = new Regex(@"#[ \t]*(\d+)");
            //var m = re.Match(input);
            //IPersistEntity entity = null;
            //if (m.Success)
            //{
            //    int isLabel;
            //    if (!int.TryParse(m.Groups[1].Value, out isLabel))
            //        return;
            //    entity = Model.Instances[isLabel];
            //}
            //else
            //{
            //    entity = Model.Instances.OfType<IIfcRoot>().FirstOrDefault(x => x.GlobalId == input);
            //}

            //if (entity != null)
            //    SelectedItem = entity;

        }

        private void ConfigureStyler(object sender, RoutedEventArgs e)
        {
            var c = new SurfaceLayerStylerConfiguration(Model);
            //if (DrawingControl.ExcludedTypes != null)
            //    c.InitialiseSettings(DrawingControl.ExcludedTypes);
            //c.ShowDialog();
            //if (!c.MustUpdate) 
            //    return;
            //DrawingControl.ExcludedTypes = c.ExcludedTypes;
            //DrawingControl.ReloadModel(DrawingControl3D.ModelRefreshOptions.ViewPreserveCameraPosition);
        }

        private void HideSelected(object sender, RoutedEventArgs e)
        {
            //if (null != DrawingControl.HiddenInstances)
            //    DrawingControl.HiddenInstances.AddRange(DrawingControl.Selection);
            //else
            //    DrawingControl.HiddenInstances = DrawingControl.Selection.ToList();

            //DrawingControl.ReloadModel(DrawingControl3D.ModelRefreshOptions.ViewPreserveCameraPosition);
        }

        private void IsolateSelected(object sender, RoutedEventArgs e)
        {
            //DrawingControl.IsolateInstances = DrawingControl.Selection.ToList();
            //DrawingControl.ReloadModel(DrawingControl3D.ModelRefreshOptions.ViewPreserveCameraPosition);
        }

        private void RestoreView(object sender, RoutedEventArgs e)
        {
            //DrawingControl.IsolateInstances = null;
            //DrawingControl.HiddenInstances = null;
            //DrawingControl.SelectedContexts = null;
            //DrawingControl.ReloadModel(DrawingControl3D.ModelRefreshOptions.ViewPreserveCameraPosition);
        }

        private void SelectRepresentationContext(object sender, RoutedEventArgs e)
        {
            //RepresentationContextSelection w = new RepresentationContextSelection();
            //IEnumerable<IIfcGeometricRepresentationContext> availableContexts = this.Model.Instances.OfType<IIfcGeometricRepresentationContext>();
            //IEnumerable<IIfcGeometricRepresentationContext> availableParentContexts = availableContexts.Except(this.Model.Instances.OfType<IIfcGeometricRepresentationSubContext>());
            //w.SetContextItems(availableParentContexts);
            //if (w.ShowDialog() == true)
            //{
            //    this.DrawingControl.SelectedContexts = new List<IIfcGeometricRepresentationContext>();
            //    foreach (ContextSelectionItem parent in w.ContextItems)
            //    {
            //        if (parent.IsChecked)
            //        {
            //            this.DrawingControl.SelectedContexts.Add(parent.RepresentationContext);
            //        }
            //        foreach (ContextSelectionItem child in parent.Children)
            //        {
            //            if (child.IsChecked)
            //            {
            //                this.DrawingControl.SelectedContexts.Add(child.RepresentationContext); 
            //            }
            //        }
            //    }
            //    this.DrawingControl.ReloadModel(DrawingControl3D.ModelRefreshOptions.ViewPreserveCameraPosition);
            //}
        }

        private void SelectionMode(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            if (mi == null)
            {  
                return;
            }
            WholeMesh.IsChecked = false;
            Normals.IsChecked = false;
            WireFrame.IsChecked = false;
            mi.IsChecked = true;
            //switch (mi.Name)
            //{
            //    case "WholeMesh":
            //        DrawingControl.SelectionHighlightMode = DrawingControl3D.SelectionHighlightModes.WholeMesh;
            //        break;
            //    case "Normals":
            //        DrawingControl.SelectionHighlightMode = DrawingControl3D.SelectionHighlightModes.Normals;
            //        break;
            //    case "WireFrame":
            //        DrawingControl.SelectionHighlightMode = DrawingControl3D.SelectionHighlightModes.WireFrame;
            //        break;
            //}
        }

        private void OpenStrippingWindow(object sender, RoutedEventArgs e)
        {
            Simplify.IfcSimplify s = new Simplify.IfcSimplify();
            s.Show();
        }

        private void MenuItem_ZoomSelected(object sender, RoutedEventArgs e)
        {
            //DrawingControl.ZoomSelected();
        }

        private void StylerIfcSpacesOnly(object sender, RoutedEventArgs e)
        {
            var module2X3 = (typeof(Xbim.Ifc2x3.Kernel.IfcProduct)).Module;
            var meta2X3 = ExpressMetaData.GetMetadata(module2X3);
            var product2X3 = meta2X3.ExpressType("IFCPRODUCT");

            var module4 = (typeof(Xbim.Ifc4.Kernel.IfcProduct)).Module;
            var meta4 = ExpressMetaData.GetMetadata(module4);
            var product4 = meta4.ExpressType("IFCPRODUCT");
            


            var tpcoll = product2X3.NonAbstractSubTypes.Select(x => x.Type).ToList();
            tpcoll.AddRange(product4.NonAbstractSubTypes.Select(x => x.Type).ToList());
            tpcoll.RemoveAll(x => x.Name == "IfcSpace");

            //DrawingControl.ExcludedTypes = tpcoll;
            //DrawingControl.ReloadModel(DrawingControl3D.ModelRefreshOptions.ViewPreserveCameraPosition);
        }

        private void SetStylerBoundCorners(object sender, RoutedEventArgs e)
        {
            //DrawingControl.DefaultLayerStyler = new BoundingBoxStyler(this.Logger);
            ConnectStylerFeedBack();
            //DrawingControl.ReloadModel();
        }

        private void CommandBoxLost(object sender, RoutedEventArgs e)
        {
            CommandBox.Visibility = Visibility.Collapsed;
        }

        private void CommandBoxEval(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CommandBox.Visibility = Visibility.Collapsed;

                var cmd = CommandPrompt.Text;
                if (string.IsNullOrWhiteSpace(cmd))
                    return;
                Type t = typeof(Commands.wdwCommands);
                var opened = OpenOrFocusPluginWindow(t) as Commands.wdwCommands;
                opened.Execute(cmd);
            }
        }

        private void ShowCommandBox(object sender, RoutedEventArgs e)
        {
            CommandBox.Visibility = Visibility.Visible;
            CommandPrompt.Focus();
        }

        private void SetRandomStyler(object sender, RoutedEventArgs e)
        {
            //DrawingControl.DefaultLayerStyler = new RandomColorStyler(Logger);
            ConnectStylerFeedBack();
            //DrawingControl.ReloadModel();

        }

        private void SelectionColorCycle(object sender, RoutedEventArgs e)
        {
            //if (DrawingControl.SelectionColor == Colors.Blue)
            //{
            //    DrawingControl.SelectionColor = Colors.LightBlue;
            //}
            //else if (DrawingControl.SelectionColor == Colors.LightBlue)
            //{
            //    DrawingControl.SelectionColor = Colors.Orange;
            //}
            //else if (DrawingControl.SelectionColor == Colors.Orange)
            //{
            //    DrawingControl.SelectionColor = Colors.Red;
            //}
            //else if (DrawingControl.SelectionColor == Colors.Red)
            //{
            //    DrawingControl.SelectionColor = Colors.Blue;
            //}
        }
		private void LoadIfcFile(string path)
		{
            projectMatrix3D = XbimMatrix3D.CreateTranslation(XbimVector3D.Zero);
            //if (!bimDataController.HaveMeshData) 
            //{
            //    //没有任何需要渲染的数据
            //    Log.Info("无几何信息，不进行渲染");
            //    return;
            //}
            DateTime startTime = DateTime.Now;
            ProgressBar.Value = 0;
            StatusMsg.Text = "";
            //if (string.IsNullOrEmpty(path))
            //	return;

            ThreadPool.QueueUserWorkItem(delegate
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(System.Windows.Application.Current.Dispatcher));
                SynchronizationContext.Current.Post(pl =>
                {
                    DateTime tempStart = DateTime.Now;
                    FilterViewModel.Instance.UpdataFilterByProject();

                    DateTime tempEnd = DateTime.Now;
                    var tempTotal = (tempEnd - tempStart).TotalSeconds;
                    Log.Info(string.Format("过虑器初始化，耗时(异步)：{0}s", tempTotal));
                }, null);
            });

            startTime = DateTime.Now;
            var formHost = winFormHost;
			var childConrol = formHost.Child as GLControl;
			childConrol.EnableNativeInput();
			childConrol.MakeCurrent();
			ExampleScene.Init(childConrol.Handle, childConrol.Width, childConrol.Height, path);
            DateTime endTime = DateTime.Now;
            var totalTime = (endTime - startTime).TotalSeconds;
            Log.Info(string.Format("渲染前准备工作完成，耗时：{0}s", totalTime));

            _dispatcherTimer.Start();
            ExampleScene.Render();
		}
        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            bimDataController.ClearAllProject();
            ExampleScene.ifcre_clear_model_data();
            LoadIfcFile("");
        }
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tab = sender as TabControl;
            if (tab.SelectedItem != null)
            {
                var tabSelect = tab.SelectedItem as LeftTabItemBtn;
                tabSelect.PanelControl.Visibility = Visibility.Visible;
                var winHost = GetOrAddLeftHost();
                if (winHost.Child != null)
                {
                    var tempHost = winHost.Child as ElementHost;
                    tempHost.Child = null;
                    tempHost.Dispose();
                    winHost.Child = null;
                }
                ElementHost elementHost = new ElementHost();
                elementHost.Child = tabSelect.PanelControl;
                elementHost.Child.IsVisibleChanged += Child_IsVisibleChanged;
                winHost.Child = elementHost;
            }
        }
        private void Child_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
                return;
            var winHost = GetOrAddLeftHost();
            if (winHost.Child == null)
                return;
            var elemHost = (winHost.Child as ElementHost);
            if (elemHost.Child == null)
                return;
            bool isClose = false;
            if (leftTabControl.SelectedItem != null) 
            {
                var tabSelect = leftTabControl.SelectedItem as LeftTabItemBtn;
                isClose = tabSelect.PanelControl == elemHost.Child;
            }
            elemHost.Child.IsVisibleChanged -= Child_IsVisibleChanged;
            elemHost.Child = null;
            elemHost.Dispose();
            winHost.Child = null;
            if(isClose)
                leftTabControl.SelectedItem = null;
        }
        WindowsFormsHost GetOrAddLeftHost() 
        {
            var winFormName = "TempShowFormHost";
            WindowsFormsHost winHost = null;
            foreach (var item in mainGrid.Children)
            {
                if (item is WindowsFormsHost host)
                {
                    if (host.Name == winFormName)
                    {
                        winHost = host;
                    }
                }
            }
            if (winHost == null)
            {
                winHost = new WindowsFormsHost();
                winHost.Name = winFormName;
                winHost.Style = this.Resources["TempHostStyle"] as Style;
                mainGrid.Children.Add(winHost);
            }
            return winHost;
        }
        void InitLeftTabItemValues() 
        {
            MainViewModel mainViewModel = new MainViewModel(MainWindow);
            leftTabControl.DataContext = mainViewModel;
        }

        private void ExportCut_Click(object sender, RoutedEventArgs e)
        {
            //导出切图数据
            ;
            ThBimCutData.Run();
        }

        private void homeView_Click(object sender, RoutedEventArgs e)
        {
            ExampleScene.ifcre_home();
        }
    }
}
