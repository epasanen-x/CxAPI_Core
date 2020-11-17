using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using CxAPI_Core.dto;
using System.Threading;

namespace CxAPI_Core
{
    class restReport_6 : IDisposable
    {
        public resultClass token;

        public restReport_6(resultClass token)
        {
            this.token = token;
        }

        public bool fetchReportsbyDate()
        {
            if (token.debug && token.verbosity > 1) { Console.WriteLine("Running: {0}", token.report_name); }
            List<ReportTrace> trace = new List<ReportTrace>();
            Dictionary<long, List<ReportResultMaxQueries>> report = new Dictionary<long, List<ReportResultMaxQueries>>();
            Dictionary<long, List<ReportResultAll>> last = new Dictionary<long, List<ReportResultAll>>();
            Dictionary<long, ScanCount> scanCount = new Dictionary<long, ScanCount>();
            getScans scans = new getScans();
            getProjects projects = new getProjects(token);

            //List<ScanObject> scan = scans.getScan(token);
            Dictionary<string, Teams> teams = projects.CxTeams;
            List<ScanObject> scan = projects.filter_by_projects(token, true);
            Dictionary<long, ScanStatistics> resultStatistics = projects.CxResultStatistics;
            getScanResults scanResults = new getScanResults();

            if (scan.Count == 0)
            {
                Console.Error.WriteLine("No scans were found, pleas check argumants and retry.");
                return false;
            }

            foreach (ScanObject s in scan)
            {

                ReportResult result = scanResults.SetResultRequest(s.Id, "XML", token);
                if (trace.Count % token.max_threads == 0)
                {
                    waitForResult(trace, scanResults, last);
                    trace.Clear();
                }
                if (result != null)
                {
                    trace.Add(new ReportTrace(s.Project.Id, s.Project.Name, teams[s.OwningTeamId].fullName, s.DateAndTime.StartedOn, s.Id, result.ReportId, "XML"));
                }

            }
            waitForResult(trace, scanResults, last);
            trace.Clear();

            List<ReportResultMaxQueries> reportOutputs = totalScansandReports(last, scan, resultStatistics);
            if (token.debug) { Console.WriteLine("Processing data, number of rows: {0}", reportOutputs.Count); }
            if (token.pipe)
            {
                foreach (ReportResultMaxQueries csv in reportOutputs)
                {
                    //   Console.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21}", csv.ProjectName, csv.TeamName, csv.PresetName, csv.scanId, csv.scanDate, csv.StartNotExploitable, csv.StartConfirmed, csv.StartToVerify, csv.LastOthers, csv.NewHigh, csv.NewMedium, csv.NewLow, csv.DiffHigh, csv.DiffMedium, csv.DiffLow, csv.LastNotExploitable, csv.LastConfirmed, csv.LastToVerify, csv.LastOthers, csv.firstScan, csv.lastScan, csv.ScanCount);
                }
            }
            else
            {
                csvHelper csvHelper = new csvHelper();
                csvHelper.writeCVSFile(reportOutputs, token);
            }
            return true;
        }

