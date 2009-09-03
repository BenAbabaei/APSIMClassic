﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;

namespace CSUserInterface
{
    public partial class RotPlotUI : Controllers.BaseView
    {
        private List<string> paddocks = null;
        private List<string> rotations = new List<string>();
        private List<Color> colours = new List<Color>();

        private int m_ColWidth = 45;
        private int m_HeaderHeight = 20;
        private int m_YearHeight = 0;
        private int m_CustomYearHeight = 80;
        private int m_DateWidth = 60;

        private int m_LegendRowHeight = 18;
        private int m_LegendTextWidth = 100;
        private int m_LegendBoxHeight = 12;
        private int m_LegendBoxWidth = 18;

        private int m_Cols = 0;
        private int m_Years = 10;
        private int m_FirstYearHeight = 0;
        private int m_LastYearHeight = 0;
        private int m_Height = 0;
        private double m_DayHeight = 0;
        private XmlDocument DocStates = null;

        private DateTime m_StartDate;
        private DateTime m_EndDate;
        private DateTime m_FirstYear;
        private DateTime m_LastYear;
        private DateTime m_SelectedDate;
        private bool m_UpdatingSelectedDate = false;
        private bool m_Dragging = false;

        private int _ncid;

        public RotPlotUI()
        {
            InitializeComponent();
        }

