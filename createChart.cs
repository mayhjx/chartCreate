using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Excel = Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;

namespace Excel折线图生成
{
    static class Program
    {
        static void Main(string[] args)
        {
            Excel.Application app = null;
            Excel.Workbook wb = null;
            Excel.Worksheet ws = null;

            try
            {
                app = (Excel.Application)Marshal.GetActiveObject("Excel.Application");
            }
            catch (Exception e)
            {
                Console.WriteLine("can not found Excel Applicatoin.");
                Console.ReadKey();
                return;
            }

            try
            {
                wb = (Excel.Workbook)app.ActiveWorkbook;
                ws = (Excel.Worksheet)wb.ActiveSheet;
            }
            catch (Exception e)
            {
                Console.WriteLine("can not found workbook: {0}", e.Message);
                Console.ReadKey();
                return;
            }

            try
            {
                app.ScreenUpdating = false;
                Console.WriteLine(wb.Name);
                CreateChart(ref ws);
                Console.WriteLine("Done！");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                app.ScreenUpdating = true;
                Marshal.ReleaseComObject(ws);
                Marshal.ReleaseComObject(wb);
                Marshal.ReleaseComObject(app);
                Console.ReadKey();
            }
 
        }


        public static void CreateChart(ref Excel.Worksheet ws)
        {

            Excel.Workbook wb;
            Excel.Chart chart;
            Excel.ChartObject chartobject;

            wb = (Excel.Workbook)ws.Parent;

            long lastrow = ws.UsedRange.Rows.Count;
            long lastcol = ws.UsedRange.Columns.Count;

            //Console.WriteLine("lastrow: {0}, lastcol: {1}", lastrow, lastcol);

            if (lastrow <= 2)
            {
                Console.WriteLine("有效数据不足");
                return;
            }

            // delete old chart
            if (((Excel.ChartObjects)ws.ChartObjects()).Count >= 1)
            {
                //Console.WriteLine("Chart num: {0}", ((Excel.ChartObjects)ws.ChartObjects()).Count);
                ((Excel.ChartObjects)ws.ChartObjects()).Delete();
            }

            for (int col = 2; col <= lastcol; col++)
            {
                if (((Excel.Range)ws.Cells[1, col]).Value == null)
                {
                    break;
                }

                List<double> data = new List<double>();
  
                for (int row = 2; row <= lastrow; row++)
                {
                    double? d = ((Excel.Range)ws.Cells[row, col]).Value;
                    if (d != null)
                    {
                        data.Add((double)d);
                    }
                }

                
                double mean = data.Average();
                double sd = data.StandardDeviation();
                double cv = sd / mean;

                string chartTitle = string.Format("{0} {1} 累积均值: {2:F2}  累积标准差: {3:F2}  CV: {4:P2}", ws.Name, ((Excel.Range)ws.Cells[1, col]).Value, mean, sd, cv);
                Console.WriteLine(chartTitle);


                //固定均值，标准差设置
                ws.Range["J1"].Offset[0,col-2].Value = ws.Cells[1, col].Value;
                ws.Range["I2"].Value = "固定均值";
                ws.Range["I3"].Value = "固定标准差";

                if (ws.Range["J2"].Offset[0, col - 2].Value != null)
                {
                    mean = ws.Range["J2"].Offset[0, col - 2].Value;
                }
                else
                {
                    Console.WriteLine("未设置{0}的固定均值", ws.Cells[1, col].Value);
                }

                if (ws.Range["J3"].Offset[0, col - 2].Value != null)
                {
                    sd = ws.Range["J3"].Offset[0, col - 2].Value;
                }
                else
                {
                    Console.WriteLine("未设置{0}的固定标准差", ws.Cells[1, col].Value);
                }


                double[] meanarray = new double[lastrow - 1];
                double[] plusOneSDarray = new double[lastrow - 1];
                double[] plusTwoSDarray = new double[lastrow - 1];
                double[] plusThreeSDarray = new double[lastrow - 1];
                double[] minusOneSDarray = new double[lastrow - 1];
                double[] minusTwoSDarray = new double[lastrow - 1];
                double[] minusThreeSDarray = new double[lastrow - 1];

                // 初始化
                for (int i = 0; i < lastrow-1; i++)
                {
                    meanarray[i] = mean;
                    plusOneSDarray[i] = mean + sd;
                    plusTwoSDarray[i] = mean + 2 * sd;
                    plusThreeSDarray[i] = mean + 3 * sd;
                    minusOneSDarray[i] = mean - sd;
                    minusTwoSDarray[i] = mean - 2 * sd;
                    minusThreeSDarray[i] = mean - 3 * sd;
                }


                // Create a new chart
                chart = (Excel.Chart)wb.Charts.Add();

                Excel.Range XRng = ws.Range[ws.Cells[2, 1], ws.Cells[lastrow, 1]];
                Excel.Range dataRng = ws.Range[ws.Cells[1, col], ws.Cells[lastrow, col]];

                chart.SetSourceData(dataRng, Excel.XlRowCol.xlColumns);

                chart.ChartType = Excel.XlChartType.xlLineMarkers;
                chart.HasLegend = false;
                chart.HasTitle = true;
                chart.ChartTitle.Text = chartTitle;

                // 纵坐标范围
                string min = (mean - 3.75 * sd) > data.Min() ? data.Min().ToString("F2") : (mean - 3.75 * sd).ToString("F2");
                string max = data.Max() > (mean + 3.75 * sd) ? data.Max().ToString("F2") : (mean + 3.75 * sd).ToString("F2");
                chart.Axes(Excel.XlAxisType.xlValue).MinimumScale = min;
                chart.Axes(Excel.XlAxisType.xlValue).MaximumScale = max;

                // Embedding chart on a worksheet
                chart.Location(Excel.XlChartLocation.xlLocationAsObject, ws.Name);

                // get activate chartobject
                chartobject = (Excel.ChartObject)ws.ChartObjects(col-1);

                // 去除网格线
                chartobject.Chart.Axes(Excel.XlAxisType.xlValue).HasMajorGridlines = false;

                // 位置大小
                chartobject.Left = 439;
                chartobject.Top = 105 + (col - 2) * 255;
                chartobject.Height = 255;
                chartobject.Width = 810;


                Excel.SeriesCollection se = chartobject.Chart.SeriesCollection();

                se.Item(1).XValues = XRng;  // 横坐标值
                se.Item(1).Format.Line.Weight = 1.2F;

                // mean
                se.NewSeries();
                se.Item(2).Name = "mean";
                se.Item(2).Values = meanarray;
                se.Item(2).Format.Line.Weight = 1;
                se.Item(2).MarkerStyle = Excel.XlMarkerStyle.xlMarkerStyleNone;
                se.Item(2).Format.Line.ForeColor.RGB = (int)Excel.XlRgbColor.rgbSkyBlue;
                

                // mean + sd
                se.NewSeries();
                se.Item(3).Name = "mean+sd";
                se.Item(3).Values = plusOneSDarray;
                se.Item(3).Format.Line.Weight = 1;
                se.Item(3).MarkerStyle = Excel.XlMarkerStyle.xlMarkerStyleNone;
                se.Item(3).Format.Line.ForeColor.RGB = (int)Excel.XlRgbColor.rgbBlue;
                

                // mean - sd
                se.NewSeries();
                se.Item(4).Name = "mean-sd";
                se.Item(4).Values = minusOneSDarray;
                se.Item(4).Format.Line.Weight = 1;
                se.Item(4).MarkerStyle = Excel.XlMarkerStyle.xlMarkerStyleNone;
                se.Item(4).Format.Line.ForeColor.RGB = (int)Excel.XlRgbColor.rgbBlue;
                

                // mean + 2sd
                se.NewSeries();
                se.Item(5).Name = "mean+2sd";
                se.Item(5).Values = plusTwoSDarray;
                se.Item(5).Format.Line.Weight = 1;
                se.Item(5).MarkerStyle = Excel.XlMarkerStyle.xlMarkerStyleNone;
                se.Item(5).Format.Line.ForeColor.RGB = (int)Excel.XlRgbColor.rgbOliveDrab;
                

                // mean - 2sd
                se.NewSeries();
                se.Item(6).Name = "mean-2sd";
                se.Item(6).Values = minusTwoSDarray;
                se.Item(6).Format.Line.Weight = 1;
                se.Item(6).MarkerStyle = Excel.XlMarkerStyle.xlMarkerStyleNone;
                se.Item(6).Format.Line.ForeColor.RGB = (int)Excel.XlRgbColor.rgbOliveDrab;
                

                // mean + 3sd
                se.NewSeries();
                se.Item(7).Name = "mean+3sd";
                se.Item(7).Values = plusThreeSDarray;
                se.Item(7).Format.Line.Weight = 1;
                se.Item(7).MarkerStyle = Excel.XlMarkerStyle.xlMarkerStyleNone;
                se.Item(7).Format.Line.ForeColor.RGB = (int)Excel.XlRgbColor.rgbIndianRed;
                

                // mean - 3sd
                se.NewSeries();
                se.Item(8).Name = "mean-3sd";
                se.Item(8).Values = minusThreeSDarray;
                se.Item(8).Format.Line.Weight = 1;
                se.Item(8).MarkerStyle = Excel.XlMarkerStyle.xlMarkerStyleNone;
                se.Item(8).Format.Line.ForeColor.RGB = (int)Excel.XlRgbColor.rgbIndianRed;

                // 修改超过Mean +- 2SD的点的颜色
                // 修改超过Mean +- 3SD的点的颜色
                double meanplus2sd = mean + 2 * sd;
                double meanminus2sd= mean - 2 * sd;
                double meanplus3sd = mean + 3 * sd;
                double meanminus3sd = mean - 3 * sd;

                for (int i=2; i<=lastrow; i++)
                {
                    Excel.Range rng = (Excel.Range)ws.Cells[i, col];

                    if (rng.Value == null)
                    {
                        continue;
                    }
                        
                    if ((double)rng.Value >= meanplus3sd)
                    {
                        rng.Interior.Color = 255; // vbred
                        se.Item(1).Points(i-1).Format.Fill.ForeColor.RGB = (int)Excel.XlRgbColor.rgbRed;
                    }
                    else if ((double)rng.Value >= meanplus2sd)
                    {
                        rng.Interior.Color = 65535; // vbYellow
                        se.Item(1).Points(i-1).Format.Fill.ForeColor.RGB = (int)Excel.XlRgbColor.rgbYellow;
                    }
                    else if ((double)rng.Value <= meanminus3sd)
                    {
                        rng.Interior.Color = 255;
                        se.Item(1).Points(i-1).Format.Fill.ForeColor.RGB = (int)Excel.XlRgbColor.rgbRed;
                    }
                    else if ((double)rng.Value <= meanminus2sd)
                    {
                        rng.Interior.Color = 65535;
                        se.Item(1).Points(i-1).Format.Fill.ForeColor.RGB = (int)Excel.XlRgbColor.rgbYellow;
                    }
                }
            }

        }

        public static double StandardDeviation(this IEnumerable<double> values)
        {
            //https://stackoverflow.com/questions/3141692/standard-deviation-of-generic-list
            double avg = values.Average();
            return Math.Sqrt(values.Average(v => Math.Pow(v - avg, 2)));
        }
    }
}
