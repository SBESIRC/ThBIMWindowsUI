using System;
using System.Collections.Generic;
using THBimEngine.Domain;
using Xbim.Common.Geometry;

namespace THBimEngine.Application
{
    public interface IEngineApplication
    {
        /// <summary>
        /// 当前Scene
        /// </summary>
        THBimScene CurrentScene { get; set; }
        /// <summary>
        /// 当前Document
        /// </summary>
        THDocument CurrentDocument { get; set; }
        /// <summary>
        /// 所有Document
        /// </summary>
        List<THDocument> AllDocuments { get; set; }
        void AddProjectToCurrentScene(THBimProject bimProject);
        /// <summary>
        /// 加载文件
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="matrix3D"></param>
        void LoadFileToCurrentDocument(string filePath, XbimMatrix3D? matrix3D);
        /// <summary>
        /// 移除当前Document中的一个Project
        /// </summary>
        /// <param name="projId"></param>
        void RemoveProjectFormCurrentDocument(string projId);
        
        #region
        /// <summary>
        /// 隔离显示，要显示的ids
        /// </summary>
        /// <param name="showEntityIds"></param>
        void ShowEntityByIds(List<int> showEntityIds);
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
        /// Document切换事件
        /// </summary>
        event EventHandler SelectDocumentChanged;
        /// <summary>
        /// Document修改事件
        /// </summary>
        event EventHandler DocumentChanged;
        #endregion
    }
}