        public override void OnRefresh()
        {
            base.OnRefresh();
            //panel1.Visible = DocStates != null;
            //panel4.Visible = DocStates != null;
            split1.Visible = DocStates != null;
            panel5.Visible = DocStates != null;
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {
            CalcHeights();
            UpdateSizes();
            DrawHeader(e);
            DrawDates(e);
            DrawRotations(e);
            DrawCols(e);
            DrawRows(e);
            DrawSelectedDate(e);
            pnlLegend.Invalidate();
        }

        private void CalcHeights()
        {
            m_YearHeight = m_CustomYearHeight;
            double dDays = 0;
            TimeSpan tmpDateSpan = m_EndDate - m_StartDate;
            dDays = tmpDateSpan.Days;

            double dAvailableHeight = panel1.Height - m_HeaderHeight - 24; //allow for the srollbar
            if (dDays > 0)
            {
                m_DayHeight = dAvailableHeight / dDays;
                if (chkFit.Checked)
                {
                    m_YearHeight = (int)(m_DayHeight * 365.25);
                    m_DayHeight = m_YearHeight / 365.25;
                }
                else
                {
                    m_DayHeight = m_YearHeight / 365.25;
                }
                TimeSpan tmpSpan = m_FirstYear - m_StartDate;
                m_FirstYearHeight = (int)(tmpSpan.Days * m_DayHeight);

                tmpSpan = m_EndDate - m_LastYear;
                m_LastYearHeight = (int)(tmpSpan.Days * m_DayHeight);
            }

            //Legend Height calcs
            int iHeight = split2.Panel1.Height;
            if (rotations != null && rotations.Count > 0)
            {
                iHeight = rotations.Count * m_LegendRowHeight + 8;
            }
            pnlLegend.Height = iHeight;

        }
        private void UpdateSizes()
        {
            panel2.Width = (int)(m_Cols * m_ColWidth + m_DateWidth + 5);
            TimeSpan tmpDateSpan = m_EndDate - m_StartDate;
            m_Height = (int)(tmpDateSpan.Days * m_DayHeight + m_HeaderHeight);
            panel2.Height = m_Height;
        }
        private void DrawHeader(PaintEventArgs e)
        {
            if (paddocks != null)
            {
                Font drawFont = new Font("Arial", 8);
                SolidBrush drawBrush = new SolidBrush(Color.Black);

                // Create point for upper-left corner of drawing.
                PointF drawPoint = new PointF(0, 0);
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                Rectangle tmp = new System.Drawing.Rectangle();
                tmp.X = m_DateWidth;
                tmp.Y = 0;
                tmp.Height = m_HeaderHeight; //1 years height
                tmp.Width = m_ColWidth;

                for (int i = 0; i < paddocks.Count; ++i)
                {
                    string paddock = paddocks[i];
                    e.Graphics.DrawString(paddock, drawFont, drawBrush, tmp, sf);
                    tmp.X += m_ColWidth;
                }
            }
        }
        private void DrawDates(PaintEventArgs e)
        {
            Rectangle tmp = new System.Drawing.Rectangle();
            tmp.X = 0;
            tmp.Y = 0;

            tmp.Height = m_Height; //full height
            tmp.Width = (int)m_DateWidth;
            //e.Graphics.FillRectangle(System.Drawing.SystemBrushes.ButtonFace, tmp);

            //draw dates
            if (paddocks != null) //paddocks not necessarily correct - but does indicate valid data
            {
                Font drawFont = new Font("Arial", 8);
                SolidBrush drawBrush = new SolidBrush(Color.Black);

                // Create point for upper-left corner of drawing.
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                Rectangle rectDate = new System.Drawing.Rectangle();
                rectDate.X = 0;
                rectDate.Y = 0 + m_FirstYearHeight - (m_YearHeight / 2) + m_HeaderHeight;
                rectDate.Height = m_YearHeight; //1 years height
                rectDate.Width = m_DateWidth;

                DateTime tmpdate = m_FirstYear;
                for (int i = 0; i < m_Years; ++i)
                {
                    string sDate = tmpdate.ToString("d");
                    e.Graphics.DrawString(sDate, drawFont, drawBrush, rectDate, sf);
                    rectDate.Y += m_YearHeight;
                    tmpdate = tmpdate.AddYears(1);
                }
            }
        }
        private void DrawLegend(PaintEventArgs e)
        {
            Rectangle tmp = new System.Drawing.Rectangle();
//            tmp.X = 0;// pnlLegend.Margin.All;
//            tmp.Y = 0;// pnlLegend.Margin.All;
//            tmp.Height = pnlLegend.Height - 1;// -pnlLegend.Margin.All - pnlLegend.Margin.All;
//            tmp.Width = pnlLegend.Width - 1;// pnlLegend.Margin.All - pnlLegend.Margin.All;
//            e.Graphics.DrawRectangle(System.Drawing.SystemPens.ControlDark, tmp);

            if (rotations != null && rotations.Count > 0)
            {
                Font drawFont = new Font("Arial", 8);
                SolidBrush drawBrush = new SolidBrush(Color.Black);
                SolidBrush ColourBrush = new SolidBrush(Color.Black);

                // Create point for upper-left corner of drawing.
                PointF drawPoint = new PointF(0, 0);
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Near;
                sf.LineAlignment = StringAlignment.Center;

                tmp.X = pnlLegend.Margin.All + m_LegendBoxWidth + 8;
                tmp.Y = pnlLegend.Margin.All;
                tmp.Height = m_LegendRowHeight; //1 years height
                tmp.Width = m_LegendTextWidth;

                Rectangle tmpBox = new System.Drawing.Rectangle();
                tmpBox.X = pnlLegend.Margin.All + 2;
                tmpBox.Y = tmp.Y + (int)((m_LegendRowHeight - m_LegendBoxHeight) / 2.0) - 1;
                tmpBox.Height = m_LegendBoxHeight; //1 years height
                tmpBox.Width = m_LegendBoxWidth;

                for (int i = 0; i < rotations.Count; ++i)
                {
                    string rotation = rotations[i];
                    e.Graphics.DrawString(rotation, drawFont, drawBrush, tmp, sf);

                    ColourBrush.Color = colours[i];
                    e.Graphics.FillRectangle(ColourBrush, tmpBox);
                    e.Graphics.DrawRectangle(System.Drawing.SystemPens.WindowText, tmpBox);

                    tmp.Y += m_LegendRowHeight;
                    tmpBox.Y += m_LegendRowHeight;
                }
            }
        }
        private void DrawRows(PaintEventArgs e)
        {
            Rectangle tmp = new System.Drawing.Rectangle();
            tmp.X = m_DateWidth;
            tmp.Y = m_HeaderHeight;
            tmp.Height = m_FirstYearHeight;
            tmp.Width = (int)(m_Cols * m_ColWidth);
            e.Graphics.DrawRectangle(System.Drawing.Pens.LightGray, tmp);
            tmp.Height = m_YearHeight;
            tmp.Y = m_HeaderHeight + m_FirstYearHeight;
            for (int i = 0; i < m_Years - 1; ++i)
            {
                e.Graphics.DrawRectangle(System.Drawing.Pens.LightGray, tmp);
                tmp.Y += (int)(m_YearHeight);
            }
            tmp.Height = m_LastYearHeight;
            e.Graphics.DrawRectangle(System.Drawing.Pens.LightGray, tmp);
        }
        private void DrawCols(PaintEventArgs e)
        {
            Rectangle tmp = new System.Drawing.Rectangle();
            tmp.X = m_DateWidth;
            tmp.Y = m_HeaderHeight;

            tmp.Height = m_Height - m_HeaderHeight; //full height
            tmp.Width = (int)m_ColWidth;

            for (int i = 0; i < m_Cols; ++i)
            {
                e.Graphics.DrawRectangle(System.Drawing.Pens.LightGray, tmp);
                tmp.X += (int)(m_ColWidth);
            }
        }
        private void DrawSelectedDate(PaintEventArgs e)
        {
            if (m_SelectedDate > m_StartDate && m_SelectedDate < m_EndDate)
            {
                Rectangle tmp = new System.Drawing.Rectangle();
                tmp.X = m_DateWidth + 2;
                tmp.Y = ConvertDateToPosition(m_SelectedDate);
                tmp.Height = 1;
                tmp.Width = (int)(m_Cols * m_ColWidth) - 4;
                e.Graphics.DrawRectangle(System.Drawing.Pens.Blue, tmp);
            }
        }
        private int ConvertDateToPosition(DateTime dDate)
        {
            TimeSpan dSpan2 = dDate - m_StartDate;
            double dResult = m_DayHeight * dSpan2.Days + m_HeaderHeight;
            return (int)dResult;
        }
        private DateTime ConvertPositionToDate(Point pos)
        {
            int iDays = (int)((double)(pos.Y - m_HeaderHeight) / m_DayHeight);
            DateTime tmpDate = m_StartDate.AddDays(iDays);
            return tmpDate;
        }
        private int ConvertPositionToCol(Point pos)
        {
            int iCol = (pos.X - m_DateWidth) / m_ColWidth;
            return iCol;
        }
        private void pnlLegend_Paint(object sender, PaintEventArgs e)
        {
            DrawLegend(e);
        }
        private void DrawRotations(PaintEventArgs e)
        {
            Rectangle tmp = new System.Drawing.Rectangle();
            tmp.Width = m_ColWidth;

            if (DocStates != null && DocStates.DocumentElement != null && paddocks != null)
            {
                XmlNodeList history_nodes = DocStates.SelectNodes("//history");
                DateTime StartDate = m_StartDate;
                DateTime EndDate = m_EndDate;

                foreach (XmlNode history_node in history_nodes)
                {
                    string sIndex = history_node.Attributes["id"].Value;
                    int idx = int.Parse(sIndex);
                    int idxColour = 0;

                    //need to read first node for the start - then read the next node to get the end date (start of the next transition)
                    //draws the previous node - finalises using the enddate
                    if (history_node.HasChildNodes)
                    {
                        XmlNode transition_Node = history_node.FirstChild;
                        StartDate = ReadJulianDate(transition_Node, "day");
                        string rotation = transition_Node.Attributes["to"].Value;

                        transition_Node = transition_Node.NextSibling;

                        while (transition_Node != null)
                        {
                            EndDate = ReadJulianDate(transition_Node, "day");
                            idxColour = rotations.IndexOf(rotation);
                            if (idxColour > -1 && idxColour < colours.Count)
                            {
                                tmp.X = m_DateWidth + idx * m_ColWidth;
                                tmp.Y = ConvertDateToPosition(StartDate);
                                tmp.Height = ConvertDateToPosition(EndDate) - tmp.Y;

                                SolidBrush rotBrush = new SolidBrush(colours[idxColour]);
                                e.Graphics.FillRectangle(rotBrush, tmp);
                            }

                            rotation = transition_Node.Attributes["to"].Value;
                            StartDate = EndDate;
                            transition_Node = transition_Node.NextSibling;
                        }
                        EndDate = m_EndDate;
                        idxColour = rotations.IndexOf(rotation);
                        if (idxColour > -1 && idxColour < colours.Count)
                        {
                            tmp.X = m_DateWidth + idx * m_ColWidth;
                            tmp.Y = ConvertDateToPosition(StartDate);
                            tmp.Height = ConvertDateToPosition(EndDate) - tmp.Y;

                            SolidBrush rotBrush = new SolidBrush(colours[idxColour]);
                            e.Graphics.FillRectangle(rotBrush, tmp);
                        }
                    }
                }
            }
        }
        void UpdateSelectedDate(int UpdatedBy, DateTime newDate)
        {
            //if cal changed, then update scrollbar
            //if scrollbar changed then update cal
            //if map clicked, then update cal, and scrollbar
            //update scrollposition??
            if (!m_UpdatingSelectedDate)
            {
                if (newDate > m_StartDate && newDate < m_EndDate)
                {
                    m_UpdatingSelectedDate = true;
                    m_SelectedDate = newDate;
                    switch (UpdatedBy)
                    {
                        case 0: //updated by Calendar
                            {
                                TimeSpan tmpSpan = m_SelectedDate - m_StartDate;
                                spnDate.Value = tmpSpan.Days;
                            }
                            break;
                        case 1: //Scrollbar
                            {
                                calSelected.Value = m_SelectedDate;
                            }
                            break;
                        case 2: //Mouse Click
                            {
                                calSelected.Value = m_SelectedDate;
                                TimeSpan tmpSpan = m_SelectedDate - m_StartDate;
                                spnDate.Value = tmpSpan.Days;
                            }
                            break;

                    }
                    panel2.Invalidate();
                }
                m_UpdatingSelectedDate = false;
            }
        }

        public void JulianToCalendar_Net(double jDay, out DateTime date)
        {
            double work = jDay + 68569.0;
            double work0 = (int)(4.0 * work / 146097.0);
            work = work - (int)((146097.0 * work0 + 3.0) / 4.0);
            double yy = (int)(4000.0 * (work + 1.0) / 1461001.0);

            work = work - (int)(1461.0 * yy / 4.0) + 31.0;
            double mm = (int)(80.0 * work / 2447.0);
            double dayd = work - (int)(2447.0 * mm / 80.0);

            work = (int)(mm / 11.0);
            double monthd = mm + 2.0 - 12.0 * work;
            double yeard = 100.0 * (work0 - 49.0) + yy + work;

            int day = (int)(dayd + 0.5);
            int month = (int)(monthd + 0.5);
            int year = (int)(yeard + 0.5);
            date = new DateTime(year, month, day);
        }
        public DateTime ReadJulianDate(XmlNode node, string sAttr)
        {
            if (node != null)
            {
                DateTime tmpDate;
                string days = node.Attributes[sAttr].Value;
                if (Julian_Str_ToCalendar_Net(days, out tmpDate))
                {
                    return tmpDate;
                }
            }
            throw new Exception("Error reading Julian Date.");
        }
        public bool Julian_Str_ToCalendar_Net(string sJulian, out DateTime date)
        {
            double jul_days;
            date = DateTime.Now;
            if (double.TryParse(sJulian, System.Globalization.NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out jul_days))
            {
                JulianToCalendar_Net(jul_days, out date);
                return true;
            }
            return false;
        }

        private void ReadXMLFile(string filename)
        {
            if (!File.Exists(filename))
            {
                return;
            }
            DocStates = new XmlDocument();
            DocStates.Load(filename);

            paddocks = new List<string>();
            rotations = new List<string>();
            colours = new List<Color>();

            XmlNode startNode = DocStates.SelectSingleNode("//startdate");
            if (startNode != null)
            {
                DateTime tmpDate;
                string days = startNode.Attributes["day"].Value;
                if (Julian_Str_ToCalendar_Net(days, out tmpDate))
                {
                    m_StartDate = tmpDate;
                }
            }
            XmlNode endNode = DocStates.SelectSingleNode("//enddate");
            if (endNode != null)
            {
                DateTime tmpDate;
                string days = endNode.Attributes["day"].Value;
                if (Julian_Str_ToCalendar_Net(days, out tmpDate))
                {
                    m_EndDate = tmpDate;
                }
                //                if (System.DateTime.TryParseExact(date, "dd/MM/yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out tmpDate))
            }
            XmlNodeList paddockNodes = DocStates.SelectNodes("/simulation/paddock");
            foreach (XmlNode paddock in paddockNodes)
            {
                paddocks.Add(paddock.Attributes["name"].Value);
            }
            m_Cols = paddocks.Count;

            XmlNodeList stateNodes = DocStates.SelectNodes("/simulation/state");
            foreach (XmlNode state in stateNodes)
            {
                rotations.Add(state.Attributes["name"].Value);
                Color tmpColor = Color.FromName(state.Attributes["colour"].Value);
                colours.Add(tmpColor);
            }
            calStart.Value = m_StartDate;
            calEnd.Value = m_EndDate;
            calSelected.MinDate = m_StartDate;
            calSelected.MaxDate = m_EndDate;
            calSelected.Value = m_StartDate;

            m_FirstYear = new DateTime(m_StartDate.Year + 1, 1, 1);
            m_LastYear = new DateTime(m_EndDate.Year, 1, 1);

            TimeSpan tmpLength = m_EndDate - m_StartDate;
            m_Years = (int)(tmpLength.Days / 365.25);
            spnDate.Maximum = tmpLength.Days;
        }
        private void ReadNetCDFFile(string filename)
        {
            string netcdf_file = Path.ChangeExtension(filename, ".nc");
            if (!File.Exists(netcdf_file))
            {
                return;
            }
            //int res = NetCDF.nc_open(netcdf_file, NetCDF.cmode.NC_NOWRITE.GetHashCode(), ref _ncid);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (dlgOpen.ShowDialog() == DialogResult.OK)
            {
                txtFile.Text = dlgOpen.FileName;
                this.Cursor = Cursors.WaitCursor;

                ReadXMLFile(txtFile.Text);
                ReadNetCDFFile(txtFile.Text);

                panel2.Invalidate();
                this.Cursor = Cursors.Arrow;
                OnRefresh();
            }
        }

        private void panel2_MouseDown(object sender, MouseEventArgs e)
        {
            m_Dragging = true;
            if (e.Button == MouseButtons.Left)
            {
                DateTime tmpDate = ConvertPositionToDate(e.Location);
                UpdateSelectedDate(2, tmpDate);
            }
            else if (e.Button == MouseButtons.Right)
            {
                //calculate selected col and rotation
                DateTime tmpDate = ConvertPositionToDate(e.Location);
                int iCol = ConvertPositionToCol(e.Location);
                //find previous start date within selected col (paddock)
                if (DocStates != null && DocStates.DocumentElement != null && paddocks != null)
                {
                    XmlNode xmlPaddock = DocStates.SelectSingleNode("/simulation/history[@id='" + iCol.ToString() + "']");
                    if (xmlPaddock != null)
                    {
                        if (xmlPaddock.HasChildNodes)
                        {
                            XmlNode xmlTrans = xmlPaddock.FirstChild;
                            DateTime tmpPrevDate = ReadJulianDate(xmlTrans, "day");
                            if (tmpDate < tmpPrevDate)
                            {
                                UpdateSelectedDate(2, tmpDate);
                            }
                            else
                            {
                                xmlTrans = xmlTrans.NextSibling;
                                while (xmlTrans != null)
                                {
                                    DateTime tmpCurrentDate = ReadJulianDate(xmlTrans, "day");
                                    if (tmpDate < tmpCurrentDate)
                                    {
                                        //previous date is the start
                                        break;
                                    }
                                    tmpPrevDate = tmpCurrentDate;
                                    xmlTrans = xmlTrans.NextSibling;
                                }
                                //tmpprevdte is correct even if notfound
                                UpdateSelectedDate(2, tmpPrevDate);
                            }
                        }
                    }
                }
            }
        }
        private void calSelected_ValueChanged(object sender, EventArgs e)
        {
            UpdateSelectedDate(0, calSelected.Value);
        }
        private void spnDate_ValueChanged(object sender, EventArgs e)
        {
            DateTime tmpDate = m_StartDate.AddDays(spnDate.Value);
            UpdateSelectedDate(1, tmpDate);
        }
        private void panel1_Resize(object sender, EventArgs e)
        {
            panel2.Invalidate();
        }

    }
}