        private List<ReportResultMaxQueries> totalScansandReports(Dictionary<long, List<ReportResultAll>> last, List<ScanObject> scan, Dictionary<long, ScanStatistics> scannedStatistics)
        {
            List<ReportResultMaxQueries> reports = new List<ReportResultMaxQueries>();

            foreach (int key in last.Keys)
            {
                ReportResultMaxQueries report = new ReportResultMaxQueries();
                List<ReportResultAll> scanObjects = last[key];
                List<Tuple<int, int, string, string, string>> topQueries = new List<Tuple<int, int, string, string, string>>();

                foreach (ReportResultAll scanObject in scanObjects)
                {

                    if (String.IsNullOrEmpty(report.Project_Name))
                    {
                        report.Project_Name = scanObject.projectName;
                        report.Project_Id = scanObject.projectId;
                        report.Team_Name = scanObject.teamName;
                        report.Scan_Id = scanObject.scanId;
                        report.Scan_Date = scanObject.scanDate;
                        report.Preset_Name = scanObject.presetName;
                        report.Languages = getLanguages(scan, scannedStatistics, scanObject.scanId);
                    }
                    // now get top queries
                    topQueries.Add(Tuple.Create(scanObject.QueryCount, scanObject.NotExploitableCount,scanObject.Group, scanObject.Severity, scanObject.Query));
                }
                topQueries = getTopCount(topQueries);
                if (topQueries.Count > 0)
                {
                    Tuple<int, int, string, string, string> rank1 = topQueries[0];
                    report.Query_Count_1 = rank1.Item1;
                    report.False_Positive_1 = rank1.Item2;
                    report.Group_1 = rank1.Item3;
                    report.Severity_1 = rank1.Item4;
                    report.Query_1 = rank1.Item5;
                }
                if (topQueries.Count > 1)
                {
                    Tuple<int, int, string, string, string> rank1 = topQueries[0];
                    report.Query_Count_2 = rank1.Item1;
                    report.False_Positive_2 = rank1.Item2;
                    report.Group_2 = rank1.Item3;
                    report.Severity_2 = rank1.Item4;
                    report.Query_2 = rank1.Item5;
                }
                if (topQueries.Count > 2)
                {
                    Tuple<int, int, string, string, string> rank1 = topQueries[0];
                    report.Query_Count_3 = rank1.Item1;
                    report.False_Positive_3 = rank1.Item2;
                    report.Group_3 = rank1.Item3;
                    report.Severity_3 = rank1.Item4;
                    report.Query_3 = rank1.Item5;
                }
                if (topQueries.Count > 3)
                {
                    Tuple<int, int, string, string, string> rank1 = topQueries[0];
                    report.Query_Count_4 = rank1.Item1;
                    report.False_Positive_4 = rank1.Item2;
                    report.Group_4 = rank1.Item3;
                    report.Severity_4 = rank1.Item4;
                    report.Query_4 = rank1.Item5;
                }
                if (topQueries.Count > 4)
                {
                    Tuple<int, int, string, string, string> rank1 = topQueries[0];
                    report.Query_Count_5 = rank1.Item1;
                    report.False_Positive_5 = rank1.Item2;
                    report.Group_5 = rank1.Item3;
                    report.Severity_5 = rank1.Item4;
                    report.Query_5 = rank1.Item5;
                }

                reports.Add(report);
            }

            return reports;
        }

        private string getLanguages(List<ScanObject> scans, Dictionary<long, ScanStatistics> scannedStatistics, long scanId)
        {
            ScanStatistics scanStatistics = scannedStatistics[scanId];
            List<string> lang = new List<string>();
            LanguageStateCollection[] lstates = getScanObject(scans,scanId).ScanState.LanguageStateCollection;
            foreach (LanguageStateCollection state in lstates)
            {
                lang.Add(state.LanguageName);
            }
            return String.Join(",", lang.ToArray());
        }
        private ScanObject getScanObject(List<ScanObject> scans, long scanId)
        {

            foreach(ScanObject scan in scans)
            {
                if (scan.Id == scanId)
                {
                    return scan;
                }
            }

            return null;

        }
        private List<Tuple<int, int, string, string, string>> getTopCount(List<Tuple<int, int, string, string, string>> listQueries)
        {
            return listQueries.OrderByDescending(x => x.Item1).ToList();         
        }

