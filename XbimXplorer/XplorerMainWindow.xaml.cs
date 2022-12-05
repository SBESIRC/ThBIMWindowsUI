#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Project:     XbimXplorer
// Published:   01, 2012

#endregion

#region Directives

using Google.Protobuf;
using log4net;
using log4net.Config;
using log4net.Repository.Hierarchy;
using Microsoft.Win32;
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
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using THBimEngine.Application;
using THBimEngine.Domain;
using THBimEngine.HttpService;
using THBimEngine.Presention;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.Common.Metadata;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.IO;
using Xbim.IO.Esent;
using Xbim.ModelGeometry.Scene;
using Xbim.Presentation;
using Xbim.Presentation.FederatedModel;
using Xbim.Presentation.XplorerPluginSystem;
using XbimXplorer.Dialogs;
using XbimXplorer.Dialogs.ExcludedTypes;
using XbimXplorer.Extensions;
using XbimXplorer.LogViewer;
using XbimXplorer.Project;
using XbimXplorer.Properties;
using XbimXplorer.ThBIMEngine;
using XbimXplorer.Deduct;
using THBimEngine.IO.GFC2;
#endregion

namespace XbimXplorer
{
    /// <summary>
    /// Interaction logic for XplorerMainWindow
    /// </summary>
    public partial class XplorerMainWindow : IXbimXplorerPluginMasterWindow, IEngineApplication, INotifyPropertyChanged
    {
        private BackgroundWorker _loadFileBackgroundWorker;
        private BackgroundWorker _loadStreamBackgroundWorker;
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
        private void SetOpenedModelFileName(string ifcFilename)
        {
            _openedModelFileName = ifcFilename;
            // try to update the window title through a delegate for multithreading
            Dispatcher.BeginInvoke(new Action(delegate
            {
                Title = string.IsNullOrEmpty(ifcFilename)
                    ? "天华结构三维平台" :
                    "天华结构三维平台 - [" + ifcFilename + "]";
            }));
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler ApplicationClosing;
        public event ProgressChangedEventHandler ProgressChanged;
        private UserInfo loginUser;
        public XplorerMainWindow(UserInfo user, bool preventPluginLoad = false)
        {
            InitializeComponent();
            loginUser = user;
            ProgressChanged = OnProgressChanged;
            _geoIndexIfcIndexMap = new Dictionary<int, int>();
            _dispatcherTimer = new DispatcherTimer();
            _dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            _dispatcherTimer.Tick += DispatcherTimer_Tick;

            InitDocument();
            PreventPluginLoad = preventPluginLoad;
            // initialise the internal elements of the UI that behave like plugins
            EvaluateXbimUiType(typeof(IfcValidation.ValidationWindow), true);
            EvaluateXbimUiType(typeof(LogViewer.LogViewer), true);
            EvaluateXbimUiType(typeof(Commands.wdwCommands), true);

            // notify the user of changes in the measures taken in the 3d viewer.
            //--DrawingControl.UserModeledDimensionChangedEvent += DrawingControl_MeasureChangedEvent;

            // Get the settings
            InitFromSettings();
            RefreshRecentFiles();

            // initialise the logging repository
            LoggedEvents = new ObservableCollection<EventViewModel>();
            // any logging event required should happen after XplorerMainWindow_Loaded

            InitPipeService();
            // attach window managment functions
            ApplicationClosing += XplorerMainWindow_ApplicationClosing;
            Closed += XplorerMainWindow_Closed;
            Loaded += XplorerMainWindow_Loaded;
            Closing += XplorerMainWindow_Closing;
        }
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
            ApplicationClosing.Invoke(this, null);
        }
        private void XplorerMainWindow_ApplicationClosing(object sender, EventArgs e)
        {
            if (null != backgroundWorker)
            {
                backgroundWorker.Dispose();
            }
            if (null != pipeServer)
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
            InitTHPlugins();
        }

        private void XplorerMainWindow_Closed(object sender, EventArgs e)
        {
            CloseAndDeleteTemporaryFiles();
        }
        
        public XbimDBAccess FileAccessMode { get; set; } = XbimDBAccess.Read;
        
        

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
            //var dlg = sender as OpenFileDialog;
            //if (dlg != null)
            //    LoadFileToCurrentDocument(dlg.FileName,null);
            //var fInfo = new FileInfo(dlg.FileName);
            //var ext = fInfo.Extension.ToLower();
            //if (ext == ".midfile")
            //{
            //    LoadIfcFile(dlg.FileName);
            //}
        }

