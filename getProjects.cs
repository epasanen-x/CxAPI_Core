
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using CxAPI_Core.dto;

namespace CxAPI_Core
{
    class getProjects
    {
        public List<ScanObject> CxScans;
        public List<ProjectObject> CxProjects;
        public Dictionary<Guid, Teams> CxTeams;
        public Dictionary<long, Presets> CxPresets;
        public Dictionary<long, ScanSettings> CxSettings;
        public Dictionary<long, ScanStatistics> CxResultStatistics;

        public getProjects(resultClass token)
        {
            CxResultStatistics = new Dictionary<long, ScanStatistics>();
            CxSettings = new Dictionary<long, ScanSettings>();
            CxProjects = projectId_in_projects(token, token.project_name);
            CxTeams = get_team_list(token);
            CxPresets = get_preset_list(token);
        }

        public List<ProjectObject> get_projects(resultClass token)
        {
            get httpGet = new get();
            List<ProjectObject> pclass = new List<ProjectObject>();
            secure token_secure = new secure(token);
            token_secure.findToken(token);
            string path = token_secure.get_rest_Uri(CxConstant.CxAllProjects);
            if (token.debug && token.verbosity > 1) { Console.WriteLine("API: {0}", path); }
            httpGet.get_Http(token, path);
            if (token.status == 0)
            {
                pclass = JsonConvert.DeserializeObject<List<ProjectObject>>(token.op_result);
            }

            return pclass;
        }

        public List<Presets> get_presets(resultClass token)
        {
            get httpGet = new get();
            List<Presets> pclass = new List<Presets>();

            secure token_secure = new secure(token);
            token_secure.findToken(token);
            string path = token_secure.get_rest_Uri(CxConstant.CxPresets);
            if (token.debug && token.verbosity > 1) { Console.WriteLine("API: {0}", path); }
            httpGet.get_Http(token, path);
            if (token.status == 0)
            {
                pclass = JsonConvert.DeserializeObject<List<Presets>>(token.op_result);
            }
            return pclass;
        }

        public Dictionary<long, ProjectObject> get_all_projects(resultClass token)
        {
            List<ProjectObject> projects = get_projects(token);
            Dictionary<long, ProjectObject> project_dictionary = new Dictionary<long, ProjectObject>();
            foreach (ProjectObject project in projects)
            {
                if (!project_dictionary.ContainsKey(Convert.ToInt64(project.id)))
                {
                    project_dictionary.Add(Convert.ToInt64(project.id), project);
                }
            }

            return project_dictionary;
        }

        public List<ScanObject> filter_by_projects(resultClass token)
        {
            filter_scans(token, token.max_scans);
            teamId_in_scan(token, CxScans, token.team_name);
            presetId_in_scan(token, CxScans, token.preset);
            return CxScans;
        }

        public Dictionary<Guid, Teams> get_team_list(resultClass token)
        {
            getScans scans = new getScans();
            Dictionary<Guid, Teams> result = new Dictionary<Guid, Teams>();
            List<Teams> tclass = scans.getTeams(token);
            foreach (Teams t in tclass)
            {
                if (!result.ContainsKey(t.id))
                {
                    result.Add(t.id, t);
                }
            }
            return result;
        }

        public Dictionary<long, Presets> get_preset_list(resultClass token)
        {
            getScans scans = new getScans();
            Dictionary<long, Presets> result = new Dictionary<long, Presets>();
            List<Presets> presets = get_presets(token);
            foreach (Presets p in presets)
            {
                if (!result.ContainsKey(Convert.ToInt64(p.id)))
                {
                    result.Add(Convert.ToInt64(p.id), p);
                }
            }
            return result;
        }