        private bool process_ScanResult(XElement result, Dictionary<long, List<ReportResultAll>> last, long projectId, long scanId)
        {
            List<ReportResultAll> reportResults = new List<ReportResultAll>();
            try
            {
                if (result.Attribute("ScanId").Value == scanId.ToString())
                {
                    IEnumerable<XElement> lastScan = from el in result.Descendants("Query") select el;
                    foreach (XElement query in lastScan)
                    {
                        XElement root = query.Parent;
                        IEnumerable<XElement> vulerabilities = from el in query.Descendants("Result") select el;
                        ReportResultAll isnew = new ReportResultAll()
                        {
                            QueryId = Convert.ToInt64(query.Attribute("id").Value.ToString()),
                            Query = query.Attribute("name").Value.ToString(),
                            Group = query.Attribute("group").Value.ToString(),
                            projectName = root.Attribute("ProjectName").Value.ToString(),
                            presetName = root.Attribute("Preset").Value.ToString(),
                            teamName = root.Attribute("Team").Value.ToString(),
                            scanDate = Convert.ToDateTime(root.Attribute("ScanStart").Value.ToString()),
                            projectId = Convert.ToInt64(root.Attribute("ProjectId").Value.ToString()),
                            scanId = Convert.ToInt64(root.Attribute("ScanId").Value.ToString()),
                            Severity = query.Attribute("Severity").Value.ToString(),
                            QueryCount = vulerabilities.Count()
                        };
                        foreach (XElement vulerability in vulerabilities)
                        {
                            int state = Convert.ToInt32(vulerability.Attribute("state").Value.ToString());
                            if (state == 1)
                            {
                                isnew.NotExploitableCount++;
                            }
                        }
                        reportResults.Add(isnew);
                    }
                    if (!last.ContainsKey(projectId))
                    {
                        last.Add(projectId, reportResults);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return false;
            }

            return true;

        }

        public bool waitForResult(List<ReportTrace> trace, getScanResults scanResults, Dictionary<long, List<ReportResultAll>> last)
        {
            ConsoleSpinner spinner = new ConsoleSpinner();
            bool waitFlag = false;
            DateTime wait_expired = DateTime.UtcNow;
            while (!waitFlag)
            {
                if (wait_expired.AddMinutes(2) < DateTime.UtcNow)
                {
                    Console.Error.WriteLine("waitForResult timeout! {0}", getTimeOutObjects(trace));
                    break;
                }
                spinner.Turn();
                waitFlag = true;
                if (token.debug && token.verbosity > 0) { Console.WriteLine("Sleeping 1 second(s)"); }
                Thread.Sleep(1000);
                foreach (ReportTrace rt in trace)
                {
                    if (!rt.isRead)
                    {
                        waitFlag = false;
                        if (token.debug && token.verbosity > 0) { Console.WriteLine("Testing report.Id {0}", rt.reportId); }
                        if (scanResults.GetResultStatus(rt.reportId, token))
                        {
                            if (token.debug && token.verbosity > 0) { Console.WriteLine("Found report.Id {0}", rt.reportId); }
                            Thread.Sleep(2000);
                            var result = scanResults.GetResult(rt.reportId, token);
                            if (result != null)
                            {
                                if (process_ScanResult(result, last, rt.projectId, rt.scanId))
                                {
                                    rt.isRead = true;
                                }
                                else
                                {
                                    rt.isRead = true;
                                    if (token.debug && token.verbosity > 1)
                                    {
                                        Console.Error.WriteLine("Dumping XML:");
                                        Console.Error.Write(result.ToString());
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        private string getTimeOutObjects(List<ReportTrace> trace)
        {
            string result = String.Empty;
            foreach (ReportTrace rt in trace)
            {
                result += String.Format("ProjectName {0}, ScanId {1}, TimeStamp {2}, isRead {3}", rt.projectName, rt.scanId, rt.TimeStamp, rt.isRead) + Environment.NewLine;
            }
            return result;
        }

        private bool writeXMLOutput(ReportTrace rt, XElement result)
        {
            try
            {
                if ((!String.IsNullOrEmpty(token.save_result)) && (!String.IsNullOrEmpty(token.save_result_path)))
                {
                    string filename = token.save_result_path + @"\" + rt.projectName + '-' + rt.scanTime.Value.ToString("yyyyMMddhhmmss") + ".xml";
                    File.WriteAllText(filename, result.ToString(), System.Text.Encoding.UTF8);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                throw ex;
            }
        }
        public bool matchProjectandTeam(ScanObject s, List<Teams> teams)
        {
            bool result = false;
            getScans scans = new getScans();

            string fullName = scans.getFullName(teams, s.OwningTeamId);

            if ((String.IsNullOrEmpty(token.project_name) || ((!String.IsNullOrEmpty(token.project_name)) && (s.Project.Name.Contains(token.project_name)))))
            {
                if ((String.IsNullOrEmpty(token.team_name) || ((!String.IsNullOrEmpty(token.team_name)) && (!String.IsNullOrEmpty(fullName)) && (fullName.Contains(token.team_name)))))
                {
                    result = true;
                }
            }
            return result;
        }

        public void Dispose()
        {

        }

    }

}