﻿using System;
using System.Collections.Generic;
using System.IO;
using CsvHelper;
using System.Data;
using System.Linq;
using System.Globalization;
using Farrier.Helpers;

namespace Farrier.Models
{
    class LoopData
    {
        private LogRouter _log;

        public DataTable Data { get; }

        public bool IsGrouped { get; }

        public List<List<DataRow>> GroupedData { get; }

        public LoopData(string csvFilePath, string orderBy = "", bool orderDesc = false, string groupBy = "", bool groupOrderBySize = false, bool groupDesc = false, string filter = "", string groupSeparator = "", LogRouter log = null)
        {
            if (log == null)
                _log = new LogRouter();
            else
                _log = log;

            try
            {
                using (var reader = new StreamReader(csvFilePath))
                using (var csv = new CsvReader(reader, CultureInfo.CurrentCulture))
                {
                    using (var datareader = new CsvDataReader(csv))
                    {
                        Data = new DataTable();
                        Data.Load(datareader);

                        _log.Info($"Retrieved {Data.Rows.Count} records across {Data.Columns.Count} columns from \"{Path.GetFileName(csvFilePath)}\"");

                        //No point in doing all this stuff if there's only 1 (or none) rows
                        if (Data.Rows.Count > 1)
                        {
                            if (!String.IsNullOrEmpty(groupBy) && Data.Columns.Contains(groupBy))
                            {
                                // Grouped Loop
                                IsGrouped = true;

                                //Apply filter if specified
                                if (!String.IsNullOrEmpty(filter))
                                {
                                    Data.DefaultView.RowFilter = filter;
                                    Data = Data.DefaultView.ToTable();
                                }

                                //Separate groups into multiple rows (if specified)
                                if (!String.IsNullOrEmpty(groupSeparator))
                                {
                                    List<DataRow> newRows = new List<DataRow>();
                                    List<DataRow> killRows = new List<DataRow>();
                                    foreach (DataRow row in Data.Rows)
                                    {
                                        var groupValues = row[groupBy].ToString().Split(groupSeparator);
                                        if (groupValues.Length > 1)
                                        {
                                            killRows.Add(row); //Remove this row
                                            for (int r = 0; r < groupValues.Length; r++)
                                            {
                                                //Map split values into clones of original row
                                                var newRow = Data.NewRow();
                                                foreach (DataColumn col in Data.Columns)
                                                {
                                                    newRow[col] = row[col];
                                                }
                                                newRow[groupBy] = groupValues[r];
                                                newRows.Add(newRow);
                                            }
                                        }
                                    }
                                    foreach (DataRow row in killRows)
                                    {
                                        Data.Rows.Remove(row);
                                    }
                                    foreach (DataRow row in newRows)
                                    {
                                        Data.Rows.Add(row);
                                    }
                                }

                                //Apply Initial Sorts
                                Data.DefaultView.Sort = groupBy + (groupDesc ? " desc" : "") + ((!String.IsNullOrEmpty(orderBy) && orderBy != groupBy) ? ", " + orderBy + (orderDesc ? " desc" : "") : "");
                                Data = Data.DefaultView.ToTable();

                                //Group the rows manually
                                string curGroupValue = Data.Rows[0][groupBy].ToString();
                                GroupedData = new List<List<DataRow>>();
                                var groupRows = new List<DataRow>();
                                foreach (DataRow row in Data.Rows)
                                {
                                    if (row[groupBy].ToString() != curGroupValue)
                                    {
                                        curGroupValue = row[groupBy].ToString();
                                        GroupedData.Add(groupRows);
                                        groupRows = new List<DataRow>();
                                    }
                                    groupRows.Add(row);
                                }
                                GroupedData.Add(groupRows);

                                //Initial grouping always ends up with grouping by value, but a custom sort is applied if by size is requested
                                if (groupOrderBySize)
                                {
                                    if (!groupDesc)
                                    {
                                        GroupedData.Sort((x, y) => x.Count.CompareTo(y.Count));
                                    }
                                    else
                                    {
                                        GroupedData.Sort((x, y) => y.Count.CompareTo(x.Count));
                                    }
                                }
                            }
                            else
                            {
                                IsGrouped = false;

                                //Apply filter if specified
                                if (!String.IsNullOrEmpty(filter))
                                {
                                    Data.DefaultView.RowFilter = filter;
                                    Data = Data.DefaultView.ToTable();
                                    _log.Info($"Appplied filter reducing to {Data.Rows.Count} records");
                                }

                                //Sort the results
                                if (!String.IsNullOrEmpty(orderBy) && Data.Columns.Contains(orderBy))
                                {
                                    Data.DefaultView.Sort = orderBy + (orderDesc ? " desc" : "");
                                    Data = Data.DefaultView.ToTable();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Error initializing LoopData using file: \"{Path.GetFileName(csvFilePath)}\"");
                throw (ex);
            }
        }
    }
}