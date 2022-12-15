using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Diagnostics;
using System.ComponentModel;

using Xbim.IO;
using Xbim.Ifc;
using Xbim.Common.Step21;
using Xbim.Common.Geometry;
using Xbim.ModelGeometry.Scene;

using THBimEngine.Presention;
using XbimXplorer.ThBIMEngine;
using THBimEngine.Application;
using System.Collections.Generic;
using System.Threading;

namespace XbimXplorer
{
    public partial class XplorerMainWindow
    {
        ProjectParameter openParameter;
        public void LoadFileToCurrentDocument(ProjectParameter openFileParameter)
        {
            openParameter = openFileParameter;
            string filePath = openFileParameter.OpenFilePath;
            var fInfo = new FileInfo(filePath);
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
            else if (ext == ".thbim")
            {
                LoadTHBimFile(filePath);
            }
            else if (ext == ".ydb") 
            {
                LoadYJKYDBFile(filePath);
            }
            else
            {
                // there's no going back; if it fails after this point the current file should be closed anyway
                CloseAndDeleteTemporaryFiles();
                //SetOpenedModelFileName(filePath.ToLower());
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
                        _loadFileBackgroundWorker.RunWorkerAsync(filePath);
                        break;
                    default:
                        Log.WarnFormat("Extension '{0}' has not been recognised.", ext);
                        break;
                }
            }
        }

        public void LoadStreamToCurrentDocument(StreamParameter streamParameter)
        {
            CloseAndDeleteTemporaryFiles();
            ProgressStatusBar.Visibility = Visibility.Visible;
            _loadStreamBackgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _loadStreamBackgroundWorker.ProgressChanged += OnProgressChanged;
            _loadStreamBackgroundWorker.DoWork += LoadStreamBackgroundWorker_DoWork;
            _loadStreamBackgroundWorker.RunWorkerCompleted += FileLoadCompleted;
            _loadStreamBackgroundWorker.RunWorkerAsync(streamParameter);
        }

        BackgroundWorker mutilLoadBWorker;
        List<ProjectParameter> openProjects;
        void LoadFilesToCurrentDocument(List<ProjectParameter> openFileParameters)
        {
            if (null == openFileParameters || openFileParameters.Count<1 || mutilLoadBWorker != null)
                return;
            openProjects = new List<ProjectParameter>();
            openProjects.AddRange(openFileParameters);
            mutilLoadBWorker = new BackgroundWorker();
            mutilLoadBWorker.DoWork += MutilLoadBWorker_DoWork;
            mutilLoadBWorker.RunWorkerCompleted += MutilLoadBWorker_RunWorkerCompleted;
            mutilLoadBWorker.RunWorkerAsync();
        }

        private void MutilLoadBWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (null == openProjects || openProjects.Count < 1)
            {
                mutilLoadBWorker = null;
                return;
            }
            var loadPrj = openProjects.First();
            openProjects.Remove(loadPrj);
            LoadFileToCurrentDocument(loadPrj);
            mutilLoadBWorker.RunWorkerAsync();
        }

        private void MutilLoadBWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                if (null == _loadFileBackgroundWorker)
                    break;
                if (!_loadFileBackgroundWorker.IsBusy)
                    break;
                Thread.Sleep(1000);
            }
        }

        private void LoadStreamBackgroundWorker_DoWork(object sender, DoWorkEventArgs args)
        {
            var worker = sender as BackgroundWorker;
            var streamParameter = args.Argument as StreamParameter;
            var startTime = DateTime.Now;
            try
            {
                if (worker == null)
                    throw new Exception("Background thread could not be accessed");
                var model = IfcStore.Open(streamParameter.IOStream, streamParameter.StorageType, streamParameter.SchemaVersion, streamParameter.ModelType, null, FileAccessMode, worker.ReportProgress);
                model.FileName = Guid.NewGuid().ToString();
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
                            sb.AppendLine($"Error creating geometry context of {geomEx.StackTrace}.");
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
                            //SetOpenedModelFileName(null);
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
                args.Result = model;
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Error opening stream fail");
                var newexception = new Exception(sb.ToString(), ex);
                Log.Error(sb.ToString(), ex);
                args.Result = newexception;
            }
            var endTime = DateTime.Now;
            var totalTime = (endTime - startTime).TotalSeconds;
            Log.Info(string.Format("Ifc数据解析完成，耗时：{0}s", totalTime));
        }

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
                //SetOpenedModelFileName(selectedFilename);
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
                            //SetOpenedModelFileName(null);
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
                if (DataHead[0] == 84 && DataHead[1] == 72 && DataHead[2] == 3)
                {
                    switch (DataHead[3])
                    {
                        case 1:
                            {
                                //CAD THBim 文件
                                var DataBody = fileArray.Skip(10).ToArray();
                                var th_Project = new ThTCHProjectData();
                                Google.Protobuf.MessageExtensions.MergeFrom(th_Project, DataBody);
                                th_Project.Root.GlobalId = fileName;
                                CurrentDocument.AddProject(th_Project, openParameter);
                                break;
                            }
                        case 2:
                            {
                                //SU THBim 文件
                                var DataBody = fileArray.Skip(10).ToArray();
                                var su_Project = new ThSUProjectData();
                                Google.Protobuf.MessageExtensions.MergeFrom(su_Project, DataBody);
                                su_Project.Root.GlobalId = fileName;
                                CurrentDocument.AddProject(su_Project, openParameter);
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
                Log.Error(string.Format("文件解析失败:{0}", ex.Message));
            }
            finally
            {
                r.Dispose();
            }
        }
        private void LoadYJKYDBFile(string fileName) 
        {
            if (string.IsNullOrEmpty(fileName) || !File.Exists(fileName))
                return;
            var thYDBToIfcConvert = new ThYDBToIfcConvertService();
            var ifcPath = thYDBToIfcConvert.Convert(fileName);
            if (string.IsNullOrEmpty(ifcPath) || !File.Exists(fileName))
            {
                MessageBox.Show("打开YDB失败!", "打开文件说明", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var openFileParameter = new ProjectParameter(ifcPath, THBimEngine.Domain.EMajor.Structure, THBimEngine.Domain.EApplcationName.IFC);
            LoadFileToCurrentDocument(openFileParameter);
        }
        private void FileLoadCompleted(object s, RunWorkerCompletedEventArgs args)
        {
            if (args.Result is IfcStore ifcStore) //all ok
            {
                DateTime startTime = DateTime.Now;
                CurrentDocument.AddProject(ifcStore, openParameter);
                DateTime endTime = DateTime.Now;
                var totalTime = (endTime - startTime).TotalSeconds;
                Log.Info(string.Format("数据解析完成，耗时：{0}s", totalTime));
                ProgressBar.Value = 0;
                StatusMsg.Text = "";
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
                //SetOpenedModelFileName("");
            }
            FireLoadingComplete(s, args);
        }
    }

    public class StreamParameter
    {
        public Stream IOStream { get; }
        public IfcStorageType StorageType { get; set; }
        public IfcSchemaVersion SchemaVersion { get; set; }
        public XbimModelType ModelType { get; set; }
        public StreamParameter(Stream stream, IfcStorageType storageType, IfcSchemaVersion schemaVersion, XbimModelType modelType)
        {
            IOStream = stream;
            StorageType = storageType;
            SchemaVersion = schemaVersion;
            ModelType = modelType;
        }
    }
}