        public void RemoveProjectFormCurrentDocument(string projectId) 
        {
            if (CurrentDocument == null)
                return;
            CurrentDocument.DeleteProject(projectId);
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
        private void IfcStoreToMidFile(IfcStore ifcStore) 
        {
            //var engineFile = new IfcStoreToEngineFile();
            //engineFile.ProgressChanged += OnProgressChanged;
            //_geoIndexIfcIndexMap = engineFile.LoadGeometry(ifcStore);
        }
        private void ShowIfcStore(IfcStore ifcStore) 
        {
            //this Triggers the event to load the model into the views 
            ModelProvider.ObjectInstance = ifcStore;
            ModelProvider.Refresh();
            ProgressBar.Value = 0;
            StatusMsg.Text = "";
            AddRecentFile();
            RenderScene();
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
            //if (string.IsNullOrEmpty(_openedModelFileName))
            //    return;
            //if (!File.Exists(_openedModelFileName))
            //    return;
            //LoadFileToCurrentDocument(_openedModelFileName,null);
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
                "thbim File (*.thbim)|*.thbim",
                "YJK File (*.ydb)|*.ydb"
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
                
                //SetOpenedModelFileName(null);
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
            //var obMenuItem = e.OriginalSource as MenuItem;
            //if (obMenuItem == null) 
            //    return;
            //var fileName = obMenuItem.Header.ToString();
            //if (!File.Exists(fileName))
            //{
            //    return;
            //}
            //LoadFileToCurrentDocument(fileName,null);
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
        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            if (CurrentDocument == null)
                return;
            CurrentDocument.ClearAllData();
            RenderScene();
        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            if (CurrentDocument == null)
                return;
            
            var structure95Project = CurrentDocument.AllBimProjects.FirstOrDefault();
            var structure5Project = CurrentDocument.AllBimProjects.LastOrDefault();
            if (CurrentDocument.AllBimProjects.Count == 1)
            {
                structure5Project = null;
            }
            if (structure95Project != null && structure95Project.SourceProject is IfcStore ifcStore)
            {
                if (ifcStore.FileName.ToLower().EndsWith("ifc") && ifcStore.FileName != ifc_ProjectPath)
                {
                    try
                    {
                        var prj95Id = structure95Project.ProjectIdentity;
                        var prjKey = documentModelCache.Where(c => c.Value.MainModel.LinkFilePath == prj95Id).FirstOrDefault().Key;
                        if (string.IsNullOrEmpty(prjKey)) 
                        {
                            MessageBox.Show("主文件未在项目中找到，无法进行合模操作");
                            return;
                        }
                        var mergeService = new Extensions.ModelMerge.THModelMergeService();
                        IfcStore mergeIfc;
                        if (structure5Project?.SourceProject is ThSUProjectData projectData)
                        {
                            mergeIfc = mergeService.ModelMerge(ifcStore, projectData);
                        }
                        else if (structure5Project?.SourceProject is IfcStore projectIfcStore)
                        {
                            mergeIfc = mergeService.ModelMerge(ifcStore, projectIfcStore);
                        }
                        else
                        {
                            mergeIfc = null;
                        }
                        var fileName = Path.GetFileNameWithoutExtension(ifcStore.FileName);
                        var dirName = Path.GetDirectoryName(ifcStore.FileName);
                        fileName = string.Format("{0}-100%.ifc", fileName);
                        var newName = Path.Combine(dirName, fileName);
                        if (mergeIfc != null)
                        {
                            mergeIfc.SaveAs(newName);
                            mergeIfc.Dispose();
                        }
                        else
                        {
                            ifcStore.SaveAs(newName);
                        }
                        var res = UploadFileToDBPlatform(prjKey, newName);
                        if(string.IsNullOrEmpty(res))
                            MessageBox.Show($"上传成功!", "操作提醒", MessageBoxButton.OK);
                        else
                            MessageBox.Show(res, "操作提醒", MessageBoxButton.OK);
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show("上传失败,未能成功上传！", "操作提醒", MessageBoxButton.OK);
                    }
                }
            }
        }
        private string UploadFileToDBPlatform(string prjKey,string localIFCFilePath) 
        {
            if (string.IsNullOrEmpty(localIFCFilePath) || !File.Exists(localIFCFilePath))
                return "IFC文件不存在，无法进行上传操作";
            //检查系统Viewer并启动Viewer
            bool haveViewer = HaveProcess("lbviewer");
            if (!haveViewer) 
            {
                var lbViewerPath = @"D:\QDBIM\QDBIM\DXMX\LbViewer\LbViewer.exe";
                if (!File.Exists(lbViewerPath)) 
                {
                    return "上传失败，本机中没有协同的三维平台";
                }
                Process MyProcess = new Process();
                MyProcess.StartInfo.FileName = lbViewerPath;//外部程序路径
                MyProcess.StartInfo.Verb = "Open";
                MyProcess.Start();
                //这里等5秒，防止程序没有启动起来后续调用会报错
                Thread.Sleep(5000);
            }
            //上传文件到协同项目中
            var docCache = documentModelCache[prjKey];
            UploadFileToDB uploadFileToDB = new UploadFileToDB();
            var prjs = uploadFileToDB.GetDBProject();
            var prj = prjs.Where(c => c.PrjNo == docCache.ParentProject.PrjNum).FirstOrDefault();
            if (prj == null)
                return "上传失败，未在协同服务器上获取到相应的项目";
            var allSubPrjs = uploadFileToDB.GetSubProjects(prj.Id);
            var subPrj = allSubPrjs.Where(c => c.SubEntryName == docCache.MainModel.SubPrjName).First();
            if (null == subPrj)
                return "上传失败，未在协同服务器上获取到相应的项目";
            var getFiles = uploadFileToDB.GetSubProjectFiles(subPrj.Id);
            var sMajorFiles = getFiles.Where(c => c.Major == "A").ToList();
            if (getFiles.Count < 1)
                return "上传失败，未在协同服务器上获取到相应的项目";
            if (sMajorFiles.Count < 1)
                return "上传失败，项目中未找到结构的相关文件，目前上传只针对结构专业的文件绑定";
            var fileNameWithOutExt = Path.GetFileNameWithoutExtension(localIFCFilePath);
            var oldFile = getFiles.Where(c => c.FileName == fileNameWithOutExt).FirstOrDefault();
            var uploadRes = "";
            //判断是否已经上传过，
            if (null != oldFile)
            {
                //上传过后续进行更新
                uploadRes = uploadFileToDB.FileUpdateToDB(prj, subPrj, oldFile, localIFCFilePath);
            }
            else
            {
                //未上传过，需要选择绑定的文件
                UploadDBFile uploadDBFile = new UploadDBFile(sMajorFiles);
                uploadDBFile.Owner = this;
                var res = uploadDBFile.ShowDialog();
                if (res != true)
                    return "上传失败,未选择绑定的数据！";
                //进行绑定
                oldFile = uploadDBFile.GetSelectFile();
                uploadRes = uploadFileToDB.FileUploadToDB(prj, subPrj, oldFile, localIFCFilePath);
            }
            if (!string.IsNullOrEmpty(uploadRes))
                return string.Format("上传失败 {0}", uploadRes);
            return string.Empty;
        }

        private bool HaveProcess(string processName) 
        {
            Process[] ps = Process.GetProcesses();
            foreach (Process p in ps)
            {
                try
                {
                    if (p.ProcessName.ToLower() == processName)
                    {
                        return true;
                    }
                }
                catch { }
            }
            return false;
        }
        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {
            if (CurrentDocument == null)
                return;
            /*
            var architectureIFC = CurrentDocument.AllBimProjects.FirstOrDefault(o => o.Major == EMajor.Architecture && o.ApplcationName == EApplcationName.CAD && o.SourceProject is IfcStore);
            if(architectureIFC == null)
            {
                MessageBox.Show("导入SU失败,未找到相应的建筑外链！", "导入失败", MessageBoxButton.OK);
                return;
            }
            var project = IFCReverse.ReverseSU(architectureIFC.SourceProject as IfcStore);
            
            ProtobufMessage message = new ProtobufMessage();
            MessageHeader header = new MessageHeader();
            header.Major = "建筑";
            header.Source = MessageSourceEnum.Platform3D;
            message.Header = header;
            message.SuProjects.Add(project);
            var CTS = new System.Threading.CancellationTokenSource();
            var Token = CTS.Token;
            //这里有一个小坑，只有设置了PipeOptions.Asynchronous，管道才会接受取消令牌的取消请求，不然不会生效
            var pipeServer = new System.IO.Pipes.NamedPipeServerStream("THCAD2SUPIPE", System.IO.Pipes.PipeDirection.InOut, 1, System.IO.Pipes.PipeTransmissionMode.Message, System.IO.Pipes.PipeOptions.Asynchronous);
            var task = new System.Threading.Tasks.Task(() =>
            {
                try
                {
                    pipeServer.WaitForConnection();
                    var bytes = message.ToByteArray();
                    pipeServer.Write(bytes, 0, bytes.Length);

                    pipeServer.Close();
                    pipeServer.Dispose();
                }
                catch (System.Exception ex)
                {
                    //线程被外部取消，说明等待连接超时
                    pipeServer.Dispose();
                }
            }, Token);
            task.Start();
            task.Wait(10000);
            if (task.Status == System.Threading.Tasks.TaskStatus.Running)
            {
                CTS.Cancel();
                pipeServer.Close();
                pipeServer.Dispose();
                //Active.Database.GetEditor().WriteMessage("未连接到SU ！\r\n");
            }
            else if (task.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
            {
                MessageBox.Show("导入SU成功！", "导入成功", MessageBoxButton.OK);
            }
            else
            {
                MessageBox.Show("导入SU失败！", "导入失败", MessageBoxButton.OK);
            }*/
        }


        private void ExportCut_Click(object sender, RoutedEventArgs e)
        {
            //导出切图数据
            if (CurrentDocument == null || CurrentDocument.AllBimProjects.Count < 1)
                return;
            ThBimCutData.Run(CurrentDocument.AllBimProjects);
        }
        
        private void ExportCut_Click_structure(object sender, RoutedEventArgs e)
        {
            if (CurrentDocument == null || CurrentDocument.AllBimProjects.Count < 1)
                return;
            var prjName = CurrentDocument.AllBimProjects.First().ProjectIdentity.Split('.').First()+"-100%.ifc";

            CurrentDocument.DocumentChanged -= RunCutData_DocumentChanged;
            CurrentDocument.ClearAllData();
            CurrentDocument.DocumentChanged += RunCutData_DocumentChanged;
            //LoadFileToCurrentDocument(prjName,null);
            //ThBimCutData.Run(CurrentDocument.AllBimProjects);
        }

        private void RunCutData_DocumentChanged(object sender, EventArgs e)
        {
            ThBimCutData.Run(CurrentDocument.AllBimProjects);
            CurrentDocument.DocumentChanged -= RunCutData_DocumentChanged;
        }

        private void ExportCut_Click_architecture(object sender, RoutedEventArgs e)
        {
            //导出建筑切图数据
            if (CurrentDocument == null || CurrentDocument.AllBimProjects.Count < 1)
                return;
            ThBimCutData.Run(CurrentDocument.AllBimProjects);
        }
        Dictionary<string, DocumentCacheModel> documentModelCache;
        private void mItemProject_Click(object sender, RoutedEventArgs e)
        {
            if (null == loginUser)
                return;
            if (null == documentModelCache)
                documentModelCache = new Dictionary<string, DocumentCacheModel>();
            var projectManage = new ProjectManage(loginUser);
            projectManage.Owner = this;
            var res = projectManage.ShowDialog();
            if (res.Value != true)
                return;
            var operateType = projectManage.GetOperateType();
            if (operateType == OperateType.Close)
                return;
            var pPrj = projectManage.GetSelectPrjSubPrj(out ShowProject subPrj, out List<ShowProject> allSubPrjs, out string prjLocalPath);
            if (pPrj == null)
                return;
            var selectProjectFile = projectManage.GetOpenModel();
            if (selectProjectFile == null)
                return;
            SetOpenedModelFileName(string.Format("{0}_{1}_{2}", pPrj.ShowName,subPrj.ShowName, selectProjectFile.ShowFileName));
            var id = string.Format("{0}_{1}_{2}", pPrj.PrjId, subPrj.PrjId, selectProjectFile.LoaclPath);
            //检查Document删除和增加的数据
            List<THDocument> rmDocs = new List<THDocument>();
            foreach (var item in DocumentManager.AllDocuments) 
            {
                if (item.DocumentId == id)
                    rmDocs.Add(item);
            }
            foreach (var item in rmDocs)
                DocumentManager.RemoveDoucment(item);
            THDocument addDoc = new THDocument(id, subPrj.ShowName, ProgressChanged, Log);
            addDoc.ProjectLoaclPath = prjLocalPath;
            DocumentManager.AddNewDoucment(addDoc);
            DocumentManager.CurrentDocument = addDoc;
            var loadPrjs = new List<ProjectParameter>();
            loadPrjs.Add(new ProjectParameter()
            {
                OpenFilePath = selectProjectFile.LinkFilePath,
                ProjectId = selectProjectFile.LinkFilePath,
                Major = selectProjectFile.Major,
                Source = selectProjectFile.ApplcationName,
            }) ;
            DocumentCacheModel docCache;
            if (documentModelCache.ContainsKey(id))
            {
                docCache = documentModelCache[id];
            }
            else 
            {
                docCache = new DocumentCacheModel(id, selectProjectFile, prjLocalPath);
                docCache.ParentProject = pPrj;
                docCache.SubProject = subPrj;
                documentModelCache.Add(id, docCache);
            }
            foreach (var linkModel in docCache.DocExternalLink.LinkModels)
            {
                var openParameter = new ProjectParameter()
                {
                    OpenFilePath = linkModel.Project.LinkFilePath,
                    ProjectId = linkModel.Project.LinkFilePath,
                    Matrix3D = linkModel.MoveMatrix3D,
                    Major = linkModel.Project.Major,
                    Source = linkModel.Project.ApplcationName,
                    SourceShowName = linkModel.Project.ShowSourceName,
                };
                loadPrjs.Add(openParameter);
            }
            LoadFilesToCurrentDocument(loadPrjs);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (null == CurrentDocument || CurrentDocument.AllBimProjects.Count < 1)
                return;
            Dictionary<Type, int> typeCount = new Dictionary<Type, int>();
            foreach (var project in CurrentDocument.AllBimProjects) 
            {
                var res = project.ProjrctEntityTypeCounts();
                if (null == res || res.Count < 1)
                    continue;
                foreach (var keyValue in res) 
                {
                    var key = keyValue.Key;
                    var keyCount = keyValue.Value.Count;
                    if (typeCount.ContainsKey(key))
                    {
                        typeCount[key] += keyCount;
                    }
                    else 
                    {
                        typeCount.Add(key, keyCount);
                    }
                }
            }
            int sumCount = 0;
            foreach (var keyValue in typeCount)
            {
                var showTypeName = keyValue.Key.Name;
                var showCount = keyValue.Value;
                sumCount += showCount;
                Log.Info(string.Format("Type : {0} Count : {1}", showTypeName, showCount));
            }
            Log.Info(string.Format("Total Count : {0}", sumCount));
            MessageBox.Show("统计完成，请前往日志中查看结果","提醒");
        }

        private void MenuItem_Deduct_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentDocument == null)
                return;

