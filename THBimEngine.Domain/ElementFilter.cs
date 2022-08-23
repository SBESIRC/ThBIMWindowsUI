﻿using System;
using System.Collections.Generic;

namespace THBimEngine.Domain
{
    public abstract class FilterBase 
    {
        public string Describe { get; set; }
        protected HashSet<string> acceptProjectIds{ get; }
        protected HashSet<string> acceptBuildingIds { get; }
        protected HashSet<string> acceptSiteIds { get; }
        protected HashSet<string> acceptStoreyIds { get; }
        protected HashSet<Type> acceptElementTypes { get; }
        public HashSet<string> ResultElementUids { get; }
        public FilterBase()
        {
            acceptProjectIds = new HashSet<string>();
            acceptBuildingIds = new HashSet<string>();
            acceptSiteIds = new HashSet<string>();
            acceptStoreyIds = new HashSet<string>();
            acceptElementTypes = new HashSet<Type>();
            ResultElementUids = new HashSet<string>();
        }
        public bool ProjectValid { get; protected set; }
        public virtual bool CheckProject(THBimProject bimProject) 
        {
            ProjectValid = true;
            if (acceptBuildingIds.Count < 1)
                return true;
            var id = bimProject.ProjectIdentity;
            ProjectValid = acceptBuildingIds.Contains(id);
            return ProjectValid;
        }
        public bool SiteValid { get; protected set; }
        public virtual bool CheckSite(THBimSite site) 
        {
            if (acceptSiteIds.Count < 1)
                return true;
            var siteId = site.Uid;
            SiteValid = acceptSiteIds.Contains(siteId);
            return SiteValid;
        }
        public bool BuildingValid { get; protected set; }
        public virtual bool CheckBuilding(THBimBuilding bimBuilding)
        {
            if (acceptBuildingIds.Count < 1)
                return true;
            var id = bimBuilding.Uid;
            BuildingValid = acceptBuildingIds.Contains(id);
            return BuildingValid;
        }
        public bool StoreyValid { get; protected set; }
        public virtual bool CheckStory(THBimStorey bimStorey) 
        {
            if (acceptStoreyIds.Count < 1)
                return true;
            var id = bimStorey.Uid;
            StoreyValid = acceptStoreyIds.Contains(id);
            return StoreyValid;
        }
        public virtual bool CheckType(THBimElement bimEntity)
        {
            if (acceptElementTypes.Count < 1)
                return true;
            var type = bimEntity.GetType();
            return acceptElementTypes.Contains(type);
        }
    }
    public class ProjectFilter : FilterBase
    {
        public ProjectFilter(List<string> prjIds) : base()
        {
            AcceptProjectIds(prjIds);
        }
        public void AddAcceptProjectIds(List<string> prjIds) 
        {
            AcceptProjectIds(prjIds);
        }
        void AcceptProjectIds(List<string> prjIds)
        {
            if (null == prjIds || prjIds.Count < 1)
                return;
            foreach (var item in prjIds)
            {
                if (acceptProjectIds.Contains(item))
                    continue;
                acceptProjectIds.Add(item);
            }
        }
    }
    public class StoreyFilter : FilterBase
    {
        public StoreyFilter(List<string> storeyIds)
        {
            AcceptSiteIds(storeyIds);
        }
        public void AddAcceptStoretIds(List<string> storeyIds)
        {
            AcceptSiteIds(storeyIds);
        }
        void AcceptSiteIds(List<string> storeyIds)
        {
            if (null == storeyIds || storeyIds.Count < 1)
                return;
            foreach (var item in storeyIds)
            {
                if (acceptStoreyIds.Contains(item))
                    continue;
                acceptStoreyIds.Add(item);
            }
        }
    }
    public class TypeFilter : FilterBase
    {
        public TypeFilter(List<Type> targetTypes) 
        {
            AcceptTypes(targetTypes);
        }
        public void AddAcceptTypes(List<Type> targetTypes) 
        {
            AcceptTypes(targetTypes);
        }
        void AcceptTypes(List<Type> targetTypes) 
        {
            if (null == targetTypes || targetTypes.Count < 1)
                return;
            foreach (var item in targetTypes)
            {
                if (acceptElementTypes.Contains(item))
                    continue;
                acceptElementTypes.Add(item);
            }
        }
    }

}
