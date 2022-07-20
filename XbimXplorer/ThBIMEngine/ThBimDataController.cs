using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using THBimEngine.Domain;
using THBimEngine.Domain.Model;

namespace XbimXplorer.ThBIMEngine
{
    class ThBimDataController
    {
        private int _globalIndex = 0;
        private Dictionary<string, THBimEntity> _allEntitys { get; }
        private List<ThTCHProject> _allProjects { get; }
        private List<THBimProject> _allBimProject { get; }
        public ThBimDataController(List<ThTCHProject> projects) 
        {
            _allProjects = new List<ThTCHProject>();
            _allEntitys = new Dictionary<string, THBimEntity>();
            _globalIndex = 0;

            ConvertProjectToTHBimProject(projects);
        }
        public void AddProject(ThTCHProject project) 
        {
            bool isAdd = true;
            foreach (var item in _allProjects) 
            {
                if(item.ProjectName == project.ProjectName)
                {
                    isAdd = false;
                    break;
                }
            }
            if (isAdd)
            {
                _allProjects.Add(project);
                ConvertProjectToTHBimProject(new List<ThTCHProject> { project });
            }
        }

        public void DeleteProject() 
        {
            
        }
        public void UpdateProject() 
        {
        
        }
        public void UpdateElement() 
        {
        
        }
        public void ClearAllProject() 
        {
            _allProjects.Clear();
            _allEntitys.Clear();
            _allBimProject.Clear();
            _globalIndex = 0;
        }
        private void ConvertProjectToTHBimProject(List<ThTCHProject> projects) 
        {
            if (null == projects || projects.Count < 1)
                return;
            foreach (var project in projects) 
            {
                THBimProject bimProject = new THBimProject(CurrentGIndex(), project.ProjectName);
                AddElementIndex();
                THBimSite bimSite = new THBimSite(CurrentGIndex(), "");
                AddElementIndex();
                THBimBuilding bimBuilding = new THBimBuilding(CurrentGIndex(), project.Site.Building.BuildingName);
                foreach (var storey in project.Site.Building.Storeys) 
                {
                    var bimStory = new THBimStorey(CurrentGIndex(), storey.Number, storey.Elevation, storey.Height);
                    AddElementIndex();
                    foreach (var wall in storey.Walls) 
                    {
                        var bimWall = new THBimWall(CurrentGIndex(), string.Format("wall#{0}", CurrentGIndex()), wall.WallGeometryParam());
                        AddElementIndex();
                        _allEntitys.Add(bimWall.Uid, bimWall);
                        foreach (var door in wall.Doors) 
                        {
                            var bimDoor = new THBimDoor(CurrentGIndex(), string.Format("door#{0}", CurrentGIndex()), door.DoorGeometryParam());
                            _allEntitys.Add(bimDoor.Uid, bimDoor);
                            AddElementIndex();
                        }
                        foreach (var window in wall.Windows)
                        {
                            var bimWindow = new THBimDoor(CurrentGIndex(), string.Format("door#{0}", CurrentGIndex()), window.WindowGeometryParam());
                            _allEntitys.Add(bimWindow.Uid, bimWindow);
                            AddElementIndex();
                        }
                        foreach (var opening in wall.Openings)
                        {
                            var bimOpening = new THBimDoor(CurrentGIndex(), string.Format("opening#{0}", CurrentGIndex()), opening.OpeningGeometryParam());
                            _allEntitys.Add(bimOpening.Uid, bimOpening);
                            AddElementIndex();
                        }
                    }

                    bimBuilding.BuildingStoreys.Add(bimStory);
                }
                bimSite.SiteBuildings.Add(bimBuilding);
                bimProject.ProjectSite = bimSite;
                _allBimProject.Add(bimProject);
            }
        }
        private void MeshBimEntity(THBimEntity bimEntity) 
        {
            
        }
        private void AddElementIndex(int addCount =1) 
        {
            _globalIndex += addCount;
        }
        private int CurrentGIndex() 
        {
            return _globalIndex;
        }
    }
}
