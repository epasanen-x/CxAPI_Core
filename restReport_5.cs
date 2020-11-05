using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using CxAPI_Core.dto;
using System.Threading;

namespace CxAPI_Core
{
    class restReport_5 : IDisposable
    {
        public resultClass token;

        public restReport_5(resultClass token)
        {
            this.token = token;
        }

        public bool fetchReportsbyDate()
        {
            if (token.debug && token.verbosity > 1) { Console.WriteLine("Running: {0}", token.report_name); }

            Dictionary<long, Dictionary<DateTime, Dictionary<string, ReportResultExtended>>> fix = new Dictionary<long, Dictionary<DateTime, Dictionary<string, ReportResultExtended>>>();
            List<ReportTrace> trace = new List<ReportTrace>();
            Dictionary<string, ReportResultExtended> resultAll = new Dictionary<string, ReportResultExtended>();
            List<ReportResultExtended> report_output = new List<ReportResultExtended>();
            //            Dictionary<long, ReportStaging> start = new Dictionary<long, ReportStaging>();
            //            Dictionary<long, ReportStaging> end = new Dictionary<long, ReportStaging>();
            Dictionary<long, ScanCount> scanCount = new Dictionary<long, ScanCount>();
            getScanResults scanResults = new getScanResults();
            getScans scans = new getScans();
            getProjects projects = new getProjects(token);
            //List<ScanObject> scan = scans.getScan(token);
            //List<Teams> teams = scans.getTeams(token);
            token.max_scans = (token.max_scans == 0) ? 1 : token.max_scans;
            List<ScanObject> scan = projects.filter_by_projects(token);

            if (scan.Count == 0)
            {
                Console.Error.WriteLine("No scans were found, please check argumants and retry.");
                return false;
            }
            Dictionary<Guid, Teams> teams = projects.CxTeams;
            Dictionary<long, ScanSettings> settings = projects.CxSettings;
            Dictionary<long, ScanStatistics> resultStatistics = projects.CxResultStatistics;
            Dictionary<long, Presets> presets = projects.CxPresets;
            List<ReportLastScan> lastScan = reportLastScan(token, scan, teams, resultStatistics, settings, presets);

            if (token.debug) { Console.WriteLine("Processing data, number of rows: {0}", lastScan.Count); }

            if (token.pipe)
            {
                foreach (ReportLastScan csv in lastScan)
                {
                    Console.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}", csv.ProjectId, csv.ProjectName, csv.Comment, csv.TeamName, csv.isIncremental, csv.Owner, csv.Origin, csv.ScanType, csv.ScanStartDate, csv.ScanFinishDate, csv.High, csv.Medium, csv.Low);
                }
            }
            else
            {
                csvHelper csvHelper = new csvHelper();
                csvHelper.writeCVSFile(lastScan, token);
            }
            return true;
        }

        public List<ReportLastScan> reportLastScan(resultClass token, List<ScanObject> scans, Dictionary<Guid, Teams> allTeams, Dictionary<long, ScanStatistics> scannedStatistics,Dictionary<long,ScanSettings> settings, Dictionary<long,Presets> presets)
        {
            List<ReportLastScan> reportScan = new List<ReportLastScan>();
            foreach (ScanObject scan in scans)
            {
                ScanStatistics scanStatistics = scannedStatistics[scan.Id];
                List<string> lang = new List<string>();
                LanguageStateCollection[] lstates = scan.ScanState.LanguageStateCollection;
                foreach (LanguageStateCollection state in lstates)
                {
                    lang.Add(state.LanguageName);
                }
                ReportLastScan oneScan = new ReportLastScan()
                {
                    Comment = scan.Comment,
                    ScanId = scan.Id,
                    Preset = presets[settings[scan.Project.Id].preset.id].name,
                    ProjectId = scan.Project.Id,
                    ProjectName = scan.Project.Name,
                    ScanFinishDate = scan.DateAndTime.FinishedOn.ToString(),
                    ScanStartDate = scan.DateAndTime.StartedOn.ToString(),
                    EngineStartDate = scan.DateAndTime.EngineStartedOn.ToString(),
                    TeamName = allTeams[scan.OwningTeamId].fullName,
                    ScanType = scan.ScanType.Value,
                    Owner = scan.Owner,
                    LOC = scan.ScanState.LinesOfCode,
                    FailedLOC = scan.ScanState.FailedLinesOfCode,
                    FileCount = scan.ScanState.FilesCount,
                    Origin = scan.Origin,
                    Languages = String.Join(",",lang.ToArray()),
                    isIncremental = scan.IsIncremental ? "true" : "false",
                    High = scanStatistics.HighSeverity,
                    Medium = scanStatistics.MediumSeverity,
                    Low = scanStatistics.LowSeverity,
                    Info = scanStatistics.InfoSeverity
                };
                reportScan.Add(oneScan);
            }

            return reportScan;
        }

        public void Dispose()
        {

        }

    }
}