        public bool filter_scans(resultClass token, int max_scans)
        {
            getScans scans = new getScans();
            List<ScanObject> outScans = new List<ScanObject>();
            List<ScanObject> pscans = new List<ScanObject>();

            foreach (ProjectObject project in CxProjects)
            {
                List<ScanObject> temp = (max_scans > 0) ? scans.getLastScanbyId(token, project.id, max_scans) : scans.getScanbyId(token, project.id);
                pscans.AddRange(temp);
            }
            foreach (ScanObject s in pscans)
            {
                if ((s.DateAndTime != null) && (s.Status.Id == 7) && (s.DateAndTime.StartedOn > token.start_time) && (s.DateAndTime.StartedOn < token.end_time))
                {
                    get_result_statistics(token, s.Id);
                    outScans.Add(s);
                }

            }
            CxScans = outScans;
            return true;
        }

        /*
               public List<ScanObject> projectId_in_scan(List<ScanObject> scans, string project_name)
               {
                   if (String.IsNullOrEmpty(project_name))
                   {
                       return scans;
                   }
                   List<ScanObject> pclass = new List<ScanObject>();
                   foreach (ScanObject scan in scans)
                   {
                       if (scan.Project.Name.Contains(project_name))
                       {
                           pclass.Add(scan);
                       }
                   }
                   return pclass;
               }

       */
        public bool get_result_statistics(resultClass token, long scanId)
        {
            getScans scans = new getScans();
            if (!CxSettings.ContainsKey(scanId))
            {
                ScanStatistics scanStatistics = scans.getScansStatistics(scanId, token);
                CxResultStatistics.Add(scanId, scanStatistics);
            }
            return true;
        }
        public bool get_scan_settings(resultClass token, long projectId)
        {
            getScans scans = new getScans();
            if (!CxSettings.ContainsKey(projectId))
            {
                ScanSettings scanSettings = scans.getScanSettings(token, projectId.ToString());
                CxSettings.Add(projectId, scanSettings);
            }
            return true;
        }

        public List<ProjectObject> projectId_in_projects(resultClass token, string project_name)
        {
            getScans scans = new getScans();
            List<ProjectObject> projectObjects = get_projects(token);

            if (String.IsNullOrEmpty(project_name))
            {
                foreach (ProjectObject project in projectObjects)
                {
                    if (!CxSettings.ContainsKey(Convert.ToInt64(project.id)))
                    {
                        ScanSettings scanSettings = scans.getScanSettings(token, project.id);
                        CxSettings.Add(Convert.ToInt64(project.id), scanSettings);
                    }
                }
                return projectObjects;
            }
            else
            {
                List<ProjectObject> pclass = new List<ProjectObject>();
                foreach (ProjectObject project in projectObjects)
                {
                    if (project.name.Contains(project_name))
                    {
                        if (!CxSettings.ContainsKey(Convert.ToInt64(project.id)))
                        {
                            ScanSettings scanSettings = scans.getScanSettings(token, project.id);
                            CxSettings.Add(Convert.ToInt64(project.id), scanSettings);
                        }
                        pclass.Add(project);
                    }
                }
                return pclass;
            }
        }


        public bool teamId_in_scan(resultClass token, List<ScanObject> CxScans, string team_name)
        {
            if (String.IsNullOrEmpty(team_name))
            {
                return false;
            }
            List<ScanObject> pclass = new List<ScanObject>();
            foreach (ScanObject scan in CxScans)
            {
                string value = CxTeams[scan.OwningTeamId].fullName;
                if (value.Contains(team_name))
                {
                    pclass.Add(scan);
                }
            }
            CxScans = pclass;
            return true;
        }

        public bool presetId_in_scan(resultClass token, List<ScanObject> CxScans, string preset_name)
        {
            if (String.IsNullOrEmpty(preset_name))
            {
                return false;
            }
            getScans getScans = new getScans();

            List<ScanObject> pclass = new List<ScanObject>();
            foreach (ScanObject scan in CxScans)
            {
                if (CxSettings.ContainsKey(scan.Project.Id))
                {
                    ScanSettings settings = CxSettings[scan.Project.Id];
                    string preset = CxPresets[settings.preset.id].name;
                    if (preset.Contains(preset_name))
                    {
                        pclass.Add(scan);
                    }
                }
            }
            CxScans = pclass;
            return true;
        }
    }
}