            var deductService = new DeductEngine(CurrentDocument);

            //deductService.Do();

            //var projectParameter = new ProjectParameter()
            //{
            //    ProjectId = aProject.ProjectIdentity,
            //    Matrix3D = aProject.Matrix3D,
            //    Major = aProject.Major,
            //    Source = aProject.ApplcationName,
            //};
            //CurrentDocument.AddProject(deductService.ArchiProject, projectParameter);

            var aProject = CurrentDocument.AllBimProjects.Where(x => x.Major == EMajor.Architecture && x.ApplcationName == EApplcationName.CAD).FirstOrDefault();
            GFCConvertEngine.ToGFCEngine(aProject);

        }

        private void MenuItem_Deduct_Open_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentDocument == null)
                return;

            var aPath = @"D:\project\14.ThBim\chart\建筑 -带轴网.thbim";
            LoadFileToCurrentDocument(new ProjectParameter()
            {
                OpenFilePath = aPath,
                ProjectId = aPath,
                Major = EMajor.Architecture,
                Source = EApplcationName.CAD,
            });

            //var sPath = @"D:\project\14.ThBim\chart\0929-结构32.ifc";
            //LoadFileToCurrentDocument(new ProjectParameter()
            //{
            //    OpenFilePath = sPath,
            //    ProjectId = sPath,
            //    Major = EMajor.Structure,
            //    Source = EApplcationName.IFC,
            //});

        }
    }
    class DocumentCacheModel
    {
        public string DocumentId { get; }
        public string RootPath { get; }
        public ShowProject ParentProject { get; set; }
        public ShowProject SubProject { get; set; }
        public ProjectFileInfo MainModel { get; }
        public List<ProjectFileInfo> ProjectAllFileCache { get; protected set; }
        public FileExternalLink DocExternalLink { get;}
        public DocumentCacheModel(string docId, ProjectFileInfo mainModel,string rootPath) 
        {
            DocumentId = docId;
            MainModel = mainModel;
            RootPath = rootPath;
            ProjectAllFileCache = new List<ProjectFileInfo>();
            DocExternalLink = new FileExternalLink(mainModel.LoaclPath, mainModel.ExternalLinkPath);
            UpdateCacheFile();
        }
        public void UpdateCacheFile() 
        {
            FileProject fileProject = new FileProject(RootPath);
            ProjectAllFileCache = fileProject.GetProjectFiles();
        }
        public ProjectFileInfo GetProjectFileInfo(string filePath) 
        {
            foreach(var item in ProjectAllFileCache) 
            {
                if (item.LoaclPath == filePath)
                    return item;
            }
            return null;
        }
    }
}
