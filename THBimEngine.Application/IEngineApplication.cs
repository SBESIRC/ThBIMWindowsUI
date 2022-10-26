using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using THBimEngine.Domain;
using Xbim.Common.Geometry;

namespace THBimEngine.Application
{
    public interface IEngineApplication
    {
        /// <summary>
        /// 当前Scene,用来显示时使用的
        /// </summary>
        THBimScene CurrentScene { get; set; }
        /// <summary>
        /// 当前Document
        /// </summary>
        THDocument CurrentDocument { get; set; }
        /// <summary>
        /// 所有Document
        /// </summary>
        THDocumentManage DocumentManage { get; set; }
        /// <summary>
        /// 加载文件到当前Document中
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="matrix3D"></param>
        void LoadFileToCurrentDocument(string filePath, XbimMatrix3D? matrix3D);
        /// <summary>
        /// 移除当前Document中的一个Project
        /// </summary>
        /// <param name="projId"></param>
        void RemoveProjectFormCurrentDocument(string projId);
        
        #region 引擎相关
        /// <summary>
        /// 隔离显示，要显示的ids
        /// </summary>
        /// <param name="showEntityIds"></param>
        void ShowEntityByIds(List<int> showEntityIds);
        /// <summary>
        /// 隔离显示，要显示的轴网Ids
        /// </summary>
        /// <param name="gridEntityIds"></param>
        void ShowGridByIds(List<string> gridEntityIds);
        /// <summary>
        /// 渲染当前Document
        /// </summary>
        void RenderScene();
        /// <summary>
        /// 选中相应的构件（Mesh后的Id,非文件数据中的Id）
        /// </summary>
        /// <param name="selectIds"></param>
        void SelectEntityIds(List<int> selectIds);
        /// <summary>
        /// 获取选中的构件Id
        /// </summary>
        /// <returns></returns>
        int GetSelectId();
        /// <summary>
        /// zoom到某些构件（如果传入null或空数据，整个模型进行zoom）
        /// </summary>
        /// <param name="roomIds"></param>
        void ZoomEntitys(List<int> roomIds);
        #endregion

        #region 事件
        /// <summary>
        /// 实体选择改变事件
        /// </summary>
        event EventHandler SelectEntityChanged;
        /// <summary>
        /// 关闭退出事件（不允许取消）
        /// </summary>
        event EventHandler ApplicationClosing;
        /// <summary>
        /// 进度条事件
        /// </summary>
        event ProgressChangedEventHandler ProgressChanged;
        #endregion

        #region 辅助相关
        ILog Log { get; set; }
        #endregion
    }
}
